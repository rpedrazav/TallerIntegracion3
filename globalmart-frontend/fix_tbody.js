const fs = require('fs');
let content = fs.readFileSync('src/pages/ProductosPage.tsx', 'utf8');

const regex = /<tbody>[\s\S]*?<\/tbody>/;
const replacement = `<tbody>
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
                      {/* Nombre */}
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

                      {/* Código de barras */}
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

                      {/* Categoría */}
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

                      {/* Precio base */}
                      <td style={{ padding: '0.85rem 1.25rem', whiteSpace: 'nowrap' }}>
                        <span style={{ fontWeight: 800, fontSize: '1rem', color: 'var(--color-primary)' }}>
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
                            background: prod.esPesoVariable ? 'var(--color-warning-bg)' : 'var(--color-success-bg)',
                            color: prod.esPesoVariable ? 'var(--color-warning-text)' : 'var(--color-success)',
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

                      {/* Acciones */}
                      <td style={{ padding: '0.85rem 1.25rem' }}>
                          <div style={{ display: 'flex', gap: '0.5rem' }}>
                            <button
                              id={`btn-editar-producto-${prod.id}`}
                              type="button"
                              onClick={() => abrirEditar(prod)}
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
                              onClick={() => handleEliminar(prod.id, prod.nombre)}
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
                              title={`Eliminar ${prod.nombre}`}
                            >
                              🗑️ Eliminar
                            </button>
                          </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>`;

content = content.replace(regex, replacement);
fs.writeFileSync('src/pages/ProductosPage.tsx', content, 'utf8');
