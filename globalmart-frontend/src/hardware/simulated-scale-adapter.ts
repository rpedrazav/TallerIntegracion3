import type {
  ScaleAdapter,
  ScaleError,
  ScaleErrorCode,
  ScaleOperationResult,
  ScaleState,
  StableWeightReading,
  StableWeightReadOptions,
} from './scale-contract';

export type SimulatedScaleReading =
  | {
      outcome: 'stable';
      value: number;
      unit: 'g' | 'kg';
      delayMs?: number;
    }
  | {
      outcome: 'disconnected';
      message?: string;
      delayMs?: number;
    }
  | {
      outcome: 'error';
      code?: ScaleErrorCode;
      message?: string;
      retryable?: boolean;
      delayMs?: number;
    };

export interface SimulatedScaleAdapterConfig {
  /** Secuencia determinista que se consume en cada lectura. */
  readings?: readonly SimulatedScaleReading[];
  connectDelayMs?: number;
  /** Repite el último escenario cuando la secuencia se agota. */
  repeatLastReading?: boolean;
  /** Reloj inyectable para obtener fechas reproducibles en pruebas. */
  now?: () => Date;
}

const DEFAULT_READING: SimulatedScaleReading = {
  outcome: 'stable',
  value: 0.75,
  unit: 'kg',
};

const MINIMUM_WEIGHT_KG = 0.01;
const GRAMS_PER_KILOGRAM = 1000;
const FLOATING_POINT_TOLERANCE = 1e-9;

const wait = async (delayMs: number): Promise<void> => {
  if (delayMs <= 0) return;
  await new Promise<void>((resolve) => setTimeout(resolve, delayMs));
};

const validateDelay = (delayMs: number | undefined, field: string): number => {
  const value = delayMs ?? 0;
  if (!Number.isFinite(value) || value < 0) {
    throw new Error(`${field} debe ser un número finito mayor o igual a cero.`);
  }
  return value;
};

/**
 * Adaptador determinista sin acceso a hardware. Las lecturas configuradas pueden
 * representar éxito, desconexión o error y siempre se devuelven con el contrato
 * serializable que consumirá el renderer mediante IPC.
 */
export class SimulatedScaleAdapter implements ScaleAdapter {
  private readonly readings: readonly SimulatedScaleReading[];
  private readonly connectDelayMs: number;
  private readonly repeatLastReading: boolean;
  private readonly now: () => Date;
  private readingIndex = 0;
  private state: ScaleState = {
    connectionStatus: 'disconnected',
    readingStatus: 'idle',
  };

  constructor(config: SimulatedScaleAdapterConfig = {}) {
    if (config.readings && config.readings.length === 0) {
      throw new Error('readings debe contener al menos un escenario.');
    }

    this.readings = config.readings ?? [DEFAULT_READING];
    this.connectDelayMs = validateDelay(config.connectDelayMs, 'connectDelayMs');
    this.repeatLastReading = config.repeatLastReading ?? true;
    this.now = config.now ?? (() => new Date());

    for (const reading of this.readings) {
      validateDelay(reading.delayMs, 'delayMs');
    }
  }

  async connect(): Promise<ScaleOperationResult<ScaleState>> {
    if (this.state.connectionStatus === 'connected') {
      const state = this.snapshot();
      return { ok: true, value: state, state };
    }

    this.state = {
      ...this.state,
      connectionStatus: 'connecting',
      readingStatus: 'idle',
      error: undefined,
    };
    await wait(this.connectDelayMs);
    this.state = {
      ...this.state,
      connectionStatus: 'connected',
      readingStatus: 'idle',
      error: undefined,
    };

    const state = this.snapshot();
    return { ok: true, value: state, state };
  }

  async disconnect(): Promise<ScaleOperationResult<ScaleState>> {
    this.state = {
      ...this.state,
      connectionStatus: 'disconnecting',
      readingStatus: 'idle',
      error: undefined,
    };
    this.state = {
      ...this.state,
      connectionStatus: 'disconnected',
      readingStatus: 'idle',
    };

    const state = this.snapshot();
    return { ok: true, value: state, state };
  }

  async readStableWeight(
    options: StableWeightReadOptions = {},
  ): Promise<ScaleOperationResult<StableWeightReading>> {
    if (this.state.connectionStatus !== 'connected') {
      return this.fail({
        code: 'not_connected',
        message: 'La balanza simulada no está conectada.',
        retryable: true,
      });
    }

    const scenario = this.nextReading();
    if (!scenario) {
      return this.fail({
        code: 'device_error',
        message: 'No quedan lecturas configuradas en la balanza simulada.',
        retryable: false,
      });
    }

    this.state = {
      ...this.state,
      readingStatus: 'waiting_for_stability',
      error: undefined,
    };

    const scenarioDelayMs = validateDelay(scenario.delayMs, 'delayMs');
    if (options.timeoutMs !== undefined && scenarioDelayMs > options.timeoutMs) {
      await wait(Math.max(0, options.timeoutMs));
      return this.fail({
        code: 'read_timeout',
        message: 'La balanza simulada excedió el tiempo máximo de lectura.',
        retryable: true,
      });
    }

    await wait(scenarioDelayMs);

    if (scenario.outcome === 'disconnected') {
      this.state = {
        ...this.state,
        connectionStatus: 'disconnected',
      };
      return this.fail({
        code: 'device_unavailable',
        message: scenario.message ?? 'La balanza simulada se desconectó durante la lectura.',
        retryable: true,
      });
    }

    if (scenario.outcome === 'error') {
      return this.fail({
        code: scenario.code ?? 'device_error',
        message: scenario.message ?? 'La balanza simulada produjo un error controlado.',
        retryable: scenario.retryable ?? true,
      });
    }

    const weightKg = this.normalizeWeight(scenario.value, scenario.unit);
    if (weightKg === null) {
      return this.fail({
        code: 'invalid_reading',
        message: 'La lectura simulada debe ser finita, de al menos 0.010 kg y precisa a 0.001 kg.',
        retryable: false,
      });
    }

    const reading: StableWeightReading = {
      weightKg,
      unit: 'kg',
      stable: true,
      capturedAt: this.now().toISOString(),
    };
    this.state = {
      ...this.state,
      readingStatus: 'stable',
      lastStableReading: reading,
      error: undefined,
    };

    return { ok: true, value: { ...reading }, state: this.snapshot() };
  }

  async getState(): Promise<ScaleState> {
    return this.snapshot();
  }

  private nextReading(): SimulatedScaleReading | undefined {
    if (this.readingIndex < this.readings.length) {
      const reading = this.readings[this.readingIndex];
      this.readingIndex += 1;
      return reading;
    }

    return this.repeatLastReading ? this.readings[this.readings.length - 1] : undefined;
  }

  private normalizeWeight(value: number, unit: 'g' | 'kg'): number | null {
    if (!Number.isFinite(value)) return null;

    const weightKg = unit === 'g' ? value / GRAMS_PER_KILOGRAM : value;
    const weightInGrams = weightKg * GRAMS_PER_KILOGRAM;
    const hasGramPrecision = Math.abs(weightInGrams - Math.round(weightInGrams))
      <= FLOATING_POINT_TOLERANCE;

    if (weightKg < MINIMUM_WEIGHT_KG || !hasGramPrecision) return null;
    return Number(weightKg.toFixed(3));
  }

  private fail<T>(error: ScaleError): ScaleOperationResult<T> {
    this.state = {
      ...this.state,
      readingStatus: 'error',
      error,
    };
    return { ok: false, error: { ...error }, state: this.snapshot() };
  }

  private snapshot(): ScaleState {
    return {
      ...this.state,
      lastStableReading: this.state.lastStableReading
        ? { ...this.state.lastStableReading }
        : undefined,
      error: this.state.error ? { ...this.state.error } : undefined,
    };
  }
}
