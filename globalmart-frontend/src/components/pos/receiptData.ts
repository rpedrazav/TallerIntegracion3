import type { VentaPendiente } from '../../api/client';

export interface SafeReceiptItem {
  id: string;
  nombre: string;
  cantidad: number;
  pesoKg: number | null;
  precioUnitario: number;
  subtotal: number;
}

export interface SafeReceiptData {
  ventaId: string;
  ventaCodigoCorto: string;
  estado: string;
  fechaIso: string | null;
  metodoPago: string;
  esTarjeta: boolean;
  subtotal: number;
  impuestos: number;
  total: number;
  items: SafeReceiptItem[];
  referenciaOpaca: string | null;
  montoRecibido: number | null;
  vuelto: number | null;
  esSimulado: true;
  rotuloSimulado: string;
  avisoNoTributario: string;
  folioDte: string;
}

export interface BuildSafeReceiptOptions {
  reference?: string | null;
  change?: number | null;
  cashReceived?: number | null;
}

export const SIMULATED_RECEIPT_LABEL = '*** COMPROBANTE DE PAGO SIMULADO ***';
export const NON_DTE_NOTICE = 'DOCUMENTO NO TRIBUTARIO · NO VÁLIDO COMO DTE';
export const NO_DTE_FOLIO_TEXT = 'No emitido (comprobante simulado)';

/**
 * Construye los datos del comprobante de pago seguro (S2-108) tomando exclusivamente
 * los campos autoritativos de VentaResponse (GET /ventas/{id}) y la referencia opaca,
 * sin exponer datos sensibles de tarjeta ni inventar un folio DTE.
 */
export function buildSafeReceiptData(
  venta: VentaPendiente,
  options: BuildSafeReceiptOptions = {},
): SafeReceiptData {
  const esTarjeta = venta.metodo_pago === 'TARJETA';
  const rawRef = options.reference?.trim() || null;

  return {
    ventaId: venta.id,
    ventaCodigoCorto: venta.id.slice(0, 8),
    estado: venta.estado,
    fechaIso: venta.created_at ?? null,
    metodoPago: venta.metodo_pago,
    esTarjeta,
    subtotal: venta.subtotal,
    impuestos: venta.impuestos,
    total: venta.total,
    items: (venta.items ?? []).map((item) => ({
      id: item.id,
      nombre: item.nombre_producto,
      cantidad: item.cantidad,
      pesoKg: item.peso_kg ?? null,
      precioUnitario: item.precio_unitario,
      subtotal: item.subtotal,
    })),
    referenciaOpaca: esTarjeta ? rawRef : null,
    montoRecibido: !esTarjeta && options.cashReceived != null && Number.isFinite(options.cashReceived)
      ? options.cashReceived
      : null,
    vuelto: !esTarjeta && options.change != null && Number.isFinite(options.change)
      ? options.change
      : null,
    esSimulado: true,
    rotuloSimulado: SIMULATED_RECEIPT_LABEL,
    avisoNoTributario: NON_DTE_NOTICE,
    folioDte: NO_DTE_FOLIO_TEXT,
  };
}
