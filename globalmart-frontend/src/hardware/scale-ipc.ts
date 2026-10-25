import type { IpcMain } from 'electron';
import type {
  ScaleAdapter,
  StableWeightReadOptions,
} from './scale-contract';

export const SCALE_IPC_CHANNELS = Object.freeze({
  connect: 'scale:connect',
  disconnect: 'scale:disconnect',
  readStableWeight: 'scale:read-stable',
  getState: 'scale:get-state',
} as const);

export const SCALE_IPC_CHANNEL_WHITELIST = Object.freeze(
  Object.values(SCALE_IPC_CHANNELS),
);

const READ_OPTION_KEYS = new Set<keyof StableWeightReadOptions>([
  'timeoutMs',
  'stabilityWindowMs',
]);
const MAX_READ_OPTION_MS = 60_000;

const parseOptionalMilliseconds = (
  value: unknown,
  field: keyof StableWeightReadOptions,
): number | undefined => {
  if (value === undefined) return undefined;
  if (!Number.isInteger(value) || (value as number) < 0 || (value as number) > MAX_READ_OPTION_MS) {
    throw new TypeError(`${field} debe ser un entero entre 0 y ${MAX_READ_OPTION_MS}.`);
  }
  return value as number;
};

/**
 * Valida el único payload permitido desde el renderer. No admite rutas de
 * puerto, baud rate, comandos seriales ni propiedades desconocidas.
 */
export const parseStableWeightReadOptions = (
  input: unknown,
): StableWeightReadOptions => {
  if (input === undefined) return {};
  if (input === null || typeof input !== 'object' || Array.isArray(input)) {
    throw new TypeError('Las opciones de lectura deben ser un objeto.');
  }

  const candidate = input as Record<string, unknown>;
  const unknownKey = Object.keys(candidate).find(
    (key) => !READ_OPTION_KEYS.has(key as keyof StableWeightReadOptions),
  );
  if (unknownKey) {
    throw new TypeError(`La opción de lectura "${unknownKey}" no está permitida.`);
  }

  return {
    timeoutMs: parseOptionalMilliseconds(candidate.timeoutMs, 'timeoutMs'),
    stabilityWindowMs: parseOptionalMilliseconds(
      candidate.stabilityWindowMs,
      'stabilityWindowMs',
    ),
  };
};

/** Registra exclusivamente los cuatro canales autorizados para la balanza. */
export const registerScaleIpcHandlers = (
  ipcMain: Pick<IpcMain, 'handle'>,
  scaleAdapter: ScaleAdapter,
): void => {
  ipcMain.handle(SCALE_IPC_CHANNELS.connect, () => scaleAdapter.connect());
  ipcMain.handle(SCALE_IPC_CHANNELS.disconnect, () => scaleAdapter.disconnect());
  ipcMain.handle(
    SCALE_IPC_CHANNELS.readStableWeight,
    (_event, options: unknown) => scaleAdapter.readStableWeight(
      parseStableWeightReadOptions(options),
    ),
  );
  ipcMain.handle(SCALE_IPC_CHANNELS.getState, () => scaleAdapter.getState());
};
