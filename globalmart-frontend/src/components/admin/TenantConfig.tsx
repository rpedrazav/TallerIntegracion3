import React, { useState, useEffect, useCallback, useMemo } from 'react';
import axios, { isAxiosError } from 'axios';
import { parseJwt } from '../../utils/jwt';

// MS-1 Tenant & Identity URL
const IDENTITY_URL = 'https://auth-rpedraza.dev.censei.cl';

export interface TenantConfigData {
  pais: string;
  moneda: string;
  idioma: string;
  zonaHoraria: string;
  porcentajeIva: number;
}

interface TenantConfigProps {
  token: string | null;
}

const inputStyle: React.CSSProperties = {
  width: '100%',
  padding: '0.6rem 0.8rem',
  borderRadius: '6px',
  border: '1px solid var(--color-line)',
  backgroundColor: 'var(--color-surface)',
  color: 'var(--color-ink)',
  fontFamily: 'var(--font-sans)',
  fontSize: '0.9375rem',
  boxSizing: 'border-box',
};

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontSize: '0.875rem',
  fontWeight: 600,
  marginBottom: '0.35rem',
  color: 'var(--color-ink)',
};

const fieldContainer: React.CSSProperties = {
  marginBottom: '1.25rem',
};

export default function TenantConfig({ token }: TenantConfigProps) {
  // Derivar tenantId y permisos de rol desde el token JWT
  const { tenantId, esAdmin } = useMemo(() => {
    if (!token) {
      return {
        tenantId: localStorage.getItem('tenant') || '',
        esAdmin: false,
      };
    }
    const payload = parseJwt(token);
    const id = payload?.tenant_id || localStorage.getItem('tenant') || '';
    const rolActivo = payload?.active_role?.toUpperCase();
    const roles = Array.isArray(payload?.roles)
      ? payload.roles.map((r) => r.toUpperCase())
      : [];
    const tieneRolAdmin =
      rolActivo === 'ADMIN' ||
      rolActivo === 'SUPER_ADMIN' ||
      roles.includes('ADMIN') ||
      roles.includes('SUPER_ADMIN');

    return {
      tenantId: id,
      esAdmin: tieneRolAdmin,
    };
  }, [token]);

  const [formData, setFormData] = useState<TenantConfigData>({
    pais: '',
    moneda: '',
    idioma: '',
    zonaHoraria: '',
    porcentajeIva: 19,
  });

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string>('');
  const [successMsg, setSuccessMsg] = useState<string>('');

  const cargarConfig = useCallback(async (id: string, jwtToken: string) => {
    setLoading(true);
    setError('');
    setSuccessMsg('');
    try {
      const res = await axios.get<TenantConfigData>(`${IDENTITY_URL}/tenants/${id}/config`, {
        headers: {
          Authorization: `Bearer ${jwtToken}`,
          'x-tenant-id': id,
        },
      });
      setFormData({
        pais: res.data.pais || '',
        moneda: res.data.moneda || '',
        idioma: res.data.idioma || '',
        zonaHoraria: res.data.zonaHoraria || '',
        porcentajeIva: Number(res.data.porcentajeIva ?? 19),
      });
    } catch (err: unknown) {
      let msg = 'No se pudo cargar la configuración del tenant.';
      if (isAxiosError(err)) {
        if (err.response?.status === 403) {
          msg = 'No tienes permisos de Administrador para ver la configuración del tenant.';
        } else if (err.response?.data?.message) {
          msg = err.response.data.message;
        }
      }
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    let cancel = false;
    if (tenantId && token) {
      (async () => {
        if (!cancel) {
          await cargarConfig(tenantId, token);
        }
      })();
    }
    return () => {
      cancel = true;
    };
  }, [tenantId, token, cargarConfig]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: name === 'porcentajeIva' ? parseFloat(value) || 0 : value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token || !tenantId) {
      setError('No hay sesión activa o identificador de tenant disponible.');
      return;
    }

    if (!esAdmin) {
      setError('Solo los usuarios con rol Administrador pueden guardar cambios en la configuración.');
      return;
    }

    if (!formData.pais.trim()) {
      setError('El código de país es obligatorio (ej: CL).');
      return;
    }
    if (!formData.moneda.trim()) {
      setError('La moneda es obligatoria (ej: CLP).');
      return;
    }
    if (!formData.idioma.trim()) {
      setError('El idioma es obligatorio (ej: es).');
      return;
    }
    if (formData.porcentajeIva < 0 || formData.porcentajeIva > 100) {
      setError('El porcentaje de IVA debe estar entre 0 y 100.');
      return;
    }

    setSaving(true);
    setError('');
    setSuccessMsg('');

    try {
      const payload = {
        pais: formData.pais.trim().toUpperCase(),
        moneda: formData.moneda.trim().toUpperCase(),
        idioma: formData.idioma.trim().toLowerCase(),
        zonaHoraria: formData.zonaHoraria.trim(),
        porcentajeIva: Number(formData.porcentajeIva),
      };

      const res = await axios.put<TenantConfigData>(
        `${IDENTITY_URL}/tenants/${tenantId}/config`,
        payload,
        {
          headers: {
            Authorization: `Bearer ${token}`,
            'x-tenant-id': tenantId,
          },
        }
      );

      setFormData({
        pais: res.data.pais,
        moneda: res.data.moneda,
        idioma: res.data.idioma,
        zonaHoraria: res.data.zonaHoraria,
        porcentajeIva: Number(res.data.porcentajeIva),
      });

      setSuccessMsg('Configuración del tenant actualizada exitosamente.');
    } catch (err: unknown) {
      let msg = 'No se pudo actualizar la configuración del tenant.';
      if (isAxiosError(err)) {
        if (err.response?.status === 403) {
          msg = 'No tienes permisos de Administrador para modificar la configuración.';
        } else if (err.response?.data?.message) {
          msg = err.response.data.message;
        }
      }
      setError(msg);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div
      style={{
        backgroundColor: 'var(--color-surface)',
        border: '1px solid var(--color-line)',
        borderRadius: '12px',
        padding: '1.5rem',
        maxWidth: '650px',
      }}
    >
      <div style={{ marginBottom: '1.25rem' }}>
        <h3 style={{ margin: '0 0 0.5rem 0', color: 'var(--color-ink)' }}>
          Configuración Regional y Fiscal del Tenant
        </h3>
        <p style={{ margin: 0, fontSize: '0.875rem', color: 'var(--color-ink-soft)' }}>
          Gestiona los parámetros fiscales de facturación e impuestos (IVA) y localización del negocio.
        </p>
        {tenantId && (
          <span
            style={{
              display: 'inline-block',
              marginTop: '0.5rem',
              fontFamily: 'var(--font-mono)',
              fontSize: '0.75rem',
              color: 'var(--color-muted)',
            }}
          >
            Tenant ID: {tenantId}
          </span>
        )}
      </div>

      {/* Banner informativo de modo lectura si no es Admin */}
      {!esAdmin && (
        <div
          role="note"
          style={{
            backgroundColor: 'var(--color-warning-bg)',
            color: 'var(--color-warning-text)',
            border: '1px solid var(--color-warning)',
            borderRadius: '8px',
            padding: '0.75rem 1rem',
            marginBottom: '1.25rem',
            fontSize: '0.875rem',
          }}
        >
          <b>Modo lectura:</b> Tu usuario actual no tiene rol de Administrador. Puedes consultar los
          parámetros, pero para guardar modificaciones se requiere un usuario con rol <b>ADMIN</b>.
        </div>
      )}

      {error && (
        <div
          role="alert"
          style={{
            backgroundColor: 'var(--color-danger-bg)',
            color: 'var(--color-danger-text)',
            border: '1px solid var(--color-danger)',
            borderRadius: '8px',
            padding: '0.75rem 1rem',
            marginBottom: '1.25rem',
            fontSize: '0.875rem',
          }}
        >
          <b>Error:</b> {error}
        </div>
      )}

      {successMsg && (
        <div
          role="status"
          style={{
            backgroundColor: 'var(--color-success-bg)',
            color: 'var(--color-success-text)',
            border: '1px solid var(--color-success)',
            borderRadius: '8px',
            padding: '0.75rem 1rem',
            marginBottom: '1.25rem',
            fontSize: '0.875rem',
          }}
        >
          <b>Éxito:</b> {successMsg}
        </div>
      )}

      {loading ? (
        <div style={{ padding: '2rem 0', textAlign: 'center', color: 'var(--color-ink-soft)' }}>
          Cargando configuración del tenant...
        </div>
      ) : (
        <form onSubmit={handleSubmit}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div style={fieldContainer}>
              <label htmlFor="input-pais" style={labelStyle}>
                País (código ISO)
              </label>
              <input
                id="input-pais"
                type="text"
                name="pais"
                value={formData.pais}
                onChange={handleChange}
                placeholder="CL"
                maxLength={2}
                disabled={!esAdmin}
                required
                style={{
                  ...inputStyle,
                  opacity: esAdmin ? 1 : 0.75,
                  cursor: esAdmin ? 'text' : 'not-allowed',
                }}
              />
              <small style={{ color: 'var(--color-ink-soft)', fontSize: '0.75rem' }}>Ej: CL, AR, US</small>
            </div>

            <div style={fieldContainer}>
              <label htmlFor="input-moneda" style={labelStyle}>
                Moneda Base (ISO)
              </label>
              <input
                id="input-moneda"
                type="text"
                name="moneda"
                value={formData.moneda}
                onChange={handleChange}
                placeholder="CLP"
                maxLength={3}
                disabled={!esAdmin}
                required
                style={{
                  ...inputStyle,
                  opacity: esAdmin ? 1 : 0.75,
                  cursor: esAdmin ? 'text' : 'not-allowed',
                }}
              />
              <small style={{ color: 'var(--color-ink-soft)', fontSize: '0.75rem' }}>Ej: CLP, ARS, USD</small>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div style={fieldContainer}>
              <label htmlFor="input-idioma" style={labelStyle}>
                Idioma
              </label>
              <input
                id="input-idioma"
                type="text"
                name="idioma"
                value={formData.idioma}
                onChange={handleChange}
                placeholder="es"
                maxLength={5}
                disabled={!esAdmin}
                required
                style={{
                  ...inputStyle,
                  opacity: esAdmin ? 1 : 0.75,
                  cursor: esAdmin ? 'text' : 'not-allowed',
                }}
              />
              <small style={{ color: 'var(--color-ink-soft)', fontSize: '0.75rem' }}>Ej: es, en</small>
            </div>

            <div style={fieldContainer}>
              <label htmlFor="input-porcentajeIva" style={labelStyle}>
                Porcentaje IVA (%)
              </label>
              <input
                id="input-porcentajeIva"
                type="number"
                step="0.01"
                min="0"
                max="100"
                name="porcentajeIva"
                value={formData.porcentajeIva}
                onChange={handleChange}
                disabled={!esAdmin}
                required
                style={{
                  ...inputStyle,
                  opacity: esAdmin ? 1 : 0.75,
                  cursor: esAdmin ? 'text' : 'not-allowed',
                }}
              />
              <small style={{ color: 'var(--color-ink-soft)', fontSize: '0.75rem' }}>Ej: 19 para 19%</small>
            </div>
          </div>

          <div style={fieldContainer}>
            <label htmlFor="input-zonaHoraria" style={labelStyle}>
              Zona Horaria (IANA)
            </label>
            <input
              id="input-zonaHoraria"
              type="text"
              name="zonaHoraria"
              value={formData.zonaHoraria}
              onChange={handleChange}
              placeholder="America/Santiago"
              disabled={!esAdmin}
              style={{
                ...inputStyle,
                opacity: esAdmin ? 1 : 0.75,
                cursor: esAdmin ? 'text' : 'not-allowed',
              }}
            />
            <small style={{ color: 'var(--color-ink-soft)', fontSize: '0.75rem' }}>
              Identificador IANA oficial (ej: America/Santiago, America/Argentina/Buenos_Aires)
            </small>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '1.5rem', gap: '0.75rem' }}>
            <button
              type="button"
              onClick={() => tenantId && token && cargarConfig(tenantId, token)}
              disabled={saving}
              style={{
                backgroundColor: 'transparent',
                color: 'var(--color-ink)',
                border: '1px solid var(--color-line)',
                padding: '0.6rem 1.2rem',
                borderRadius: '8px',
                fontWeight: 600,
                cursor: saving ? 'not-allowed' : 'pointer',
              }}
            >
              Recargar
            </button>
            <button
              type="submit"
              disabled={saving || !esAdmin}
              title={!esAdmin ? 'Se requiere rol Administrador para guardar cambios' : ''}
              style={{
                backgroundColor: 'var(--color-primary)',
                color: 'var(--color-on-primary)',
                border: 'none',
                padding: '0.6rem 1.4rem',
                borderRadius: '8px',
                fontWeight: 600,
                cursor: saving || !esAdmin ? 'not-allowed' : 'pointer',
                opacity: saving || !esAdmin ? 0.5 : 1,
              }}
            >
              {saving ? 'Guardando...' : esAdmin ? 'Guardar Cambios' : 'Solo Administradores'}
            </button>
          </div>
        </form>
      )}
    </div>
  );
}
