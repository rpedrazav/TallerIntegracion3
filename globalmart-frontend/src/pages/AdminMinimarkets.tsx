import React, { useEffect, useState } from 'react';
import { AxiosError } from 'axios';
import { tenantsApi, Minimarket, CrearMinimarketRequest } from '../api/client';

const formularioVacio: CrearMinimarketRequest = { nombre: '', codigo: '', adminEmail: '', adminPassword: '' };

function mensajeError(err: unknown): string {
  const res = (err as AxiosError<{ message?: string; errors?: Record<string, string[]> }>).response;
  if (res?.status === 400 && res.data?.errors) return Object.values(res.data.errors).flat().join(' ');
  if (res?.status === 403) return 'Solo el administrador de la plataforma (SUPER_ADMIN) gestiona minimarkets.';
  if (res?.status === 409) return res.data?.message ?? 'Ya existe un minimarket con ese código o nombre.';
  return 'No se pudo completar la operación. Intenta nuevamente.';
}

/** S3-098: listado y alta de minimarkets (tenants) para SUPER_ADMIN. */
export default function AdminMinimarkets() {
  const [minimarkets, setMinimarkets] = useState<Minimarket[]>([]);
  const [form, setForm] = useState(formularioVacio);
  const [error, setError] = useState('');
  const [creado, setCreado] = useState<Minimarket | null>(null);
  const [saving, setSaving] = useState(false);

  const cargar = () => tenantsApi.list().then(setMinimarkets).catch((err) => setError(mensajeError(err)));

  useEffect(() => { void cargar(); }, []);

  const crear = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setCreado(null);
    setSaving(true);
    try {
      setCreado(await tenantsApi.create(form));
      setForm(formularioVacio);
      await cargar();
    } catch (err) {
      setError(mensajeError(err));
    } finally {
      setSaving(false);
    }
  };

  const cambiar = (campo: keyof CrearMinimarketRequest) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm({ ...form, [campo]: e.target.value });

  return <section>
    <h2>Minimarkets</h2>
    <p>Cada minimarket tiene sus propios usuarios, productos y ventas. Su administrador entra con el código.</p>
    {error && <p role="alert">{error}</p>}
    {creado && <p role="status">Minimarket «{creado.nombre}» creado. Su administrador ya puede entrar con el código «{creado.codigo}».</p>}
    <table style={{ width: '100%', marginBottom: '1.5rem' }}>
      <thead><tr><th>Nombre</th><th>Código</th><th>Estado</th><th>Creado</th></tr></thead>
      <tbody>
        {minimarkets.map(m => (
          <tr key={m.id}>
            <td>{m.nombre}</td>
            <td>{m.codigo}</td>
            <td>{m.activo ? 'Activo' : 'Inactivo'}</td>
            <td>{new Date(m.creadoEn).toLocaleDateString('es-CL')}</td>
          </tr>
        ))}
      </tbody>
    </table>
    <form onSubmit={crear} style={{ display: 'grid', gap: '0.75rem', maxWidth: 520 }}>
      <h3>Nuevo minimarket</h3>
      <label>Nombre <input value={form.nombre} onChange={cambiar('nombre')} required maxLength={200} /></label>
      <label>Código de acceso <input value={form.codigo} onChange={cambiar('codigo')} required pattern="[a-zA-Z0-9\-]{3,50}" title="Entre 3 y 50 letras, números o guiones" placeholder="ej: donpepe" /></label>
      <label>Correo del administrador <input type="email" value={form.adminEmail} onChange={cambiar('adminEmail')} required maxLength={200} /></label>
      <label>Contraseña inicial del administrador <input type="password" value={form.adminPassword} onChange={cambiar('adminPassword')} required minLength={8} autoComplete="new-password" /></label>
      <button type="submit" disabled={saving}>{saving ? 'Creando…' : 'Crear minimarket'}</button>
    </form>
  </section>;
}
