import React, { useState, useCallback, useMemo } from 'react';
import BarcodeInput from '../components/pos/BarcodeInput';
import CartItem from '../components/pos/CartItem';

export interface ProductItem {
  id: string;
  name: string;
  price: number;
  quantity: number;
}

export default function Pos() {
  const [cartItems, setCartItems] = useState<ProductItem[]>([
    { id: '1', name: 'Coca Cola 2L', price: 2500, quantity: 2 },
    { id: '2', name: 'Pan de Molde Castao', price: 1800, quantity: 1 }
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
    if (window.confirm('Ests seguro de que deseas cancelar la venta actual? Se vaciar el carrito.')) {
      setCartItems([]);
    }
  };

  const handleCheckout = () => {
    console.log('Iniciando proceso de cobro...');
    alert('Funcionalidad de cobro se implementar en el futuro.');
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
    <div style={{ display: 'flex', height: 'calc(100vh - 100px)', gap: '1rem' }}>
      
      {/* Columna Izquierda: Bsqueda y Escaneo */}
      <div style={{ flex: '0 0 320px', display: 'flex', flexDirection: 'column', background: '#fff', padding: '1rem', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <h3 style={{ marginTop: 0, color: '#0ea5e9', fontSize: '1.25rem' }}>Buscar Producto</h3>
        <BarcodeInput onSearch={handleSearch} />
        <div style={{ flex: 1, border: '2px dashed #e5e7eb', borderRadius: '8px', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#9ca3af', backgroundColor: '#f9fafb' }}>
          [Resultados de bsqueda]
        </div>
      </div>

      {/* Columna Central: Tabla del Carrito */}
      <div style={{ flex: '1', display: 'flex', flexDirection: 'column', background: '#fff', padding: '1rem', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <h3 style={{ marginTop: 0, color: '#0ea5e9', fontSize: '1.25rem' }}>
          Carrito de Compras ({totalItems} items)
        </h3>
        <div style={{ flex: 1, overflow: 'auto', display: 'flex', flexDirection: 'column' }}>
          
          {isCartEmpty ? (
            <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', color: '#9ca3af' }}>
              <svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" strokeLinecap="round" strokeLinejoin="round" style={{ marginBottom: '1rem', color: '#d1d5db' }}>
                <circle cx="9" cy="21" r="1"></circle>
                <circle cx="20" cy="21" r="1"></circle>
                <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
              </svg>
              <p style={{ fontSize: '1.25rem', margin: '0 0 0.5rem 0', color: '#6b7280', fontWeight: 500 }}>Carrito vaco</p>
              <p style={{ fontSize: '0.875rem', margin: 0 }}>Escanea o busca un producto para comenzar</p>
            </div>
          ) : (
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
              <thead>
                <tr style={{ borderBottom: '2px solid #e5e7eb', color: '#6b7280', fontSize: '0.875rem', textTransform: 'uppercase' }}>
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
      <div style={{ flex: '0 0 300px', display: 'flex', flexDirection: 'column', background: '#fff', padding: '1.5rem', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <h3 style={{ marginTop: 0, color: '#0ea5e9', fontSize: '1.25rem', borderBottom: '2px solid #e5e7eb', paddingBottom: '0.5rem', marginBottom: '1.5rem' }}>
          Resumen de Venta
        </h3>
        
        <div style={{ flex: 1 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem', color: '#4b5563' }}>
            <span>Subtotal:</span>
            <span>$ {subtotal.toLocaleString('es-CL')}</span>
          </div>
          
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem', color: '#4b5563' }}>
            <span>IVA (19%):</span>
            <span>$ {iva.toLocaleString('es-CL')}</span>
          </div>

          <div style={{ borderTop: '2px dashed #e5e7eb', margin: '1.5rem 0' }}></div>

          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
            <span style={{ fontSize: '1.25rem', fontWeight: 600, color: '#111827' }}>TOTAL:</span>
            <span style={{ fontSize: '2rem', fontWeight: 700, color: '#0ea5e9' }}>
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
              color: 'white', 
              backgroundColor: isCartEmpty ? '#9ca3af' : '#0ea5e9', 
              border: 'none', 
              borderRadius: '8px', 
              cursor: isCartEmpty ? 'not-allowed' : 'pointer',
              transition: 'background-color 0.2s',
              boxShadow: isCartEmpty ? 'none' : '0 4px 6px rgba(14, 165, 233, 0.25)'
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
              color: isCartEmpty ? '#d1d5db' : '#4b5563', 
              backgroundColor: isCartEmpty ? '#f3f4f6' : '#e5e7eb', 
              border: 'none', 
              borderRadius: '8px', 
              cursor: isCartEmpty ? 'not-allowed' : 'pointer',
              transition: 'background-color 0.2s'
            }}
          >
            Cancelar Venta
          </button>
        </div>

      </div>

    </div>
  );
}

