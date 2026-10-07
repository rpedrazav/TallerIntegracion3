const fs = require('fs');
let content = fs.readFileSync('src/pages/ProductosPage.tsx', 'utf8');

const regex = /<td style=\{\{ padding: '0\.85rem 1\.25rem' \}\}>[\s\S]*?<\/td>/;
const replacement = `<td style={{ padding: '0.85rem 1.25rem' }}>
                          <div style={{ display: 'flex', gap: '0.5rem' }}>
                            <button
                              id={\`btn-editar-producto-\${prod.id}\`}
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
                              title={\`Editar \${prod.nombre}\`}
                            >
                              Editar
                            </button>
                            <button
                              id={\`btn-eliminar-producto-\${prod.id}\`}
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
                              title={\`Eliminar \${prod.nombre}\`}
                            >
                              Eliminar
                            </button>
                          </div>
                      </td>`;

content = content.replace(regex, replacement);
fs.writeFileSync('src/pages/ProductosPage.tsx', content, 'utf8');
