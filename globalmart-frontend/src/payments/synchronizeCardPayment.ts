import type {
  CobrarTarjetaResponse,
  EstadoIntentoTarjeta,
  VentaPendiente,
} from '../api/client';
import type { TerminalState } from './terminalStateMachine';
import type { TerminalPort } from './TerminalPort';

type CardTerminal = Pick<TerminalPort, 'iniciarCobro' | 'consultarEstado'>;

export interface Ms5CardPaymentClient {
  chargeCard(saleId: string, idempotencyKey: string): Promise<CobrarTarjetaResponse>;
  cardAttemptStatus(saleId: string): Promise<EstadoIntentoTarjeta>;
}

export interface CardPaymentResult {
  estado: TerminalState;
  referenciaIntento: string | null;
  gatewayReference: string | null;
  venta: VentaPendiente | null;
}

interface PaymentSingleFlightLock {
  current: boolean;
}

export function tryAcquirePaymentSingleFlight(lock: PaymentSingleFlightLock): boolean {
  if (lock.current) return false;
  lock.current = true;
  return true;
}

export function releasePaymentSingleFlight(lock: PaymentSingleFlightLock): void {
  lock.current = false;
}

interface SynchronizeCardPaymentOptions {
  terminal: CardTerminal;
  ms5: Ms5CardPaymentClient;
  ventaId: string;
  monto: number;
  claveIdempotencia: string;
}

interface ReconcileCardPaymentOptions {
  ms5: Ms5CardPaymentClient;
  ventaId: string;
  referenciaEsperada?: string | null;
}

function sameReference(left: string, right: string): boolean {
  return left.toLowerCase() === right.toLowerCase();
}

function isConfirmedSale(venta: VentaPendiente, ventaId: string): boolean {
  return venta.id === ventaId && venta.estado === 'COMPLETADA';
}

function confirmedChargeResult(
  response: CobrarTarjetaResponse,
  ventaId: string,
  referenciaIntento: string,
): CardPaymentResult | null {
  if (response.status !== 'APPROVED' || !isConfirmedSale(response.venta, ventaId)) {
    return null;
  }

  return {
    estado: 'approved',
    referenciaIntento,
    gatewayReference: response.gateway_reference,
    venta: response.venta,
  };
}

export async function reconcileCardPayment({
  ms5,
  ventaId,
  referenciaEsperada = null,
}: ReconcileCardPaymentOptions): Promise<CardPaymentResult> {
  try {
    const status = await ms5.cardAttemptStatus(ventaId);
    const referenciaIntento = status.request_id ?? referenciaEsperada;
    const correlated = !!status.request_id
      && (!referenciaEsperada || sameReference(status.request_id, referenciaEsperada));

    if (correlated && status.status === 'APPROVED' && isConfirmedSale(status.venta, ventaId)) {
      return {
        estado: 'approved',
        referenciaIntento,
        gatewayReference: status.gateway_reference ?? null,
        venta: status.venta,
      };
    }

    if (correlated && status.status === 'DECLINED') {
      return {
        estado: 'declined',
        referenciaIntento,
        gatewayReference: null,
        venta: status.venta,
      };
    }

    return {
      estado: status.status === 'NONE' && !referenciaEsperada ? 'available' : 'uncertain',
      referenciaIntento,
      gatewayReference: null,
      venta: status.venta,
    };
  } catch {
    return {
      estado: 'uncertain',
      referenciaIntento: referenciaEsperada,
      gatewayReference: null,
      venta: null,
    };
  }
}

export async function synchronizeCardPayment({
  terminal,
  ms5,
  ventaId,
  monto,
  claveIdempotencia,
}: SynchronizeCardPaymentOptions): Promise<CardPaymentResult> {
  let terminalResult = await terminal.iniciarCobro({
    ventaId,
    monto,
    claveIdempotencia,
  });

  if (!terminalResult.referenciaIntento
    || !sameReference(terminalResult.referenciaIntento, claveIdempotencia)) {
    return {
      estado: 'uncertain',
      referenciaIntento: terminalResult.referenciaIntento,
      gatewayReference: null,
      venta: null,
    };
  }

  if (terminalResult.estado === 'processing') {
    terminalResult = await terminal.consultarEstado({
      ventaId,
      monto,
      referenciaIntento: claveIdempotencia,
    });
  }

  if (!terminalResult.referenciaIntento
    || !sameReference(terminalResult.referenciaIntento, claveIdempotencia)) {
    return {
      estado: 'uncertain',
      referenciaIntento: terminalResult.referenciaIntento,
      gatewayReference: null,
      venta: null,
    };
  }

  if (terminalResult.estado !== 'approved') {
    return {
      estado: terminalResult.estado,
      referenciaIntento: terminalResult.referenciaIntento,
      gatewayReference: null,
      venta: null,
    };
  }

  try {
    const response = await ms5.chargeCard(ventaId, claveIdempotencia);
    const confirmed = confirmedChargeResult(response, ventaId, claveIdempotencia);
    if (confirmed) return confirmed;
  } catch {
    // Un error después de enviar la autorización puede ser ambiguo. Se reconcilia
    // el mismo intento y nunca se genera una clave nueva automáticamente.
  }

  return reconcileCardPayment({
    ms5,
    ventaId,
    referenciaEsperada: claveIdempotencia,
  });
}
