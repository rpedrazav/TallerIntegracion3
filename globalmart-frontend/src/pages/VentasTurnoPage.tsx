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
      <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '1rem' }}>
        <thead><tr><th align="left">Venta</th><th align="left">Método</th><th align="right">Total</th><th align="left">Estado</th>{isAdmin && <th />}</tr></thead>
        <tbody>
          {ventas.map(v => (
            <tr key={v.id} style={{ borderTop: '1px solid var(--color-line)' }}>
              <td>{v.id.slice(0, 8)}</td>
              <td>{v.metodo_pago}</td>
              <td align="right">$ {v.total.toLocaleString('es-CL')}</td>
              <td>{v.estado}</td>
              {isAdmin && <td align="right">{v.estado === 'COMPLETADA' && <button type="button" style={buttonStyle} onClick={() => abrirAnulacion(v)}>Anular</button>}</td>}
            </tr>
          ))}
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
