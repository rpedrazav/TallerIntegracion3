import React, { useState, useCallback, useMemo, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import axios from 'axios';
import BarcodeInput from '../components/pos/BarcodeInput';
import CartItem from '../components/pos/CartItem';

const CATALOG_URL = 'https://catalog-rpedraza.dev.censei.cl';

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

  // Inicializar leyendo de localStorage
  const [cartItems, setCartItems] = useState<ProductItem[]>(() => {
    const saved = localStorage.getItem('pos_cart');
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch (e) {
        console.error('Error al leer carrito:', e);
      }
    }
    return [];
  });

  // Guardar en localStorage cada vez que cambie
  useEffect(() => {
    localStorage.setItem('pos_cart', JSON.stringify(cartItems));
  }, [cartItems]);

    const [searchResults, setSearchResults] = useState<any[]>([]);

  const addProductToCart = useCallback((product: any) => {
    setCartItems(prev => {
      const existing = prev.find(item => item.id === product.id);
      if (existing) {
        return prev.map(item => item.id === product.id ? { ...item, quantity: item.quantity + 1 } : item);
      }
      return [...prev, {
        id: product.id,
        name: product.nombre,
        price: product.precioBase,
        quantity: 1
      }];
    });
    setSearchResults([]);
  }, []);

  const handleTypingSearch = useCallback(async (query: string) => {
    if (!query || query.trim() === '') {
      setSearchResults([]);
      return;
    }
    try {
      const token = await window.api?.getToken();
      if (!token) return;
      
      // En lugar de usar /search (que es case-sensitive en Postgres), 
      // pedimos la lista de productos y filtramos localmente ignorando mayúsculas
      const response = await axios.get(`${CATALOG_URL}/products`, {
        params: { page: 1, pageSize: 500 },
        headers: { Authorization: `Bearer ${token}` }
      });
      
      const todos = response.data?.data ?? response.data ?? [];
      const q = query.trim().toLowerCase();
      
      const filtrados = todos.filter((p: any) => 
        (p.nombre && p.nombre.toLowerCase().includes(q)) || 
        (p.codigoBarras && p.codigoBarras.includes(q))
      ).slice(0, 15); // Mostrar solo los primeros 15
      
      setSearchResults(filtrados);
    } catch (error) {
      console.error('Error buscando productos:', error);
      setSearchResults([]);
    }
  }, []);

  const handleSearch = useCallback(async (query: string) => {
    try {
      const token = await window.api?.getToken();
      if (!token) {
        alert('No hay sesiÃ³n activa. Por favor inicie sesiÃ³n.');
        return;
      }
      
      const response = await axios.get(`${CATALOG_URL}/products/lookup`, {
        params: { barcode: query },
        headers: { Authorization: `Bearer ${token}` }
      });
      
      if (response.data) {
        const product = response.data;
        setCartItems(prev => {
          const existing = prev.find(item => item.id === product.id);
          if (existing) {
            return prev.map(item => item.id === product.id ? { ...item, quantity: item.quantity + 1 } : item);
          }
          return [...prev, {
            id: product.id,
            name: product.nombre,
            price: product.precioBase,
            quantity: 1
          }];
        });
      }
    } catch (error: any) {
      console.error('Error al buscar producto:', error);
      if (error.response?.status === 404) {
        alert(`Producto con cÃ³digo de barras '${query}' no encontrado.`);
      } else {
        alert('Error de conexiÃ³n al buscar el producto.');
      }
    }
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
    if (window.confirm('Â¿EstÃ¡s seguro de que deseas cancelar la venta actual? Se vaciarÃ¡ el carrito.')) {
      setCartItems([]);
    }
  };

  const handleCheckout = () => {
    console.log('Iniciando proceso de cobro...');
    alert('Funcionalidad de cobro se implementarÃ¡ en el futuro.');
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
            <span>âœ…</span>
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
            âœ•
          </button>
        </div>
      )}

      <div style={{ display: 'flex', flex: 1, gap: '1rem', minHeight: 0 }}>
        {/* Columna Izquierda: BÃºsqueda y Escaneo */}
        <div style={{ flex: '0 0 320px', display: 'flex', flexDirection: 'column', background: 'var(--color-surface)', padding: '1rem', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
                <h3 style={{ marginTop: 0, color: 'var(--color-primary)', fontSize: '1.25rem' }}>Buscar Producto</h3>
        <BarcodeInput onSearch={handleSearch} onTyping={handleTypingSearch} />
        <div style={{ flex: 1, border: searchResults.length === 0 ? '2px dashed var(--color-line)' : 'none', borderRadius: '8px', overflow: 'auto', backgroundColor: searchResults.length === 0 ? 'var(--color-bg)' : 'transparent', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          {searchResults.length === 0 ? (
            <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--color-muted)', textAlign: 'center', padding: '1rem' }}>
              Escribe el nombre del producto o escanea el código de barras
            </div>
          ) : (
            searchResults.map(prod => (
              <div 
                key={prod.id}
                onClick={() => addProductToCart(prod)}
                style={{
                  padding: '0.75rem',
                  border: '1px solid var(--color-line)',
                  borderRadius: '8px',
                  backgroundColor: 'var(--color-surface)',
                  cursor: 'pointer',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  transition: 'all 0.2s',
                  boxShadow: '0 1px 2px rgba(0,0,0,0.05)'
                }}
                onMouseOver={(e) => {
                  e.currentTarget.style.borderColor = 'var(--color-primary)';
                  e.currentTarget.style.backgroundColor = 'var(--color-primary-bg)';
                }}
                onMouseOut={(e) => {
                  e.currentTarget.style.borderColor = 'var(--color-line)';
                  e.currentTarget.style.backgroundColor = 'var(--color-surface)';
                }}
              >
                <div>
                  <div style={{ fontWeight: 600, color: 'var(--color-ink)' }}>{prod.nombre}</div>
                  <div style={{ fontSize: '0.75rem', color: 'var(--color-ink-soft)' }}>{prod.codigoBarras}</div>
                </div>
                <div style={{ fontWeight: 'bold', color: 'var(--color-primary)' }}>
                  ${prod.precioBase.toLocaleString('es-CL')}
                </div>
              </div>
            ))
          )}
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
              <p style={{ fontSize: '1.25rem', margin: '0 0 0.5rem 0', color: 'var(--color-ink-soft)', fontWeight: 500 }}>Carrito vacÃ­o</p>
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
            <span>ðŸ§¾</span>
            <span>Cerrar Turno / Cuadre de Caja</span>
          </button>
        </div>

      </div>
    </div>
  </div>
  );
}



