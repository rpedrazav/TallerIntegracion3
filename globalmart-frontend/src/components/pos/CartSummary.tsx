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
    <div
      style={{
        flex: '0 0 300px',
        display: 'flex',
        flexDirection: 'column',
        background: 'var(--color-surface)',
        padding: '1.5rem',
        borderRadius: '8px',
        boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
      }}
    >
      <h3
        style={{
          marginTop: 0,
          color: 'var(--color-primary)',
          fontSize: '1.25rem',
          borderBottom: '2px solid var(--color-line)',
          paddingBottom: '0.5rem',
          marginBottom: '1.5rem',
        }}
      >
        Resumen de Venta
      </h3>

      {venta ? (
        <p style={{ marginTop: 0, color: 'var(--color-ink-soft)', fontSize: '0.85rem' }}>
          Método de pago: {venta.metodo_pago}
        </p>
      ) : (
        <label style={{ marginTop: 0, color: 'var(--color-ink-soft)', fontSize: '0.85rem' }}>
          Método de pago
          <select
            value={draftPaymentMethod}
            onChange={(event) => onDraftPaymentMethodChange(event.target.value as 'EFECTIVO' | 'TARJETA')}
            style={{ display: 'block', marginTop: '0.4rem', width: '100%' }}
          >
            <option value="EFECTIVO">Efectivo</option>
            <option value="TARJETA">Tarjeta simulada</option>
          </select>
        </label>
      )}

      <div style={{ flex: 1 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem', color: 'var(--color-ink-soft)' }}>
          <span>{venta ? 'Subtotal:' : 'Subtotal estimado:'}</span>
          <span>$ {subtotal.toLocaleString('es-CL')}</span>
        </div>

        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem', color: 'var(--color-ink-soft)' }}>
          <span>Impuestos:</span>
          <span>{iva === undefined ? 'Se calculan en MS-5' : `$ ${iva.toLocaleString('es-CL')}`}</span>
        </div>

        <div style={{ borderTop: '2px dashed var(--color-line)', margin: '1.5rem 0' }}></div>

        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <span style={{ fontSize: '1.25rem', fontWeight: 600, color: 'var(--color-ink)' }}>TOTAL:</span>
          <span style={{ fontSize: '2rem', fontWeight: 700, color: 'var(--color-primary)' }}>
            {total === undefined ? 'Pendiente de MS-5' : `$ ${total.toLocaleString('es-CL')}`}
          </span>
        </div>
      </div>

      <div style={{ marginTop: 'auto', display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
        <button
          onClick={onCheckout}
          disabled={isCheckoutDisabled}
          style={{
            width: '100%',
            padding: '1rem',
            fontSize: '1.125rem',
            fontWeight: 'bold',
            color: 'var(--color-surface)',
            backgroundColor: isCheckoutDisabled ? 'var(--color-muted)' : 'var(--color-primary)',
            border: 'none',
            borderRadius: '8px',
            cursor: isCheckoutDisabled ? 'not-allowed' : 'pointer',
            transition: 'background-color 0.2s',
            boxShadow: isCheckoutDisabled
              ? 'none'
              : '0 4px 6px color-mix(in srgb, var(--color-primary) 25%, transparent)',
          }}
        >
          {isStartingSale ? 'INICIANDO VENTA...' : venta ? 'COBRAR' : 'INICIAR VENTA'}
        </button>

        <button
          onClick={onCancelSale}
          disabled={isCancelDisabled}
          style={{
            width: '100%',
            padding: '0.75rem',
            fontSize: '1rem',
            fontWeight: 600,
            color: isCancelDisabled ? 'var(--color-line)' : 'var(--color-ink-soft)',
            backgroundColor: isCancelDisabled ? 'var(--color-bg)' : 'var(--color-line)',
            border: 'none',
            borderRadius: '8px',
            cursor: isCancelDisabled ? 'not-allowed' : 'pointer',
            transition: 'background-color 0.2s',
          }}
        >
          {venta ? 'Venta pendiente' : 'Cancelar borrador'}
        </button>

        <button
          type="button"
          onClick={onNavigateCerrarTurno}
          style={{
            width: '100%',
            padding: '0.7rem',
            fontSize: '0.9rem',
            fontWeight: 600,
            color: 'var(--color-primary)',
            backgroundColor: 'var(--color-primary-bg)',
            border: '1px solid var(--color-muted)',
            borderRadius: '8px',
            cursor: 'pointer',
            transition: 'background-color 0.2s',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '0.5rem',
          }}
        >
          <span>💼</span>
          <span>Cerrar Turno / Cuadre de Caja</span>
        </button>
      </div>
    </div>
  );
}
