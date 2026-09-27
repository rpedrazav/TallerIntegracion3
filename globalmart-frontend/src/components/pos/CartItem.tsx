import React from 'react';

export interface CartItemProps {
  id: string;
  name: string;
  price: number;
  quantity: number;
  onQuantityChange: (id: string, quantity: number) => void;
  onRemove: (id: string) => void;
}

export default function CartItem({ id, name, price, quantity, onQuantityChange, onRemove }: CartItemProps) {
  const handleQuantityChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = parseInt(e.target.value, 10);
    // Validar nmero positivo
    if (!isNaN(value) && value > 0) {
      onQuantityChange(id, value);
    } else if (e.target.value === '') {
      // Permitir borrar el nmero temporalmente sin que se ponga en 0 o NaN
      // Se podra manejar con estado local, pero para simplificar obligamos a > 0
    }
  };

  const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    const value = parseInt(e.target.value, 10);
    if (isNaN(value) || value <= 0) {
      onQuantityChange(id, 1); // Si lo dejan vaco o en 0, restaurar a 1
    }
  };

  const subtotal = price * quantity;

  return (
    <tr style={{ borderBottom: '1px solid #e5e7eb', transition: 'background-color 0.2s' }}>
      <td style={{ padding: '1rem 0.5rem', fontWeight: 500, color: '#374151' }}>
        {name}
      </td>
      <td style={{ padding: '1rem 0.5rem', color: '#6b7280' }}>
        $ {price.toLocaleString('es-CL')}
      </td>
      <td style={{ padding: '1rem 0.5rem' }}>
        <input
          type="number"
          min="1"
          value={quantity}
          onChange={handleQuantityChange}
          style={{
            width: '60px',
            padding: '0.5rem',
            borderRadius: '4px',
            border: '1px solid #d1d5db',
            textAlign: 'center',
            outline: 'none',
          }}
          onFocus={(e) => e.target.style.borderColor = '#0ea5e9'}
          onBlur={(e) => {
            e.target.style.borderColor = '#d1d5db';
            handleBlur(e);
          }}
        />
      </td>
      <td style={{ padding: '1rem 0.5rem', fontWeight: 600, color: '#111827' }}>
        $ {subtotal.toLocaleString('es-CL')}
      </td>
      <td style={{ padding: '1rem 0.5rem', textAlign: 'center' }}>
        <button
          onClick={() => onRemove(id)}
          style={{
            background: 'none',
            border: 'none',
            color: '#ef4444',
            cursor: 'pointer',
            padding: '0.5rem',
            borderRadius: '4px',
            transition: 'background-color 0.2s'
          }}
          onMouseOver={(e) => e.currentTarget.style.backgroundColor = '#fee2e2'}
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
        </button>
      </td>
    </tr>
  );
}

