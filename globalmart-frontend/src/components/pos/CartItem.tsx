import React, { useEffect, useState } from 'react';

export interface CartItemProps {
  id: string;
  name: string;
  price: number;
  quantity: number;
  subtotal?: number;
  isWeightVariable?: boolean;
  readOnly?: boolean;
  canRemove?: boolean;
  onQuantityChange: (id: string, quantity: number) => void;
  onRemove: (id: string) => void;
}

export default function CartItem({ id, name, price, quantity, subtotal, isWeightVariable = false, readOnly = false, canRemove = true, onQuantityChange, onRemove }: CartItemProps) {
  const [draftQuantity, setDraftQuantity] = useState(String(quantity));

  useEffect(() => {
    setDraftQuantity(String(quantity));
  }, [quantity]);

  const commitQuantity = () => {
    const value = Number(draftQuantity);
    if (Number.isInteger(value) && value > 0 && value !== quantity) {
      setDraftQuantity(String(quantity));
      onQuantityChange(id, value);
    } else {
      setDraftQuantity(String(quantity));
    }
  };

  return (
    <tr style={{ borderBottom: '1px solid var(--color-line)', transition: 'background-color 0.2s' }}>
      <td style={{ padding: '1rem 0.5rem', fontWeight: 500, color: 'var(--color-ink)' }}>
        {name}
      </td>
      <td style={{ padding: '1rem 0.5rem', color: 'var(--color-ink-soft)' }}>
        $ {price.toLocaleString('es-CL')}
      </td>
      <td style={{ padding: '1rem 0.5rem' }}>
        {isWeightVariable ? (
          <span aria-label="Peso estable">{quantity.toFixed(3)} kg</span>
        ) : <input
          type="number"
          min="1"
          value={draftQuantity}
          disabled={readOnly}
          onChange={(event) => setDraftQuantity(event.target.value)}
          onKeyDown={(event) => { if (event.key === 'Enter') event.currentTarget.blur(); }}
          style={{
            width: '60px',
            padding: '0.5rem',
            borderRadius: '4px',
            border: '1px solid var(--color-line)',
            textAlign: 'center',
            outline: 'none',
          }}
          onFocus={(e) => e.target.style.borderColor = 'var(--color-primary)'}
          onBlur={(e) => {
            e.target.style.borderColor = 'var(--color-line)';
            commitQuantity();
          }}
        />}
      </td>
      <td style={{ padding: '1rem 0.5rem', fontWeight: 600, color: 'var(--color-ink)' }}>
        $ {(subtotal ?? price * quantity).toLocaleString('es-CL')}
      </td>
      <td style={{ padding: '1rem 0.5rem', textAlign: 'center' }}>
        {canRemove && <button
          onClick={() => onRemove(id)}
          disabled={readOnly}
          style={{
            background: 'none',
            border: 'none',
            color: 'var(--color-danger)',
            cursor: 'pointer',
            padding: '0.5rem',
            borderRadius: '4px',
            transition: 'background-color 0.2s'
          }}
          onMouseOver={(e) => e.currentTarget.style.backgroundColor = 'var(--color-danger-bg)'}
          onMouseOut={(e) => e.currentTarget.style.backgroundColor = 'transparent'}
          title="Eliminar producto"
        >
          {/* Icono de Basura SVG */}
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="3 6 5 6 21 6"></polyline>
            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
            <line x1="10" y1="11" x2="10" y2="17"></line>
            <line x1="14" y1="11" x2="14" y2="17"></line>
          </svg>
        </button>}
      </td>
    </tr>
  );
}

