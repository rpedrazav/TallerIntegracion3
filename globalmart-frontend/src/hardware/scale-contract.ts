/**
 * Contrato independiente del dispositivo para una balanza física o simulada.
 *
 * La implementación pertenece al proceso principal de Electron. El renderer
 * solo puede consumir una implementación de ScaleRendererApi mediante IPC y
 * nunca recibe un puerto serial, baud rate ni handle del dispositivo.
 */

export const SCALE_CONNECTION_STATUSES = [
  'disconnected',
  'connecting',
  'connected',
  'disconnecting',
  'error',
] as const;

export type ScaleConnectionStatus = typeof SCALE_CONNECTION_STATUSES[number];

export const SCALE_READING_STATUSES = [
  'idle',
  'waiting_for_stability',
  'stable',
  'error',
] as const;

export type ScaleReadingStatus = typeof SCALE_READING_STATUSES[number];

export const SCALE_ERROR_CODES = [
  'device_unavailable',
  'connection_failed',
  'not_connected',
  'read_timeout',
  'unstable_reading',
  'invalid_reading',
  'device_error',
] as const;

export type ScaleErrorCode = typeof SCALE_ERROR_CODES[number];

export interface ScaleError {
  code: ScaleErrorCode;
  message: string;
  retryable: boolean;
}

/** Lectura canónica estable. Todo adaptador convierte la unidad física a kg. */
export interface StableWeightReading {
  weightKg: number;
  unit: 'kg';
  stable: true;
  capturedAt: string;
}

export interface ScaleState {
  connectionStatus: ScaleConnectionStatus;
  readingStatus: ScaleReadingStatus;
  lastStableReading?: StableWeightReading;
  error?: ScaleError;
}

export interface StableWeightReadOptions {
  /** Tiempo máximo total para obtener una lectura estable. */
  timeoutMs?: number;
  /** Tiempo durante el cual la lectura debe permanecer estable. */
  stabilityWindowMs?: number;
}

export type ScaleOperationResult<T> =
  | { ok: true; value: T; state: ScaleState }
  | { ok: false; error: ScaleError; state: ScaleState };

/**
 * Contrato que implementarán el adaptador físico y el simulador en Electron main.
 * La configuración serial se inyecta al construir el adaptador, fuera del renderer.
 */
export interface ScaleAdapter {
  connect(): Promise<ScaleOperationResult<ScaleState>>;
  disconnect(): Promise<ScaleOperationResult<ScaleState>>;
  readStableWeight(
    options?: StableWeightReadOptions,
  ): Promise<ScaleOperationResult<StableWeightReading>>;
  getState(): Promise<ScaleState>;
}

/** API serializable que un preload futuro podrá exponer mediante contextBridge. */
export type ScaleRendererApi = Pick<
  ScaleAdapter,
  'connect' | 'disconnect' | 'readStableWeight' | 'getState'
>;
