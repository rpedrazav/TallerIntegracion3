import React from 'react';
import { VentaPendiente } from '../../api/client';

interface CartSummaryProps {
  venta: VentaPendiente | null;
  draftPaymentMethod: 'EFECTIVO' | 'TARJETA';
  onDraftPaymentMethodChange: (method: 'EFECTIVO' | 'TARJETA') => void;
  subtotal: number;
  iva?: number;
  total?: number;
  isCartEmpty: boolean;
  isStartingSale: boolean;
  pendingMutations: number;
  saleNeedsRefresh: boolean;
  onCheckout: () => void;
  onCancelSale: () => void;
  onNavigateCerrarTurno: () => void;
}

export default function CartSummary({
  venta,
  draftPaymentMethod,
  onDraftPaymentMethodChange,
  subtotal,
  iva,
  total,
  isCartEmpty,
  isStartingSale,
  pendingMutations,
  saleNeedsRefresh,
  onCheckout,
  onCancelSale,
  onNavigateCerrarTurno,
}: CartSummaryProps) {
  const isCheckoutDisabled = isCartEmpty || isStartingSale || pendingMutations > 0 || saleNeedsRefresh;
  const isCancelDisabled = isCartEmpty || !!venta || isStartingSale;

  return (
    <div className="pos-panel pos-panel--summary">
      <h3 className="pos-panel-title pos-panel-title--divided">Resumen de Venta</h3>

      {venta ? (
        <p className="pos-summary-method">Método de pago: {venta.metodo_pago}</p>
      ) : (
        <label className="pos-summary-method">
          Método de pago
          <select
            value={draftPaymentMethod}
            onChange={(event) => onDraftPaymentMethodChange(event.target.value as 'EFECTIVO' | 'TARJETA')}
            className="pos-summary-select"
          >
            <option value="EFECTIVO">Efectivo</option>
            <option value="TARJETA">Tarjeta simulada</option>
          </select>
        </label>
      )}

      <div className="pos-summary-totals">
        <div className="pos-summary-row">
          <span>{venta ? 'Subtotal:' : 'Subtotal estimado:'}</span>
          <span className="pos-summary-num">$ {subtotal.toLocaleString('es-CL')}</span>
        </div>

        <div className="pos-summary-row">
          <span>Impuestos:</span>
          <span className={iva === undefined ? undefined : 'pos-summary-num'}>
            {iva === undefined ? 'Se calculan en MS-5' : `$ ${iva.toLocaleString('es-CL')}`}
          </span>
        </div>

        <div className="pos-summary-divider" />

        <div className="pos-summary-total-row">
          <span className="pos-summary-total-label">TOTAL:</span>
          <span className="pos-summary-total-value">
            {total === undefined ? 'Pendiente de MS-5' : `$ ${total.toLocaleString('es-CL')}`}
          </span>
        </div>
      </div>

      <div className="pos-summary-actions">
        <button
          onClick={onCheckout}
          disabled={isCheckoutDisabled}
          className={`pos-btn-checkout${venta ? ' pos-btn-checkout--cobrar' : ''}`}
        >
          {isStartingSale ? 'INICIANDO VENTA...' : venta ? 'COBRAR' : 'INICIAR VENTA'}
        </button>

        <button
          onClick={onCancelSale}
          disabled={isCancelDisabled}
          className="pos-btn-cancel"
        >
          {venta ? 'Venta pendiente' : 'Cancelar borrador'}
        </button>

        <button
          type="button"
          onClick={onNavigateCerrarTurno}
          className="pos-btn-shift"
        >
          <span aria-hidden="true">💼</span>
          <span>Cerrar Turno / Cuadre de Caja</span>
        </button>
      </div>
    </div>
  );
}
