import React, { useState, useCallback, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import axios from 'axios';
import { catalogApi, CatalogProduct, posApi } from '../api/client';
import BarcodeInput from '../components/pos/BarcodeInput';
import PesoProducto, { PesoEstado } from '../components/pos/PesoProducto';
import PaymentModal from '../components/pos/PaymentModal';
import CartTable from '../components/pos/CartTable';
import CartSummary from '../components/pos/CartSummary';
import { manualWeightForCart, readStableWeightForCart } from '../hardware/scale-cart';
import { usePosRecovery, PosRecoveryResolved } from '../hooks/usePosRecovery';
import { useVenta, ProductItem } from '../hooks/useVenta';
import { useCobro } from '../hooks/useCobro';

export type { ProductItem };

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

  const [searchResults, setSearchResults] = useState<CatalogProduct[]>([]);
  const [searchState, setSearchState] = useState<'idle' | 'loading' | 'empty' | 'error'>('idle');
  const [clearSearchSignal, setClearSearchSignal] = useState(0);
  const searchRequest = useRef(0);

  const scaleReadInFlight = useRef(false);
  const [isReadingScale, setIsReadingScale] = useState(false);
  const [capturaPeso, setCapturaPeso] = useState<{
    product: CatalogProduct;
    estado: PesoEstado;
    origen: 'simulado' | 'manual';
    pesoKg?: number;
    error?: string;
    manualValue?: string;
  } | null>(null);

  // Balanza simulada
  const [pesoBalanza, setPesoBalanza] = useState<number>(0);
  const [pesoEstable, setPesoEstable] = useState<boolean>(true);

  // Hook de recuperación del turno y estado previo
  const { turnoStatus, turnoError, turnoId, retryTurno } = usePosRecovery(
    useCallback((data: PosRecoveryResolved) => {
      setVenta(data.venta);
      if (data.venta) {
        setDraftItems([]);
        setSearchResults([]);
      }
      cobroManager.applyRecoveryPayment(data);
    }, [])
  );

  // Hook de venta y carrito
  const {
    venta,
    setVenta,
    draftItems,
    setDraftItems,
    cartItems,
    isCartEmpty,
    totalItems,
    draftSubtotal,
    isStartingSale,
    pendingMutations,
    saleNeedsRefresh,
    ventaError,
    setVentaError,
    draftPaymentMethod,
    setDraftPaymentMethod,
    queueSaleMutation,
    retrySaleRefresh,
    handleQuantityChange,
    handleRemoveItem,
    handleCancelSale,
    handleCheckout,
  } = useVenta({
    turnoId,
    turnoStatus,
    paymentState: 'available', // el guard dinámico usa paymentState
    onOpenPayment: () => cobroManager.openPaymentModal(),
  });

  const subtotal = venta ? venta.subtotal : draftSubtotal;
  const iva = venta?.impuestos;
  const total = venta?.total;

  // Hook de cobro
  const cobroManager = useCobro({
    venta,
    onUpdateVenta: setVenta,
    turnoId,
  });

  const paymentState = cobroManager.paymentState;

  const commitProductToCart = useCallback((product: CatalogProduct, weightKg?: number): boolean => {
    if (venta) {
      void queueSaleMutation(async (current) => {
        const existing = current.items.find((item) => item.producto_id === product.id);
        if (existing) {
          if (weightKg !== undefined) {
            const accumulatedWeight = existing.cantidad + weightKg;
            await posApi.updateSaleItem(
              current.id,
              existing.id,
              accumulatedWeight,
              accumulatedWeight,
            );
          } else {
            await posApi.updateSaleItem(current.id, existing.id, existing.cantidad + 1);
          }
        } else {
          await posApi.addSaleItem(
            current.id,
            product.id,
            weightKg === undefined ? 1 : weightKg,
            weightKg,
          );
        }
      });
      setSearchResults([]);
      setClearSearchSignal((value) => value + 1);
      return true;
    }
    setDraftItems((prev) => {
      const existing = prev.find((item) => item.id === product.id);
      if (existing) {
        return prev.map((item) => {
          if (item.id !== product.id) return item;
          const quantity = item.quantity + (weightKg ?? 1);
          return {
            ...item,
            quantity,
            weightKg: product.esPesoVariable ? quantity : undefined,
            isWeightVariable: product.esPesoVariable,
          };
        });
      }
      return [
        ...prev,
        {
          id: product.id,
          name: product.nombre,
          price: product.precioBase,
          quantity: weightKg ?? 1,
          weightKg,
          isWeightVariable: product.esPesoVariable,
        },
      ];
    });
    setSearchResults([]);
    setClearSearchSignal((value) => value + 1);
    return true;
  }, [venta, queueSaleMutation, setDraftItems]);

  const readProductWeight = useCallback(async (product: CatalogProduct): Promise<boolean> => {
    if (scaleReadInFlight.current) return false;
    scaleReadInFlight.current = true;
    setIsReadingScale(true);
    setCapturaPeso({ product, estado: 'leyendo', origen: 'simulado' });
    setVentaError('');
    try {
      const reading = await readStableWeightForCart(window.api.scale);
      if (!reading.ok) {
        setCapturaPeso({ product, estado: 'error', origen: 'simulado', error: reading.message });
        return false;
      }
      setCapturaPeso({ product, estado: 'estable', origen: 'simulado', pesoKg: reading.weightKg });
      return true;
    } finally {
      scaleReadInFlight.current = false;
      setIsReadingScale(false);
    }
  }, [setVentaError]);

  const addProductToCart = useCallback(async (product: CatalogProduct): Promise<boolean> => {
    if (isStartingSale || saleNeedsRefresh || capturaPeso || paymentState === 'uncertain' || paymentState === 'processing') return false;
    if (product.esPesoVariable) return readProductWeight(product);
    return commitProductToCart(product);
  }, [isStartingSale, saleNeedsRefresh, capturaPeso, paymentState, readProductWeight, commitProductToCart]);

  const handleSimularPeso = async (kilos: number) => {
    if (window.api?.devices?.scale) {
      const res = await window.api.devices.scale.simularPeso(kilos);
      setPesoBalanza(res.pesoKg);
      setPesoEstable(res.estable);
    } else {
      setPesoBalanza(kilos);
      setPesoEstable(true);
    }
  };

  const handleTarar = async () => {
    if (window.api?.devices?.scale) {
      await window.api.devices.scale.tarar();
      const res = await window.api.devices.scale.obtenerPeso();
      setPesoBalanza(res.pesoKg);
      setPesoEstable(res.estable);
    } else {
      setPesoBalanza(0);
      setPesoEstable(true);
    }
  };

  const confirmWeight = () => {
    if (
      !capturaPeso ||
      capturaPeso.estado !== 'estable' ||
      capturaPeso.pesoKg === undefined ||
      isStartingSale ||
      saleNeedsRefresh ||
      paymentState === 'uncertain' ||
      paymentState === 'processing'
    ) {
      return;
    }
    commitProductToCart(capturaPeso.product, capturaPeso.pesoKg);
    setCapturaPeso(null);
  };

  const handleTypingSearch = useCallback(async (query: string) => {
    const request = ++searchRequest.current;
    const q = query.trim();
    if (!q) {
      setSearchResults([]);
      setSearchState('idle');
      return;
    }
    setSearchState('loading');
    try {
      const result = await catalogApi.searchProducts(q);
      if (request !== searchRequest.current) return;
      setSearchResults(result.data);
      setSearchState(result.data.length ? 'idle' : 'empty');
    } catch {
      if (request !== searchRequest.current) return;
      setSearchResults([]);
      setSearchState('error');
    }
  }, []);

  const handleSearch = useCallback(async (query: string): Promise<boolean> => {
    const q = query.trim();
    if (!q) return false;
    ++searchRequest.current;
    setSearchState('loading');
    try {
      const product = await catalogApi.lookupProduct(q);
      const added = await addProductToCart(product);
      setSearchState('idle');
      return added;
    } catch (error: unknown) {
      if (!(axios.isAxiosError(error) && error.response?.status === 404)) {
        setSearchState('error');
        return false;
      }
    }
    try {
      const result = await catalogApi.searchProducts(q);
      if (result.data.length === 1) {
        const added = await addProductToCart(result.data[0]);
        setSearchState('idle');
        return added;
      }
      setSearchResults(result.data);
      setSearchState(result.data.length ? 'idle' : 'empty');
    } catch {
      setSearchState('error');
    }
    return false;
  }, [addProductToCart]);

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
        <p>{turnoError}</p>
        <button type="button" onClick={retryTurno}>
          Reintentar
        </button>
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 100px)', gap: '1rem' }}>
      {cobroManager.isPaymentOpen && venta && (
        <PaymentModal
          total={venta.total}
          saleMethod={venta.metodo_pago === 'EFECTIVO' ? 'cash' : 'card'}
          state={cobroManager.paymentState}
          change={cobroManager.paymentChange}
          cashReceived={cobroManager.paymentCashReceived}
          reference={cobroManager.paymentReference}
          attemptReference={cobroManager.attemptReferenceView}
          recoveryNotice={cobroManager.recoveryNotice}
          errorMessage={cobroManager.paymentError}
          shortfall={cobroManager.paymentShortfall}
          terminalConnected={true}
          venta={venta}
          onClose={cobroManager.closePayment}
          onConfirm={cobroManager.handlePayment}
          onChangeMethod={cobroManager.changePaymentMethod}
          onCashInputChange={cobroManager.handleCashInputChange}
          onVerify={cobroManager.paymentState === 'uncertain' ? cobroManager.verifyPayment : undefined}
          onRetry={cobroManager.paymentState === 'declined' && venta.metodo_pago === 'TARJETA' ? cobroManager.retryDeclinedCard : undefined}
          onCancelAttempt={
            venta.metodo_pago === 'TARJETA' &&
            (cobroManager.paymentState === 'processing' || cobroManager.paymentState === 'uncertain')
              ? () => { void cobroManager.cancelCardAttempt(); }
              : undefined
          }
          onContinueAfterCancel={
            cobroManager.paymentState === 'cancelled' && venta.estado === 'PENDIENTE'
              ? cobroManager.continueAfterCancel
              : undefined
          }
        />
      )}

      {ventaError && (
        <div role="alert" style={{ padding: '0.75rem 1rem', border: '1px solid var(--color-danger)', background: 'var(--color-danger-bg)', color: 'var(--color-danger)', borderRadius: '8px' }}>
          {ventaError}{' '}
          {saleNeedsRefresh && (
            <button type="button" onClick={retrySaleRefresh} disabled={pendingMutations > 0}>
              Actualizar venta
            </button>
          )}
        </div>
      )}

      {cobroManager.paymentState === 'uncertain' && venta && (
        <div role="alert" style={{ padding: '0.75rem 1rem', border: '1px solid var(--color-warning)', background: 'var(--color-warning-bg)', color: 'var(--color-warning-text)', borderRadius: '8px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '0.75rem' }}>
          <span>Resultado del cobro sin confirmar para venta #{venta.id.slice(0, 8)}. Verifica el estado antes de continuar.</span>
          {!cobroManager.isPaymentOpen && (
            <button type="button" onClick={() => cobroManager.setIsPaymentOpen(true)}>
              Verificar estado del cobro
            </button>
          )}
        </div>
      )}

      {cobroManager.paymentState === 'cancelled' && venta && !cobroManager.isPaymentOpen && (
        <div role="status" style={{ padding: '0.75rem 1rem', border: '1px solid var(--color-line)', background: 'var(--color-bg)', color: 'var(--color-ink)', borderRadius: '8px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '0.75rem' }}>
          <span>Intento cancelado para venta #{venta.id.slice(0, 8)}. Puedes continuar el cobro sin perder el carrito.</span>
          <button type="button" onClick={() => cobroManager.setIsPaymentOpen(true)}>
            Continuar cobro
          </button>
        </div>
      )}

      {pendingMutations > 0 && <div role="status">Sincronizando ítems con MS-5...</div>}
      {isReadingScale && <div role="status">Esperando un peso estable de la balanza...</div>}

      {venta && (
        <div role="status" style={{ padding: '0.75rem 1rem', border: '1px solid var(--color-success)', background: 'var(--color-success-bg)', color: 'var(--color-success)', borderRadius: '8px' }}>
          Venta {venta.estado === 'COMPLETADA' ? 'completada' : 'pendiente'} #{venta.id.slice(0, 8)}. Los ítems y totales provienen de MS-5.{cobroManager.recoveryNotice ? ` ${cobroManager.recoveryNotice}` : ''}
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
          {isStartingSale ||
          isReadingScale ||
          capturaPeso ||
          saleNeedsRefresh ||
          paymentState === 'uncertain' ||
          paymentState === 'processing' ||
          (venta && venta.estado !== 'PENDIENTE') ? (
            <p style={{ color: 'var(--color-ink-soft)' }}>
              {isStartingSale
                ? 'Iniciando venta...'
                : isReadingScale
                ? 'Leyendo balanza...'
                : 'Actualiza la venta para seguir agregando productos.'}
            </p>
          ) : (
            <BarcodeInput onSearch={handleSearch} onTyping={handleTypingSearch} clearSignal={clearSearchSignal} />
          )}

          <div
            style={{
              flex: 1,
              border: searchResults.length === 0 ? '2px dashed var(--color-line)' : 'none',
              borderRadius: '8px',
              overflow: 'auto',
              backgroundColor: searchResults.length === 0 ? 'var(--color-bg)' : 'transparent',
              display: 'flex',
              flexDirection: 'column',
              gap: '0.5rem',
            }}
          >
            {searchResults.length === 0 ? (
              <div
                style={{
                  flex: 1,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: 'var(--color-muted)',
                  textAlign: 'center',
                  padding: '1rem',
                }}
              >
                {searchState === 'loading'
                  ? 'Buscando productos...'
                  : searchState === 'error'
                  ? 'No se pudo consultar el catálogo. Reintenta la búsqueda.'
                  : searchState === 'empty'
                  ? 'No se encontraron productos activos.'
                  : 'Escribe el nombre del producto o escanea el código de barras'}
              </div>
            ) : (
              searchResults.map((prod) => (
                <div
                  key={prod.id}
                  onClick={() => { void addProductToCart(prod); }}
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
                    boxShadow: '0 1px 2px rgba(0,0,0,0.05)',
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
                    {prod.esPesoVariable && <div style={{ fontSize: '0.75rem', color: 'var(--color-warning-text)' }}>Requiere peso estable</div>}
                  </div>
                  <div style={{ fontWeight: 'bold', color: 'var(--color-primary)' }}>
                    ${prod.precioBase.toLocaleString('es-CL')}
                  </div>
                </div>
              ))
            )}
          </div>

          {capturaPeso && (
            <PesoProducto
              nombre={capturaPeso.product.nombre}
              estado={capturaPeso.estado}
              origen={capturaPeso.origen}
              pesoKg={capturaPeso.pesoKg}
              error={capturaPeso.error}
              manualValue={capturaPeso.manualValue}
              onManualChange={(value) => {
                const parsed = manualWeightForCart(value);
                setCapturaPeso((current) =>
                  current
                    ? {
                        ...current,
                        manualValue: value,
                        estado: parsed.ok ? 'estable' : 'error',
                        pesoKg: parsed.ok ? parsed.weightKg : undefined,
                        error: parsed.ok || !value ? undefined : parsed.message,
                      }
                    : null
                );
              }}
              onConfirmar={confirmWeight}
              onReintentar={
                capturaPeso.origen === 'simulado' && capturaPeso.estado === 'error'
                  ? () => { void readProductWeight(capturaPeso.product); }
                  : undefined
              }
              onManual={
                capturaPeso.origen === 'simulado' && capturaPeso.estado === 'error'
                  ? () => setCapturaPeso({ product: capturaPeso.product, estado: 'error', origen: 'manual', manualValue: '' })
                  : undefined
              }
              onCancelar={() => setCapturaPeso(null)}
            />
          )}

          {capturaPeso && (
            <div style={{ marginTop: '0.75rem', padding: '0.75rem', backgroundColor: 'var(--color-bg)', borderRadius: '8px', border: '1px solid var(--color-line)' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                <span style={{ fontWeight: 600, fontSize: '0.85rem', color: 'var(--color-ink)' }}>⚖️ Balanza Simulada</span>
                <span style={{ fontSize: '0.8rem', color: pesoEstable ? 'var(--color-success)' : 'var(--color-muted)' }}>
                  {pesoEstable ? '● Estable' : '○ Leyendo...'}
                </span>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                <span style={{ fontSize: '1.2rem', fontWeight: 700, color: 'var(--color-primary)' }}>
                  {pesoBalanza.toFixed(3)} kg
                </span>
                <button
                  type="button"
                  onClick={handleTarar}
                  style={{
                    padding: '0.25rem 0.5rem',
                    fontSize: '0.75rem',
                    backgroundColor: 'var(--color-surface)',
                    border: '1px solid var(--color-line)',
                    borderRadius: '4px',
                    cursor: 'pointer',
                  }}
                >
                  Tarar
                </button>
              </div>
              <div style={{ display: 'flex', gap: '0.25rem', flexWrap: 'wrap' }}>
                {[0.25, 0.5, 1.0, 2.5].map((kg) => (
                  <button
                    key={kg}
                    type="button"
                    onClick={() => handleSimularPeso(kg)}
                    style={{
                      flex: 1,
                      padding: '0.2rem',
                      fontSize: '0.7rem',
                      backgroundColor: 'var(--color-surface)',
                      border: '1px solid var(--color-line)',
                      borderRadius: '4px',
                      cursor: 'pointer',
                    }}
                  >
                    +{kg}kg
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Columna Central: Tabla del Carrito */}
        <CartTable
          cartItems={cartItems}
          totalItems={totalItems}
          isCartEmpty={isCartEmpty}
          venta={venta}
          isStartingSale={isStartingSale}
          pendingMutations={pendingMutations}
          saleNeedsRefresh={saleNeedsRefresh}
          paymentState={paymentState}
          onQuantityChange={handleQuantityChange}
          onRemove={handleRemoveItem}
        />

        {/* Columna Derecha: Totales y Botones */}
        <CartSummary
          venta={venta}
          draftPaymentMethod={draftPaymentMethod}
          onDraftPaymentMethodChange={setDraftPaymentMethod}
          subtotal={subtotal}
          iva={iva}
          total={total}
          isCartEmpty={isCartEmpty}
          isStartingSale={isStartingSale}
          pendingMutations={pendingMutations}
          saleNeedsRefresh={saleNeedsRefresh}
          onCheckout={handleCheckout}
          onCancelSale={handleCancelSale}
          onNavigateCerrarTurno={() => navigate('/cerrar-turno')}
        />
      </div>
    </div>
  );
}
