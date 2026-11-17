import type { EstadoIntentoTarjeta, VentaPendiente } from '../api/client';
import type { TerminalPort } from './TerminalPort';
import type { TerminalState } from './terminalStateMachine';
import { reconcileCardPayment } from './synchronizeCardPayment';

export interface PersistedPaymentAttempt {
  ventaId: string;
  turnoId: string;
  metodoPago: 'EFECTIVO' | 'TARJETA';
  claveIdempotencia: string | null;
  estado: Exclude<TerminalState, 'available'>;
  updatedAt: string;
}

export interface RecoveryStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

export interface Ms5RecoveryClient {
  getSale(saleId: string): Promise<VentaPendiente>;
  cardAttemptStatus(saleId: string): Promise<EstadoIntentoTarjeta>;
}

export type RecoveryTerminalClient = Pick<TerminalPort, 'consultarEstado'> & {
  cancelarIntento?: TerminalPort['cancelarIntento'];
};

export interface PosRecoveryResult {
  venta: VentaPendiente | null;
  paymentState: TerminalState;
  gatewayReference: string | null;
  attemptReference: string | null;
  openModal: boolean;
  errorMessage: string;
  recoveryNotice: string | null;
  shouldClearPersisted: boolean;
}

export interface SafeCancelResult {
  outcome: 'cancelled' | 'already_approved' | 'cannot_cancel_uncertain';
  venta: VentaPendiente | null;
  gatewayReference: string | null;
  attemptReference: string | null;
  message: string;
}

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const VALID_PERSISTED_STATES = new Set<PersistedPaymentAttempt['estado']>([
  'processing',
  'uncertain',
  'approved',
  'declined',
  'cancelled',
]);

export function recoveryStorageKey(turnoId: string): string {
  return `pos_recovery_attempt:${turnoId}`;
}

export function saveRecoveryAttempt(
  storage: RecoveryStorage | null | undefined,
  attempt: PersistedPaymentAttempt,
): void {
  if (!storage || !attempt.turnoId || !attempt.ventaId) return;
  try {
    const safePayload: PersistedPaymentAttempt = {
      ventaId: attempt.ventaId,
      turnoId: attempt.turnoId,
      metodoPago: attempt.metodoPago,
      claveIdempotencia: attempt.claveIdempotencia,
      estado: attempt.estado,
      updatedAt: attempt.updatedAt,
    };
    storage.setItem(recoveryStorageKey(attempt.turnoId), JSON.stringify(safePayload));
  } catch {
    // Si el almacenamiento no está disponible, se continúa sin romper el POS.
  }
}

export function loadRecoveryAttempt(
  storage: RecoveryStorage | null | undefined,
  turnoId: string,
): PersistedPaymentAttempt | null {
  if (!storage || !turnoId) return null;
  try {
    const raw = storage.getItem(recoveryStorageKey(turnoId));
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<PersistedPaymentAttempt>;
    if (
      typeof parsed.ventaId !== 'string'
      || !UUID_REGEX.test(parsed.ventaId)
      || parsed.turnoId !== turnoId
      || (parsed.metodoPago !== 'EFECTIVO' && parsed.metodoPago !== 'TARJETA')
      || !parsed.estado
      || !VALID_PERSISTED_STATES.has(parsed.estado)
    ) {
      return null;
    }
    const clave = typeof parsed.claveIdempotencia === 'string' && UUID_REGEX.test(parsed.claveIdempotencia)
      ? parsed.claveIdempotencia
      : null;
    return {
      ventaId: parsed.ventaId,
      turnoId: parsed.turnoId,
      metodoPago: parsed.metodoPago,
      claveIdempotencia: clave,
      estado: parsed.estado,
      updatedAt: typeof parsed.updatedAt === 'string' ? parsed.updatedAt : '',
    };
  } catch {
    return null;
  }
}

export function clearRecoveryAttempt(
  storage: RecoveryStorage | null | undefined,
  turnoId: string | null | undefined,
): void {
  if (!storage || !turnoId) return;
  try {
    storage.removeItem(recoveryStorageKey(turnoId));
  } catch {
    // Ignorar errores de limpieza de almacenamiento.
  }
}

/**
 * Reconstruye el estado de recuperación del carrito y del terminal tras una recarga o reinicio
 * del POS (S2-109). Consulta únicamente estados autoritativos sin iniciar nuevas autorizaciones.
 */
export async function recoverPosStateOnReload(options: {
  turnoId: string;
  ventas: VentaPendiente[];
  persistedAttempt: PersistedPaymentAttempt | null;
  ms5: Ms5RecoveryClient;
  terminal?: RecoveryTerminalClient | null;
}): Promise<PosRecoveryResult> {
  const { ventas, persistedAttempt, ms5, terminal } = options;
  const pendingFromShift = ventas.filter((item) => item.estado === 'PENDIENTE').pop()?.id ?? null;

  // Caso 1: No hay venta PENDIENTE en el turno, pero existía un intento persistido antes de recargar.
  // Se consulta GET /ventas/{id} para no perder el identificador ni duplicar el cobro si ya se aprobó.
  if (!pendingFromShift && persistedAttempt) {
    try {
      const recoveredSale = await ms5.getSale(persistedAttempt.ventaId);
      if (recoveredSale.estado === 'COMPLETADA') {
        let gatewayReference: string | null = null;
        let attemptReference = persistedAttempt.claveIdempotencia;
        if (recoveredSale.metodo_pago === 'TARJETA') {
          try {
            const intento = await ms5.cardAttemptStatus(recoveredSale.id);
            gatewayReference = intento.gateway_reference ?? null;
            attemptReference = intento.request_id ?? attemptReference;
          } catch {
            // La venta ya figura COMPLETADA; conservar la aprobación aunque falle la consulta de referencia.
          }
        }
        return {
          venta: recoveredSale,
          paymentState: 'approved',
          gatewayReference,
          attemptReference,
          openModal: true,
          errorMessage: '',
          recoveryNotice: `Venta #${recoveredSale.id.slice(0, 8)} recuperada como COMPLETADA tras recarga. No se generó ningún cargo duplicado.`,
          shouldClearPersisted: false,
        };
      }
      if (recoveredSale.estado !== 'PENDIENTE') {
        return {
          venta: null,
          paymentState: 'available',
          gatewayReference: null,
          attemptReference: null,
          openModal: false,
          errorMessage: '',
          recoveryNotice: null,
          shouldClearPersisted: true,
        };
      }
    } catch {
      return {
        venta: null,
        paymentState: 'available',
        gatewayReference: null,
        attemptReference: null,
        openModal: false,
        errorMessage: '',
        recoveryNotice: null,
        shouldClearPersisted: false,
      };
    }
  }

  const targetSaleId = pendingFromShift ?? persistedAttempt?.ventaId ?? null;
  if (!targetSaleId) {
    return {
      venta: null,
      paymentState: 'available',
      gatewayReference: null,
      attemptReference: null,
      openModal: false,
      errorMessage: '',
      recoveryNotice: null,
      shouldClearPersisted: !!persistedAttempt,
    };
  }

  const recovered = await ms5.getSale(targetSaleId);
  if (recovered.estado !== 'PENDIENTE') {
    if (recovered.estado === 'COMPLETADA') {
      return {
        venta: recovered,
        paymentState: 'approved',
        gatewayReference: null,
        attemptReference: persistedAttempt?.claveIdempotencia ?? null,
        openModal: true,
        errorMessage: '',
        recoveryNotice: `Venta #${recovered.id.slice(0, 8)} recuperada como COMPLETADA tras recarga.`,
        shouldClearPersisted: false,
      };
    }
    return {
      venta: null,
      paymentState: 'available',
      gatewayReference: null,
      attemptReference: null,
      openModal: false,
      errorMessage: '',
      recoveryNotice: null,
      shouldClearPersisted: true,
    };
  }

  const matchedAttempt = persistedAttempt?.ventaId === recovered.id ? persistedAttempt : null;
  const expectedRef = matchedAttempt?.claveIdempotencia ?? null;

  if (recovered.metodo_pago === 'TARJETA') {
    try {
      const intento = await reconcileCardPayment({
        ms5,
        ventaId: recovered.id,
        referenciaEsperada: expectedRef,
      });

      if (intento.estado === 'approved' && intento.venta?.estado === 'COMPLETADA') {
        return {
          venta: intento.venta,
          paymentState: 'approved',
          gatewayReference: intento.gatewayReference,
          attemptReference: intento.referenciaIntento,
          openModal: true,
          errorMessage: '',
          recoveryNotice: `Cobro con tarjeta de la venta #${recovered.id.slice(0, 8)} recuperado como aprobado tras recarga.`,
          shouldClearPersisted: false,
        };
      }

      if (intento.estado === 'declined') {
        return {
          venta: intento.venta ?? recovered,
          paymentState: 'declined',
          gatewayReference: null,
          attemptReference: intento.referenciaIntento,
          openModal: true,
          errorMessage: 'La pasarela rechazó el cobro.',
          recoveryNotice: `Venta #${recovered.id.slice(0, 8)} recuperada tras recarga con intento rechazado.`,
          shouldClearPersisted: false,
        };
      }

      if (intento.estado === 'uncertain' || intento.estado === 'approved') {
        // Si MS-5 no tiene confirmación y el intento venía del terminal, consultar estado sin reautorizar.
        if (expectedRef && terminal) {
          try {
            const terminalStatus = await terminal.consultarEstado({
              ventaId: recovered.id,
              monto: recovered.total,
              referenciaIntento: expectedRef,
            });
            if (terminalStatus.estado === 'cancelled' && intento.referenciaIntento === expectedRef) {
              // Solo permitir estado cancelled si MS-5 no registró intento pendiente en pasarela.
              const rawStatus = await ms5.cardAttemptStatus(recovered.id);
              if (rawStatus.status === 'NONE') {
                return {
                  venta: recovered,
                  paymentState: 'cancelled',
                  gatewayReference: null,
                  attemptReference: null,
                  openModal: true,
                  errorMessage: 'El intento previo en el terminal fue cancelado. Puedes continuar con el cobro.',
                  recoveryNotice: `Venta #${recovered.id.slice(0, 8)} recuperada tras recarga sin cargos en pasarela.`,
                  shouldClearPersisted: true,
                };
              }
            }
          } catch {
            // Ante error de consulta del terminal, conservar estado incierto por seguridad.
          }
        }

        return {
          venta: intento.venta ?? recovered,
          paymentState: 'uncertain',
          gatewayReference: null,
          attemptReference: intento.referenciaIntento,
          openModal: true,
          errorMessage: intento.estado === 'approved'
            ? 'MS-5 no confirmó la venta como COMPLETADA. Verifica su estado antes de cobrar.'
            : 'El intento de tarjeta sigue sin resultado confirmado. Verifica su estado antes de cobrar.',
          recoveryNotice: `Venta #${recovered.id.slice(0, 8)} recuperada en verificación tras recarga. No se iniciará otra autorización sin confirmar.`,
          shouldClearPersisted: false,
        };
      }

      return {
        venta: recovered,
        paymentState: 'available',
        gatewayReference: null,
        attemptReference: null,
        openModal: false,
        errorMessage: '',
        recoveryNotice: `Carrito recuperado para venta pendiente #${recovered.id.slice(0, 8)}.`,
        shouldClearPersisted: !!matchedAttempt,
      };
    } catch {
      return {
        venta: recovered,
        paymentState: 'uncertain',
        gatewayReference: null,
        attemptReference: expectedRef,
        openModal: true,
        errorMessage: 'No se pudo consultar el intento de tarjeta. Verifica el estado antes de otro cobro.',
        recoveryNotice: `Venta #${recovered.id.slice(0, 8)} recuperada con error de consulta. Verifica antes de cobrar.`,
        shouldClearPersisted: false,
      };
    }
  }

  // Venta pendiente en EFECTIVO con intento en vuelo/incierto antes de la recarga
  if (matchedAttempt && (matchedAttempt.estado === 'processing' || matchedAttempt.estado === 'uncertain')) {
    return {
      venta: recovered,
      paymentState: 'uncertain',
      gatewayReference: null,
      attemptReference: null,
      openModal: true,
      errorMessage: 'El cobro en efectivo quedó pendiente de confirmación tras la recarga. Verifica la venta antes de volver a cobrar.',
      recoveryNotice: `Venta #${recovered.id.slice(0, 8)} recuperada en verificación tras recarga.`,
      shouldClearPersisted: false,
    };
  }

  return {
    venta: recovered,
    paymentState: 'available',
    gatewayReference: null,
    attemptReference: null,
    openModal: false,
    errorMessage: '',
    recoveryNotice: `Carrito recuperado para venta pendiente #${recovered.id.slice(0, 8)}.`,
    shouldClearPersisted: !!matchedAttempt,
  };
}

/**
 * Cancela un intento de cobro con tarjeta de forma segura (S2-109):
 * consulta MS-5 y el terminal antes de permitir cancelar o continuar, evitando reautorizaciones a ciegas.
 */
export async function cancelPaymentAttemptSafely(options: {
  ventaId: string;
  monto: number;
  referenciaIntento: string | null;
  ms5: Ms5RecoveryClient;
  terminal?: RecoveryTerminalClient | null;
}): Promise<SafeCancelResult> {
  const { ventaId, monto, referenciaIntento, ms5, terminal } = options;

  let ms5Status: EstadoIntentoTarjeta;
  try {
    ms5Status = await ms5.cardAttemptStatus(ventaId);
  } catch {
    return {
      outcome: 'cannot_cancel_uncertain',
      venta: null,
      gatewayReference: null,
      attemptReference: referenciaIntento,
      message: 'No se pudo consultar el estado en MS-5 para cancelar con seguridad. Verifica la venta antes de continuar.',
    };
  }

  if (ms5Status.status === 'APPROVED' && ms5Status.venta?.estado === 'COMPLETADA') {
    return {
      outcome: 'already_approved',
      venta: ms5Status.venta,
      gatewayReference: ms5Status.gateway_reference ?? null,
      attemptReference: ms5Status.request_id ?? referenciaIntento,
      message: 'El cobro ya fue confirmado por MS-5 y no puede cancelarse.',
    };
  }

  if (ms5Status.status === 'UNCERTAIN') {
    return {
      outcome: 'cannot_cancel_uncertain',
      venta: ms5Status.venta ?? null,
      gatewayReference: null,
      attemptReference: ms5Status.request_id ?? referenciaIntento,
      message: 'El intento sigue pendiente de confirmación en la pasarela/MS-5 y no puede cancelarse a ciegas. Usa Verificar venta.',
    };
  }

  if (ms5Status.status === 'DECLINED') {
    return {
      outcome: 'cancelled',
      venta: ms5Status.venta ?? null,
      gatewayReference: null,
      attemptReference: null,
      message: 'El intento anterior fue rechazado por la pasarela. Puedes continuar con un nuevo cobro o cambiar de método.',
    };
  }

  // ms5Status.status === 'NONE': MS-5 no registró ningún cargo en pasarela.
  // Intentamos cancelar en el terminal si expone cancelarIntento; en caso contrario consultamos su estado.
  if (terminal && referenciaIntento) {
    try {
      if (typeof terminal.cancelarIntento === 'function') {
        const cancelResponse = await terminal.cancelarIntento({
          ventaId,
          monto,
          referenciaIntento,
        });
        if (cancelResponse.soportada && cancelResponse.intento.estado === 'cancelled') {
          return {
            outcome: 'cancelled',
            venta: ms5Status.venta ?? null,
            gatewayReference: null,
            attemptReference: null,
            message: 'Intento cancelado en el terminal sin cargos registrados en MS-5. Puedes continuar el cobro.',
          };
        }
      }
      // Si el adaptador reporta CANCELACION_NO_SOPORTADA o no expone cancelarIntento pero MS-5
      // confirmó NONE (sin cobro enviado), verificamos que el terminal no siga procesando o incierto.
      const terminalStatus = await terminal.consultarEstado({
        ventaId,
        monto,
        referenciaIntento,
      });
      if (terminalStatus.estado === 'processing' || terminalStatus.estado === 'uncertain') {
        return {
          outcome: 'cannot_cancel_uncertain',
          venta: ms5Status.venta ?? null,
          gatewayReference: null,
          attemptReference: referenciaIntento,
          message: 'El terminal aún reporta un intento activo o incierto y no soporta cancelación remota. Verifica antes de continuar.',
        };
      }
    } catch {
      return {
        outcome: 'cannot_cancel_uncertain',
        venta: ms5Status.venta ?? null,
        gatewayReference: null,
        attemptReference: referenciaIntento,
        message: 'No se pudo confirmar la cancelación con el terminal. Verifica el estado antes de continuar.',
      };
    }
  }

  return {
    outcome: 'cancelled',
    venta: ms5Status.venta ?? null,
    gatewayReference: null,
    attemptReference: null,
    message: 'Intento cancelado sin cargos en MS-5. Puedes continuar el cobro o cambiar de método.',
  };
}
