import React, { useEffect, useRef } from 'react';
import type { Categoria } from './ProductosFiltros';

export interface FormState {
  nombre: string;
  descripcion: string;
  codigoBarras: string;
  categoriaId: string;
  precioBase: string;
  esPesoVariable: boolean;
  isActive: boolean;
}

export const FORM_VACÍO: FormState = {
  nombre: '',
  descripcion: '',
  codigoBarras: '',
  categoriaId: '',
  precioBase: '',
  esPesoVariable: false,
  isActive: true,
};

const inputStyle: React.CSSProperties = {
  width: '100%',
  padding: '0.6rem 0.85rem',
  border: '1px solid var(--color-line)',
  borderRadius: '8px',
  fontSize: '0.95rem',
  boxSizing: 'border-box',
  color: 'var(--color-ink)',
  backgroundColor: 'var(--color-bg)',
};

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontSize: '0.82rem',
  fontWeight: 600,
  color: 'var(--color-ink-soft)',
  marginBottom: '0.35rem',
  textTransform: 'uppercase',
  letterSpacing: '0.04em',
};

const PRODUCT_FOCUSABLE_SELECTOR = [
  'button:not([disabled])',
  'input:not([disabled])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[href]',
  '[tabindex]:not([tabindex="-1"])',
].join(',');

interface ProductoModalProps {
  modo: 'crear' | 'editar';
  form: FormState;
  categorias: Categoria[];
  guardando: boolean;
  errorModal: string;
  errorField: keyof FormState | null;
  onChange: (field: keyof FormState, value: string | boolean) => void;
  onGuardar: () => void;
  onCerrar: () => void;
}

export default function ProductoModal({
  modo,
  form,
  categorias,
  guardando,
  errorModal,
  errorField,
  onChange,
  onGuardar,
  onCerrar,
}: ProductoModalProps) {
  const dialogRef = useRef<HTMLFormElement>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);
  const titleId = 'producto-modal-titulo';
  const errorId = 'producto-modal-error';

  useEffect(() => {
    previousFocusRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const frame = window.requestAnimationFrame(() => {
      const dialog = dialogRef.current;
      const first = dialog?.querySelector<HTMLElement>('#modal-nombre')
        ?? dialog?.querySelector<HTMLElement>(PRODUCT_FOCUSABLE_SELECTOR);
      (first ?? dialog)?.focus();
    });
    return () => {
      window.cancelAnimationFrame(frame);
      previousFocusRef.current?.focus();
    };
  }, []);

  const handleDialogKeyDown = (event: React.KeyboardEvent<HTMLFormElement>) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      if (!guardando) onCerrar();
      return;
    }
    if (event.key !== 'Tab') return;
    const dialog = dialogRef.current;
    if (!dialog) return;
    const focusable = Array.from(dialog.querySelectorAll<HTMLElement>(PRODUCT_FOCUSABLE_SELECTOR));
    if (focusable.length === 0) {
      event.preventDefault();
      dialog.focus();
      return;
    }
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (event.shiftKey && (document.activeElement === first || !dialog.contains(document.activeElement))) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && (document.activeElement === last || !dialog.contains(document.activeElement))) {
      event.preventDefault();
      first.focus();
    }
  };

  return (
    <div
      style={{
        position: 'fixed', inset: 0, zIndex: 1000,
        background: 'color-mix(in srgb, var(--color-ink) 55%, transparent)',
        backdropFilter: 'blur(4px)',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        padding: '1rem',
      }}
      onClick={(e) => { if (e.target === e.currentTarget) onCerrar(); }}
    >
      <form
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        onKeyDown={handleDialogKeyDown}
        onSubmit={(event) => {
          event.preventDefault();
          if (!guardando) onGuardar();
        }}
        style={{
          background: 'var(--color-surface)',
          borderRadius: '18px',
          width: '100%',
          maxWidth: '520px',
          boxShadow: '0 25px 60px -10px rgba(0,0,0,0.35)',
          overflow: 'hidden',
          animation: 'modalIn 0.18s ease-out',
        }}
      >
        <div
          style={{
            background: 'linear-gradient(135deg, var(--color-ink) 0%, var(--color-sidebar) 60%, var(--color-primary) 100%)',
            padding: '1.5rem 1.75rem',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <span style={{ fontSize: '1.6rem' }}>{modo === 'crear' ? '➕' : '✏️'}</span>
            <div>
              <h2 id={titleId} style={{ margin: 0, color: 'var(--color-surface)', fontSize: '1.2rem', fontWeight: 800 }}>
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
              color: 'var(--color-surface)',
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

        <div style={{ padding: '1.75rem', display: 'flex', flexDirection: 'column', gap: '1.1rem' }}>
          {errorModal && (
            <div id={errorId} role="alert" style={{
              background: 'var(--color-danger-bg)', border: '1px solid var(--color-danger)',
              borderRadius: '8px', padding: '0.75rem 1rem',
              color: 'var(--color-danger)', fontSize: '0.875rem',
              display: 'flex', gap: '0.5rem', alignItems: 'flex-start',
            }}>
              <span>⚠️</span><span>{errorModal}</span>
            </div>
          )}

          <div>
            <label style={labelStyle} htmlFor="modal-nombre">Nombre *</label>
            <input
              id="modal-nombre"
              type="text"
              placeholder="Ej: Leche entera 1L"
              value={form.nombre}
              aria-invalid={errorField === 'nombre'}
              aria-describedby={errorField === 'nombre' ? errorId : undefined}
              onChange={(e) => onChange('nombre', e.target.value)}
              style={inputStyle}
              disabled={guardando}
            />
          </div>

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
                aria-invalid={errorField === 'precioBase'}
                aria-describedby={errorField === 'precioBase' ? errorId : undefined}
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

          <div style={{ display: 'flex', gap: '2rem', flexWrap: 'wrap' }}>
            <label
              htmlFor="modal-peso-variable"
              style={{
                display: 'flex', alignItems: 'center', gap: '0.5rem',
                cursor: 'pointer', userSelect: 'none',
                fontSize: '0.9rem', color: 'var(--color-ink)', fontWeight: 500,
              }}
            >
              <input
                id="modal-peso-variable"
                type="checkbox"
                checked={form.esPesoVariable}
                onChange={(e) => onChange('esPesoVariable', e.target.checked)}
                disabled={guardando}
                style={{ width: '16px', height: '16px', accentColor: 'var(--color-primary)' }}
              />
              ⚖️ Peso variable
            </label>

            <label
              htmlFor="modal-activo"
              style={{
                display: 'flex', alignItems: 'center', gap: '0.5rem',
                cursor: 'pointer', userSelect: 'none',
                fontSize: '0.9rem', color: 'var(--color-ink)', fontWeight: 500,
              }}
            >
              <input
                id="modal-activo"
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => onChange('isActive', e.target.checked)}
                disabled={guardando}
                style={{ width: '16px', height: '16px', accentColor: 'var(--color-success)' }}
              />
              ✅ Activo
            </label>
          </div>
        </div>

        <div
          style={{
            padding: '1rem 1.75rem',
            borderTop: '1px solid var(--color-line)',
            background: 'var(--color-bg)',
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
              border: '1px solid var(--color-line)',
              borderRadius: '8px',
              fontWeight: 600,
              fontSize: '0.9rem',
              cursor: 'pointer',
              color: 'var(--color-ink-soft)',
            }}
          >
            Cancelar
          </button>
          <button
            id="btn-guardar-modal-producto"
            type="submit"
            disabled={guardando}
            style={{
              padding: '0.6rem 1.5rem',
              background: guardando ? 'var(--color-muted)' : 'var(--color-primary)',
              color: 'var(--color-surface)',
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
      </form>

      <style>{`
        @keyframes modalIn {
          from { opacity: 0; transform: scale(0.95) translateY(-8px); }
          to   { opacity: 1; transform: scale(1)   translateY(0); }
        }
      `}</style>
    </div>
  );
}
