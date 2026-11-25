import React from 'react';
import CartItem from './CartItem';
import { ProductItem } from '../../hooks/useVenta';
import { VentaPendiente } from '../../api/client';
import { PaymentState } from './PaymentModal';

interface CartTableProps {
  cartItems: ProductItem[];
  totalItems: number;
  isCartEmpty: boolean;
  venta: VentaPendiente | null;
  isStartingSale: boolean;
  pendingMutations: number;
  saleNeedsRefresh: boolean;
  paymentState: PaymentState;
  onQuantityChange: (id: string, quantity: number) => void;
  onRemove: (id: string) => void;
}

export default function CartTable({
  cartItems,
  totalItems,
  isCartEmpty,
  venta,
  isStartingSale,
  pendingMutations,
  saleNeedsRefresh,
  paymentState,
  onQuantityChange,
  onRemove,
}: CartTableProps) {
  return (
    <div
      style={{
        flex: '1',
        display: 'flex',
        flexDirection: 'column',
        background: 'var(--color-surface)',
        padding: '1rem',
        borderRadius: '8px',
        boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
      }}
    >
      <h3 style={{ marginTop: 0, color: 'var(--color-primary)', fontSize: '1.25rem' }}>
        Carrito de Compras ({totalItems} items)
      </h3>
      <div style={{ flex: 1, overflow: 'auto', display: 'flex', flexDirection: 'column' }}>
        {isCartEmpty ? (
          <div
            style={{
              flex: 1,
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'var(--color-muted)',
            }}
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              width="64"
              height="64"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="1"
              strokeLinecap="round"
              strokeLinejoin="round"
              style={{ marginBottom: '1rem', color: 'var(--color-line)' }}
            >
              <circle cx="9" cy="21" r="1"></circle>
              <circle cx="20" cy="21" r="1"></circle>
              <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
            </svg>
            <p style={{ fontSize: '1.25rem', margin: '0 0 0.5rem 0', color: 'var(--color-ink-soft)', fontWeight: 500 }}>
              Carrito vacío
            </p>
            <p style={{ fontSize: '0.875rem', margin: 0 }}>Escanea o busca un producto para comenzar</p>
          </div>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
            <thead>
              <tr
                style={{
                  borderBottom: '2px solid var(--color-line)',
                  color: 'var(--color-ink-soft)',
                  fontSize: '0.875rem',
                  textTransform: 'uppercase',
                }}
              >
                <th style={{ padding: '0.75rem 0.5rem' }}>Producto</th>
                <th style={{ padding: '0.75rem 0.5rem' }}>Precio</th>
                <th style={{ padding: '0.75rem 0.5rem' }}>Cant.</th>
                <th style={{ padding: '0.75rem 0.5rem' }}>Subtotal</th>
                <th style={{ padding: '0.75rem 0.5rem', textAlign: 'center' }}>X</th>
              </tr>
            </thead>
            <tbody>
              {cartItems.map((item) => (
                <CartItem
                  key={item.itemId ?? item.id}
                  id={item.itemId ?? item.id}
                  name={item.name}
                  price={item.price}
                  quantity={item.quantity}
                  subtotal={item.subtotal}
                  isWeightVariable={item.isWeightVariable}
                  readOnly={
                    item.isWeightVariable ||
                    isStartingSale ||
                    pendingMutations > 0 ||
                    saleNeedsRefresh ||
                    paymentState === 'uncertain' ||
                    paymentState === 'processing' ||
                    !!(venta && venta.estado !== 'PENDIENTE')
                  }
                  canRemove={!venta || venta.estado === 'PENDIENTE'}
                  onQuantityChange={onQuantityChange}
                  onRemove={onRemove}
                />
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
