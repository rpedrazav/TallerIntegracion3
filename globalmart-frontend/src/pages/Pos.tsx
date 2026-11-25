import React, { useState, useCallback, useRef, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import axios from 'axios';
import { catalogApi, CatalogProduct, posApi } from '../api/client';
import BarcodeInput from '../components/pos/BarcodeInput';
import PesoProducto, { PesoEstado } from '../components/pos/PesoProducto';
import PaymentModal from '../components/pos/PaymentModal';
import CartTable from '../components/pos/CartTable';
import CartSummary from '../components/pos/CartSummary';
import './Pos.css';
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

  // Ref para evitar referencias adelantadas a setters/hooks declarados abajo
  const recoveryHandlerRef = useRef<((data: PosRecoveryResolved) => void) | null>(null);
  const handleRecoveryResolved = useCallback((data: PosRecoveryResolved) => {
    recoveryHandlerRef.current?.(data);
  }, []);

  // Hook de recuperación del turno y estado previo
  const { turnoStatus, turnoError, turnoId, retryTurno } = usePosRecovery(handleRecoveryResolved);

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

  useEffect(() => {
    recoveryHandlerRef.current = (data: PosRecoveryResolved) => {
      setVenta(data.venta);
      if (data.venta) {
        setDraftItems([]);
        setSearchResults([]);
      }
      cobroManager.applyRecoveryPayment(data);
    };
  }, [cobroManager, setDraftItems, setVenta]);

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
      <div role="status" className="pos-loading">
        <span className="spinner pos-loading-spinner" />
        Verificando turno de caja...
      </div>
    );
  }

  if (turnoStatus === 'error') {
    return (
      <div role="alert" className="pos-error-state">
        <p>{turnoError}</p>
        <button type="button" onClick={retryTurno} className="pos-alert-action">
          Reintentar
        </button>
      </div>
    );
  }

  return (
    <div className="pos-page">
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
        <div role="alert" className="pos-alert pos-alert--danger">
          <span>{ventaError}</span>
          {saleNeedsRefresh && (
            <button type="button" onClick={retrySaleRefresh} disabled={pendingMutations > 0} className="pos-alert-action">
              Actualizar venta
            </button>
          )}
        </div>
      )}

      {cobroManager.paymentState === 'uncertain' && venta && (
        <div role="alert" className="pos-alert pos-alert--warning">
          <span>Resultado del cobro sin confirmar para venta #{venta.id.slice(0, 8)}. Verifica el estado antes de continuar.</span>
          {!cobroManager.isPaymentOpen && (
            <button type="button" onClick={() => cobroManager.setIsPaymentOpen(true)} className="pos-alert-action">
              Verificar estado del cobro
            </button>
          )}
        </div>
      )}

      {cobroManager.paymentState === 'cancelled' && venta && !cobroManager.isPaymentOpen && (
        <div role="status" className="pos-alert pos-alert--info">
          <span>Intento cancelado para venta #{venta.id.slice(0, 8)}. Puedes continuar el cobro sin perder el carrito.</span>
          <button type="button" onClick={() => cobroManager.setIsPaymentOpen(true)} className="pos-alert-action">
            Continuar cobro
          </button>
        </div>
      )}

      {pendingMutations > 0 && <div role="status">Sincronizando ítems con MS-5...</div>}
      {isReadingScale && <div role="status">Esperando un peso estable de la balanza...</div>}

      {venta && (
        <div role="status" className="pos-alert pos-alert--success">
          Venta {venta.estado === 'COMPLETADA' ? 'completada' : 'pendiente'} #{venta.id.slice(0, 8)}. Los ítems y totales provienen de MS-5.{cobroManager.recoveryNotice ? ` ${cobroManager.recoveryNotice}` : ''}
        </div>
      )}

      {mensajeConfirmacion && (
        <div className="pos-alert pos-alert--success" role="status">
          <div className="pos-alert-title-row">
            <span aria-hidden="true">✅</span>
            <span>{mensajeConfirmacion}</span>
          </div>
          <button
            type="button"
            onClick={() => setMensajeConfirmacion(null)}
            className="pos-alert-close"
            title="Cerrar mensaje"
          >
            ✕
          </button>
        </div>
      )}

      <div className="pos-columns">
        {/* Columna Izquierda: Búsqueda y Escaneo */}
        <div className="pos-panel pos-panel--search">
          <h3 className="pos-panel-title">Buscar Producto</h3>
          {isStartingSale ||
          isReadingScale ||
          capturaPeso ||
          saleNeedsRefresh ||
          paymentState === 'uncertain' ||
          paymentState === 'processing' ||
          (venta && venta.estado !== 'PENDIENTE') ? (
            <p className="pos-search-blocked">
              {isStartingSale
                ? 'Iniciando venta...'
                : isReadingScale
                ? 'Leyendo balanza...'
                : 'Actualiza la venta para seguir agregando productos.'}
            </p>
          ) : (
            <BarcodeInput onSearch={handleSearch} onTyping={handleTypingSearch} clearSignal={clearSearchSignal} />
          )}

          <div className={`pos-search-results${searchResults.length === 0 ? ' pos-search-results--empty' : ''}`}>
            {searchResults.length === 0 ? (
              <div className="pos-search-empty-msg">
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
                  className="pos-product-item"
                >
                  <div>
                    <div className="pos-product-name">{prod.nombre}</div>
                    <div className="pos-product-barcode">{prod.codigoBarras}</div>
                    {prod.esPesoVariable && <div className="pos-product-weight-tag">Requiere peso estable</div>}
                  </div>
                  <div className="pos-product-price">
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
            <div className="pos-scale-widget">
              <div className="pos-scale-header">
                <span className="pos-scale-title">⚖️ Balanza Simulada</span>
                <span className={`pos-scale-status${pesoEstable ? ' pos-scale-status--stable' : ''}`}>
                  {pesoEstable ? '● Estable' : '○ Leyendo...'}
                </span>
              </div>
              <div className="pos-scale-reading-row">
                <span className="pos-scale-value">{pesoBalanza.toFixed(3)} kg</span>
                <button type="button" onClick={handleTarar} className="pos-scale-tare-btn">
                  Tarar
                </button>
              </div>
              <div className="pos-scale-presets">
                {[0.25, 0.5, 1.0, 2.5].map((kg) => (
                  <button
                    key={kg}
                    type="button"
                    onClick={() => handleSimularPeso(kg)}
                    className="pos-scale-preset-btn"
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
