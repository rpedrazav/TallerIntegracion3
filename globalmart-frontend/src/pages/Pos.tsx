import React, { useCallback } from 'react';
import BarcodeInput from '../components/pos/BarcodeInput';
import CartItem from '../components/pos/CartItem';

export default function Pos() {
  const handleSearch = useCallback((query: string) => {
    console.log('Buscando producto (debounce disparado):', query);
  }, []);

  return (
    <div style={{ display: 'flex', height: 'calc(100vh - 100px)', gap: '1rem' }}>
      
      {/* Columna Izquierda: Bsqueda y Escaneo */}
      <div style={{ flex: '0 0 320px', display: 'flex', flexDirection: 'column', background: '#fff', padding: '1rem', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <h3 style={{ marginTop: 0, color: '#1B4332', fontSize: '1.25rem' }}>Buscar Producto</h3>
        <BarcodeInput onSearch={handleSearch} />
        <div style={{ flex: 1, border: '2px dashed #e5e7eb', borderRadius: '8px', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#9ca3af', backgroundColor: '#f9fafb' }}>
          [Resultados de bsqueda]
        </div>
      </div>

      {/* Columna Central: Tabla del Carrito */}
      <div style={{ flex: '1', display: 'flex', flexDirection: 'column', background: '#fff', padding: '1rem', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <h3 style={{ marginTop: 0, color: '#1B4332', fontSize: '1.25rem' }}>Carrito de Compras</h3>
        <div style={{ flex: 1, overflow: 'auto' }}>
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
              {/* Dummy data para visualizar el componente CartItem */}
              <CartItem 
                id="1" 
                name="Coca Cola 2L" 
                price={2500} 
                quantity={2} 
                onQuantityChange={(id, q) => console.log('Cambio cantidad', id, q)}
                onRemove={(id) => console.log('Eliminar', id)}
              />
              <CartItem 
                id="2" 
                name="Pan de Molde Castao" 
                price={1800} 
                quantity={1} 
                onQuantityChange={(id, q) => console.log('Cambio cantidad', id, q)}
                onRemove={(id) => console.log('Eliminar', id)}
              />
            </tbody>
          </table>
        </div>
      </div>

      {/* Columna Derecha: Totales y Botones */}
      <div style={{ flex: '0 0 300px', display: 'flex', flexDirection: 'column', background: '#fff', padding: '1rem', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <h3 style={{ marginTop: 0, color: '#1B4332', fontSize: '1.25rem' }}>Resumen</h3>
        <div style={{ flex: 1, border: '2px dashed #e5e7eb', borderRadius: '8px', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#9ca3af', backgroundColor: '#f9fafb' }}>
          [Totales y Botones]
        </div>
      </div>

    </div>
  );
}
