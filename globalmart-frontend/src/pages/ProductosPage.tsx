import React, { useState, useEffect, useCallback, useMemo } from 'react';
import axios, { AxiosError } from 'axios';
import { useAuth } from '../hooks/useAuth';

// Puerto del CatalogPricingService (MS-3)
const CATALOG_URL = 'https://catalog-rpedraza.dev.censei.cl';

// UomBaseId requerido por el DTO (uuid genérico para "unidad")
const DEFAULT_UOM_ID = '00000000-0000-0000-0000-000000000001';

// ── Tipos ────────────────────────────────────────────────────────────────────

interface Producto {
  id: string;
  nombre: string;
  descripcion?: string;
  codigoBarras: string;
  categoriaId?: string;
  precioBase: number;
  esPesoVariable: boolean;
  isActive: boolean;
  createdAt: string;
}

interface Categoria {
  id: string;
  nombre: string;
}

interface FormState {
  nombre: string;
  descripcion: string;
  codigoBarras: string;
  categoriaId: string;
  precioBase: string;
  esPesoVariable: boolean;
  isActive: boolean;
}

const FORM_VACÍO: FormState = {
  nombre: '',
  descripcion: '',
  codigoBarras: '',
  categoriaId: '',
  precioBase: '',
  esPesoVariable: false,
  isActive: true,
};

// ── Estilos reutilizables ─────────────────────────────────────────────────────

const inputStyle: React.CSSProperties = {
  width: '100%',
  padding: '0.6rem 0.85rem',
  border: '1px solid #cbd5e1',
  borderRadius: '8px',
  fontSize: '0.95rem',
  outline: 'none',
  boxSizing: 'border-box',
  color: '#0f172a',
  backgroundColor: '#f8fafc',
};

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontSize: '0.82rem',
  fontWeight: 600,
  color: '#475569',
  marginBottom: '0.35rem',
  textTransform: 'uppercase',
  letterSpacing: '0.04em',
};

// ── Componente Modal ──────────────────────────────────────────────────────────

interface ModalProps {
  modo: 'crear' | 'editar';
  form: FormState;
  categorias: Categoria[];
  guardando: boolean;
  errorModal: string;
  onChange: (field: keyof FormState, value: string | boolean) => void;
  onGuardar: () => void;
  onCerrar: () => void;
}

function ProductoModal({
  modo, form, categorias, guardando, errorModal, onChange, onGuardar, onCerrar,
}: ModalProps) {
  return (
    <div
      style={{
        position: 'fixed', inset: 0, zIndex: 1000,
        background: 'rgba(15, 23, 42, 0.55)',
        backdropFilter: 'blur(4px)',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        padding: '1rem',
      }}
      onClick={(e) => { if (e.target === e.currentTarget) onCerrar(); }}
    >
      <div
        style={{
          background: '#ffffff',
          borderRadius: '18px',
          width: '100%',
          maxWidth: '520px',
          boxShadow: '0 25px 60px -10px rgba(0,0,0,0.35)',
          overflow: 'hidden',
          animation: 'modalIn 0.18s ease-out',
        }}
      >
        {/* Cabecera del modal */}
        <div
          style={{
            background: 'linear-gradient(135deg, #0f172a 0%, #1e3a5f 60%, #0369a1 100%)',
            padding: '1.5rem 1.75rem',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <span style={{ fontSize: '1.6rem' }}>{modo === 'crear' ? '➕' : '✏️'}</span>
            <div>
              <h2 style={{ margin: 0, color: 'white', fontSize: '1.2rem', fontWeight: 800 }}>
                {modo === 'crear' ? 'Nuevo Producto' : 'Editar Producto'}
              </h2>
              <p style={{ margin: 0, color: 'rgba(255,255,255,0.7)', fontSize: '0.8rem' }}>
                MS-3 Catalog &amp; Pricing
              </p>
            </div>
          </div>
          <button
            id="btn-cerrar-modal-producto"
            type="button"
            onClick={onCerrar}
            style={{
              background: 'rgba(255,255,255,0.15)',
              border: 'none',
              borderRadius: '8px',
              color: 'white',
              fontSize: '1.1rem',
              width: '32px', height: '32px',
              cursor: 'pointer',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
            }}
            title="Cerrar"
          >
            ✕
          </button>
        </div>

        {/* Cuerpo del formulario */}
        <div style={{ padding: '1.75rem', display: 'flex', flexDirection: 'column', gap: '1.1rem' }}>

          {errorModal && (
            <div style={{
              background: '#fef2f2', border: '1px solid #fca5a5',
              borderRadius: '8px', padding: '0.75rem 1rem',
              color: '#b91c1c', fontSize: '0.875rem',
              display: 'flex', gap: '0.5rem', alignItems: 'flex-start',
            }}>
              <span>⚠️</span><span>{errorModal}</span>
            </div>
          )}

          {/* Nombre */}
          <div>
            <label style={labelStyle} htmlFor="modal-nombre">Nombre *</label>
            <input
              id="modal-nombre"
              type="text"
              placeholder="Ej: Leche entera 1L"
              value={form.nombre}
              onChange={(e) => onChange('nombre', e.target.value)}
              style={inputStyle}
              disabled={guardando}
            />
          </div>

          {/* Descripción */}
          <div>
            <label style={labelStyle} htmlFor="modal-descripcion">Descripción</label>
            <input
              id="modal-descripcion"
              type="text"
              placeholder="Descripción opcional"
              value={form.descripcion}
              onChange={(e) => onChange('descripcion', e.target.value)}
              style={inputStyle}
              disabled={guardando}
            />
          </div>

          {/* Precio Base + Código Barras */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div>
              <label style={labelStyle} htmlFor="modal-precio">Precio Base (CLP) *</label>
              <input
                id="modal-precio"
                type="number"
                min="0"
                step="1"
                placeholder="Ej: 1200"
                value={form.precioBase}
                onChange={(e) => onChange('precioBase', e.target.value)}
                style={inputStyle}
                disabled={guardando}
              />
            </div>
            <div>
              <label style={labelStyle} htmlFor="modal-barras">Código de Barras</label>
              <input
                id="modal-barras"
                type="text"
                placeholder="Ej: 7801234567890"
                value={form.codigoBarras}
                onChange={(e) => onChange('codigoBarras', e.target.value)}
                style={inputStyle}
                disabled={guardando}
              />
            </div>
          </div>

          {/* Categoría */}
          <div>
            <label style={labelStyle} htmlFor="modal-categoria">Categoría</label>
            <select
              id="modal-categoria"
              value={form.categoriaId}
              onChange={(e) => onChange('categoriaId', e.target.value)}
              style={{ ...inputStyle, cursor: 'pointer' }}
              disabled={guardando}
            >
              <option value="">Sin categoría</option>
              {categorias.map((c) => (
                <option key={c.id} value={c.id}>{c.nombre}</option>
              ))}
            </select>
          </div>

          {/* Toggles */}
          <div style={{ display: 'flex', gap: '2rem', flexWrap: 'wrap' }}>
            <label
              htmlFor="modal-peso-variable"
              style={{
                display: 'flex', alignItems: 'center', gap: '0.5rem',
                cursor: 'pointer', userSelect: 'none',
                fontSize: '0.9rem', color: '#374151', fontWeight: 500,
              }}
            >
              <input
                id="modal-peso-variable"
                type="checkbox"
                checked={form.esPesoVariable}
                onChange={(e) => onChange('esPesoVariable', e.target.checked)}
                disabled={guardando}
                style={{ width: '16px', height: '16px', accentColor: '#0284c7' }}
              />
              ⚖️ Peso variable
            </label>

            <label
              htmlFor="modal-activo"
              style={{
                display: 'flex', alignItems: 'center', gap: '0.5rem',
                cursor: 'pointer', userSelect: 'none',
                fontSize: '0.9rem', color: '#374151', fontWeight: 500,
              }}
            >
              <input
                id="modal-activo"
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => onChange('isActive', e.target.checked)}
                disabled={guardando}
                style={{ width: '16px', height: '16px', accentColor: '#16a34a' }}
              />
              ✅ Activo
            </label>
          </div>
        </div>

        {/* Pie del modal */}
        <div
          style={{
            padding: '1rem 1.75rem',
            borderTop: '1px solid #e2e8f0',
            background: '#f8fafc',
            display: 'flex',
            justifyContent: 'flex-end',
            gap: '0.75rem',
          }}
        >
          <button
            id="btn-cancelar-modal-producto"
            type="button"
            onClick={onCerrar}
            disabled={guardando}
            style={{
              padding: '0.6rem 1.25rem',
              background: 'transparent',
              border: '1px solid #cbd5e1',
              borderRadius: '8px',
              fontWeight: 600,
              fontSize: '0.9rem',
              cursor: 'pointer',
              color: '#475569',
            }}
          >
            Cancelar
          </button>
          <button
            id="btn-guardar-modal-producto"
            type="button"
            onClick={onGuardar}
            disabled={guardando}
            style={{
              padding: '0.6rem 1.5rem',
              background: guardando ? '#93c5fd' : '#0284c7',
              color: 'white',
              border: 'none',
              borderRadius: '8px',
              fontWeight: 700,
              fontSize: '0.9rem',
              cursor: guardando ? 'not-allowed' : 'pointer',
              transition: 'background 0.15s',
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem',
            }}
          >
            {guardando ? '⏳ Guardando…' : modo === 'crear' ? '➕ Crear Producto' : '💾 Guardar Cambios'}
          </button>
        </div>
      </div>

      <style>{`
        @keyframes modalIn {
          from { opacity: 0; transform: scale(0.95) translateY(-8px); }
          to   { opacity: 1; transform: scale(1)   translateY(0); }
        }
      `}</style>
    </div>
  );
}

// ── Componente Principal ──────────────────────────────────────────────────────

export default function ProductosPage() {
  const { token, loading: authLoading } = useAuth();

  const [productos, setProductos] = useState<Producto[]>([]);
  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState('');

  // Filtros
  const [query, setQuery] = useState('');
  const [categoriaFiltro, setCategoriaFiltro] = useState('');
  const [soloActivos, setSoloActivos] = useState(false);
  const [reloadTick, setReloadTick] = useState(0);

  // Modal
  const [modalAbierto, setModalAbierto] = useState(false);
  const [modalModo, setModalModo] = useState<'crear' | 'editar'>('crear');
  const [editandoId, setEditandoId] = useState<string | null>(null);
  const [form, setForm] = useState<FormState>(FORM_VACÍO);
  const [guardando, setGuardando] = useState(false);
  const [errorModal, setErrorModal] = useState('');

  // ── Fetch unificado ──────────────────────────────────────────────────────

  useEffect(() => {
    if (authLoading) return;
    let isMounted = true;

    async function load() {
      if (!token) { setLoading(false); return; }
      setLoading(true);
      setErrorMsg('');
      const headers = { Authorization: `Bearer ${token}` };
      try {
        const [prodRes, catRes] = await Promise.all([
          axios.get(`${CATALOG_URL}/products`, { params: { page: 1, pageSize: 500 }, headers }),
          axios.get(`${CATALOG_URL}/categories`, { headers }),
        ]);
        if (!isMounted) return;
        setProductos(prodRes.data?.data ?? prodRes.data ?? []);
        setCategorias(Array.isArray(catRes.data) ? catRes.data : catRes.data?.data ?? []);
      } catch (err) {
        if (!isMounted) return;
        const axErr = err as AxiosError<{ message?: string }>;
        setErrorMsg(
          axErr.response?.data?.message ??
            'No se pudo conectar con el servicio de catálogo (MS-3). Verifica que esté levantado en el puerto 5203.'
        );
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => { isMounted = false; };
  }, [token, authLoading, reloadTick]);

  const fetchDatos = useCallback(() => setReloadTick(t => t + 1), []);

  // ── Mapa categoría id → nombre ───────────────────────────────────────────

  const catMap = useMemo(() => {
    const m: Record<string, string> = {};
    categorias.forEach((c) => { m[c.id] = c.nombre; });
    return m;
  }, [categorias]);

  // ── Filtrado instantáneo ─────────────────────────────────────────────────

  const productosFiltrados = useMemo(() => {
    const q = query.trim().toLowerCase();
    return productos.filter((p) => {
      if (soloActivos && !p.isActive) return false;
      if (categoriaFiltro && p.categoriaId !== categoriaFiltro) return false;
      if (!q) return true;
      return (
        p.nombre.toLowerCase().includes(q) ||
        (p.codigoBarras ?? '').toLowerCase().includes(q) ||
        (p.descripcion ?? '').toLowerCase().includes(q) ||
        (p.categoriaId ? (catMap[p.categoriaId] ?? '').toLowerCase().includes(q) : false)
      );
    });
  }, [productos, query, categoriaFiltro, soloActivos, catMap]);

  // ── Handlers del Modal ───────────────────────────────────────────────────

  const abrirCrear = () => {
    setForm(FORM_VACÍO);
    setEditandoId(null);
    setModalModo('crear');
    setErrorModal('');
    setModalAbierto(true);
  };

  const abrirEditar = (prod: Producto) => {
    setForm({
      nombre: prod.nombre,
      descripcion: prod.descripcion ?? '',
      codigoBarras: prod.codigoBarras ?? '',
      categoriaId: prod.categoriaId ?? '',
      precioBase: String(prod.precioBase),
      esPesoVariable: prod.esPesoVariable,
      isActive: prod.isActive,
    });
    setEditandoId(prod.id);
    setModalModo('editar');
    setErrorModal('');
    setModalAbierto(true);
  };

  const cerrarModal = () => {
    if (guardando) return;
    setModalAbierto(false);
    setErrorModal('');
  };

  const handleFormChange = (field: keyof FormState, value: string | boolean) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const handleGuardar = async () => {
    if (!form.nombre.trim()) { setErrorModal('El nombre es obligatorio.'); return; }
    const precio = parseFloat(form.precioBase);
    if (isNaN(precio) || precio < 0) { setErrorModal('El precio debe ser un número válido mayor o igual a 0.'); return; }

    setGuardando(true);
    setErrorModal('');
    const headers = { Authorization: `Bearer ${token}` };

    const payload = {
      nombre: form.nombre.trim(),
      descripcion: form.descripcion.trim() || null,
      codigoBarras: form.codigoBarras.trim() || null,
      categoriaId: form.categoriaId || null,
      precioBase: precio,
      esPesoVariable: form.esPesoVariable,
      isActive: form.isActive,
      uomBaseId: DEFAULT_UOM_ID,
    };

    try {
      if (modalModo === 'crear') {
        await axios.post(`${CATALOG_URL}/products`, payload, { headers });
      } else {
        await axios.put(`${CATALOG_URL}/products/${editandoId}`, payload, { headers });
      }
      setModalAbierto(false);
      fetchDatos(); // recargar lista
    } catch (err) {
      const axErr = err as AxiosError<{ message?: string; errors?: Record<string, string[]> }>;
      const msg =
        axErr.response?.data?.message ??
        Object.values(axErr.response?.data?.errors ?? {}).flat().join(', ') ??
        'Error al guardar el producto.';
      setErrorModal(msg);
    } finally {
      setGuardando(false);
    }
  };

  // ── Render ───────────────────────────────────────────────────────────────

  return (
    <div style={{ maxWidth: '1150px', margin: '0 auto' }}>

      {/* ── Cabecera ── */}
      <div
        style={{
          background: 'linear-gradient(135deg, #0f172a 0%, #1e3a5f 60%, #0369a1 100%)',
          borderRadius: '16px',
          padding: '2rem 2.5rem',
          marginBottom: '1.75rem',
          color: 'white',
          boxShadow: '0 10px 25px -5px rgba(3, 105, 161, 0.35)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: '1rem',
        }}
      >
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.35rem' }}>
            <span style={{ fontSize: '2rem' }}>📦</span>
            <h1 style={{ margin: 0, fontSize: '1.75rem', fontWeight: 800, letterSpacing: '-0.025em' }}>
              Catálogo de Productos
            </h1>
          </div>
          <p style={{ margin: 0, opacity: 0.85, fontSize: '0.95rem' }}>
            Administración del catálogo del tenant — MS-3 Catalog &amp; Pricing
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', flexWrap: 'wrap' }}>
          {/* Botón Nuevo Producto */}
          <button
            id="btn-nuevo-producto"
            type="button"
            onClick={abrirCrear}
            style={{
              padding: '0.7rem 1.35rem',
              background: 'linear-gradient(135deg, #16a34a, #15803d)',
              color: 'white',
              border: 'none',
              borderRadius: '10px',
              fontWeight: 700,
              fontSize: '0.95rem',
              cursor: 'pointer',
              boxShadow: '0 4px 12px rgba(22,163,74,0.4)',
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem',
              whiteSpace: 'nowrap',
              transition: 'transform 0.1s, box-shadow 0.1s',
            }}
            onMouseEnter={e => { (e.currentTarget.style.transform = 'translateY(-1px)'); (e.currentTarget.style.boxShadow = '0 6px 16px rgba(22,163,74,0.5)'); }}
            onMouseLeave={e => { (e.currentTarget.style.transform = 'translateY(0)'); (e.currentTarget.style.boxShadow = '0 4px 12px rgba(22,163,74,0.4)'); }}
          >
            ➕ Nuevo Producto
          </button>

          {/* Contador */}
          <div
            style={{
              background: 'rgba(255,255,255,0.12)',
              backdropFilter: 'blur(8px)',
              border: '1px solid rgba(255,255,255,0.22)',
              borderRadius: '12px',
              padding: '0.85rem 1.5rem',
              textAlign: 'center',
            }}
          >
            <div style={{ fontSize: '2rem', fontWeight: 800, lineHeight: 1 }}>
              {loading ? '…' : productosFiltrados.length}
            </div>
            <div style={{ fontSize: '0.8rem', opacity: 0.85, marginTop: '0.15rem' }}>
              {query || categoriaFiltro || soloActivos ? 'filtrados' : 'productos totales'}
            </div>
          </div>
        </div>
      </div>

      {/* ── Panel de filtros ── */}
      <div
        style={{
          background: '#ffffff',
          border: '1px solid #e2e8f0',
          borderRadius: '14px',
          padding: '1.25rem 1.5rem',
          marginBottom: '1.5rem',
          boxShadow: '0 2px 8px rgba(0,0,0,0.05)',
          display: 'flex',
          gap: '1rem',
          flexWrap: 'wrap',
          alignItems: 'center',
        }}
      >
        {/* Búsqueda de texto */}
        <div style={{ flex: '1 1 280px', position: 'relative' }}>
          <span
            style={{
              position: 'absolute', left: '0.85rem', top: '50%',
              transform: 'translateY(-50%)', fontSize: '1.1rem', pointerEvents: 'none',
            }}
          >
            🔍
          </span>
          <input
            id="input-busqueda-productos"
            type="text"
            placeholder="Buscar por nombre, código de barras, descripción o categoría…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            style={{
              width: '100%',
              padding: '0.65rem 0.9rem 0.65rem 2.4rem',
              border: `1px solid ${query ? '#38bdf8' : '#cbd5e1'}`,
              borderRadius: '10px',
              fontSize: '0.95rem',
              outline: 'none',
              boxSizing: 'border-box',
              color: '#0f172a',
              backgroundColor: query ? '#f0f9ff' : '#f8fafc',
            }}
          />
          {query && (
            <button
              type="button"
              onClick={() => setQuery('')}
              style={{
                position: 'absolute', right: '0.6rem', top: '50%',
                transform: 'translateY(-50%)', background: 'none',
                border: 'none', cursor: 'pointer', fontSize: '1rem',
                color: '#94a3b8', padding: '0.2rem',
              }}
              title="Limpiar búsqueda"
            >
              ✕
            </button>
          )}
        </div>

        {/* Filtro por categoría */}
        <select
          id="select-categoria-filtro"
          value={categoriaFiltro}
          onChange={(e) => setCategoriaFiltro(e.target.value)}
          style={{
            flex: '0 1 200px',
            padding: '0.65rem 0.9rem',
            border: `1px solid ${categoriaFiltro ? '#38bdf8' : '#cbd5e1'}`,
            borderRadius: '10px',
            fontSize: '0.9rem',
            backgroundColor: categoriaFiltro ? '#f0f9ff' : '#f8fafc',
            color: '#0f172a',
            cursor: 'pointer',
            outline: 'none',
          }}
        >
          <option value="">Todas las categorías</option>
          {categorias.map((c) => (
            <option key={c.id} value={c.id}>{c.nombre}</option>
          ))}
        </select>

        {/* Toggle solo activos */}
        <label
          htmlFor="toggle-solo-activos"
          style={{
            display: 'flex', alignItems: 'center', gap: '0.5rem',
            cursor: 'pointer', userSelect: 'none',
            fontSize: '0.9rem', color: '#475569', fontWeight: 500, whiteSpace: 'nowrap',
          }}
        >
          <input
            id="toggle-solo-activos"
            type="checkbox"
            checked={soloActivos}
            onChange={(e) => setSoloActivos(e.target.checked)}
            style={{ width: '16px', height: '16px', accentColor: '#0284c7', cursor: 'pointer' }}
          />
          Solo activos
        </label>

        {/* Botón recargar */}
        <button
          type="button"
          onClick={fetchDatos}
          disabled={loading}
          style={{
            padding: '0.65rem 1.2rem',
            background: loading ? '#e2e8f0' : '#0284c7',
            color: loading ? '#94a3b8' : 'white',
            border: 'none', borderRadius: '10px',
            fontWeight: 600, fontSize: '0.9rem',
            cursor: loading ? 'not-allowed' : 'pointer',
            transition: 'background 0.15s', whiteSpace: 'nowrap',
          }}
          title="Recargar desde el servidor"
        >
          {loading ? '⏳ Cargando…' : '🔄 Recargar'}
        </button>
      </div>

      {/* ── Error ── */}
      {errorMsg && (
        <div
          style={{
            background: '#fef2f2', border: '1px solid #fca5a5',
            borderRadius: '12px', padding: '1rem 1.25rem',
            color: '#b91c1c', marginBottom: '1.5rem',
            display: 'flex', alignItems: 'center', gap: '0.75rem', fontSize: '0.925rem',
          }}
        >
          <span style={{ fontSize: '1.4rem' }}>⚠️</span>
          <span>{errorMsg}</span>
        </div>
      )}

      {/* ── Tabla de productos ── */}
      <div
        style={{
          background: '#ffffff',
          border: '1px solid #e2e8f0',
          borderRadius: '14px',
          overflow: 'hidden',
          boxShadow: '0 4px 12px rgba(0,0,0,0.06)',
        }}
      >
        {loading ? (
          <div style={{ padding: '3rem', textAlign: 'center', color: '#94a3b8' }}>
            <div style={{ fontSize: '2.5rem', marginBottom: '0.75rem' }}>⏳</div>
            <div style={{ fontWeight: 600, fontSize: '1rem' }}>Cargando catálogo de productos…</div>
            <div style={{ fontSize: '0.85rem', marginTop: '0.35rem' }}>
              Conectando con MS-3 en el puerto 5203
            </div>
          </div>
        ) : productosFiltrados.length === 0 ? (
          <div style={{ padding: '3rem', textAlign: 'center', color: '#94a3b8' }}>
            <div style={{ fontSize: '2.5rem', marginBottom: '0.75rem' }}>🔍</div>
            <div style={{ fontWeight: 600, fontSize: '1rem', color: '#475569' }}>
              {query || categoriaFiltro || soloActivos
                ? 'Ningún producto coincide con los filtros aplicados'
                : 'No hay productos registrados para este tenant'}
            </div>
            {(query || categoriaFiltro || soloActivos) && (
              <button
                type="button"
                onClick={() => { setQuery(''); setCategoriaFiltro(''); setSoloActivos(false); }}
                style={{
                  marginTop: '1rem', padding: '0.5rem 1.2rem',
                  background: '#0284c7', color: 'white',
                  border: 'none', borderRadius: '8px',
                  fontWeight: 600, cursor: 'pointer', fontSize: '0.9rem',
                }}
              >
                Limpiar filtros
              </button>
            )}
            <div style={{ marginTop: '1.5rem' }}>
              <button
                id="btn-nuevo-producto-empty"
                type="button"
                onClick={abrirCrear}
                style={{
                  padding: '0.65rem 1.35rem',
                  background: 'linear-gradient(135deg, #16a34a, #15803d)',
                  color: 'white', border: 'none', borderRadius: '10px',
                  fontWeight: 700, fontSize: '0.9rem', cursor: 'pointer',
                }}
              >
                ➕ Crear primer producto
              </button>
            </div>
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
              <thead>
                <tr
                  style={{
                    background: 'linear-gradient(90deg, #0f172a 0%, #1e3a5f 100%)',
                    color: 'white',
                  }}
                >
                  {['Nombre', 'Código de Barras', 'Categoría', 'Precio Base', 'Tipo', 'Estado', 'Acciones'].map(
                    (header) => (
                      <th
                        key={header}
                        style={{
                          padding: '0.9rem 1.25rem',
                          fontSize: '0.78rem',
                          fontWeight: 700,
                          textTransform: 'uppercase',
                          letterSpacing: '0.05em',
                          whiteSpace: 'nowrap',
                        }}
                      >
                        {header}
                      </th>
                    )
                  )}
                </tr>
              </thead>
              <tbody>
                {productosFiltrados.map((prod, index) => {
                  const isEven = index % 2 === 0;
                  const categoriaNombre = prod.categoriaId ? catMap[prod.categoriaId] ?? '—' : '—';

                  return (
                    <tr
                      key={prod.id}
                      style={{
                        backgroundColor: isEven ? '#ffffff' : '#f8fafc',
                        borderBottom: '1px solid #f1f5f9',
                        transition: 'background-color 0.12s',
                      }}
                      onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#e0f2fe')}
                      onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = isEven ? '#ffffff' : '#f8fafc')}
                    >
                      {/* Nombre */}
                      <td style={{ padding: '0.85rem 1.25rem', maxWidth: '260px' }}>
                        <div
                          style={{
                            fontWeight: 700, color: '#0f172a', fontSize: '0.95rem',
                            whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis',
                          }}
                          title={prod.nombre}
                        >
                          {prod.nombre}
                        </div>
                        {prod.descripcion && (
                          <div
                            style={{
                              fontSize: '0.78rem', color: '#64748b', marginTop: '0.15rem',
                              whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', maxWidth: '240px',
                            }}
                            title={prod.descripcion}
                          >
                            {prod.descripcion}
                          </div>
                        )}
                      </td>

                      {/* Código de barras */}
                      <td style={{ padding: '0.85rem 1.25rem' }}>
                        <code
                          style={{
                            fontSize: '0.85rem', background: '#f1f5f9',
                            padding: '0.2rem 0.5rem', borderRadius: '6px',
                            color: '#334155', fontFamily: 'monospace', letterSpacing: '0.05em',
                          }}
                        >
                          {prod.codigoBarras || '—'}
                        </code>
                      </td>

                      {/* Categoría */}
                      <td style={{ padding: '0.85rem 1.25rem' }}>
                        {prod.categoriaId ? (
                          <span
                            style={{
                              background: '#dbeafe', color: '#1d4ed8',
                              padding: '0.22rem 0.65rem', borderRadius: '9999px',
                              fontSize: '0.78rem', fontWeight: 600, whiteSpace: 'nowrap',
                            }}
                          >
                            {categoriaNombre}
                          </span>
                        ) : (
                          <span style={{ color: '#94a3b8', fontSize: '0.85rem' }}>Sin categoría</span>
                        )}
                      </td>

                      {/* Precio base */}
                      <td style={{ padding: '0.85rem 1.25rem', whiteSpace: 'nowrap' }}>
                        <span style={{ fontWeight: 800, fontSize: '1rem', color: '#0369a1' }}>
                          ${Number(prod.precioBase).toLocaleString('es-CL', {
                            minimumFractionDigits: 0,
                            maximumFractionDigits: 0,
                          })}
                        </span>
                      </td>

                      {/* Tipo */}
                      <td style={{ padding: '0.85rem 1.25rem' }}>
                        <span
                          style={{
                            background: prod.esPesoVariable ? '#fef3c7' : '#f0fdf4',
                            color: prod.esPesoVariable ? '#b45309' : '#15803d',
                            padding: '0.22rem 0.65rem', borderRadius: '9999px',
                            fontSize: '0.78rem', fontWeight: 600, whiteSpace: 'nowrap',
                          }}
                        >
                          {prod.esPesoVariable ? '⚖️ Peso variable' : '📦 Unidad'}
                        </span>
                      </td>

                      {/* Estado */}
                      <td style={{ padding: '0.85rem 1.25rem' }}>
                        <span
                          style={{
                            display: 'inline-flex', alignItems: 'center', gap: '0.3rem',
                            background: prod.isActive ? '#dcfce7' : '#fee2e2',
                            color: prod.isActive ? '#15803d' : '#b91c1c',
                            padding: '0.22rem 0.65rem', borderRadius: '9999px',
                            fontSize: '0.78rem', fontWeight: 700,
                          }}
                        >
                          <span
                            style={{
                              width: '7px', height: '7px', borderRadius: '50%',
                              background: prod.isActive ? '#16a34a' : '#dc2626',
                              display: 'inline-block',
                            }}
                          />
                          {prod.isActive ? 'Activo' : 'Inactivo'}
                        </span>
                      </td>

                      {/* Acciones */}
                      <td style={{ padding: '0.85rem 1.25rem' }}>
                        <button
                          id={`btn-editar-producto-${prod.id}`}
                          type="button"
                          onClick={() => abrirEditar(prod)}
                          style={{
                            padding: '0.35rem 0.85rem',
                            background: '#f0f9ff',
                            color: '#0284c7',
                            border: '1px solid #bae6fd',
                            borderRadius: '7px',
                            fontWeight: 600,
                            fontSize: '0.82rem',
                            cursor: 'pointer',
                            whiteSpace: 'nowrap',
                            transition: 'background 0.12s, border-color 0.12s',
                          }}
                          onMouseEnter={e => {
                            (e.currentTarget.style.background = '#0284c7');
                            (e.currentTarget.style.color = 'white');
                          }}
                          onMouseLeave={e => {
                            (e.currentTarget.style.background = '#f0f9ff');
                            (e.currentTarget.style.color = '#0284c7');
                          }}
                          title={`Editar ${prod.nombre}`}
                        >
                          ✏️ Editar
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {/* Footer */}
        {!loading && productosFiltrados.length > 0 && (
          <div
            style={{
              padding: '0.85rem 1.5rem',
              borderTop: '1px solid #e2e8f0',
              background: '#f8fafc',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              flexWrap: 'wrap',
              gap: '0.5rem',
              fontSize: '0.85rem',
              color: '#64748b',
            }}
          >
            <span>
              Mostrando <strong style={{ color: '#0f172a' }}>{productosFiltrados.length}</strong> de{' '}
              <strong style={{ color: '#0f172a' }}>{productos.length}</strong> productos
              {(query || categoriaFiltro || soloActivos) && (
                <span style={{ color: '#0284c7', marginLeft: '0.5rem' }}>(filtrado)</span>
              )}
            </span>
            <span style={{ fontSize: '0.78rem' }}>MS-3 Catalog &amp; Pricing · Puerto 5203</span>
          </div>
        )}
      </div>

      {/* ── Modal ── */}
      {modalAbierto && (
        <ProductoModal
          modo={modalModo}
          form={form}
          categorias={categorias}
          guardando={guardando}
          errorModal={errorModal}
          onChange={handleFormChange}
          onGuardar={handleGuardar}
          onCerrar={cerrarModal}
        />
      )}
    </div>
  );
}
