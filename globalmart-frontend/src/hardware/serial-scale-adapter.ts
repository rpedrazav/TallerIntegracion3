import type {
  ScaleAdapter,
  ScaleError,
  ScaleOperationResult,
  ScaleState,
  StableWeightReading,
  StableWeightReadOptions,
} from './scale-contract';

/** Configuración privada de Electron main; nunca forma parte de ScaleRendererApi. */
export interface SerialScaleConfiguration {
  portPath: string;
  baudRate: number;
  /** Identifica un parser específico que deberá implementarse para el modelo real. */
  parserId: string;
}

export interface SerialScaleTransport {
  open(configuration: Readonly<Pick<SerialScaleConfiguration, 'portPath' | 'baudRate'>>): Promise<void>;
  close(): Promise<void>;
  readFrame(options?: StableWeightReadOptions): Promise<string>;
}

export interface ParsedSerialScaleFrame {
  value: number;
  unit: 'g' | 'kg';
  stable: boolean;
}

export interface SerialScaleParser {
  readonly id: string;
  parse(frame: string): ParsedSerialScaleFrame | null;
}

export interface SerialScaleAdapterDependencies {
  transport: SerialScaleTransport;
  parser: SerialScaleParser;
  now?: () => Date;
}

export type SerialScaleTransportErrorCode = 'timeout' | 'unavailable' | 'device';

/** Error neutral que un transporte real puede usar sin filtrar detalles seriales. */
export class SerialScaleTransportError extends Error {
  constructor(
    readonly code: SerialScaleTransportErrorCode,
    message: string,
  ) {
    super(message);
    this.name = 'SerialScaleTransportError';
  }
}

const MINIMUM_WEIGHT_KG = 0.01;
const GRAMS_PER_KILOGRAM = 1000;
const FLOATING_POINT_TOLERANCE = 1e-9;

export const validateSerialScaleConfiguration = (
  configuration: SerialScaleConfiguration,
): SerialScaleConfiguration => {
  const portPath = configuration.portPath.trim();
  const parserId = configuration.parserId.trim();

  if (!portPath) throw new Error('El puerto serial de la balanza es obligatorio.');
  if (!Number.isInteger(configuration.baudRate) || configuration.baudRate <= 0) {
    throw new Error('El baud rate de la balanza debe ser un entero positivo.');
  }
  if (!parserId) throw new Error('El identificador del parser serial es obligatorio.');

  return { portPath, baudRate: configuration.baudRate, parserId };
};

/**
 * Adaptador independiente de biblioteca y modelo. Requiere transporte y parser
 * inyectados; por sí solo no abre puertos ni afirma compatibilidad física.
 */
export class SerialScaleAdapter implements ScaleAdapter {
  private readonly configuration: SerialScaleConfiguration;
  private readonly transport: SerialScaleTransport;
  private readonly parser: SerialScaleParser;
  private readonly now: () => Date;
  private state: ScaleState = {
    connectionStatus: 'disconnected',
    readingStatus: 'idle',
  };

  constructor(
    configuration: SerialScaleConfiguration,
    dependencies: SerialScaleAdapterDependencies,
  ) {
    this.configuration = validateSerialScaleConfiguration(configuration);
    this.transport = dependencies.transport;
    this.parser = dependencies.parser;
    this.now = dependencies.now ?? (() => new Date());

    if (this.parser.id !== this.configuration.parserId) {
      throw new Error(
        `El parser inyectado "${this.parser.id}" no coincide con "${this.configuration.parserId}".`,
      );
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

    try {
      await this.transport.open({
        portPath: this.configuration.portPath,
        baudRate: this.configuration.baudRate,
      });
      this.state = {
        ...this.state,
        connectionStatus: 'connected',
        readingStatus: 'idle',
        error: undefined,
      };
      const state = this.snapshot();
      return { ok: true, value: state, state };
    } catch {
      return this.fail({
        code: 'connection_failed',
        message: 'No fue posible conectar la balanza serial.',
        retryable: true,
      }, 'error');
    }
  }

  async disconnect(): Promise<ScaleOperationResult<ScaleState>> {
    if (this.state.connectionStatus === 'disconnected') {
      const state = this.snapshot();
      return { ok: true, value: state, state };
    }

    this.state = {
      ...this.state,
      connectionStatus: 'disconnecting',
      readingStatus: 'idle',
      error: undefined,
    };

    try {
      await this.transport.close();
      this.state = {
        ...this.state,
        connectionStatus: 'disconnected',
        readingStatus: 'idle',
        error: undefined,
      };
      const state = this.snapshot();
      return { ok: true, value: state, state };
    } catch {
      return this.fail({
        code: 'device_error',
        message: 'No fue posible desconectar la balanza serial de forma limpia.',
        retryable: true,
      }, 'error');
    }
  }

  async readStableWeight(
    options: StableWeightReadOptions = {},
  ): Promise<ScaleOperationResult<StableWeightReading>> {
    if (this.state.connectionStatus !== 'connected') {
      return this.fail({
        code: 'not_connected',
        message: 'La balanza serial no está conectada.',
        retryable: true,
      });
    }

    this.state = {
      ...this.state,
      readingStatus: 'waiting_for_stability',
      error: undefined,
    };

    let frame: string;
    try {
      frame = await this.transport.readFrame(options);
    } catch (error) {
      return this.fail(this.mapTransportError(error));
    }

    let parsed: ParsedSerialScaleFrame | null;
    try {
      parsed = this.parser.parse(frame);
    } catch {
      parsed = null;
    }

    if (!parsed) {
      return this.fail({
        code: 'invalid_reading',
        message: 'El parser configurado no reconoció la lectura serial.',
        retryable: true,
      });
    }

    if (!parsed.stable) {
      return this.fail({
        code: 'unstable_reading',
        message: 'La balanza serial todavía no informa un peso estable.',
        retryable: true,
      });
    }

    const weightKg = this.normalizeWeight(parsed.value, parsed.unit);
    if (weightKg === null) {
      return this.fail({
        code: 'invalid_reading',
        message: 'La lectura serial debe ser finita, de al menos 0.010 kg y precisa a 0.001 kg.',
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

  private normalizeWeight(value: number, unit: unknown): number | null {
    if (!Number.isFinite(value) || (unit !== 'g' && unit !== 'kg')) return null;

    const weightKg = unit === 'g' ? value / GRAMS_PER_KILOGRAM : value;
    const grams = weightKg * GRAMS_PER_KILOGRAM;
    const hasGramPrecision = Math.abs(grams - Math.round(grams))
      <= FLOATING_POINT_TOLERANCE;

    if (weightKg < MINIMUM_WEIGHT_KG || !hasGramPrecision) return null;
    return Number(weightKg.toFixed(3));
  }

  private mapTransportError(error: unknown): ScaleError {
    if (error instanceof SerialScaleTransportError) {
      if (error.code === 'timeout') {
        return {
          code: 'read_timeout',
          message: 'La balanza serial excedió el tiempo máximo de lectura.',
          retryable: true,
        };
      }
      if (error.code === 'unavailable') {
        this.state = { ...this.state, connectionStatus: 'disconnected' };
        return {
          code: 'device_unavailable',
          message: 'La balanza serial dejó de estar disponible.',
          retryable: true,
        };
      }
    }

    return {
      code: 'device_error',
      message: 'La balanza serial produjo un error de lectura.',
      retryable: true,
    };
  }

  private fail<T>(
    error: ScaleError,
    connectionStatus = this.state.connectionStatus,
  ): ScaleOperationResult<T> {
    this.state = {
      ...this.state,
      connectionStatus,
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
