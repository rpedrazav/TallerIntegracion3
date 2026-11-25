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
    <div className="pos-panel pos-panel--cart">
      <h3 className="pos-panel-title">
        Carrito de Compras ({totalItems} items)
      </h3>
      <div className="pos-cart-scroll">
        {isCartEmpty ? (
          <div className="pos-cart-empty">
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
              className="pos-cart-empty-icon"
              aria-hidden="true"
            >
              <circle cx="9" cy="21" r="1"></circle>
              <circle cx="20" cy="21" r="1"></circle>
              <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
            </svg>
            <p className="pos-cart-empty-title">Carrito vacío</p>
            <p className="pos-cart-empty-hint">Escanea o busca un producto para comenzar</p>
          </div>
        ) : (
          <table className="pos-cart-table">
            <thead>
              <tr className="pos-cart-thead-row">
                <th className="pos-cart-th">Producto</th>
                <th className="pos-cart-th">Precio</th>
                <th className="pos-cart-th">Cant.</th>
                <th className="pos-cart-th">Subtotal</th>
                <th className="pos-cart-th pos-cart-th--center">X</th>
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
