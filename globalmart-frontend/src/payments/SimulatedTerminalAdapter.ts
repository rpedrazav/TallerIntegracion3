import type { SimulatedTerminalScenario } from '../devices/types';
import type {
  CancelarIntentoTerminal,
  ConsultarEstadoTerminal,
  IniciarCobroTerminal,
  ResultadoCancelacionTerminal,
  ResultadoIntentoTerminal,
  TerminalPort,
} from './TerminalPort';
import { transitionTerminalState } from './terminalStateMachine';

export interface SimulatedTerminalOptions {
  scenario: SimulatedTerminalScenario;
  delayMs?: number;
}

function createOpaqueReference(attemptReference: string): string {
  let hash = 2166136261;
  for (const character of attemptReference) {
    hash ^= character.charCodeAt(0);
    hash = Math.imul(hash, 16777619);
  }
  return `SIM-${(hash >>> 0).toString(16).padStart(8, '0').toUpperCase()}`;
}

export class SimulatedTerminalAdapter implements TerminalPort {
  private readonly scenario: SimulatedTerminalScenario;
  private readonly delayMs: number;
  private readonly attempts = new Map<string, ResultadoIntentoTerminal>();
  private readonly latestAttemptBySale = new Map<string, string>();

  constructor(options: SimulatedTerminalOptions) {
    this.scenario = options.scenario;
    this.delayMs = Math.min(Math.max(options.delayMs ?? 400, 0), 30_000);
  }

  async iniciarCobro(solicitud: IniciarCobroTerminal): Promise<ResultadoIntentoTerminal> {
    const intento: ResultadoIntentoTerminal = {
      ventaId: solicitud.ventaId,
      monto: solicitud.monto,
      referenciaIntento: solicitud.claveIdempotencia,
      estado: transitionTerminalState('available', 'START'),
      referenciaOpaca: null,
    };

    this.attempts.set(solicitud.claveIdempotencia, intento);
    this.latestAttemptBySale.set(solicitud.ventaId, solicitud.claveIdempotencia);
    return { ...intento };
  }

  async consultarEstado(solicitud: ConsultarEstadoTerminal): Promise<ResultadoIntentoTerminal> {
    const reference = solicitud.referenciaIntento
      ?? this.latestAttemptBySale.get(solicitud.ventaId)
      ?? null;
    const intento = reference ? this.attempts.get(reference) : undefined;

    if (!intento) {
      return {
        ventaId: solicitud.ventaId,
        monto: solicitud.monto,
        referenciaIntento: reference,
        estado: 'available',
        referenciaOpaca: null,
      };
    }

    if (intento.estado !== 'processing') return { ...intento };
    if (this.scenario === 'waiting' || this.scenario === 'cancelled') return { ...intento };

    await this.waitConfiguredDelay();

    if (this.scenario === 'approved') {
      intento.estado = transitionTerminalState(intento.estado, 'APPROVE');
      intento.referenciaOpaca = createOpaqueReference(intento.referenciaIntento ?? solicitud.ventaId);
    } else if (this.scenario === 'declined') {
      intento.estado = transitionTerminalState(intento.estado, 'DECLINE');
    } else if (this.scenario === 'timeout' || this.scenario === 'disconnected') {
      intento.estado = transitionTerminalState(intento.estado, 'MARK_UNCERTAIN');
    }

    return { ...intento };
  }

  async cancelarIntento(solicitud: CancelarIntentoTerminal): Promise<ResultadoCancelacionTerminal> {
    const intento = this.attempts.get(solicitud.referenciaIntento) ?? {
      ventaId: solicitud.ventaId,
      monto: solicitud.monto,
      referenciaIntento: solicitud.referenciaIntento,
      estado: transitionTerminalState('available', 'START'),
      referenciaOpaca: null,
    };

    if (intento.estado !== 'cancelled') {
      intento.estado = transitionTerminalState(intento.estado, 'CANCEL');
      intento.referenciaOpaca = null;
    }

    this.attempts.set(solicitud.referenciaIntento, intento);
    this.latestAttemptBySale.set(solicitud.ventaId, solicitud.referenciaIntento);

    return {
      soportada: true,
      intento: { ...intento, estado: 'cancelled' },
    };
  }

  private async waitConfiguredDelay(): Promise<void> {
    if (this.delayMs === 0) return;
    await new Promise<void>(resolve => setTimeout(resolve, this.delayMs));
  }
}
