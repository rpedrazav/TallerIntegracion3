import React from 'react';

export interface Categoria {
  id: string;
  nombre: string;
}

interface ProductosFiltrosProps {
  query: string;
  categoriaFiltro: string;
  soloActivos: boolean;
  categorias: Categoria[];
  loading: boolean;
  onQueryChange: (value: string) => void;
  onCategoriaChange: (value: string) => void;
  onSoloActivosChange: (value: boolean) => void;
  onRecargar: () => void;
}

export default function ProductosFiltros({
  query,
  categoriaFiltro,
  soloActivos,
  categorias,
  loading,
  onQueryChange,
  onCategoriaChange,
  onSoloActivosChange,
  onRecargar,
}: ProductosFiltrosProps) {
  return (
    <div
      style={{
        background: 'var(--color-surface)',
        border: '1px solid var(--color-line)',
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
          onChange={(e) => onQueryChange(e.target.value)}
          style={{
            width: '100%',
            padding: '0.65rem 0.9rem 0.65rem 2.4rem',
            border: `1px solid ${query ? 'var(--color-primary)' : 'var(--color-line)'}`,
            borderRadius: '10px',
            fontSize: '0.95rem',
            outline: 'none',
            boxSizing: 'border-box',
            color: 'var(--color-ink)',
            backgroundColor: query ? 'var(--color-primary-bg)' : 'var(--color-bg)',
          }}
        />
        {query && (
          <button
            type="button"
            onClick={() => onQueryChange('')}
            style={{
              position: 'absolute', right: '0.6rem', top: '50%',
              transform: 'translateY(-50%)', background: 'none',
              border: 'none', cursor: 'pointer', fontSize: '1rem',
              color: 'var(--color-muted)', padding: '0.2rem',
            }}
            title="Limpiar búsqueda"
          >
            ✕
          </button>
        )}
      </div>

      <select
        id="select-categoria-filtro"
        value={categoriaFiltro}
        onChange={(e) => onCategoriaChange(e.target.value)}
        style={{
          flex: '0 1 200px',
          padding: '0.65rem 0.9rem',
          border: `1px solid ${categoriaFiltro ? 'var(--color-primary)' : 'var(--color-line)'}`,
          borderRadius: '10px',
          fontSize: '0.9rem',
          backgroundColor: categoriaFiltro ? 'var(--color-primary-bg)' : 'var(--color-bg)',
          color: 'var(--color-ink)',
          cursor: 'pointer',
          outline: 'none',
        }}
      >
        <option value="">Todas las categorías</option>
        {categorias.map((c) => (
          <option key={c.id} value={c.id}>{c.nombre}</option>
        ))}
      </select>

      <label
        htmlFor="toggle-solo-activos"
        style={{
          display: 'flex', alignItems: 'center', gap: '0.5rem',
          cursor: 'pointer', userSelect: 'none',
          fontSize: '0.9rem', color: 'var(--color-ink-soft)', fontWeight: 500, whiteSpace: 'nowrap',
        }}
      >
        <input
          id="toggle-solo-activos"
          type="checkbox"
          checked={soloActivos}
          onChange={(e) => onSoloActivosChange(e.target.checked)}
          style={{ width: '16px', height: '16px', accentColor: 'var(--color-primary)', cursor: 'pointer' }}
        />
        Solo activos
      </label>

      <button
        type="button"
        onClick={onRecargar}
        disabled={loading}
        style={{
          padding: '0.65rem 1.2rem',
          background: loading ? 'var(--color-line)' : 'var(--color-primary)',
          color: loading ? 'var(--color-muted)' : 'var(--color-surface)',
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
  );
}
