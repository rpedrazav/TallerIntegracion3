import React, { useState, useCallback, useMemo, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import axios from 'axios';
import { catalogApi, CatalogProduct, posApi, VentaPendiente } from '../api/client';
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
  const [turnoStatus, setTurnoStatus] = useState<'checking' | 'ready' | 'error'>('checking');
  const [turnoRetry, setTurnoRetry] = useState(0);
  const [turnoId, setTurnoId] = useState<string | null>(null);
  const [venta, setVenta] = useState<VentaPendiente | null>(null);
  const [ventaError, setVentaError] = useState('');
  const [isStartingSale, setIsStartingSale] = useState(false);

  useEffect(() => {
    let active = true;
    setTurnoStatus('checking');

    posApi.activeShift()
      .then(async (turno) => {
        const ventas = await posApi.salesByShift(turno.id);
        if (!active) return;
        const pendiente = ventas.filter((item) => item.estado === 'PENDIENTE').pop() ?? null;
        setTurnoId(turno.id);
        setVenta(pendiente);
        if (pendiente) {
          setDraftItems([]);
          setSearchResults([]);
        }
        setTurnoStatus('ready');
      })
      .catch((error: unknown) => {
        if (!active) return;
        if (axios.isAxiosError(error) && error.response?.status === 404) {
          navigate('/abrir-turno', { replace: true });
        } else if (!(axios.isAxiosError(error) && error.response?.status === 401)) {
          setTurnoStatus('error');
        }
      });

    return () => { active = false; };
  }, [navigate, turnoRetry]);

  // Inicializar leyendo de localStorage
  const [draftItems, setDraftItems] = useState<ProductItem[]>(() => {
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
    localStorage.setItem('pos_cart', JSON.stringify(draftItems));
  }, [draftItems]);

  const cartItems: ProductItem[] = venta
    ? venta.items.map((item) => ({
        id: item.producto_id,
        name: item.nombre_producto,
        price: item.precio_unitario,
        quantity: item.cantidad,
      }))
    : draftItems;

    const [searchResults, setSearchResults] = useState<CatalogProduct[]>([]);
  const [todosProductos, setTodosProductos] = useState<CatalogProduct[]>([]);

  useEffect(() => {
    if (turnoStatus !== 'ready') return;
    const fetchTodos = async () => {
      try {
        const response = await catalogApi.listProducts();
        setTodosProductos(response.data);
      } catch (e) {
        console.error('Error al cargar catalogo POS', e);
      }
    };
    fetchTodos();
  }, [turnoStatus]);

  const addProductToCart = useCallback((product: CatalogProduct) => {
    if (venta || isStartingSale) return;
    setDraftItems(prev => {
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
  }, [venta, isStartingSale]);

  const handleTypingSearch = useCallback((query: string) => {
    if (!query || query.trim() === '') {
      setSearchResults([]);
      return;
    }
    const q = query.trim().toLowerCase();
    const filtrados = todosProductos.filter((p) =>
      (p.nombre && p.nombre.toLowerCase().includes(q)) || 
      (p.codigoBarras && p.codigoBarras.includes(q))
    ).slice(0, 15);
    
    setSearchResults(filtrados);
  }, [todosProductos]);

  const handleSearch = useCallback(async (query: string) => {
    const q = query.trim();
    if (!q) return;

    const exactMatch = todosProductos.find(p => p.codigoBarras === q);
    if (exactMatch) {
      addProductToCart(exactMatch);
      return;
    }

    const qLower = q.toLowerCase();
    const matches = todosProductos.filter(p => 
      (p.nombre && p.nombre.toLowerCase().includes(qLower)) || 
      (p.codigoBarras && p.codigoBarras.includes(qLower))
    );

    if (matches.length === 1) {
      addProductToCart(matches[0]);
    } else if (matches.length > 1) {
      setSearchResults(matches.slice(0, 15));
    } else {
      try {
        const product = await catalogApi.lookupProduct(query);
        if (product) addProductToCart(product);
      } catch (error: unknown) {
        if (axios.isAxiosError(error) && error.response?.status === 404) {
          alert(`Producto con el término '${query}' no encontrado.`);
        }
      }
    }
  }, [todosProductos, addProductToCart]);

  const handleQuantityChange = useCallback((id: string, newQuantity: number) => {
    if (venta || isStartingSale) return;
    setDraftItems(prev => prev.map(item =>
      item.id === id ? { ...item, quantity: newQuantity } : item
    ));
  }, [venta, isStartingSale]);

  const handleRemoveItem = useCallback((id: string) => {
    if (venta || isStartingSale) return;
    setDraftItems(prev => prev.filter(item => item.id !== id));
  }, [venta, isStartingSale]);

  const handleCancelSale = () => {
    if (venta || isStartingSale || draftItems.length === 0) return;
    if (window.confirm('¿Estás seguro de que deseas cancelar la venta actual? Se vaciará el carrito.')) {
      setDraftItems([]);
    }
  };

  const handleCheckout = async () => {
    if (venta) {
      alert('Funcionalidad de cobro se implementará en el futuro.');
      return;
    }
    if (draftItems.length === 0 || isStartingSale || turnoStatus !== 'ready' || !turnoId) return;

    setVentaError('');
    setIsStartingSale(true);
    setSearchResults([]);
    try {
      const ventas = await posApi.salesByShift(turnoId);
      const pendiente = ventas.filter((item) => item.estado === 'PENDIENTE').pop();
      if (pendiente) {
        setVenta(pendiente);
        setDraftItems([]);
        return;
      }

      const creada = await posApi.createSale(draftItems.map((item) => ({
        producto_id: item.id,
        cantidad: item.quantity,
      })));
      setVenta(creada);
      setDraftItems([]);
    } catch (error: unknown) {
      if (axios.isAxiosError(error) && !error.response) {
        try {
          const ventas = await posApi.salesByShift(turnoId);
          const pendiente = ventas.filter((item) => item.estado === 'PENDIENTE').pop();
          if (pendiente) {
            setVenta(pendiente);
            setDraftItems([]);
            return;
          }
        } catch {
          // La consulta de recuperación también falló: conservar el borrador y mostrar el error.
        }
      }
      if (axios.isAxiosError(error)) {
        const data = error.response?.data as { error?: string; message?: string } | undefined;
        setVentaError(data?.error || data?.message || (error.code === 'ECONNABORTED'
          ? 'La solicitud tardó demasiado. Consulta la venta pendiente antes de reintentar.'
          : 'No se pudo iniciar la venta. Intenta nuevamente.'));
        if (error.response?.status === 409) navigate('/abrir-turno', { replace: true });
      } else {
        setVentaError('No se pudo iniciar la venta. Intenta nuevamente.');
      }
    } finally {
      setIsStartingSale(false);
    }
  };

  const { subtotal: draftSubtotal, totalItems } = useMemo(() => {
    return cartItems.reduce(
      (acc, item) => {
        acc.subtotal += item.price * item.quantity;
        acc.totalItems += item.quantity;
        return acc;
      },
      { subtotal: 0, totalItems: 0 }
    );
  }, [cartItems]);

  const subtotal = venta ? venta.subtotal : draftSubtotal;
  const tasaIva = 0.19; 
  const iva = venta ? venta.impuestos : Math.round(subtotal * tasaIva);
  const total = venta ? venta.total : subtotal + iva;
  
  const isCartEmpty = cartItems.length === 0;

  if (turnoStatus === 'checking') {
    return (
      <div role="status" style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', padding: '2rem', color: 'var(--color-ink-soft)' }}>
        <span className="spinner" style={{ borderColor: 'var(--color-line)', borderTopColor: 'var(--color-primary)' }} />
        Verificando turno de caja...
      </div>
    );
  }

  if (turnoStatus === 'error') {
    return (
      <div role="alert" style={{ padding: '2rem', color: 'var(--color-danger)' }}>
        <p>No se pudo verificar el turno de caja. Intenta nuevamente antes de iniciar una venta.</p>
        <button type="button" onClick={() => setTurnoRetry((retry) => retry + 1)}>
          Reintentar
        </button>
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 100px)', gap: '1rem' }}>
      {ventaError && (
        <div role="alert" style={{ padding: '0.75rem 1rem', border: '1px solid var(--color-danger)', background: 'var(--color-danger-bg)', color: 'var(--color-danger)', borderRadius: '8px' }}>
          {ventaError}
        </div>
      )}
      {venta && (
        <div role="status" style={{ padding: '0.75rem 1rem', border: '1px solid var(--color-success)', background: 'var(--color-success-bg)', color: 'var(--color-success)', borderRadius: '8px' }}>
          Venta pendiente #{venta.id.slice(0, 8)}. Los ítems y totales provienen de MS-5.
        </div>
      )}
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
        {venta || isStartingSale ? (
          <p style={{ color: 'var(--color-ink-soft)' }}>La venta pendiente se muestra con los datos de MS-5. No se puede modificar desde esta pantalla.</p>
        ) : (
          <BarcodeInput onSearch={handleSearch} onTyping={handleTypingSearch} />
        )}
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
                    readOnly={!!venta || isStartingSale}
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
        <p style={{ marginTop: 0, color: 'var(--color-ink-soft)', fontSize: '0.85rem' }}>
          Método de pago: {venta?.metodo_pago ?? 'EFECTIVO'}
        </p>
        
        <div style={{ flex: 1 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem', color: 'var(--color-ink-soft)' }}>
            <span>Subtotal:</span>
            <span>$ {subtotal.toLocaleString('es-CL')}</span>
          </div>
          
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem', color: 'var(--color-ink-soft)' }}>
            <span>{venta ? 'Impuestos:' : 'IVA (19%):'}</span>
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
            disabled={isCartEmpty || isStartingSale}
            style={{ 
              width: '100%', 
              padding: '1rem', 
              fontSize: '1.125rem', 
              fontWeight: 'bold', 
              color: 'var(--color-surface)', 
              backgroundColor: isCartEmpty || isStartingSale ? 'var(--color-muted)' : 'var(--color-primary)',
              border: 'none', 
              borderRadius: '8px', 
              cursor: isCartEmpty || isStartingSale ? 'not-allowed' : 'pointer',
              transition: 'background-color 0.2s',
              boxShadow: isCartEmpty || isStartingSale ? 'none' : '0 4px 6px color-mix(in srgb, var(--color-primary) 25%, transparent)'
            }}
          >
            {isStartingSale ? 'INICIANDO VENTA...' : venta ? 'COBRAR' : 'INICIAR VENTA'}
          </button>
          
          <button 
            onClick={handleCancelSale}
            disabled={isCartEmpty || !!venta || isStartingSale}
            style={{ 
              width: '100%', 
              padding: '0.75rem', 
              fontSize: '1rem', 
              fontWeight: 600, 
              color: isCartEmpty || venta || isStartingSale ? 'var(--color-line)' : 'var(--color-ink-soft)',
              backgroundColor: isCartEmpty || venta || isStartingSale ? 'var(--color-bg)' : 'var(--color-line)',
              border: 'none', 
              borderRadius: '8px', 
              cursor: isCartEmpty || venta || isStartingSale ? 'not-allowed' : 'pointer',
              transition: 'background-color 0.2s'
            }}
          >
            {venta ? 'Venta pendiente' : 'Cancelar borrador'}
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
            <span>💼</span>
            <span>Cerrar Turno / Cuadre de Caja</span>
          </button>
        </div>

      </div>
    </div>
  </div>
  );
}



