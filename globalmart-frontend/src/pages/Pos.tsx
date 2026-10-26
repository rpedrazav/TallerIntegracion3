import React, { useState, useCallback, useMemo, useEffect, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import axios from 'axios';
import { catalogApi, CatalogProduct, posApi, VentaPendiente } from '../api/client';
import BarcodeInput from '../components/pos/BarcodeInput';
import CartItem from '../components/pos/CartItem';
import PaymentModal, { PaymentMethod, PaymentState } from '../components/pos/PaymentModal';
import { readStableWeightForCart } from '../hardware/scale-cart';
import { createTerminalPort } from '../payments/createTerminalPort';

const terminalPort = createTerminalPort();

export interface ProductItem {
  id: string;
  name: string;
  price: number;
  quantity: number;
  subtotal?: number;
  itemId?: string;
  isWeightVariable?: boolean;
  weightKg?: number;
}

interface PosLocationState {
  mensajeConfirmacion?: string;
  message?: string;
}

function saleErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { error?: string; message?: string } | undefined;
    return data?.error || data?.message || (error.code === 'ECONNABORTED'
      ? 'La solicitud tardó demasiado. Se debe actualizar la venta desde MS-5.'
      : 'No se pudo sincronizar el ítem con MS-5.');
  }
  return 'No se pudo sincronizar el ítem con MS-5.';
}

export default function Pos() {
  const navigate = useNavigate();
  const location = useLocation();
  const locationState = location.state as PosLocationState | null;
  const [mensajeConfirmacion, setMensajeConfirmacion] = useState<string | null>(
    locationState?.mensajeConfirmacion || locationState?.message || null
  );
  const [turnoStatus, setTurnoStatus] = useState<'checking' | 'ready' | 'error'>('checking');
  const [turnoError, setTurnoError] = useState('No se pudo verificar el turno de caja. Intenta nuevamente antes de iniciar una venta.');
  const [turnoRetry, setTurnoRetry] = useState(0);
  const [turnoId, setTurnoId] = useState<string | null>(null);
  const [venta, setVenta] = useState<VentaPendiente | null>(null);
  const [ventaError, setVentaError] = useState('');
  const [isStartingSale, setIsStartingSale] = useState(false);
  const [isPaymentOpen, setIsPaymentOpen] = useState(false);
  const [paymentState, setPaymentState] = useState<PaymentState>('available');
  const [paymentError, setPaymentError] = useState('');
  const [paymentShortfall, setPaymentShortfall] = useState<number | null>(null);
  const [paymentChange, setPaymentChange] = useState<number | null>(null);
  const [paymentReference, setPaymentReference] = useState<string | null>(null);
  const [draftPaymentMethod, setDraftPaymentMethod] = useState<'EFECTIVO' | 'TARJETA'>('EFECTIVO');
  const paymentInFlight = useRef(false);
  const paymentAttemptReference = useRef<string | null>(null);
  const scaleReadInFlight = useRef(false);
  const [isReadingScale, setIsReadingScale] = useState(false);
  const [pendingMutations, setPendingMutations] = useState(0);
  const [saleNeedsRefresh, setSaleNeedsRefresh] = useState(false);
  const saleRef = useRef<VentaPendiente | null>(null);
  const saleNeedsRefreshRef = useRef(false);
  const mutationQueue = useRef<Promise<void>>(Promise.resolve());

  // Estado de balanza simulada (S2-087 / S2-089)
  const [pesoBalanza, setPesoBalanza] = useState<number>(0);
  const [pesoEstable, setPesoEstable] = useState<boolean>(true);

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

  useEffect(() => {
    let active = true;
    let shiftConfirmed = false;
    setTurnoStatus('checking');
    setTurnoError('No se pudo verificar el turno de caja. Intenta nuevamente antes de iniciar una venta.');

    posApi.activeShift()
      .then(async (turno) => {
        shiftConfirmed = true;
        const ventas = await posApi.salesByShift(turno.id);
        if (!active) return;
        const pendingId = ventas.filter((item) => item.estado === 'PENDIENTE').pop()?.id;
        const recovered = pendingId ? await posApi.getSale(pendingId) : null;
        let pendiente = recovered?.estado === 'PENDIENTE' ? recovered : null;
        if (pendiente?.metodo_pago === 'TARJETA') {
          try {
            const intento = await terminalPort.consultarEstado({
              ventaId: pendiente.id,
              monto: pendiente.total,
              referenciaIntento: paymentAttemptReference.current,
            });
            if (!active) return;
            paymentAttemptReference.current = intento.referenciaIntento;
            if (intento.estado === 'uncertain') {
              setPaymentState('uncertain');
              setPaymentError('El intento de tarjeta sigue sin resultado confirmado. Verifica su estado antes de cobrar.');
              setIsPaymentOpen(true);
            } else if (intento.estado === 'approved') {
              setPaymentReference(intento.referenciaOpaca);
              setPaymentState('approved');
              setIsPaymentOpen(true);
            } else if (intento.estado === 'declined') {
              setPaymentState('declined');
              setPaymentError('La pasarela rechazó el cobro.');
              setIsPaymentOpen(true);
            }
          } catch {
            if (!active) return;
            setPaymentState('uncertain');
            setPaymentError('No se pudo consultar el intento de tarjeta. Verifica el estado antes de otro cobro.');
            setIsPaymentOpen(true);
          }
        }
        if (!active) return;
        setTurnoId(turno.id);
        saleRef.current = pendiente;
        setVenta(pendiente);
        if (pendingId) {
          setDraftItems([]);
          setSearchResults([]);
        }
        setTurnoStatus('ready');
      })
      .catch((error: unknown) => {
        if (!active) return;
        if (!shiftConfirmed && axios.isAxiosError(error) && error.response?.status === 404) {
          navigate('/abrir-turno', { replace: true });
        } else if (!(axios.isAxiosError(error) && error.response?.status === 401)) {
          setTurnoError(shiftConfirmed
            ? 'No se pudo recuperar la venta pendiente desde MS-5. Reintenta antes de modificar el carrito.'
            : 'No se pudo verificar el turno de caja. Intenta nuevamente antes de iniciar una venta.');
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
        itemId: item.id,
        name: item.nombre_producto,
        price: item.precio_unitario,
        quantity: item.cantidad,
        subtotal: item.subtotal,
        isWeightVariable: item.peso_kg != null,
        weightKg: item.peso_kg ?? undefined,
      }))
    : draftItems;

  const [searchResults, setSearchResults] = useState<CatalogProduct[]>([]);
  const [searchState, setSearchState] = useState<'idle' | 'loading' | 'empty' | 'error'>('idle');
  const [clearSearchSignal, setClearSearchSignal] = useState(0);
  const searchRequest = useRef(0);

  const refreshSale = useCallback(async (saleId: string) => {
    const updated = await posApi.getSale(saleId);
    saleRef.current = updated;
    setVenta(updated);
    saleNeedsRefreshRef.current = false;
    setSaleNeedsRefresh(false);
  }, []);

  const queueSaleMutation = useCallback((operation: (current: VentaPendiente) => Promise<void>) => {
    setPendingMutations((count) => count + 1);
    const task = mutationQueue.current.then(async () => {
      if (saleNeedsRefreshRef.current) {
        setVentaError('Actualiza la venta desde MS-5 antes de continuar.');
        return;
      }
      const current = saleRef.current;
      if (!current || current.estado !== 'PENDIENTE') {
        setVentaError('La venta ya no está pendiente. Actualiza la pantalla.');
        return;
      }

      setVentaError('');
      try {
        await operation(current);
      } catch (error: unknown) {
        try {
          await refreshSale(current.id);
        } catch {
          saleNeedsRefreshRef.current = true;
          setSaleNeedsRefresh(true);
        }
        setVentaError(saleErrorMessage(error));
        return;
      }

      try {
        await refreshSale(current.id);
      } catch {
        saleNeedsRefreshRef.current = true;
        setSaleNeedsRefresh(true);
        setVentaError('El ítem se envió, pero no se pudo actualizar la venta. Reintenta la consulta.');
      }
    }).finally(() => setPendingMutations((count) => count - 1));
    mutationQueue.current = task.catch(() => undefined);
    return task;
  }, [refreshSale]);

  const retrySaleRefresh = async () => {
    if (!saleRef.current || pendingMutations > 0) return;
    setPendingMutations(1);
    try {
      await refreshSale(saleRef.current.id);
      setVentaError('');
    } catch (error: unknown) {
      setVentaError(saleErrorMessage(error));
    } finally {
      setPendingMutations(0);
    }
  };

  const addProductToCart = useCallback(async (product: CatalogProduct): Promise<boolean> => {
    if (isStartingSale || saleNeedsRefresh || paymentState === 'uncertain' || paymentState === 'processing') return false;

    let weightKg: number | undefined;
    if (product.esPesoVariable) {
      if (scaleReadInFlight.current) return false;
      scaleReadInFlight.current = true;
      setIsReadingScale(true);
      setVentaError('');
      try {
        const reading = await readStableWeightForCart(window.api.scale);
        if (!reading.ok) {
          setVentaError(`No se agregó ${product.nombre}: ${reading.message}`);
          return false;
        }
        weightKg = reading.weightKg;
      } finally {
        scaleReadInFlight.current = false;
        setIsReadingScale(false);
      }
    }

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
    setDraftItems(prev => {
      const existing = prev.find(item => item.id === product.id);
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
      return [...prev, {
        id: product.id,
        name: product.nombre,
        price: product.precioBase,
        quantity: weightKg ?? 1,
        weightKg,
        isWeightVariable: product.esPesoVariable,
      }];
    });
    setSearchResults([]);
    setClearSearchSignal((value) => value + 1);
    return true;
  }, [venta, isStartingSale, saleNeedsRefresh, paymentState, queueSaleMutation]);

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

  const handleQuantityChange = useCallback((id: string, newQuantity: number) => {
    if (isStartingSale || saleNeedsRefresh || paymentState === 'uncertain' || paymentState === 'processing') return;
    if (venta) {
      void queueSaleMutation(async (current) => {
        const item = current.items.find((entry) => entry.id === id);
        if (!item) return;
        await posApi.updateSaleItem(current.id, item.id, newQuantity);
      });
      return;
    }
    setDraftItems(prev => prev.map(item =>
      item.id === id ? { ...item, quantity: newQuantity } : item
    ));
  }, [venta, isStartingSale, saleNeedsRefresh, paymentState, queueSaleMutation]);

  const handleRemoveItem = useCallback((id: string) => {
    if (isStartingSale || saleNeedsRefresh || paymentState === 'uncertain' || paymentState === 'processing') return;
    if (venta) {
      void queueSaleMutation(async (current) => {
        if (!current.items.some((item) => item.id === id)) return;
        await posApi.deleteSaleItem(current.id, id);
      });
      return;
    }
    setDraftItems(prev => prev.filter(item => item.id !== id));
  }, [venta, isStartingSale, saleNeedsRefresh, paymentState, queueSaleMutation]);

  const handleCancelSale = () => {
    if (venta || isStartingSale || draftItems.length === 0) return;
    if (window.confirm('¿Estás seguro de que deseas cancelar la venta actual? Se vaciará el carrito.')) {
      setDraftItems([]);
    }
  };

  const handleCheckout = async () => {
    if (venta) {
      if (pendingMutations > 0 || saleNeedsRefresh) return;
      if (paymentState === 'available') {
        setPaymentError('');
        setPaymentShortfall(null);
        setPaymentChange(null);
      }
      setIsPaymentOpen(true);
      return;
    }
    if (draftItems.length === 0 || isStartingSale || turnoStatus !== 'ready' || !turnoId) return;

    setVentaError('');
    setIsStartingSale(true);
    setSearchResults([]);
    try {
      const ventas = await posApi.salesByShift(turnoId);
      const pendingId = ventas.filter((item) => item.estado === 'PENDIENTE').pop()?.id;
      if (pendingId) {
        const pendiente = await posApi.getSale(pendingId);
        if (pendiente.estado === 'PENDIENTE') {
          saleRef.current = pendiente;
          setVenta(pendiente);
          setDraftItems([]);
          return;
        }
      }

      const creada = await posApi.createSale(draftItems.map((item) => ({
        producto_id: item.id,
        cantidad: item.quantity,
        peso_kg: item.isWeightVariable ? item.weightKg ?? item.quantity : undefined,
      })), draftPaymentMethod);
      saleRef.current = creada;
      setVenta(creada);
      setDraftItems([]);
    } catch (error: unknown) {
      if (axios.isAxiosError(error) && !error.response) {
        try {
          const ventas = await posApi.salesByShift(turnoId);
          const pendingId = ventas.filter((item) => item.estado === 'PENDIENTE').pop()?.id;
          if (pendingId) {
            const pendiente = await posApi.getSale(pendingId);
            if (pendiente.estado === 'PENDIENTE') {
              saleRef.current = pendiente;
              setVenta(pendiente);
              setDraftItems([]);
              return;
            }
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

  const handleCashPayment = async (method: PaymentMethod, cashReceived?: number) => {
    const current = saleRef.current;
    if (paymentState !== 'available' || method !== 'cash' || !current || current.estado !== 'PENDIENTE' || current.metodo_pago !== 'EFECTIVO'
      || cashReceived === undefined || !Number.isFinite(cashReceived) || cashReceived <= 0 || paymentInFlight.current) return;

    paymentInFlight.current = true;
    setPaymentState('processing');
    setPaymentError('');
    setPaymentShortfall(null);
    try {
      const result = await posApi.chargeCash(current.id, cashReceived);
      if (result.venta?.id !== current.id || result.venta.estado !== 'COMPLETADA' || !Number.isFinite(result.vuelto)) {
        setPaymentState('uncertain');
        setPaymentError('MS-5 no devolvió una confirmación completa. Verifica la venta antes de otro intento.');
        return;
      }
      saleRef.current = result.venta;
      setVenta(result.venta);
      setPaymentChange(result.vuelto);
      setPaymentState('approved');
    } catch (error: unknown) {
      if (axios.isAxiosError(error) && error.response?.status === 400) {
        const data = error.response.data as { error?: string; diferencia?: number } | undefined;
        setPaymentState('available');
        setPaymentError(data?.error || 'No se pudo cobrar la venta en efectivo.');
        setPaymentShortfall(data?.error === 'Monto insuficiente' && typeof data.diferencia === 'number' ? data.diferencia : null);
      } else {
        setPaymentState('uncertain');
        setPaymentError('No se pudo confirmar el resultado del cobro. Verifica la venta antes de otro intento.');
      }
    } finally {
      paymentInFlight.current = false;
    }
  };

  const handleCardPayment = async (method: PaymentMethod) => {
    const current = saleRef.current;
    if (paymentState !== 'available' || method !== 'card' || !current || current.estado !== 'PENDIENTE'
      || current.metodo_pago !== 'TARJETA' || paymentInFlight.current) return;
    paymentInFlight.current = true;
    setPaymentState('processing');
    setPaymentError('');
    setPaymentReference(null);
    try {
      const claveIdempotencia = paymentAttemptReference.current ?? crypto.randomUUID();
      paymentAttemptReference.current = claveIdempotencia;
      let result = await terminalPort.iniciarCobro({
        ventaId: current.id,
        monto: current.total,
        claveIdempotencia,
      });
      paymentAttemptReference.current = result.referenciaIntento;
      if (result.estado === 'processing') {
        result = await terminalPort.consultarEstado({
          ventaId: current.id,
          monto: current.total,
          referenciaIntento: result.referenciaIntento,
        });
        paymentAttemptReference.current = result.referenciaIntento;
      }
      if (result.estado === 'declined') {
        setPaymentState('declined');
        setPaymentError('La pasarela rechazó el cobro.');
        return;
      }
      if (result.estado === 'cancelled') {
        setPaymentState('cancelled');
        return;
      }
      if (result.estado === 'processing') return;
      if (result.estado !== 'approved') {
        setPaymentState('uncertain');
        setPaymentError('MS-5 no devolvió una confirmación completa. Verifica la venta antes de otro intento.');
        return;
      }
      setPaymentReference(result.referenciaOpaca);
      setPaymentState('approved');
    } catch {
      setPaymentState('uncertain');
      setPaymentError('No se pudo confirmar el resultado de la pasarela. Verifica la venta antes de otro cobro.');
    } finally {
      paymentInFlight.current = false;
    }
  };

  const handlePayment = (method: PaymentMethod, cashReceived?: number) => {
    if (method === 'cash') void handleCashPayment(method, cashReceived);
    else void handleCardPayment(method);
  };

  const changePaymentMethod = async (method: PaymentMethod) => {
    const current = saleRef.current;
    if (!current || current.estado !== 'PENDIENTE' || paymentInFlight.current || paymentState === 'uncertain') return;
    paymentInFlight.current = true;
    setPaymentError('');
    try {
      const updated = await posApi.changeSaleMethod(current.id, method === 'cash' ? 'EFECTIVO' : 'TARJETA');
      saleRef.current = updated;
      setVenta(updated);
      setPaymentState('available');
      setPaymentReference(null);
      paymentAttemptReference.current = null;
    } catch (error: unknown) {
      setPaymentError(saleErrorMessage(error));
    } finally {
      paymentInFlight.current = false;
    }
  };

  const verifyPayment = async () => {
    const current = saleRef.current;
    if (!current || paymentInFlight.current) return;
    paymentInFlight.current = true;
    setPaymentState('processing');
    setPaymentError('');
    try {
      if (current.metodo_pago === 'TARJETA') {
        const intento = await terminalPort.consultarEstado({
          ventaId: current.id,
          monto: current.total,
          referenciaIntento: paymentAttemptReference.current,
        });
        paymentAttemptReference.current = intento.referenciaIntento;
        if (intento.estado === 'approved') {
          setPaymentReference(intento.referenciaOpaca);
          setPaymentState('approved');
        } else if (intento.estado === 'declined') {
          setPaymentState('declined');
          setPaymentError('La pasarela rechazó el pago. Puedes elegir otro método o reintentar manualmente.');
        } else {
          setPaymentState('uncertain');
          setPaymentError('El intento aún no tiene un resultado definitivo. No se iniciará otra autorización.');
        }
        return;
      }
      const updated = await posApi.getSale(current.id);
      saleRef.current = updated;
      setVenta(updated);
      if (updated.estado === 'COMPLETADA') {
        setPaymentState('approved');
        setPaymentError('La venta está completada. El vuelto no está disponible en esta consulta.');
      } else {
        setPaymentState('uncertain');
        setPaymentError('La venta aún figura pendiente. Confirma el resultado antes de intentar otro cobro.');
      }
    } catch {
      setPaymentState('uncertain');
      setPaymentError('No se pudo consultar la venta. Reintenta la verificación.');
    } finally {
      paymentInFlight.current = false;
    }
  };

  const retryDeclinedCard = () => {
    if (paymentState !== 'declined' || saleRef.current?.metodo_pago !== 'TARJETA' || paymentInFlight.current) return;
    setPaymentState('available');
    setPaymentError('');
    setPaymentReference(null);
    paymentAttemptReference.current = null;
  };

  const closePayment = () => {
    if (paymentState === 'processing') return;
    setIsPaymentOpen(false);
    if (paymentState === 'approved') {
      saleRef.current = null;
      setVenta(null);
      setPaymentState('available');
      setPaymentChange(null);
      setPaymentError('');
      setPaymentShortfall(null);
      setPaymentReference(null);
      paymentAttemptReference.current = null;
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
        <p>{turnoError}</p>
        <button type="button" onClick={() => setTurnoRetry((retry) => retry + 1)}>
          Reintentar
        </button>
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 100px)', gap: '1rem' }}>
      {isPaymentOpen && venta && <PaymentModal
        total={venta.total}
        saleMethod={venta.metodo_pago === 'EFECTIVO' ? 'cash' : 'card'}
        state={paymentState}
        change={paymentChange}
        reference={paymentReference}
        errorMessage={paymentError}
        shortfall={paymentShortfall}
        onClose={closePayment}
        onConfirm={handlePayment}
        onChangeMethod={changePaymentMethod}
        onCashInputChange={() => { setPaymentError(''); setPaymentShortfall(null); }}
        onVerify={paymentState === 'uncertain' ? verifyPayment : undefined}
        onRetry={paymentState === 'declined' && venta.metodo_pago === 'TARJETA' ? retryDeclinedCard : undefined}
      />}
      {ventaError && (
        <div role="alert" style={{ padding: '0.75rem 1rem', border: '1px solid var(--color-danger)', background: 'var(--color-danger-bg)', color: 'var(--color-danger)', borderRadius: '8px' }}>
          {ventaError} {saleNeedsRefresh && <button type="button" onClick={retrySaleRefresh} disabled={pendingMutations > 0}>Actualizar venta</button>}
        </div>
      )}
      {paymentState === 'uncertain' && venta && <div role="alert" style={{ padding: '0.75rem 1rem', border: '1px solid var(--color-warning)', background: 'var(--color-warning-bg)', color: 'var(--color-warning-text)', borderRadius: '8px' }}>Resultado del cobro sin confirmar. Abre Cobrar y verifica la venta antes de continuar.</div>}
      {pendingMutations > 0 && <div role="status">Sincronizando ítems con MS-5...</div>}
      {isReadingScale && <div role="status">Esperando un peso estable de la balanza...</div>}
      {venta && (
        <div role="status" style={{ padding: '0.75rem 1rem', border: '1px solid var(--color-success)', background: 'var(--color-success-bg)', color: 'var(--color-success)', borderRadius: '8px' }}>
          Venta {venta.estado === 'COMPLETADA' ? 'completada' : 'pendiente'} #{venta.id.slice(0, 8)}. Los ítems y totales provienen de MS-5.
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
        {isStartingSale || isReadingScale || saleNeedsRefresh || paymentState === 'uncertain' || paymentState === 'processing' || (venta && venta.estado !== 'PENDIENTE') ? (
          <p style={{ color: 'var(--color-ink-soft)' }}>{isStartingSale ? 'Iniciando venta...' : isReadingScale ? 'Leyendo balanza...' : 'Actualiza la venta para seguir agregando productos.'}</p>
        ) : (
          <BarcodeInput onSearch={handleSearch} onTyping={handleTypingSearch} clearSignal={clearSearchSignal} />
        )}
        <div style={{ flex: 1, border: searchResults.length === 0 ? '2px dashed var(--color-line)' : 'none', borderRadius: '8px', overflow: 'auto', backgroundColor: searchResults.length === 0 ? 'var(--color-bg)' : 'transparent', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          {searchResults.length === 0 ? (
            <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--color-muted)', textAlign: 'center', padding: '1rem' }}>
              {searchState === 'loading' ? 'Buscando productos...' : searchState === 'error' ? 'No se pudo consultar el catálogo. Reintenta la búsqueda.' : searchState === 'empty' ? 'No se encontraron productos activos.' : 'Escribe el nombre del producto o escanea el código de barras'}
            </div>
          ) : (
            searchResults.map(prod => (
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
                  {prod.esPesoVariable && <div style={{ fontSize: '0.75rem', color: 'var(--color-warning-text)' }}>Requiere peso estable</div>}
                </div>
                <div style={{ fontWeight: 'bold', color: 'var(--color-primary)' }}>
                  ${prod.precioBase.toLocaleString('es-CL')}
                </div>
              </div>
            ))
          )}
        </div>

        {/* Simulador de Balanza Integrado (S2-087 / S2-089) */}
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
                cursor: 'pointer'
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
                  cursor: 'pointer'
                }}
              >
                +{kg}kg
              </button>
            ))}
          </div>
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
                    key={item.itemId ?? item.id}
                    id={item.itemId ?? item.id}
                    name={item.name} 
                    price={item.price} 
                    quantity={item.quantity} 
                    subtotal={item.subtotal}
                    isWeightVariable={item.isWeightVariable}
                    readOnly={item.isWeightVariable || isStartingSale || pendingMutations > 0 || saleNeedsRefresh || paymentState === 'uncertain' || paymentState === 'processing' || !!(venta && venta.estado !== 'PENDIENTE')}
                    canRemove={!venta || venta.estado === 'PENDIENTE'}
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
        {venta ? <p style={{ marginTop: 0, color: 'var(--color-ink-soft)', fontSize: '0.85rem' }}>Método de pago: {venta.metodo_pago}</p> :
          <label style={{ marginTop: 0, color: 'var(--color-ink-soft)', fontSize: '0.85rem' }}>Método de pago
            <select value={draftPaymentMethod} onChange={(event) => setDraftPaymentMethod(event.target.value as 'EFECTIVO' | 'TARJETA')} style={{ display: 'block', marginTop: '0.4rem', width: '100%' }}>
              <option value="EFECTIVO">Efectivo</option>
              <option value="TARJETA">Tarjeta simulada</option>
            </select>
          </label>}
        
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
            disabled={isCartEmpty || isStartingSale || pendingMutations > 0 || saleNeedsRefresh}
            style={{ 
              width: '100%', 
              padding: '1rem', 
              fontSize: '1.125rem', 
              fontWeight: 'bold', 
              color: 'var(--color-surface)', 
              backgroundColor: isCartEmpty || isStartingSale || pendingMutations > 0 || saleNeedsRefresh ? 'var(--color-muted)' : 'var(--color-primary)',
              border: 'none', 
              borderRadius: '8px', 
              cursor: isCartEmpty || isStartingSale || pendingMutations > 0 || saleNeedsRefresh ? 'not-allowed' : 'pointer',
              transition: 'background-color 0.2s',
              boxShadow: isCartEmpty || isStartingSale || pendingMutations > 0 || saleNeedsRefresh ? 'none' : '0 4px 6px color-mix(in srgb, var(--color-primary) 25%, transparent)'
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



