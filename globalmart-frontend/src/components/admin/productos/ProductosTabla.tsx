import React from 'react';

export interface Producto {
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

interface ProductosTablaProps {
  productos: Producto[];
  productosFiltrados: Producto[];
  catMap: Record<string, string>;
  loading: boolean;
  filtrosActivos: boolean;
  onLimpiarFiltros: () => void;
  onCrear: () => void;
  onEditar: (producto: Producto) => void;
  onDesactivar: (id: string, nombre: string) => void;
}

export default function ProductosTabla({
  productos,
  productosFiltrados,
  catMap,
  loading,
  filtrosActivos,
  onLimpiarFiltros,
  onCrear,
  onEditar,
  onDesactivar,
}: ProductosTablaProps) {
  return (
    <div
      style={{
        background: 'var(--color-surface)',
        border: '1px solid var(--color-line)',
        borderRadius: '14px',
        overflow: 'hidden',
        boxShadow: '0 4px 12px rgba(0,0,0,0.06)',
      }}
    >
      {loading ? (
        <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--color-muted)' }}>
          <div style={{ fontSize: '2.5rem', marginBottom: '0.75rem' }}>⏳</div>
          <div style={{ fontWeight: 600, fontSize: '1rem' }}>Cargando catálogo de productos…</div>
          <div style={{ fontSize: '0.85rem', marginTop: '0.35rem' }}>
            Conectando con MS-3 en el puerto 5203
          </div>
        </div>
      ) : productosFiltrados.length === 0 ? (
        <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--color-muted)' }}>
          <div style={{ fontSize: '2.5rem', marginBottom: '0.75rem' }}>🔍</div>
          <div style={{ fontWeight: 600, fontSize: '1rem', color: 'var(--color-ink-soft)' }}>
            {filtrosActivos
              ? 'Ningún producto coincide con los filtros aplicados'
              : 'No hay productos registrados para este tenant'}
          </div>
          {filtrosActivos && (
            <button
              type="button"
              onClick={onLimpiarFiltros}
              style={{
                marginTop: '1rem', padding: '0.5rem 1.2rem',
                background: 'var(--color-primary)', color: 'var(--color-surface)',
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
              onClick={onCrear}
              style={{
                padding: '0.65rem 1.35rem',
                background: 'linear-gradient(135deg, var(--color-success), var(--color-success))',
                color: 'var(--color-surface)', border: 'none', borderRadius: '10px',
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
                  background: 'linear-gradient(90deg, var(--color-ink) 0%, var(--color-sidebar) 100%)',
                  color: 'var(--color-surface)',
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
                const categoriaNombre = prod.categoriaId ? catMap[prod.categoriaId] ?? '-' : '-';

                return (
                  <tr
                    key={prod.id}
                    style={{
                      backgroundColor: isEven ? 'var(--color-surface)' : 'var(--color-bg)',
                      borderBottom: '1px solid var(--color-bg)',
                      transition: 'background-color 0.12s',
                    }}
                    onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--color-primary-bg)')}
                    onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = isEven ? 'var(--color-surface)' : 'var(--color-bg)')}
                  >
                    <td style={{ padding: '0.85rem 1.25rem', maxWidth: '260px' }}>
                      <div
                        style={{
                          fontWeight: 700, color: 'var(--color-ink)', fontSize: '0.95rem',
                          whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis',
                        }}
                        title={prod.nombre}
                      >
                        {prod.nombre}
                      </div>
                      {prod.descripcion && (
                        <div
                          style={{
                            fontSize: '0.78rem', color: 'var(--color-ink-soft)', marginTop: '0.15rem',
                            whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', maxWidth: '240px',
                          }}
                          title={prod.descripcion}
                        >
                          {prod.descripcion}
                        </div>
                      )}
                    </td>

                    <td style={{ padding: '0.85rem 1.25rem' }}>
                      {prod.codigoBarras ? (
                        <span
                          style={{
                            fontFamily: 'monospace',
                            background: 'var(--color-bg)',
                            padding: '0.2rem 0.5rem',
                            borderRadius: '4px',
                            border: '1px solid var(--color-muted)',
                            fontSize: '0.85rem',
                          }}
                        >
                          {prod.codigoBarras}
                        </span>
                      ) : (
                        <span style={{ color: 'var(--color-muted)', fontSize: '0.85rem' }}>-</span>
                      )}
                    </td>

                    <td style={{ padding: '0.85rem 1.25rem' }}>
                      {prod.categoriaId ? (
                        <span
                          style={{
                            background: 'var(--color-primary-bg)', color: 'var(--color-primary)',
                            padding: '0.22rem 0.65rem', borderRadius: '9999px',
                            fontSize: '0.78rem', fontWeight: 600, whiteSpace: 'nowrap',
                          }}
                        >
                          {categoriaNombre}
                        </span>
                      ) : (
                        <span style={{ color: 'var(--color-muted)', fontSize: '0.85rem' }}>Sin categoría</span>
                      )}
                    </td>

                    <td style={{ padding: '0.85rem 1.25rem', whiteSpace: 'nowrap' }}>
                      <span style={{ fontWeight: 800, fontSize: '1rem', color: 'var(--color-primary)' }}>
                        ${Number(prod.precioBase).toLocaleString('es-CL', {
                          minimumFractionDigits: 0,
                          maximumFractionDigits: 0,
                        })}
                      </span>
                    </td>

                    <td style={{ padding: '0.85rem 1.25rem' }}>
                      <span
                        style={{
                          background: prod.esPesoVariable ? 'var(--color-warning-bg)' : 'var(--color-success-bg)',
                          color: prod.esPesoVariable ? 'var(--color-warning-text)' : 'var(--color-success)',
                          padding: '0.22rem 0.65rem', borderRadius: '9999px',
                          fontSize: '0.78rem', fontWeight: 600, whiteSpace: 'nowrap',
                        }}
                      >
                        {prod.esPesoVariable ? '⚖️ Peso variable' : '📦 Unidad'}
                      </span>
                    </td>

                    <td style={{ padding: '0.85rem 1.25rem' }}>
                      <span
                        style={{
                          display: 'inline-flex', alignItems: 'center', gap: '0.3rem',
                          background: prod.isActive ? 'var(--color-success-bg)' : 'var(--color-danger-bg)',
                          color: prod.isActive ? 'var(--color-success)' : 'var(--color-danger)',
                          padding: '0.22rem 0.65rem', borderRadius: '9999px',
                          fontSize: '0.78rem', fontWeight: 700,
                        }}
                      >
                        <span
                          style={{
                            width: '7px', height: '7px', borderRadius: '50%',
                            background: prod.isActive ? 'var(--color-success)' : 'var(--color-danger)',
                            display: 'inline-block',
                          }}
                        />
                        {prod.isActive ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>

                    <td style={{ padding: '0.85rem 1.25rem' }}>
                      <div style={{ display: 'flex', gap: '0.5rem' }}>
                        <button
                          id={`btn-editar-producto-${prod.id}`}
                          type="button"
                          onClick={() => onEditar(prod)}
                          style={{
                            padding: '0.35rem 0.85rem',
                            background: 'var(--color-primary-bg)',
                            color: 'var(--color-primary)',
                            border: '1px solid var(--color-muted)',
                            borderRadius: '4px',
                            cursor: 'pointer',
                            fontSize: '0.85rem',
                            fontWeight: 600,
                            transition: 'all 0.2s',
                          }}
                          onMouseOver={(e) => {
                            e.currentTarget.style.background = 'var(--color-primary)';
                            e.currentTarget.style.color = 'var(--color-surface)';
                          }}
                          onMouseOut={(e) => {
                            e.currentTarget.style.background = 'var(--color-primary-bg)';
                            e.currentTarget.style.color = 'var(--color-primary)';
                          }}
                          title={`Editar ${prod.nombre}`}
                        >
                          ✏️ Editar
                        </button>
                        <button
                          id={`btn-eliminar-producto-${prod.id}`}
                          type="button"
                          onClick={() => onDesactivar(prod.id, prod.nombre)}
                          style={{
                            padding: '0.35rem 0.85rem',
                            background: 'var(--color-danger-bg)',
                            color: 'var(--color-danger)',
                            border: '1px solid var(--color-danger)',
                            borderRadius: '4px',
                            cursor: 'pointer',
                            fontSize: '0.85rem',
                            fontWeight: 600,
                            transition: 'all 0.2s',
                          }}
                          onMouseOver={(e) => {
                            e.currentTarget.style.background = 'var(--color-danger)';
                            e.currentTarget.style.color = 'white';
                          }}
                          onMouseOut={(e) => {
                            e.currentTarget.style.background = 'var(--color-danger-bg)';
                            e.currentTarget.style.color = 'var(--color-danger)';
                          }}
                          title={`Desactivar ${prod.nombre}`}
                        >
                          🗑️ Desactivar
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {!loading && productosFiltrados.length > 0 && (
        <div
          style={{
            padding: '0.85rem 1.5rem',
            borderTop: '1px solid var(--color-line)',
            background: 'var(--color-bg)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: '0.5rem',
            fontSize: '0.85rem',
            color: 'var(--color-ink-soft)',
          }}
        >
          <span>
            Mostrando <strong style={{ color: 'var(--color-ink)' }}>{productosFiltrados.length}</strong> de{' '}
            <strong style={{ color: 'var(--color-ink)' }}>{productos.length}</strong> productos
            {filtrosActivos && (
              <span style={{ color: 'var(--color-primary)', marginLeft: '0.5rem' }}>(filtrado)</span>
            )}
          </span>
          <span style={{ fontSize: '0.78rem' }}>MS-3 Catalog &amp; Pricing · Puerto 5203</span>
        </div>
      )}
    </div>
  );
}
