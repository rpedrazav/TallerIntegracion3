import React, { useState, useCallback, useMemo } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import BarcodeInput from '../components/pos/BarcodeInput';
import CartItem from '../components/pos/CartItem';

export interface ProductItem {
  id: string;
  name: string;
  price: number;
  quantity: number;
}

interface PosLocationState {
  mensajeConfirmacion?: string;
  message?: string;
}

export default function Pos() {
  const navigate = useNavigate();
  const location = useLocation();
  const locationState = location.state as PosLocationState | null;
  const [mensajeConfirmacion, setMensajeConfirmacion] = useState<string | null>(
    locationState?.mensajeConfirmacion || locationState?.message || null
  );

  const [cartItems, setCartItems] = useState<ProductItem[]>([
    { id: '1', name: 'Coca Cola 2L', price: 2500, quantity: 2 },
    { id: '2', name: 'Pan de Molde Castaño', price: 1800, quantity: 1 }
  ]);

  const handleSearch = useCallback((query: string) => {
    console.log('Buscando producto (debounce disparado):', query);
  }, []);

  const handleQuantityChange = useCallback((id: string, newQuantity: number) => {
    setCartItems(prev => prev.map(item => 
      item.id === id ? { ...item, quantity: newQuantity } : item
    ));
  }, []);

  const handleRemoveItem = useCallback((id: string) => {
    setCartItems(prev => prev.filter(item => item.id !== id));
  }, []);

  const handleCancelSale = () => {
    if (cartItems.length === 0) return;
    if (window.confirm('¿Estás seguro de que deseas cancelar la venta actual? Se vaciará el carrito.')) {
      setCartItems([]);
    }
  };

  const handleCheckout = () => {
    console.log('Iniciando proceso de cobro...');
    alert('Funcionalidad de cobro se implementará en el futuro.');
  };

  const { subtotal, totalItems } = useMemo(() => {
    return cartItems.reduce(
      (acc, item) => {
        acc.subtotal += item.price * item.quantity;
        acc.totalItems += item.quantity;
        return acc;
      },
      { subtotal: 0, totalItems: 0 }
    );
  }, [cartItems]);

  const tasaIva = 0.19; 
  const iva = Math.round(subtotal * tasaIva);
  const total = subtotal + iva;
  
  const isCartEmpty = cartItems.length === 0;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 100px)', gap: '1rem' }}>
      {mensajeConfirmacion && (
        <div
          style={{
            backgroundColor: 'var(--color-success-bg)',
            border: '1px solid var(--color-success)',
            color: 'var(--color-success)',
            padding: '0.75rem 1rem',
            borderRadius: '8px',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            boxShadow: '0 1px 3px rgba(0, 0, 0, 0.05)',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 600, fontSize: '0.925rem' }}>
            <span>✅</span>
            <span>{mensajeConfirmacion}</span>
          </div>
          <button
            type="button"
            onClick={() => setMensajeConfirmacion(null)}
            style={{
              background: 'none',
              border: 'none',
              color: 'var(--color-success)',
              fontWeight: 'bold',
              cursor: 'pointer',
              fontSize: '1rem',
              padding: '0.2rem 0.5rem',
            }}
            title="Cerrar mensaje"
          >
            ✕
          </button>
        </div>
      )}

      <div style={{ display: 'flex', flex: 1, gap: '1rem', minHeight: 0 }}>
        {/* Columna Izquierda: Búsqueda y Escaneo */}
        <div style={{ flex: '0 0 320px', display: 'flex', flexDirection: 'column', background: 'var(--color-surface)', padding: '1rem', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <h3 style={{ marginTop: 0, color: 'var(--color-primary)', fontSize: '1.25rem' }}>Buscar Producto</h3>
        <BarcodeInput onSearch={handleSearch} />
        <div style={{ flex: 1, border: '2px dashed var(--color-line)', borderRadius: '8px', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--color-muted)', backgroundColor: 'var(--color-bg)' }}>
          [Resultados de búsqueda]
        </div>
      </div>

      {/* Columna Central: Tabla del Carrito */}
      <div style={{ flex: '1', display: 'flex', flexDirection: 'column', background: 'var(--color-surface)', padding: '1rem', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <h3 style={{ marginTop: 0, color: 'var(--color-primary)', fontSize: '1.25rem' }}>
          Carrito de Compras ({totalItems} items)
        </h3>
        <div style={{ flex: 1, overflow: 'auto', display: 'flex', flexDirection: 'column' }}>
          
          {isCartEmpty ? (
            <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', color: 'var(--color-muted)' }}>
              <svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" strokeLinecap="round" strokeLinejoin="round" style={{ marginBottom: '1rem', color: 'var(--color-line)' }}>
                <circle cx="9" cy="21" r="1"></circle>
                <circle cx="20" cy="21" r="1"></circle>
                <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
              </svg>
              <p style={{ fontSize: '1.25rem', margin: '0 0 0.5rem 0', color: 'var(--color-ink-soft)', fontWeight: 500 }}>Carrito vacío</p>
              <p style={{ fontSize: '0.875rem', margin: 0 }}>Escanea o busca un producto para comenzar</p>
            </div>
          ) : (
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
              <thead>
                <tr style={{ borderBottom: '2px solid var(--color-line)', color: 'var(--color-ink-soft)', fontSize: '0.875rem', textTransform: 'uppercase' }}>
                  <th style={{ padding: '0.75rem 0.5rem' }}>Producto</th>
                  <th style={{ padding: '0.75rem 0.5rem' }}>Precio</th>
                  <th style={{ padding: '0.75rem 0.5rem' }}>Cant.</th>
                  <th style={{ padding: '0.75rem 0.5rem' }}>Subtotal</th>
                  <th style={{ padding: '0.75rem 0.5rem', textAlign: 'center' }}>X</th>
                </tr>
              </thead>
              <tbody>
                {cartItems.map(item => (
                  <CartItem 
                    key={item.id}
                    id={item.id} 
                    name={item.name} 
                    price={item.price} 
                    quantity={item.quantity} 
                    onQuantityChange={handleQuantityChange}
                    onRemove={handleRemoveItem}
                  />
                ))}
              </tbody>
            </table>
          )}
          
        </div>
      </div>

      {/* Columna Derecha: Totales y Botones */}
      <div style={{ flex: '0 0 300px', display: 'flex', flexDirection: 'column', background: 'var(--color-surface)', padding: '1.5rem', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <h3 style={{ marginTop: 0, color: 'var(--color-primary)', fontSize: '1.25rem', borderBottom: '2px solid var(--color-line)', paddingBottom: '0.5rem', marginBottom: '1.5rem' }}>
          Resumen de Venta
        </h3>
        
        <div style={{ flex: 1 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem', color: 'var(--color-ink-soft)' }}>
            <span>Subtotal:</span>
            <span>$ {subtotal.toLocaleString('es-CL')}</span>
          </div>
          
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem', color: 'var(--color-ink-soft)' }}>
            <span>IVA (19%):</span>
            <span>$ {iva.toLocaleString('es-CL')}</span>
          </div>

          <div style={{ borderTop: '2px dashed var(--color-line)', margin: '1.5rem 0' }}></div>

          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
            <span style={{ fontSize: '1.25rem', fontWeight: 600, color: 'var(--color-ink)' }}>TOTAL:</span>
            <span style={{ fontSize: '2rem', fontWeight: 700, color: 'var(--color-primary)' }}>
              $ {total.toLocaleString('es-CL')}
            </span>
          </div>
        </div>

        <div style={{ marginTop: 'auto', display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          <button 
            onClick={handleCheckout}
            disabled={isCartEmpty}
            style={{ 
              width: '100%', 
              padding: '1rem', 
              fontSize: '1.125rem', 
              fontWeight: 'bold', 
              color: 'var(--color-surface)', 
              backgroundColor: isCartEmpty ? 'var(--color-muted)' : 'var(--color-primary)', 
              border: 'none', 
              borderRadius: '8px', 
              cursor: isCartEmpty ? 'not-allowed' : 'pointer',
              transition: 'background-color 0.2s',
              boxShadow: isCartEmpty ? 'none' : '0 4px 6px color-mix(in srgb, var(--color-primary) 25%, transparent)'
            }}
          >
            COBRAR
          </button>
          
          <button 
            onClick={handleCancelSale}
            disabled={isCartEmpty}
            style={{ 
              width: '100%', 
              padding: '0.75rem', 
              fontSize: '1rem', 
              fontWeight: 600, 
              color: isCartEmpty ? 'var(--color-line)' : 'var(--color-ink-soft)', 
              backgroundColor: isCartEmpty ? 'var(--color-bg)' : 'var(--color-line)', 
              border: 'none', 
              borderRadius: '8px', 
              cursor: isCartEmpty ? 'not-allowed' : 'pointer',
              transition: 'background-color 0.2s'
            }}
          >
            Cancelar Venta
          </button>

          <button 
            type="button"
            onClick={() => navigate('/cerrar-turno')}
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
              gap: '0.5rem'
            }}
          >
            <span>🧾</span>
            <span>Cerrar Turno / Cuadre de Caja</span>
          </button>
        </div>

      </div>
    </div>
  </div>
  );
}

