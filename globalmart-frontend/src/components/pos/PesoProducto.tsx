import React from 'react';

export type PesoEstado = 'leyendo' | 'estable' | 'error';

interface PesoProductoProps {
  nombre: string;
  estado: PesoEstado;
  origen: 'simulado' | 'manual';
  pesoKg?: number;
  error?: string;
  onConfirmar?: () => void;
  onReintentar?: () => void;
  onManual?: () => void;
  onCancelar: () => void;
}

export default function PesoProducto({ nombre, estado, origen, pesoKg, error, onConfirmar, onReintentar, onManual, onCancelar }: PesoProductoProps) {
  return (
    <section aria-label={`Captura de peso de ${nombre}`} style={{ border: '1px solid var(--color-line)', borderRadius: 8, padding: '0.75rem', marginTop: '0.75rem', background: 'var(--color-bg)' }}>
      <strong>{nombre}</strong>
      <p style={{ margin: '0.5rem 0' }}>Origen: {origen === 'manual' ? 'Manual' : 'Balanza simulada'} · Unidad: kg</p>
      <p role="status" style={{ margin: '0.5rem 0' }}>
        {estado === 'leyendo' ? 'Esperando peso estable...' : estado === 'estable' ? `Peso estable: ${pesoKg?.toFixed(3)} kg` : 'No se obtuvo un peso válido.'}
      </p>
      {error && <p role="alert" style={{ color: 'var(--color-danger)' }}>{error}</p>}
      <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
        {estado === 'estable' && <button type="button" onClick={onConfirmar}>Confirmar peso</button>}
        {estado === 'error' && onReintentar && <button type="button" onClick={onReintentar}>Reintentar lectura</button>}
        {estado === 'error' && onManual && <button type="button" onClick={onManual}>Ingresar peso manualmente</button>}
        <button type="button" onClick={onCancelar}>Cancelar</button>
      </div>
    </section>
  );
}
