import React from 'react';

export interface Usuario {
  id: string;
  nombre: string;
  email: string;
  activo: boolean;
  creadoEn: string;
  roles?: string[];
}

interface UsuariosListProps {
  usuarios: Usuario[];
  loading?: boolean;
  error?: string;
}

const th: React.CSSProperties = {
  textAlign: 'left', padding: '0.6rem 0.9rem', fontSize: '0.875rem',
  color: 'var(--color-ink-soft)', borderBottom: '1px solid var(--color-line)',
};
const td: React.CSSProperties = { padding: '0.6rem 0.9rem', borderBottom: '1px solid var(--color-line)' };

const badge = (activo: boolean): React.CSSProperties => ({
  padding: '0.1rem 0.6rem', borderRadius: '9999px', fontSize: '0.875rem', fontWeight: 600,
  background: activo ? 'var(--color-success-bg)' : 'var(--color-danger-bg)',
  color: activo ? 'var(--color-success-text)' : 'var(--color-danger-text)',
});

const rolChip: React.CSSProperties = {
  display: 'inline-block', padding: '0.1rem 0.5rem', marginRight: '0.35rem',
  border: '1px solid var(--color-line)', borderRadius: '6px', fontSize: '0.8125rem',
  background: 'var(--color-surface)',
};

/** Tabla de usuarios del tenant: nombre, email, roles y estado. */
export default function UsuariosList({ usuarios, loading = false, error = '' }: UsuariosListProps) {
  return (
    <div style={{ background: 'var(--color-surface)', border: '1px solid var(--color-line)', borderRadius: '12px', overflow: 'hidden' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr>
            <th style={th}>Nombre</th>
            <th style={th}>Correo</th>
            <th style={th}>Roles</th>
            <th style={th}>Estado</th>
          </tr>
        </thead>
        <tbody>
          {loading && <tr><td style={td} colSpan={4}>Cargando…</td></tr>}
          {!loading && usuarios.length === 0 && !error && (
            <tr><td style={td} colSpan={4}>No hay usuarios.</td></tr>
          )}
          {usuarios.map(u => (
            <tr key={u.id}>
              <td style={td}>{u.nombre}</td>
              <td style={td}>{u.email}</td>
              <td style={td}>
                {u.roles && u.roles.length > 0
                  ? u.roles.map(r => <span key={r} style={rolChip}>{r}</span>)
                  : <span style={{ color: 'var(--color-ink-soft)' }}>Sin roles</span>}
              </td>
              <td style={td}><span style={badge(u.activo)}>{u.activo ? 'Activo' : 'Inactivo'}</span></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
