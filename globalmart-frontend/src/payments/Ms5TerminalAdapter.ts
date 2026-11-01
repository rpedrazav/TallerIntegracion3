import { isAxiosError } from 'axios';
import { posApi } from '../api/client';
import type {
  CancelarIntentoTerminal,
  ConsultarEstadoTerminal,
  IniciarCobroTerminal,
  ResultadoCancelacionTerminal,
  ResultadoIntentoTerminal,
  TerminalPort,
} from './TerminalPort';
import type { TerminalState } from './terminalStateMachine';

type Ms5CardApi = Pick<typeof posApi, 'chargeCard' | 'cardAttemptStatus'>;

export class Ms5TerminalAdapter implements TerminalPort {
  constructor(private readonly api: Ms5CardApi = posApi) {}

  async iniciarCobro(solicitud: IniciarCobroTerminal): Promise<ResultadoIntentoTerminal> {
    try {
      const response = await this.api.chargeCard(solicitud.ventaId, solicitud.claveIdempotencia);
      let estado: TerminalState = 'uncertain';
      if (
        response.status === 'APPROVED'
        && response.venta?.id === solicitud.ventaId
        && response.venta.estado === 'COMPLETADA'
      ) {
        estado = 'approved';
      } else if (response.status === 'DECLINED') {
        estado = 'declined';
      }

      return {
        ventaId: solicitud.ventaId,
        monto: solicitud.monto,
        referenciaIntento: solicitud.claveIdempotencia,
        estado,
        referenciaOpaca: response.gateway_reference,
      };
    } catch (error: unknown) {
      return {
        ventaId: solicitud.ventaId,
        monto: solicitud.monto,
        referenciaIntento: solicitud.claveIdempotencia,
        estado: isAxiosError(error) && error.response?.status === 422 ? 'declined' : 'uncertain',
        referenciaOpaca: null,
      };
    }
  }

  async consultarEstado(solicitud: ConsultarEstadoTerminal): Promise<ResultadoIntentoTerminal> {
    try {
      const response = await this.api.cardAttemptStatus(solicitud.ventaId);
      let estado: TerminalState = 'available';
      if (
        response.status === 'APPROVED'
        && response.venta?.id === solicitud.ventaId
        && response.venta.estado === 'COMPLETADA'
      ) {
        estado = 'approved';
      } else if (response.status === 'DECLINED') {
        estado = 'declined';
      } else if (response.status === 'UNCERTAIN') {
        estado = 'uncertain';
      }

      return {
        ventaId: solicitud.ventaId,
        monto: solicitud.monto,
        referenciaIntento: response.request_id ?? solicitud.referenciaIntento ?? null,
        estado,
        referenciaOpaca: response.gateway_reference ?? null,
      };
    } catch {
      return {
        ventaId: solicitud.ventaId,
        monto: solicitud.monto,
        referenciaIntento: solicitud.referenciaIntento ?? null,
        estado: 'uncertain',
        referenciaOpaca: null,
      };
    }
  }

  async cancelarIntento(solicitud: CancelarIntentoTerminal): Promise<ResultadoCancelacionTerminal> {
    return {
      soportada: false,
      codigo: 'CANCELACION_NO_SOPORTADA',
      ventaId: solicitud.ventaId,
      monto: solicitud.monto,
      referenciaIntento: solicitud.referenciaIntento,
    };
  }
}
