import React, { useRef, useState } from 'react';
import axios from 'axios';
import { buildServiceUrl } from '../../config/api';

interface Opcion { id: string; nombre: string }

interface Props {
  productos: Opcion[];
  sucursales: Opcion[];
  sucursalId: string;
  onSucursalChange: (id: string) => void;
  token: string;
  ajustePendiente: boolean;
  onPendiente: (pendiente: boolean) => void;
  onRegistrada: () => Promise<void>;
}

function mensajeRecepcion(error: unknown): string {
  if (!axios.isAxiosError(error)) return 'No se pudo confirmar el registro de la recepción.';
  if (!error.response) return 'No se pudo conectar con MS-4. Verifica el stock antes de volver a enviar.';
  const detalle = error.response.data?.message;
  const mensaje = typeof detalle === 'string' ? detalle : undefined;
  switch (error.response.status) {
    case 400: return mensaje ?? 'Revisa los datos de la recepción.';
    case 401: return 'La sesión no es válida. Vuelve a iniciar sesión.';
    case 403: return 'No tienes permisos para registrar recepciones.';
    case 404: return mensaje ?? 'El producto o la sucursal no está disponible, o la ruta de recepción no está publicada.';
    case 503: return mensaje ?? 'MS-4 no pudo validar el producto o la sucursal. Intenta más tarde.';
    default: return 'No se pudo confirmar el registro de la recepción.';
  }
}

export default function RecepcionMercaderiaForm({
  productos, sucursales, sucursalId, onSucursalChange, token, ajustePendiente, onPendiente, onRegistrada,
}: Props) {
  const [productoId, setProductoId] = useState('');
  const [cantidad, setCantidad] = useState('');
  const [vencimiento, setVencimiento] = useState('');
  const [ubicacion, setUbicacion] = useState('');
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState('');
  const [confirmacion, setConfirmacion] = useState('');
  const enCurso = useRef(false);
  const hoyUtc = new Date().toISOString().slice(0, 10);

  async function registrar(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (enCurso.current || ajustePendiente) return;
    setError('');
    setConfirmacion('');

    const unidades = Number(cantidad);
    if (!productos.some(p => p.id === productoId) || !sucursales.some(s => s.id === sucursalId)) {
      setError('Selecciona un producto y una sucursal activos.');
      return;
    }
    if (!Number.isFinite(unidades) || unidades <= 0 || unidades > 99_999_999 ||
        Math.round(unidades * 10_000) / 10_000 !== unidades) {
      setError('Ingresa una cantidad positiva de hasta 99.999.999 y cuatro decimales.');
      return;
    }
    if (!vencimiento || vencimiento < new Date().toISOString().slice(0, 10) || ubicacion.trim().length > 100) {
      setError('Ingresa un vencimiento desde hoy (UTC) y una ubicación de hasta 100 caracteres.');
      return;
    }

    enCurso.current = true;
    setGuardando(true);
    onPendiente(true);
    try {
      try {
        const respuesta = await axios.post(buildServiceUrl('inventory', '/recepciones'), {
          productoId, sucursalId, cantidad: unidades, fechaVencimiento: vencimiento,
          ubicacion: ubicacion.trim() || null,
        }, { headers: { Authorization: `Bearer ${token}` } });
        if (respuesta.status !== 201) throw new Error('Respuesta de recepción inesperada');
      } catch (err) {
        setError(mensajeRecepcion(err));
        return;
      }

      setConfirmacion('Recepción registrada.');
      setCantidad('');
      setVencimiento('');
      setUbicacion('');
      await onRegistrada();
    } finally {
      enCurso.current = false;
      setGuardando(false);
      onPendiente(false);
    }
  }

  return <form aria-labelledby="recepcion-titulo" onSubmit={registrar}
    style={{ maxWidth: 520, marginTop: '1rem', background: 'var(--color-surface)', padding: '1rem', border: '1px solid var(--color-line)', borderRadius: 8 }}>
    <h3 id="recepcion-titulo">Recepción de mercadería</h3>
    {error && <p role="alert">{error}</p>}
    {confirmacion && <p role="status">{confirmacion}</p>}
    <fieldset disabled={guardando || ajustePendiente} style={{ display: 'grid', gap: '0.75rem', border: 0, padding: 0, margin: 0 }}>
      <label>Artículo a recibir <select value={productoId} onChange={e => setProductoId(e.target.value)} required>
        <option value="">Selecciona un producto</option>
        {productos.map(p => <option key={p.id} value={p.id}>{p.nombre}</option>)}
      </select></label>
      <label>Sucursal <select value={sucursalId} onChange={e => onSucursalChange(e.target.value)} required>
        <option value="">Selecciona una sucursal</option>
        {sucursales.map(s => <option key={s.id} value={s.id}>{s.nombre}</option>)}
      </select></label>
      <label>Cantidad recibida <input type="number" min="0.0001" max="99999999" step="0.0001" required
        value={cantidad} onChange={e => setCantidad(e.target.value)} /></label>
      <label>Fecha de vencimiento <input type="date" min={hoyUtc} required
        value={vencimiento} onChange={e => setVencimiento(e.target.value)} /></label>
      <label>Ubicación (opcional) <input maxLength={100} value={ubicacion} onChange={e => setUbicacion(e.target.value)} /></label>
      <button type="submit" disabled={!productos.length || !sucursales.length}>
        {guardando ? 'Registrando…' : 'Registrar recepción'}
      </button>
    </fieldset>
  </form>;
}
