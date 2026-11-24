import React from 'react';

interface ResultadoCierreProps {
  successMsg: string;
  totalDeclarado: number;
  onAbrirTurno: () => void;
  onVolverPos: () => void;
}

export default function ResultadoCierre({
  successMsg,
  totalDeclarado,
  onAbrirTurno,
  onVolverPos,
}: ResultadoCierreProps) {
  return (
    <div
      style={{
        backgroundColor: 'var(--color-success-bg)',
        border: '1px solid var(--color-success)',
        color: 'var(--color-success)',
        padding: '1.25rem',
        borderRadius: '10px',
        fontSize: '0.95rem',
        marginBottom: '1.5rem',
        display: 'flex',
        flexDirection: 'column',
        gap: '0.75rem',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 700, fontSize: '1.05rem' }}>
        <span>✅</span>
        <span>{successMsg}</span>
      </div>
      <p style={{ margin: 0 }}>
        El turno ha finalizado correctamente. La gaveta ha sido registrada con un monto total declarado de{' '}
        <strong>${totalDeclarado.toLocaleString('es-CL')}</strong>.
      </p>
      <div style={{ display: 'flex', gap: '1rem', marginTop: '0.5rem' }}>
        <button
          type="button"
          onClick={onAbrirTurno}
          style={{
            backgroundColor: 'var(--color-success)',
            color: 'var(--color-surface)',
            border: 'none',
            padding: '0.6rem 1.2rem',
            borderRadius: '8px',
            fontWeight: 600,
            cursor: 'pointer',
          }}
        >
          Abrir Nuevo Turno →
        </button>
        <button
          type="button"
          onClick={onVolverPos}
          style={{
            backgroundColor: 'var(--color-surface)',
            color: 'var(--color-success)',
            border: '1px solid var(--color-success)',
            padding: '0.6rem 1.2rem',
            borderRadius: '8px',
            fontWeight: 600,
            cursor: 'pointer',
          }}
        >
          Volver al POS
        </button>
      </div>
    </div>
  );
}
