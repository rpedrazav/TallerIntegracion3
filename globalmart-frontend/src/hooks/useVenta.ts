import { useState, useCallback, useMemo, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { posApi, VentaPendiente } from '../api/client';
import { useAuth } from './useAuth';
import { parseJwt } from '../utils/jwt';
import { PaymentState } from '../components/pos/PaymentModal';

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

function saleErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { error?: string; message?: string } | undefined;
    return data?.error || data?.message || (error.code === 'ECONNABORTED'
      ? 'La solicitud tardó demasiado. Se debe actualizar la venta desde MS-5.'
      : 'No se pudo sincronizar el ítem con MS-5.');
  }
  return 'No se pudo sincronizar el ítem con MS-5.';
}

async function fetchLatestPendingSale(shiftId: string): Promise<VentaPendiente | null> {
  const ventas = await posApi.salesByShift(shiftId);
  const pendingId = ventas.filter((item) => item.estado === 'PENDIENTE').pop()?.id;
  if (!pendingId) return null;
  const recovered = await posApi.getSale(pendingId);
  return recovered?.estado === 'PENDIENTE' ? recovered : null;
}

interface UseVentaOptions {
  turnoId: string | null;
  turnoStatus: 'checking' | 'ready' | 'error';
  paymentState: PaymentState;
  onOpenPayment: () => void;
}

export function useVenta({
  turnoId,
  turnoStatus,
  paymentState,
  onOpenPayment,
}: UseVentaOptions) {
  const navigate = useNavigate();
  const { token } = useAuth();
  const cartOwner = parseJwt(token);
  const cartStorageKey = cartOwner?.tenant_id && cartOwner?.sub
    ? `pos_cart:${cartOwner.tenant_id}:${cartOwner.sub}` : null;

  const [venta, setVenta] = useState<VentaPendiente | null>(null);
  const [ventaError, setVentaError] = useState('');
  const [isStartingSale, setIsStartingSale] = useState(false);
  const [draftPaymentMethod, setDraftPaymentMethod] = useState<'EFECTIVO' | 'TARJETA'>('EFECTIVO');
  const [draftItems, setDraftItems] = useState<ProductItem[]>([]);
  const [loadedCartKey, setLoadedCartKey] = useState<string | null>(null);
  const [pendingMutations, setPendingMutations] = useState(0);
  const [saleNeedsRefresh, setSaleNeedsRefresh] = useState(false);

  const saleRef = useRef<VentaPendiente | null>(null);
  const saleNeedsRefreshRef = useRef(false);
  const mutationQueue = useRef<Promise<void>>(Promise.resolve());

  // Sincronizar saleRef con venta
  useEffect(() => {
    saleRef.current = venta;
  }, [venta]);

  // Inicializar leyendo de localStorage
  useEffect(() => {
    let items: ProductItem[] = [];
    try {
      const saved = cartStorageKey ? localStorage.getItem(cartStorageKey) : null;
      if (saved) {
        const parsed = JSON.parse(saved);
        if (Array.isArray(parsed)) items = parsed;
      }
    } catch {
      /* Un borrador inválido se descarta. */
    }
    setDraftItems(items);
    setLoadedCartKey(cartStorageKey);
  }, [cartStorageKey]);

  useEffect(() => {
    if (cartStorageKey && loadedCartKey === cartStorageKey) {
      localStorage.setItem(cartStorageKey, JSON.stringify(draftItems));
    }
  }, [draftItems, cartStorageKey, loadedCartKey]);

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
    setDraftItems((prev) => prev.map((item) =>
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
    setDraftItems((prev) => prev.filter((item) => item.id !== id));
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
      onOpenPayment();
      return;
    }
    if (draftItems.length === 0 || isStartingSale || turnoStatus !== 'ready' || !turnoId) return;

    setVentaError('');
    setIsStartingSale(true);
    try {
      const pendienteExistente = await fetchLatestPendingSale(turnoId);
      if (pendienteExistente) {
        saleRef.current = pendienteExistente;
        setVenta(pendienteExistente);
        setDraftItems([]);
        return;
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
          const pendienteRecuperada = await fetchLatestPendingSale(turnoId);
          if (pendienteRecuperada) {
            saleRef.current = pendienteRecuperada;
            setVenta(pendienteRecuperada);
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
        acc.totalItems += 1;
        return acc;
      },
      { subtotal: 0, totalItems: 0 }
    );
  }, [cartItems]);

  const subtotal = venta ? venta.subtotal : draftSubtotal;
  const iva = venta?.impuestos;
  const total = venta?.total;
  const isCartEmpty = cartItems.length === 0;

  return {
    venta,
    setVenta,
    saleRef,
    draftItems,
    setDraftItems,
    cartItems,
    isCartEmpty,
    totalItems,
    draftSubtotal,
    subtotal,
    iva,
    total,
    isStartingSale,
    pendingMutations,
    saleNeedsRefresh,
    ventaError,
    setVentaError,
    draftPaymentMethod,
    setDraftPaymentMethod,
    queueSaleMutation,
    refreshSale,
    retrySaleRefresh,
    handleQuantityChange,
    handleRemoveItem,
    handleCancelSale,
    handleCheckout,
  };
}
