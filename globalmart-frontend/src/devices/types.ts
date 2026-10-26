/**
 * Tipos e interfaces para periféricos y dispositivos POS (S2-086 / S2-087).
 * Desacoplamiento estricto: el terminal POS maneja la interacción de hardware/simulada,
 * mientras que la pasarela de pagos maneja la autorización financiera en backend (RN-02).
 */

export type TerminalState =
  | 'IDLE'
  | 'ESPERANDO_TARJETA'
  | 'PROCESANDO'
  | 'APROBADO'
  | 'RECHAZADO'
  | 'CANCELADO'
  | 'ERROR';

export type SimulatedAction =
  | 'APROBAR'
  | 'RECHAZAR'
  | 'TIMEOUT'
  | 'ERROR_LECTURA';

export type DeviceMode = 'mock' | 'real';

export type SimulatedTerminalScenario =
  | 'waiting'
  | 'approved'
  | 'declined'
  | 'cancelled'
  | 'disconnected';

export interface IniciarOperacionParams {
  monto: number;
  moneda?: string;
  correlationId?: string;
}

export interface OperacionTerminalResult {
  exito: boolean;
  estado: TerminalState;
  mensaje?: string;
  correlationId?: string;
}

export interface EstadoTerminalResult {
  estado: TerminalState;
  operacionActiva?: {
    monto: number;
    moneda: string;
    correlationId: string;
    iniciadoEn: number;
  };
}

export interface AutorizacionTerminalResult {
  exito: boolean;
  estado: TerminalState;
  tokenTransaccion?: string;
  codigoAutorizacion?: string;
  mensaje?: string;
  correlationId: string;
}

export interface ITerminalAdapter {
  iniciar(params: IniciarOperacionParams): Promise<OperacionTerminalResult>;
  obtenerEstado(): Promise<EstadoTerminalResult>;
  cancelar(): Promise<OperacionTerminalResult>;
  autorizar(simulacion?: SimulatedAction): Promise<AutorizacionTerminalResult>;
}

export interface ScaleReading {
  pesoKg: number;
  estable: boolean;
  unidad: 'kg' | 'g';
  timestamp: number;
}

export interface IScaleAdapter {
  obtenerPeso(): Promise<ScaleReading>;
  tarar(): Promise<{ exito: boolean; mensaje?: string }>;
  simularPeso(pesoKg: number): Promise<ScaleReading>;
}

export interface DevicesConfig {
  terminalMode: DeviceMode;
  scaleMode: DeviceMode;
  mockTerminalDelayMs: number;
  autoApproveMock: boolean;
  mockTerminalScenario: SimulatedTerminalScenario;
}
