import React, { useEffect, useState } from 'react';
import axios, { isAxiosError } from 'axios';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTenantStore } from '../../store/useTenantStore';

const IDENTITY_URL = 'https://auth-rpedraza.dev.censei.cl';

const configSchema = z.object({
  pais: z.string()
    .length(2, 'El código de país debe tener exactamente 2 letras (ej: CL)')
    .toUpperCase(),
  moneda: z.string()
    .length(3, 'La moneda debe tener exactamente 3 letras (ej: CLP)')
    .toUpperCase(),
  idioma: z.string()
    .min(2, 'Mínimo 2 letras')
    .max(5, 'Máximo 5 letras')
    .toLowerCase(),
  porcentajeIva: z.coerce.number({ invalid_type_error: 'Debe ser un número' })
    .min(0, 'El IVA no puede ser negativo')
    .max(100, 'El IVA máximo es 100'),
  zonaHoraria: z.string()
    .regex(/^(?:Africa|America|Antarctica|Arctic|Asia|Atlantic|Australia|Europe|Indian|Pacific|Etc|UTC)(?:\/[A-Za-z_]+)*$/, 'Debe ser un identificador IANA válido (ej. America/Santiago)')
});

export type TenantConfigFormData = z.infer<typeof configSchema>;

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

const errorStyle: React.CSSProperties = {
  color: 'var(--color-danger)',
  fontSize: '0.75rem',
  marginTop: '0.25rem',
  display: 'block',
};

export default function TenantConfig({ token: tokenProp }: TenantConfigProps) {
  const { setToken, tenantId, esAdmin, token } = useTenantStore();

  useEffect(() => {
    setToken(tokenProp);
  }, [tokenProp, setToken]);

  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string>('');
  const [successMsg, setSuccessMsg] = useState<string>('');

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<TenantConfigFormData>({
    resolver: zodResolver(configSchema),
    defaultValues: {
      pais: '',
      moneda: '',
      idioma: '',
      porcentajeIva: 19,
      zonaHoraria: '',
    },
  });

  const cargarConfig = async () => {
    if (!tenantId || !token) return;
    setLoading(true);
    setErrorMsg('');
    setSuccessMsg('');
    try {
      const res = await axios.get(`${IDENTITY_URL}/tenants/${tenantId}/config`, {
        headers: {
          Authorization: `Bearer ${token}`,
          'x-tenant-id': tenantId,
        },
      });
      reset({
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
      setErrorMsg(msg);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    cargarConfig();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tenantId, token]);

  const onSubmit = async (data: TenantConfigFormData) => {
    if (!token || !tenantId) {
      setErrorMsg('No hay sesión activa o identificador de tenant disponible.');
      return;
    }

    if (!esAdmin) {
      setErrorMsg('Solo los usuarios con rol Administrador pueden guardar cambios en la configuración.');
      return;
    }

    setErrorMsg('');
    setSuccessMsg('');

    try {
      const res = await axios.put(`${IDENTITY_URL}/tenants/${tenantId}/config`, data, {
        headers: {
          Authorization: `Bearer ${token}`,
          'x-tenant-id': tenantId,
        },
      });

      reset({
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
      setErrorMsg(msg);
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

      {errorMsg && (
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
          <b>Error:</b> {errorMsg}
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
        <form onSubmit={handleSubmit(onSubmit)}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <div style={fieldContainer}>
              <label htmlFor="input-pais" style={labelStyle}>
                País (código ISO)
              </label>
              <input
                id="input-pais"
                type="text"
                placeholder="CL"
                maxLength={2}
                disabled={!esAdmin || isSubmitting}
                style={{
                  ...inputStyle,
                  opacity: esAdmin ? 1 : 0.75,
                  cursor: esAdmin ? 'text' : 'not-allowed',
                  borderColor: errors.pais ? 'var(--color-danger)' : 'var(--color-line)',
                }}
                {...register('pais')}
              />
              {errors.pais && <span style={errorStyle}>{errors.pais.message}</span>}
              {!errors.pais && <small style={{ color: 'var(--color-ink-soft)', fontSize: '0.75rem' }}>Ej: CL, AR, US</small>}
            </div>

            <div style={fieldContainer}>
              <label htmlFor="input-moneda" style={labelStyle}>
                Moneda Base (ISO)
              </label>
              <input
                id="input-moneda"
                type="text"
                placeholder="CLP"
                maxLength={3}
                disabled={!esAdmin || isSubmitting}
                style={{
                  ...inputStyle,
                  opacity: esAdmin ? 1 : 0.75,
                  cursor: esAdmin ? 'text' : 'not-allowed',
                  borderColor: errors.moneda ? 'var(--color-danger)' : 'var(--color-line)',
                }}
                {...register('moneda')}
              />
              {errors.moneda && <span style={errorStyle}>{errors.moneda.message}</span>}
              {!errors.moneda && <small style={{ color: 'var(--color-ink-soft)', fontSize: '0.75rem' }}>Ej: CLP, ARS, USD</small>}
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
                placeholder="es"
                maxLength={5}
                disabled={!esAdmin || isSubmitting}
                style={{
                  ...inputStyle,
                  opacity: esAdmin ? 1 : 0.75,
                  cursor: esAdmin ? 'text' : 'not-allowed',
                  borderColor: errors.idioma ? 'var(--color-danger)' : 'var(--color-line)',
                }}
                {...register('idioma')}
              />
              {errors.idioma && <span style={errorStyle}>{errors.idioma.message}</span>}
              {!errors.idioma && <small style={{ color: 'var(--color-ink-soft)', fontSize: '0.75rem' }}>Ej: es, en</small>}
            </div>

            <div style={fieldContainer}>
              <label htmlFor="input-porcentajeIva" style={labelStyle}>
                Porcentaje IVA (%)
              </label>
              <input
                id="input-porcentajeIva"
                type="number"
                step="0.01"
                disabled={!esAdmin || isSubmitting}
                style={{
                  ...inputStyle,
                  opacity: esAdmin ? 1 : 0.75,
                  cursor: esAdmin ? 'text' : 'not-allowed',
                  borderColor: errors.porcentajeIva ? 'var(--color-danger)' : 'var(--color-line)',
                }}
                {...register('porcentajeIva')}
              />
              {errors.porcentajeIva && <span style={errorStyle}>{errors.porcentajeIva.message}</span>}
              {!errors.porcentajeIva && <small style={{ color: 'var(--color-ink-soft)', fontSize: '0.75rem' }}>Ej: 19 para 19%</small>}
            </div>
          </div>

          <div style={fieldContainer}>
            <label htmlFor="input-zonaHoraria" style={labelStyle}>
              Zona Horaria (IANA)
            </label>
            <input
              id="input-zonaHoraria"
              type="text"
              placeholder="America/Santiago"
              disabled={!esAdmin || isSubmitting}
              style={{
                ...inputStyle,
                opacity: esAdmin ? 1 : 0.75,
                cursor: esAdmin ? 'text' : 'not-allowed',
                borderColor: errors.zonaHoraria ? 'var(--color-danger)' : 'var(--color-line)',
              }}
              {...register('zonaHoraria')}
            />
            {errors.zonaHoraria && <span style={errorStyle}>{errors.zonaHoraria.message}</span>}
            {!errors.zonaHoraria && (
              <small style={{ color: 'var(--color-ink-soft)', fontSize: '0.75rem' }}>
                Identificador IANA oficial (ej: America/Santiago, America/Argentina/Buenos_Aires)
              </small>
            )}
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '1.5rem', gap: '0.75rem' }}>
            <button
              type="button"
              onClick={cargarConfig}
              disabled={isSubmitting}
              style={{
                backgroundColor: 'transparent',
                color: 'var(--color-ink)',
                border: '1px solid var(--color-line)',
                padding: '0.6rem 1.2rem',
                borderRadius: '8px',
                fontWeight: 600,
                cursor: isSubmitting ? 'not-allowed' : 'pointer',
              }}
            >
              Recargar
            </button>
            <button
              type="submit"
              disabled={isSubmitting || !esAdmin}
              title={!esAdmin ? 'Se requiere rol Administrador para guardar cambios' : ''}
              style={{
                backgroundColor: 'var(--color-primary)',
                color: 'var(--color-on-primary)',
                border: 'none',
                padding: '0.6rem 1.4rem',
                borderRadius: '8px',
                fontWeight: 600,
                cursor: isSubmitting || !esAdmin ? 'not-allowed' : 'pointer',
                opacity: isSubmitting || !esAdmin ? 0.5 : 1,
              }}
            >
              {isSubmitting ? 'Guardando...' : esAdmin ? 'Guardar Cambios' : 'Solo Administradores'}
            </button>
          </div>
        </form>
      )}
    </div>
  );
}
