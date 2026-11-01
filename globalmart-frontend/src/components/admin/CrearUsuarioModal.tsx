import React, { useEffect, useState } from 'react';

// Roles asignables por un ADMIN de tenant (SUPER_ADMIN está prohibido en MS-1).
export const ROLES_ASIGNABLES = ['CAJERO', 'REPONEDOR', 'ADMIN'] as const;
export type RolAsignable = string;

export interface CrearUsuarioValues {
  nombre: string;
  email: string;
  password: string;
  rol: RolAsignable;
  sucursalId?: string;
}

interface FormState extends CrearUsuarioValues {
  confirmarPassword: string;
}

type Errors = Partial<Record<keyof FormState, string>>;

interface Props {
  isOpen: boolean;
  tenantId: string;
  sucursales: Array<{ id: string; nombre: string }>;
  roles?: string[];
  onClose: () => void;
  /** Recibe los valores ya validados. Puede lanzar un Error para mostrar un mensaje del servidor. */
  onSubmit: (values: CrearUsuarioValues) => Promise<void> | void;
}

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const PASSWORD_MIN = 8; // Coincide con [MinLength(8)] de CrearUsuarioDto

const FORM_INICIAL: FormState = {
  nombre: '',
  email: '',
  password: '',
  confirmarPassword: '',
  rol: 'CAJERO',
  sucursalId: '',
};

function generarPasswordInicial(): string {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%';
  const bytes = crypto.getRandomValues(new Uint8Array(14));
  return 'Gm7!' + Array.from(bytes, b => alphabet[b % alphabet.length]).join('');
}

export function validarUsuario(f: FormState, roles: readonly string[] = ROLES_ASIGNABLES): Errors {
  const e: Errors = {};
  if (!f.nombre.trim()) e.nombre = 'El nombre es obligatorio.';
  else if (f.nombre.trim().length < 2) e.nombre = 'El nombre debe tener al menos 2 caracteres.';

  if (!f.email.trim()) e.email = 'El correo es obligatorio.';
  else if (!EMAIL_REGEX.test(f.email.trim())) e.email = 'Ingresa un correo válido.';

  if (!f.password) e.password = 'La contraseña es obligatoria.';
  else if (f.password.length < PASSWORD_MIN)
    e.password = `La contraseña debe tener al menos ${PASSWORD_MIN} caracteres.`;
  else if (!/[A-Z]/.test(f.password))
    e.password = 'La contraseña debe contener al menos una letra mayúscula.';
  else if (!/[a-z]/.test(f.password))
    e.password = 'La contraseña debe contener al menos una letra minúscula.';
  else if (!/[0-9]/.test(f.password))
    e.password = 'La contraseña debe contener al menos un dígito.';
  else if (!/[!@#$%^&*()-_=+[\]{}|;':",./<>?\\]/.test(f.password))
    e.password = 'La contraseña debe contener al menos un carácter especial (!@#$%...).';

  if (!f.confirmarPassword) e.confirmarPassword = 'Confirma la contraseña.';
  else if (f.confirmarPassword !== f.password) e.confirmarPassword = 'Las contraseñas no coinciden.';

  if (!roles.includes(f.rol)) e.rol = 'Selecciona un rol válido.';
  return e;
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
const labelStyle: React.CSSProperties = { display: 'block', fontSize: '0.875rem', fontWeight: 600, marginBottom: '0.25rem' };
const errorStyle: React.CSSProperties = { color: 'var(--color-danger-text)', fontSize: '0.875rem', marginTop: '0.2rem' };
const btnBase: React.CSSProperties = {
  padding: '0.55rem 1.1rem', borderRadius: '8px', fontSize: '0.95rem', fontWeight: 600, cursor: 'pointer',
};
const btnPrimary: React.CSSProperties = {
  ...btnBase, background: 'var(--color-primary)', color: 'var(--color-on-primary)', border: '1px solid var(--color-primary)',
};
const btnSecondary: React.CSSProperties = {
  ...btnBase, background: 'transparent', color: 'var(--color-ink)', border: '1px solid var(--color-line)',
};

const CrearUsuarioModal: React.FC<Props> = ({ isOpen, tenantId, sucursales, roles = [...ROLES_ASIGNABLES], onClose, onSubmit }) => {
  const [form, setForm] = useState<FormState>(FORM_INICIAL);
  const [touched, setTouched] = useState<Partial<Record<keyof FormState, boolean>>>({});
  const [submitted, setSubmitted] = useState(false);
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState('');
  const [mostrarPassword, setMostrarPassword] = useState(false);

  useEffect(() => {
    if (!isOpen) return;
    const password = generarPasswordInicial();
    setForm({ ...FORM_INICIAL, password, confirmarPassword: password });
    setMostrarPassword(false);
  }, [isOpen]);

  if (!isOpen) return null;

  const errors = validarUsuario(form, roles);
  const show = (k: keyof FormState) => (submitted || touched[k]) && errors[k];

  const set = <K extends keyof FormState>(k: K, v: FormState[K]) => {
    setForm(prev => ({ ...prev, [k]: v }));
    setServerError('');
  };

  const cerrar = () => {
    if (saving) return;
    setForm(FORM_INICIAL);
    setTouched({});
    setSubmitted(false);
    setServerError('');
    onClose();
  };

  const handleSubmit = async (ev: React.FormEvent) => {
    ev.preventDefault();
    setSubmitted(true);
    if (Object.keys(errors).length > 0) return;
    setSaving(true);
    try {
      await onSubmit({
        nombre: form.nombre.trim(),
        email: form.email.trim().toLowerCase(),
        password: form.password,
        rol: form.rol,
        sucursalId: form.sucursalId || undefined,
      });
      setSaving(false);
      cerrar();
    } catch (err) {
      setServerError(err instanceof Error ? err.message : 'No se pudo crear el usuario.');
      setSaving(false);
    }
  };

  const campo = (
    k: keyof FormState, label: string, type: string, autoComplete?: string,
  ) => (
    <div style={{ marginBottom: '0.9rem' }}>
      <label htmlFor={`cu-${k}`} style={labelStyle}>{label}</label>
      <input
        id={`cu-${k}`}
        type={type}
        autoComplete={autoComplete}
        value={form[k]}
        disabled={saving}
        aria-invalid={!!show(k)}
        onChange={e => set(k, e.target.value as never)}
        onBlur={() => setTouched(t => ({ ...t, [k]: true }))}
        style={inputStyle(!!show(k))}
      />
      {show(k) && <div role="alert" style={errorStyle}>{errors[k]}</div>}
    </div>
  );

  return (
    <div style={overlayStyle} onClick={cerrar}>
      <form
        style={modalStyle}
        onClick={e => e.stopPropagation()}
        onSubmit={handleSubmit}
        noValidate
        role="dialog"
        aria-modal="true"
        aria-labelledby="cu-titulo"
      >
        <h3 id="cu-titulo" style={{ marginTop: 0 }}>Crear usuario</h3>

        {campo('nombre', 'Nombre', 'text', 'name')}
        {campo('email', 'Correo electrónico', 'email', 'email')}
        <div style={{ marginBottom: '0.9rem' }}><label htmlFor="cu-tenant" style={labelStyle}>Tenant ID</label>
          <input id="cu-tenant" value={tenantId} readOnly style={inputStyle(false)} />
          <small>El usuario pertenece al tenant de esta sesión.</small>
        </div>
        {campo('password', 'Contraseña inicial', mostrarPassword ? 'text' : 'password', 'new-password')}
        <button type="button" onClick={() => setMostrarPassword(v => !v)} style={btnSecondary}>{mostrarPassword ? 'Ocultar' : 'Mostrar'} contraseña</button>
        <small style={{ color: 'var(--color-ink-soft)', fontSize: '0.75rem', display: 'block', marginTop: '-0.5rem', marginBottom: '0.7rem' }}>
          Mínimo 8 caracteres, al menos una mayúscula, minúscula, número y símbolo.
        </small>
        {campo('confirmarPassword', 'Confirmar contraseña', 'password', 'new-password')}

        <div style={{ marginBottom: '0.9rem' }}>
          <label htmlFor="cu-rol" style={labelStyle}>Rol</label>
          <select
            id="cu-rol"
            value={form.rol}
            disabled={saving}
            onChange={e => set('rol', e.target.value)}
            style={inputStyle(!!show('rol'))}
          >
            {roles.map(r => <option key={r} value={r}>{r}</option>)}
          </select>
          {show('rol') && <div role="alert" style={errorStyle}>{errors.rol}</div>}
        </div>

        <div style={{ marginBottom: '0.9rem' }}>
          <label htmlFor="cu-sucursal" style={labelStyle}>Sucursal inicial</label>
          <select id="cu-sucursal" value={form.sucursalId} onChange={e => set('sucursalId', e.target.value)} style={inputStyle(false)}>
            <option value="">Sin asignar</option>
            {sucursales.map(s => <option key={s.id} value={s.id}>{s.nombre}</option>)}
          </select>
        </div>

        {serverError && <div role="alert" style={{ ...errorStyle, marginBottom: '0.75rem' }}>{serverError}</div>}

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' }}>
          <button type="button" onClick={cerrar} disabled={saving} style={btnSecondary}>Cancelar</button>
          <button type="submit" disabled={saving} style={btnPrimary}>{saving ? 'Creando…' : 'Crear usuario'}</button>
        </div>
      </form>
    </div>
  );
};

export default CrearUsuarioModal;
