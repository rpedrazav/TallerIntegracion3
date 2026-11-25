import React, { useCallback, useEffect, useRef, useState } from 'react';
import { AxiosError } from 'axios';
import { posApi, TurnoActivo, VentaPendiente } from '../api/client';
import { useAuth } from '../hooks/useAuth';
import { parseJwt } from '../utils/jwt';
import { mensajeErrorAnulacion } from './erroresAnulacion';

const buttonStyle: React.CSSProperties = { padding: '0.5rem 1rem', borderRadius: 8, border: '1px solid var(--color-line)', background: 'transparent', color: 'var(--color-ink)', fontWeight: 600, cursor: 'pointer' };

// El backend exige rol ADMIN (SUPER_ADMIN recibiría 403), por eso se compara el rol exacto.
export default function VentasTurnoPage() {
  const { token } = useAuth();
  const isAdmin = parseJwt(token)?.active_role?.toUpperCase() === 'ADMIN';
  const [turnos, setTurnos] = useState<TurnoActivo[]>([]);
  const [turnoId, setTurnoId] = useState('');
  const [ventas, setVentas] = useState<VentaPendiente[]>([]);
  const [expandidas, setExpandidas] = useState<Record<string, boolean>>({});
  const [error, setError] = useState('');
  const [anulando, setAnulando] = useState<VentaPendiente | null>(null);
  const [motivo, setMotivo] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [errorAnulacion, setErrorAnulacion] = useState('');
  const solicitudEnCurso = useRef(false);
  const dialogRef = useRef<HTMLDialogElement>(null);

  // ADMIN elige entre los turnos abiertos del tenant; el CAJERO solo ve el suyo.
  useEffect(() => {
    if (!token) return;
    (isAdmin ? posApi.openShifts() : posApi.activeShift().then(t => [t]))
      .then(list => { setTurnos(list); setTurnoId(list[0]?.id ?? ''); })
      .catch(err => setError((err as AxiosError).response?.status === 404 ? 'No hay un turno abierto.' : 'No se pudieron cargar los turnos.'));
  }, [token, isAdmin]);

  const cargarVentas = useCallback(async () => {
    if (!turnoId) return;
    try {
      setVentas(await posApi.salesByShift(turnoId));
    } catch {
      setVentas([]);
      setError('No se pudo actualizar el estado de las ventas. Recarga la vista antes de continuar.');
    }
  }, [turnoId]);
  useEffect(() => { void cargarVentas(); }, [cargarVentas]);

  const abrirAnulacion = (venta: VentaPendiente) => {
    setAnulando(venta); setMotivo(''); setError(''); setErrorAnulacion('');
    dialogRef.current?.showModal();
  };

  const confirmarAnulacion = async () => {
    if (!anulando || !motivo.trim() || solicitudEnCurso.current) return;
    solicitudEnCurso.current = true;
    setEnviando(true);
    setErrorAnulacion('');
    setError('');
    try {
      await posApi.anularVenta(anulando.id, motivo.trim());
      dialogRef.current?.close();
    } catch (err) {
      setErrorAnulacion(mensajeErrorAnulacion(err));
    } finally {
      await cargarVentas();
      solicitudEnCurso.current = false;
      setEnviando(false);
    }
  };

  const toggleDetalle = (ventaId: string) => {
    setExpandidas(prev => ({ ...prev, [ventaId]: !prev[ventaId] }));
  };

  const completadas = ventas.filter(v => v.estado === 'COMPLETADA');
  const totalRecaudado = completadas.reduce((acc, v) => acc + v.total, 0);
  const totalEfectivo = completadas.filter(v => v.metodo_pago === 'EFECTIVO').reduce((acc, v) => acc + v.total, 0);
  const totalTarjeta = completadas.filter(v => v.metodo_pago === 'TARJETA').reduce((acc, v) => acc + v.total, 0);
  const totalColumnas = isAdmin ? 6 : 5;

  return (
    <section>
      <h2>Ventas del turno</h2>
      {errorAnulacion && <p role="alert" style={{ color: 'var(--color-danger-text)' }}>{errorAnulacion}</p>}
      {error && <div role="alert" style={{ color: 'var(--color-danger-text)', background: 'var(--color-danger-bg)', padding: '0.75rem', borderRadius: 8, marginBottom: '1rem' }}>{error}</div>}
      {isAdmin && turnos.length > 0 && (
        <label>Turno{' '}
          <select value={turnoId} onChange={e => setTurnoId(e.target.value)}>
            {turnos.map(t => <option key={t.id} value={t.id}>{new Date(t.abiertoAt).toLocaleString('es-CL')} · cajero {t.cajeroId.slice(0, 8)}</option>)}
          </select>
        </label>
      )}
      {ventas.length > 0 && (
        <div
          style={{
            display: 'flex',
            flexWrap: 'wrap',
            gap: '1rem',
            marginTop: '0.75rem',
            padding: '0.75rem 1rem',
            borderRadius: 8,
            border: '1px solid var(--color-line)',
            background: 'var(--color-surface)',
            fontSize: '0.9rem',
          }}
        >
          <span>Ventas completadas: <strong>{completadas.length}</strong> / {ventas.length}</span>
          <span>Efectivo: <strong>$ {totalEfectivo.toLocaleString('es-CL')}</strong></span>
          <span>Tarjeta: <strong>$ {totalTarjeta.toLocaleString('es-CL')}</strong></span>
          <span>Total recaudado: <strong>$ {totalRecaudado.toLocaleString('es-CL')}</strong></span>
        </div>
      )}
      <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '1rem' }}>
        <thead><tr><th align="left">Venta</th><th align="left">Método</th><th align="right">Total</th><th align="left">Estado</th><th align="right">Detalle</th>{isAdmin && <th />}</tr></thead>
        <tbody>
          {ventas.map(v => {
            const abierta = !!expandidas[v.id];
            const items = Array.isArray(v.items) ? v.items : [];
            return (
              <React.Fragment key={v.id}>
                <tr style={{ borderTop: '1px solid var(--color-line)' }}>
                  <td>{v.id.slice(0, 8)}</td>
                  <td>{v.metodo_pago}</td>
                  <td align="right">$ {v.total.toLocaleString('es-CL')}</td>
                  <td>
                    {v.estado === 'ANULADA' ? (
                      <span
                        data-testid="estado-anulada"
                        style={{
                          background: 'var(--color-danger-bg)',
                          color: 'var(--color-danger-text)',
                          padding: '0.22rem 0.65rem',
                          borderRadius: '9999px',
                          fontSize: '0.78rem',
                          fontWeight: 700,
                          whiteSpace: 'nowrap',
                          border: '1px solid var(--color-danger)',
                          display: 'inline-block',
                        }}
                      >
                        ANULADA
                      </span>
                    ) : (
                      v.estado
                    )}
                  </td>
                  <td align="right">
                    <button
                      type="button"
                      style={buttonStyle}
                      aria-expanded={abierta}
                      onClick={() => toggleDetalle(v.id)}
                    >
                      {abierta ? 'Ocultar detalle' : `Ver detalle (${items.length})`}
                    </button>
                  </td>
                  {isAdmin && <td align="right">{v.estado === 'COMPLETADA' && <button type="button" style={buttonStyle} onClick={() => abrirAnulacion(v)}>Anular</button>}</td>}
                </tr>
                {abierta && (
                  <tr style={{ background: 'var(--color-surface)' }}>
                    <td colSpan={totalColumnas} style={{ padding: '0.75rem 1rem', borderTop: '1px dashed var(--color-line)' }}>
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '1rem', fontSize: '0.85rem', marginBottom: '0.6rem', color: 'var(--color-ink-soft)' }}>
                        <span>ID: <code>{v.id}</code></span>
                        {v.created_at && <span>Fecha: <strong>{new Date(v.created_at).toLocaleString('es-CL')}</strong></span>}
                        <span>Subtotal: <strong>$ {v.subtotal.toLocaleString('es-CL')}</strong></span>
                        <span>IVA: <strong>$ {v.impuestos.toLocaleString('es-CL')}</strong></span>
                        <span>Total: <strong>$ {v.total.toLocaleString('es-CL')}</strong></span>
                      </div>
                      {items.length === 0 ? (
                        <p style={{ margin: 0, fontSize: '0.85rem', color: 'var(--color-ink-soft)' }}>
                          Sin detalle de productos registrado para esta venta.
                        </p>
                      ) : (
                        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.875rem' }}>
                          <thead>
                            <tr style={{ borderBottom: '1px solid var(--color-line)' }}>
                              <th align="left" style={{ padding: '0.35rem 0.25rem' }}>Producto</th>
                              <th align="right" style={{ padding: '0.35rem 0.25rem' }}>Cantidad / Peso</th>
                              <th align="right" style={{ padding: '0.35rem 0.25rem' }}>Precio unit.</th>
                              <th align="right" style={{ padding: '0.35rem 0.25rem' }}>Subtotal</th>
                            </tr>
                          </thead>
                          <tbody>
                            {items.map(item => (
                              <tr key={item.id} style={{ borderBottom: '1px dotted var(--color-line)' }}>
                                <td style={{ padding: '0.35rem 0.25rem' }}>{item.nombre_producto || item.producto_id}</td>
                                <td align="right" style={{ padding: '0.35rem 0.25rem' }}>
                                  {item.peso_kg != null ? `${item.peso_kg.toFixed(3)} kg` : `${item.cantidad} un.`}
                                </td>
                                <td align="right" style={{ padding: '0.35rem 0.25rem' }}>$ {item.precio_unitario.toLocaleString('es-CL')}</td>
                                <td align="right" style={{ padding: '0.35rem 0.25rem' }}>$ {item.subtotal.toLocaleString('es-CL')}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      )}
                    </td>
                  </tr>
                )}
              </React.Fragment>
            );
          })}
        </tbody>
      </table>
      {ventas.length === 0 && turnoId && !error && <p>Este turno aún no tiene ventas.</p>}

      <dialog ref={dialogRef} aria-labelledby="anular-titulo" onCancel={e => { if (solicitudEnCurso.current) e.preventDefault(); }} style={{ border: '1px solid var(--color-line)', borderRadius: 12, background: 'var(--color-surface)', color: 'var(--color-ink)' }}>
        <div style={{ padding: '1.5rem', maxWidth: 420 }}>
          <h3 id="anular-titulo" style={{ marginTop: 0 }}>Anular venta {anulando?.id.slice(0, 8)}</h3>
          {errorAnulacion && <p role="alert" style={{ color: 'var(--color-danger-text)' }}>{errorAnulacion}</p>}
          {error && <p role="alert">{error}</p>}
          <label>Motivo (obligatorio)
            <textarea value={motivo} onChange={e => setMotivo(e.target.value)} maxLength={500} rows={3} style={{ width: '100%', display: 'block' }} />
          </label>
          <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end', marginTop: '1rem' }}>
            <button type="button" style={buttonStyle} disabled={enviando} onClick={() => dialogRef.current?.close()}>Cerrar</button>
            <button type="button" style={{ ...buttonStyle, background: 'var(--color-danger)', color: 'var(--color-surface)', border: 'none' }} disabled={!motivo.trim() || enviando || !!errorAnulacion || !!error} onClick={confirmarAnulacion}>{enviando ? 'Anulando…' : 'Confirmar anulación'}</button>
          </div>
        </div>
      </dialog>
    </section>
  );
}
