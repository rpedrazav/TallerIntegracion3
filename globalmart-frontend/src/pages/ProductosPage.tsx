import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import axios, { AxiosError } from 'axios';
import { useAuth } from '../hooks/useAuth';
import { buildServiceUrl } from '../config/api';
import ProductoModal, {
  FORM_VACÍO,
  type FormState,
} from '../components/admin/productos/ProductoModal';
import ProductosFiltros, {
  type Categoria,
} from '../components/admin/productos/ProductosFiltros';
import ProductosTabla, {
  type Producto,
} from '../components/admin/productos/ProductosTabla';

// UomBaseId requerido por el DTO (uuid genérico para "unidad")
const DEFAULT_UOM_ID = '00000000-0000-0000-0000-000000000001';

interface ValidationFailurePayload {
  errorMessage?: string;
  ErrorMessage?: string;
  message?: string;
}

interface ApiErrorPayload {
  message?: string;
  errors?: Record<string, string[]> | ValidationFailurePayload[];
}

type ProductAction = 'cargar' | 'guardar' | 'desactivar';

function validationMessages(payload: unknown): string[] {
  if (Array.isArray(payload)) {
    return payload
      .map((item: unknown) => {
        if (typeof item !== 'object' || item === null) return '';
        const failure = item as ValidationFailurePayload;
        return failure.errorMessage ?? failure.ErrorMessage ?? failure.message ?? '';
      })
      .filter((message): message is string => Boolean(message));
  }

  if (typeof payload !== 'object' || payload === null) return [];
  const errors = (payload as ApiErrorPayload).errors;
  if (Array.isArray(errors)) return validationMessages(errors);
  if (typeof errors === 'object' && errors !== null) {
    return Object.values(errors).flat().filter((message): message is string => Boolean(message));
  }
  return [];
}

export function getProductErrorMessage(error: unknown, action: ProductAction): string {
  if (!axios.isAxiosError(error)) {
    return action === 'desactivar'
      ? 'Error inesperado al desactivar el producto.'
      : 'Error inesperado al guardar el producto.';
  }

  if (!error.response) {
    return 'No se pudo conectar con el servicio de catálogo (MS-3).';
  }

  const payload = error.response.data as ApiErrorPayload | ValidationFailurePayload[] | undefined;
  const explicitMessage = !Array.isArray(payload) && typeof payload?.message === 'string'
    ? payload.message.trim()
    : '';
  const validationMessage = validationMessages(payload).join(', ');

  if (error.response.status === 400) {
    return validationMessage || explicitMessage || 'Los datos del producto no son válidos.';
  }
  if (error.response.status === 401) {
    return 'Tu sesión no es válida o expiró. Inicia sesión nuevamente.';
  }
  if (error.response.status === 403) {
    return action === 'desactivar'
      ? 'No tienes permisos para desactivar productos.'
      : 'No tienes permisos para guardar productos.';
  }
  if (error.response.status === 404) {
    return explicitMessage || 'El producto no existe o pertenece a otro tenant.';
  }

  return explicitMessage || (action === 'desactivar'
    ? 'Error al desactivar el producto.'
    : 'Error al guardar el producto.');
}

export default function ProductosPage() {
  const { token, loading: authLoading } = useAuth();

  const [productos, setProductos] = useState<Producto[]>([]);
  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState('');

  const [query, setQuery] = useState('');
  const [categoriaFiltro, setCategoriaFiltro] = useState('');
  const [soloActivos, setSoloActivos] = useState(false);
  const [reloadTick, setReloadTick] = useState(0);

  const [modalAbierto, setModalAbierto] = useState(false);
  const [modalModo, setModalModo] = useState<'crear' | 'editar'>('crear');
  const [editandoId, setEditandoId] = useState<string | null>(null);
  const [form, setForm] = useState<FormState>(FORM_VACÍO);
  const [guardando, setGuardando] = useState(false);
  const [errorModal, setErrorModal] = useState('');
  const [errorField, setErrorField] = useState<keyof FormState | null>(null);
  const guardandoRef = useRef(false);

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
          axios.get(buildServiceUrl('catalog', '/products'), { params: { page: 1, pageSize: 500 }, headers }),
          axios.get(buildServiceUrl('catalog', '/categories'), { headers }),
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

  const catMap = useMemo(() => {
    const m: Record<string, string> = {};
    categorias.forEach((c) => { m[c.id] = c.nombre; });
    return m;
  }, [categorias]);

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

  const abrirCrear = () => {
    setForm(FORM_VACÍO);
    setEditandoId(null);
    setModalModo('crear');
    setErrorModal('');
    setErrorField(null);
    setModalAbierto(true);
  };

  const handleEliminar = async (id: string, nombre: string) => {
    if (!window.confirm(`El producto '${nombre}' será DESACTIVADO y se conservará en el catálogo. ¿Deseas continuar?`)) return;
    setErrorMsg('');
    try {
      const token = await window.api?.getToken();
      if (!token) {
        setErrorMsg('Tu sesión no es válida o expiró. Inicia sesión nuevamente.');
        return;
      }
      await axios.delete(buildServiceUrl('catalog', `/products/${id}`), {
        headers: { Authorization: `Bearer ${token}` }
      });
      fetchDatos();
    } catch (error) {
      console.error('Error al desactivar producto', error);
      setErrorMsg(getProductErrorMessage(error, 'desactivar'));
    }
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
    setErrorField(null);
    setModalAbierto(true);
  };

  const cerrarModal = () => {
    if (guardando) return;
    setModalAbierto(false);
    setErrorModal('');
    setErrorField(null);
  };

  const handleFormChange = (field: keyof FormState, value: string | boolean) => {
    setForm(prev => ({ ...prev, [field]: value }));
    setErrorModal('');
    setErrorField(null);
  };

  const handleGuardar = async () => {
    if (guardandoRef.current) return;
    if (!form.nombre.trim()) { setErrorField('nombre'); setErrorModal('El nombre es obligatorio.'); return; }
    const precio = parseFloat(form.precioBase);
    if (isNaN(precio) || precio <= 0) { setErrorField('precioBase'); setErrorModal('El precio debe ser un número válido mayor que 0.'); return; }

    guardandoRef.current = true;
    setGuardando(true);
    setErrorModal('');
    setErrorField(null);
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
        await axios.post(buildServiceUrl('catalog', '/products'), payload, { headers });
      } else {
        await axios.put(buildServiceUrl('catalog', `/products/${editandoId}`), payload, { headers });
      }
      setModalAbierto(false);
      fetchDatos();
    } catch (err) {
      setErrorField(null);
      setErrorModal(getProductErrorMessage(err, 'guardar'));
    } finally {
      guardandoRef.current = false;
      setGuardando(false);
    }
  };

  const filtrosActivos = Boolean(query || categoriaFiltro || soloActivos);

  return (
    <div style={{ maxWidth: '1150px', margin: '0 auto' }}>
      <div
        style={{
          background: 'linear-gradient(135deg, var(--color-ink) 0%, var(--color-sidebar) 60%, var(--color-primary) 100%)',
          borderRadius: '16px',
          padding: '2rem 2.5rem',
          marginBottom: '1.75rem',
          color: 'var(--color-surface)',
          boxShadow: '0 10px 25px -5px color-mix(in srgb, var(--color-primary) 35%, transparent)',
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
          <button
            id="btn-nuevo-producto"
            type="button"
            onClick={abrirCrear}
            style={{
              padding: '0.7rem 1.35rem',
              background: 'linear-gradient(135deg, var(--color-success), var(--color-success))',
              color: 'var(--color-surface)',
              border: 'none',
              borderRadius: '10px',
              fontWeight: 700,
              fontSize: '0.95rem',
              cursor: 'pointer',
              boxShadow: '0 4px 12px color-mix(in srgb, var(--color-success) 40%, transparent)',
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem',
              whiteSpace: 'nowrap',
              transition: 'transform 0.1s, box-shadow 0.1s',
            }}
            onMouseEnter={e => { (e.currentTarget.style.transform = 'translateY(-1px)'); (e.currentTarget.style.boxShadow = '0 6px 16px color-mix(in srgb, var(--color-success) 50%, transparent)'); }}
            onMouseLeave={e => { (e.currentTarget.style.transform = 'translateY(0)'); (e.currentTarget.style.boxShadow = '0 4px 12px color-mix(in srgb, var(--color-success) 40%, transparent)'); }}
          >
            ➕ Nuevo Producto
          </button>

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
              {filtrosActivos ? 'filtrados' : 'productos totales'}
            </div>
          </div>
        </div>
      </div>

      <ProductosFiltros
        query={query}
        categoriaFiltro={categoriaFiltro}
        soloActivos={soloActivos}
        categorias={categorias}
        loading={loading}
        onQueryChange={setQuery}
        onCategoriaChange={setCategoriaFiltro}
        onSoloActivosChange={setSoloActivos}
        onRecargar={fetchDatos}
      />

      {errorMsg && (
        <div
          role="alert"
          style={{
            background: 'var(--color-danger-bg)', border: '1px solid var(--color-danger)',
            borderRadius: '12px', padding: '1rem 1.25rem',
            color: 'var(--color-danger)', marginBottom: '1.5rem',
            display: 'flex', alignItems: 'center', gap: '0.75rem', fontSize: '0.925rem',
          }}
        >
          <span style={{ fontSize: '1.4rem' }}>⚠️</span>
          <span>{errorMsg}</span>
        </div>
      )}

      <ProductosTabla
        productos={productos}
        productosFiltrados={productosFiltrados}
        catMap={catMap}
        loading={loading}
        filtrosActivos={filtrosActivos}
        onLimpiarFiltros={() => {
          setQuery('');
          setCategoriaFiltro('');
          setSoloActivos(false);
        }}
        onCrear={abrirCrear}
        onEditar={abrirEditar}
        onDesactivar={handleEliminar}
      />

      {modalAbierto && (
        <ProductoModal
          modo={modalModo}
          form={form}
          categorias={categorias}
          guardando={guardando}
          errorModal={errorModal}
          errorField={errorField}
          onChange={handleFormChange}
          onGuardar={handleGuardar}
          onCerrar={cerrarModal}
        />
      )}
    </div>
  );
}
