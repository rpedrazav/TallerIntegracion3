import type { IpcMain } from 'electron';
import type {
  ConsultarEstadoTerminal,
  IniciarCobroTerminal,
  ResultadoIntentoTerminal,
  TerminalPort,
} from './TerminalPort';

export const TERMINAL_IPC_CHANNELS = {
  iniciarCobro: 'devices:terminal:iniciarCobro',
  consultarEstado: 'devices:terminal:consultarEstado',
} as const;

export type TerminalRendererApi = Pick<TerminalPort, 'iniciarCobro' | 'consultarEstado'>;

const MAX_TERMINAL_AMOUNT = 100_000_000;
const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const SAFE_REFERENCE_PATTERN = /^[a-zA-Z0-9_-]{1,128}$/;

function parsePlainObject(payload: unknown, operation: string): Record<string, unknown> {
  if (!payload || typeof payload !== 'object' || Array.isArray(payload)) {
    throw new Error(`IPC Terminal: ${operation} requiere un objeto plano.`);
  }
  const prototype = Object.getPrototypeOf(payload);
  if (prototype !== Object.prototype && prototype !== null) {
    throw new Error(`IPC Terminal: ${operation} requiere un objeto plano.`);
  }
  return payload as Record<string, unknown>;
}

function rejectUnexpectedProperties(
  payload: Record<string, unknown>,
  allowed: readonly string[],
  operation: string,
): void {
  const unexpected = Object.keys(payload).filter(key => !allowed.includes(key));
  if (unexpected.length > 0) {
    throw new Error(`IPC Terminal: ${operation} contiene propiedades no permitidas.`);
  }
}

function parseVentaId(value: unknown): string {
  if (typeof value !== 'string' || !UUID_PATTERN.test(value)) {
    throw new Error('IPC Terminal: ventaId debe ser un UUID válido.');
  }
  return value;
}

function parseMonto(value: unknown): number {
  if (
    typeof value !== 'number'
    || !Number.isFinite(value)
    || value <= 0
    || value > MAX_TERMINAL_AMOUNT
  ) {
    throw new Error('IPC Terminal: monto debe ser un número finito mayor que cero y dentro del máximo permitido.');
  }
  return value;
}

function parseReference(value: unknown, field: string): string {
  if (typeof value !== 'string' || !SAFE_REFERENCE_PATTERN.test(value)) {
    throw new Error(`IPC Terminal: ${field} debe ser una referencia segura de 1 a 128 caracteres.`);
  }
  return value;
}

export function parseIniciarCobroTerminal(payload: unknown): IniciarCobroTerminal {
  const data = parsePlainObject(payload, 'iniciarCobro');
  rejectUnexpectedProperties(data, ['ventaId', 'monto', 'claveIdempotencia'], 'iniciarCobro');
  return {
    ventaId: parseVentaId(data.ventaId),
    monto: parseMonto(data.monto),
    claveIdempotencia: parseReference(data.claveIdempotencia, 'claveIdempotencia'),
  };
}

export function parseConsultarEstadoTerminal(payload: unknown): ConsultarEstadoTerminal {
  const data = parsePlainObject(payload, 'consultarEstado');
  rejectUnexpectedProperties(data, ['ventaId', 'monto', 'referenciaIntento'], 'consultarEstado');

  const referenciaIntento = data.referenciaIntento == null
    ? data.referenciaIntento as null | undefined
    : parseReference(data.referenciaIntento, 'referenciaIntento');

  return {
    ventaId: parseVentaId(data.ventaId),
    monto: parseMonto(data.monto),
    referenciaIntento,
  };
}

function safeTerminalResult(result: ResultadoIntentoTerminal): ResultadoIntentoTerminal {
  return {
    ventaId: result.ventaId,
    monto: result.monto,
    referenciaIntento: result.referenciaIntento,
    estado: result.estado,
    referenciaOpaca: result.referenciaOpaca,
  };
}

function requireTerminalPort(terminalPort: TerminalPort | undefined): TerminalPort {
  if (!terminalPort) {
    throw new Error('IPC Terminal: el transporte real/MS-5 desde main no está soportado; pendiente TI3-705.');
  }
  return terminalPort;
}

export function registerTerminalIpcHandlers(
  ipcMain: Pick<IpcMain, 'handle'>,
  terminalPort: TerminalPort | undefined,
): void {
  ipcMain.handle(TERMINAL_IPC_CHANNELS.iniciarCobro, async (_event, payload: unknown) => {
    const solicitud = parseIniciarCobroTerminal(payload);
    const result = await requireTerminalPort(terminalPort).iniciarCobro(solicitud);
    return safeTerminalResult(result);
  });

  ipcMain.handle(TERMINAL_IPC_CHANNELS.consultarEstado, async (_event, payload: unknown) => {
    const solicitud = parseConsultarEstadoTerminal(payload);
    const result = await requireTerminalPort(terminalPort).consultarEstado(solicitud);
    return safeTerminalResult(result);
  });
}
