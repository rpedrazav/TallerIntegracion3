import React, { useState, useCallback, useMemo, useEffect } from 'react';
import BarcodeInput from '../components/pos/BarcodeInput';
import CartItem from '../components/pos/CartItem';
import { TerminalState, SimulatedAction } from '../devices/types';

export interface ProductItem {
  id: string;
  name: string;
  price: number;
  quantity: number;
  pesoKg?: number;
}

export default function Pos() {
  const [cartItems, setCartItems] = useState<ProductItem[]>([
    { id: '1', name: 'Coca Cola 2L', price: 2500, quantity: 2 },
    { id: '2', name: 'Pan de Molde Castaño', price: 1800, quantity: 1 }
  ]);

  // Estados de balanza simulada (S2-087 / S2-089)
  const [pesoBalanza, setPesoBalanza] = useState<number>(0);
  const [pesoEstable, setPesoEstable] = useState<boolean>(true);

  // Estados de terminal de pago simulado (S2-086 / S2-089)
  const [isPaymentModalOpen, setIsPaymentModalOpen] = useState<boolean>(false);
  const [terminalState, setTerminalState] = useState<TerminalState>('IDLE');
  const [paymentMessage, setPaymentMessage] = useState<string>('');
  const [authCode, setAuthCode] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState<boolean>(false);

  // Consultar peso inicial de la balanza
  const refrescarBalanza = useCallback(async () => {
    try {
      if (window.api?.devices?.scale) {
        const lectura = await window.api.devices.scale.obtenerPeso();
        setPesoBalanza(lectura.pesoKg);
        setPesoEstable(lectura.estable);
      }
    } catch (err) {
      console.warn('Balanza no disponible o error al leer:', err);
    }
  }, []);

  useEffect(() => {
    refrescarBalanza();
  }, [refrescarBalanza]);

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

  // Acciones de balanza simulada
  const handleSimularPeso = async (kilos: number) => {
    if (window.api?.devices?.scale) {
      const res = await window.api.devices.scale.simularPeso(kilos);
      setPesoBalanza(res.pesoKg);
    } else {
      setPesoBalanza(kilos);
    }
  };

  const handleTararBalanza = async () => {
    if (window.api?.devices?.scale) {
      await window.api.devices.scale.tarar();
      setPesoBalanza(0);
    } else {
      setPesoBalanza(0);
    }
  };

  const handleAgregarProductoPesable = () => {
    if (pesoBalanza <= 0) {
      alert('La balanza debe tener un peso mayor a 0 kg para agregar producto a granel.');
      return;
    }
    const precioKilo = 1200;
    const nuevoItem: ProductItem = {
      id: `granel_${Date.now()}`,
      name: `Manzanas Granel (${pesoBalanza.toFixed(3)} kg)`,
      price: Math.round(precioKilo * pesoBalanza),
      quantity: 1,
      pesoKg: pesoBalanza
    };
    setCartItems(prev => [...prev, nuevoItem]);
  };

  // Acciones de Terminal de Pago Simulado
  const handleOpenPayment = async () => {
    if (isCartEmpty) return;
    setIsPaymentModalOpen(true);
    setTerminalState('ESPERANDO_TARJETA');
    setPaymentMessage('Terminal POS iniciado. Inserte, deslice o acerque tarjeta...');
    setAuthCode(null);
    setIsProcessing(false);

    try {
      if (window.api?.devices?.terminal) {
        await window.api.devices.terminal.iniciar({ monto: total });
      }
    } catch (err: unknown) {
      setPaymentMessage(`Error al iniciar terminal: ${err instanceof Error ? err.message : String(err)}`);
    }
  };

  const handleSimularAccionTerminal = async (accion: SimulatedAction) => {
    setIsProcessing(true);
    setPaymentMessage('Procesando transacción en terminal...');

    try {
      if (window.api?.devices?.terminal) {
        const res = await window.api.devices.terminal.autorizar(accion);
        setTerminalState(res.estado);

        if (res.exito) {
          setAuthCode(res.codigoAutorizacion || 'AUTH-OK');
          setPaymentMessage('¡Pago con tarjeta APROBADO exitosamente! Venta completada.');
          setTimeout(() => {
            // Vaciar carrito tras venta exitosa
            setCartItems([]);
          }, 1500);
        } else {
          setPaymentMessage(res.mensaje || 'Pago con tarjeta RECHAZADO por el emisor.');
        }
      } else {
        // Fallback en memoria si se corre fuera de Electron
        if (accion === 'APROBAR') {
          setTerminalState('APROBADO');
          setAuthCode('AUTH-MOCK-SIM');
          setPaymentMessage('¡Pago simulado APROBADO!');
          setTimeout(() => setCartItems([]), 1500);
        } else {
          setTerminalState('RECHAZADO');
          setPaymentMessage('Pago RECHAZADO (fondos insuficientes o tarjeta declinada).');
        }
      }
    } catch (err: unknown) {
      setTerminalState('ERROR');
      setPaymentMessage(`Fallo en el terminal: ${err instanceof Error ? err.message : String(err)}`);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleCancelarCobroTerminal = async () => {
    try {
      if (window.api?.devices?.terminal) {
        await window.api.devices.terminal.cancelar();
      }
    } catch (err) {
      console.warn('Error al cancelar terminal:', err);
    } finally {
      setIsPaymentModalOpen(false);
      setTerminalState('IDLE');
      setPaymentMessage('');
      setAuthCode(null);
    }
  };

  return (
    <div style={{ display: 'flex', height: 'calc(100vh - 100px)', gap: '1rem', position: 'relative' }}>
      
      {/* Columna Izquierda: Búsqueda y Balanza Simulada */}
      <div style={{ flex: '0 0 320px', display: 'flex', flexDirection: 'column', background: '#fff', padding: '1rem', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)', gap: '1rem' }}>
        <div>
          <h3 style={{ marginTop: 0, color: '#0ea5e9', fontSize: '1.25rem' }}>Buscar Producto</h3>
          <BarcodeInput onSearch={handleSearch} />
        </div>

        {/* Módulo de Balanza Simulada (S2-087 / S2-089) */}
        <div style={{ border: '1px solid #e2e8f0', borderRadius: '8px', padding: '0.875rem', background: '#f8fafc' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
            <span style={{ fontWeight: 600, fontSize: '0.9rem', color: '#334155' }}>⚖️ Balanza POS (Mock)</span>
            <span style={{ fontSize: '0.75rem', padding: '0.15rem 0.4rem', borderRadius: '4px', backgroundColor: pesoEstable ? '#dcfce7' : '#fef9c3', color: pesoEstable ? '#15803d' : '#854d0e' }}>
              {pesoEstable ? 'Estable' : 'Pesando'}
            </span>
          </div>

          <div style={{ fontSize: '1.75rem', fontWeight: 'bold', textAlign: 'center', margin: '0.5rem 0', color: '#0f172a', fontFamily: 'monospace' }}>
            {pesoBalanza.toFixed(3)} <span style={{ fontSize: '1rem' }}>kg</span>
          </div>

          <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <button 
              onClick={() => handleSimularPeso(0.850)}
              style={{ flex: 1, padding: '0.35rem', fontSize: '0.75rem', borderRadius: '4px', border: '1px solid #cbd5e1', cursor: 'pointer', background: '#fff' }}
            >
              +0.85 kg
            </button>
            <button 
              onClick={() => handleSimularPeso(1.500)}
              style={{ flex: 1, padding: '0.35rem', fontSize: '0.75rem', borderRadius: '4px', border: '1px solid #cbd5e1', cursor: 'pointer', background: '#fff' }}
            >
              +1.50 kg
            </button>
            <button 
              onClick={handleTararBalanza}
              style={{ flex: 1, padding: '0.35rem', fontSize: '0.75rem', borderRadius: '4px', border: '1px solid #cbd5e1', cursor: 'pointer', background: '#fff' }}
            >
              Tarar (0)
            </button>
          </div>

          <button
            onClick={handleAgregarProductoPesable}
            disabled={pesoBalanza <= 0}
            style={{
              width: '100%',
              padding: '0.5rem',
              backgroundColor: pesoBalanza > 0 ? '#10b981' : '#94a3b8',
              color: '#fff',
              border: 'none',
              borderRadius: '4px',
              fontSize: '0.8rem',
              fontWeight: 600,
              cursor: pesoBalanza > 0 ? 'pointer' : 'not-allowed'
            }}
          >
            Agregar Granel ({pesoBalanza.toFixed(3)} kg)
          </button>
        </div>

        <div style={{ flex: 1, border: '2px dashed #e5e7eb', borderRadius: '8px', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#9ca3af', backgroundColor: '#f9fafb', fontSize: '0.85rem' }}>
          [Resultados de búsqueda]
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
              <p style={{ fontSize: '1.25rem', margin: '0 0 0.5rem 0', color: '#6b7280', fontWeight: 500 }}>Carrito vacío</p>
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
                {cartItems.map((item) => (
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

      {/* Columna Derecha: Totales y Cobro con Tarjeta */}
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
            onClick={handleOpenPayment}
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
            💳 COBRAR CON TARJETA
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

      {/* Modal Interactivo de Terminal POS Simulado (S2-086 / S2-089) */}
      {isPaymentModalOpen && (
        <div style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: 'rgba(15, 23, 42, 0.75)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 999
        }}>
          <div style={{
            background: '#fff',
            borderRadius: '12px',
            padding: '2rem',
            width: '460px',
            boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.2)',
            display: 'flex',
            flexDirection: 'column',
            gap: '1rem'
          }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h3 style={{ margin: 0, color: '#0f172a', fontSize: '1.25rem' }}>Terminal POS (Simulador)</h3>
              <span style={{
                fontSize: '0.8rem',
                padding: '0.2rem 0.5rem',
                borderRadius: '6px',
                backgroundColor: terminalState === 'APROBADO' ? '#dcfce7' : terminalState === 'RECHAZADO' ? '#fee2e2' : '#e0f2fe',
                color: terminalState === 'APROBADO' ? '#166534' : terminalState === 'RECHAZADO' ? '#991b1b' : '#075985',
                fontWeight: 600
              }}>
                {terminalState}
              </span>
            </div>

            <div style={{ textAlign: 'center', padding: '1rem', background: '#f8fafc', borderRadius: '8px', border: '1px solid #e2e8f0' }}>
              <div style={{ fontSize: '0.875rem', color: '#64748b' }}>Monto a cobrar:</div>
              <div style={{ fontSize: '1.75rem', fontWeight: 'bold', color: '#0ea5e9' }}>
                $ {total.toLocaleString('es-CL')}
              </div>
              <p style={{ margin: '0.75rem 0 0 0', fontSize: '0.9rem', color: '#334155' }}>
                {paymentMessage}
              </p>
              {authCode && (
                <div style={{ marginTop: '0.5rem', fontSize: '0.85rem', color: '#16a34a', fontWeight: 600 }}>
                  Código Autorización: {authCode}
                </div>
              )}
            </div>

            {/* Botones de Simulación de Lectura (S2-089) */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
              <div style={{ fontSize: '0.8rem', color: '#64748b', fontWeight: 600, textTransform: 'uppercase' }}>
                Simular respuesta del cliente / tarjeta:
              </div>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button
                  onClick={() => handleSimularAccionTerminal('APROBAR')}
                  disabled={isProcessing || terminalState === 'APROBADO'}
                  style={{
                    flex: 1,
                    padding: '0.75rem',
                    backgroundColor: '#10b981',
                    color: '#fff',
                    border: 'none',
                    borderRadius: '6px',
                    fontWeight: 600,
                    cursor: (isProcessing || terminalState === 'APROBADO') ? 'not-allowed' : 'pointer'
                  }}
                >
                  ✓ Aprobar
                </button>
                <button
                  onClick={() => handleSimularAccionTerminal('RECHAZAR')}
                  disabled={isProcessing || terminalState === 'APROBADO'}
                  style={{
                    flex: 1,
                    padding: '0.75rem',
                    backgroundColor: '#ef4444',
                    color: '#fff',
                    border: 'none',
                    borderRadius: '6px',
                    fontWeight: 600,
                    cursor: (isProcessing || terminalState === 'APROBADO') ? 'not-allowed' : 'pointer'
                  }}
                >
                  ✗ Rechazar
                </button>
              </div>
            </div>

            <div style={{ borderTop: '1px solid #e2e8f0', paddingTop: '1rem', display: 'flex', justifyContent: 'flex-end' }}>
              <button
                onClick={handleCancelarCobroTerminal}
                style={{
                  padding: '0.5rem 1rem',
                  backgroundColor: '#f1f5f9',
                  color: '#475569',
                  border: 'none',
                  borderRadius: '6px',
                  fontWeight: 600,
                  cursor: 'pointer'
                }}
              >
                {terminalState === 'APROBADO' ? 'Cerrar' : 'Cancelar Cobro'}
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  );
}
