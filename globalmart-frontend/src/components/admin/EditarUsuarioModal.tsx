import React, { useEffect, useState } from 'react';
import {
  crearSolicitudEdicionUsuario,
  obtenerEmailInicial,
  SolicitudEdicionUsuario,
  validarEmailEdicion,
} from './editarUsuario';
import { Usuario } from './UsuariosList';

interface Props {
  usuario: Usuario | null;
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (solicitud: SolicitudEdicionUsuario) => Promise<void>;
  onSuccess: () => void;
}

const overlayStyle: React.CSSProperties = {
  position: 'fixed', inset: 0, background: 'color-mix(in srgb, var(--color-ink) 55%, transparent)',
  display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000,
};
const modalStyle: React.CSSProperties = {
  background: 'var(--color-surface)', color: 'var(--color-ink)', border: '1px solid var(--color-line)',
  borderRadius: '12px', padding: '1.5rem', fontFamily: 'var(--font-sans)',
  width: '100%', maxWidth: '440px', boxShadow: '0 20px 40px rgba(51,42,34,0.25)',
};
const inputStyle = (hasError: boolean): React.CSSProperties => ({
  width: '100%', padding: '0.6rem 0.85rem', borderRadius: '8px', fontSize: '0.95rem',
  border: `1px solid ${hasError ? 'var(--color-danger)' : 'var(--color-line)'}`, boxSizing: 'border-box',
  color: 'var(--color-ink)', background: 'var(--color-bg)',
});
const messageStyle: React.CSSProperties = { fontSize: '0.875rem', marginTop: '0.5rem' };
const buttonStyle: React.CSSProperties = {
  padding: '0.55rem 1.1rem', borderRadius: '8px', fontSize: '0.95rem', fontWeight: 600,
};

export default function EditarUsuarioModal({ usuario, isOpen, onClose, onSubmit, onSuccess }: Props) {
  const [email, setEmail] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    if (!isOpen || !usuario) return;
    setEmail(obtenerEmailInicial(usuario));
    setSubmitted(false);
    setSaving(false);
    setError('');
    setSuccess('');
  }, [isOpen, usuario]);

  if (!isOpen || !usuario) return null;

  const emailError = validarEmailEdicion(email);

  const cerrar = () => {
    if (!saving) onClose();
  };

  const guardar = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitted(true);
    if (emailError) return;

    setSaving(true);
    setError('');
    setSuccess('');
    try {
      await onSubmit(crearSolicitudEdicionUsuario(usuario, email));
      setSuccess('Usuario actualizado exitosamente.');
      setSaving(false);
      await new Promise(resolve => setTimeout(resolve, 500));
      onSuccess();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo actualizar el usuario.');
      setSaving(false);
    }
  };

  return (
    <div style={overlayStyle} onClick={cerrar}>
      <form
        style={modalStyle}
        onClick={event => event.stopPropagation()}
        onSubmit={guardar}
        noValidate
        role="dialog"
        aria-modal="true"
        aria-labelledby="eu-titulo"
      >
        <h3 id="eu-titulo" style={{ marginTop: 0 }}>Editar usuario</h3>
        <p style={{ color: 'var(--color-ink-soft)', fontSize: '0.875rem' }}>
          {usuario.nombre}
        </p>

        <div style={{ marginBottom: '0.9rem' }}>
          <label htmlFor="eu-email" style={{ display: 'block', fontWeight: 600, marginBottom: '0.25rem' }}>
            Correo electrónico
          </label>
          <input
            id="eu-email"
            type="email"
            maxLength={200}
            autoComplete="email"
            value={email}
            disabled={saving}
            aria-invalid={submitted && !!emailError}
            onChange={event => {
              setEmail(event.target.value);
              setError('');
              setSuccess('');
            }}
            style={inputStyle(submitted && !!emailError)}
          />
          {submitted && emailError && (
            <div role="alert" style={{ ...messageStyle, color: 'var(--color-danger-text)' }}>{emailError}</div>
          )}
        </div>

        {error && <div role="alert" style={{ ...messageStyle, color: 'var(--color-danger-text)' }}>{error}</div>}
        {success && <div role="status" style={{ ...messageStyle, color: 'var(--color-success-text)' }}>{success}</div>}

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem', marginTop: '1rem' }}>
          <button
            type="button"
            onClick={cerrar}
            disabled={saving}
            style={{ ...buttonStyle, background: 'transparent', border: '1px solid var(--color-line)' }}
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={saving}
            style={{ ...buttonStyle, background: 'var(--color-primary)', color: 'var(--color-on-primary)', border: 'none' }}
          >
            {saving ? 'Guardando…' : 'Guardar cambios'}
          </button>
        </div>
      </form>
    </div>
  );
}
