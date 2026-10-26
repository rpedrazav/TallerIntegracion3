import type { TerminalState } from './terminalStateMachine';

export interface IniciarCobroTerminal {
  ventaId: string;
  monto: number;
  claveIdempotencia: string;
}

export interface ConsultarEstadoTerminal {
  ventaId: string;
  monto: number;
  referenciaIntento?: string | null;
}

export interface CancelarIntentoTerminal {
  ventaId: string;
  monto: number;
  referenciaIntento: string;
}

export interface ResultadoIntentoTerminal {
  ventaId: string;
  monto: number;
  referenciaIntento: string | null;
  estado: TerminalState;
  referenciaOpaca: string | null;
}

export type ResultadoCancelacionTerminal =
  | {
      soportada: true;
      intento: ResultadoIntentoTerminal & { estado: 'cancelled' };
    }
  | {
      soportada: false;
      codigo: 'CANCELACION_NO_SOPORTADA';
      ventaId: string;
      monto: number;
      referenciaIntento: string;
    };

export interface TerminalPort {
  iniciarCobro(solicitud: IniciarCobroTerminal): Promise<ResultadoIntentoTerminal>;
  consultarEstado(solicitud: ConsultarEstadoTerminal): Promise<ResultadoIntentoTerminal>;
  cancelarIntento(solicitud: CancelarIntentoTerminal): Promise<ResultadoCancelacionTerminal>;
}
