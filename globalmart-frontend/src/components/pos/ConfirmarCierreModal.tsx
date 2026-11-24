import React from 'react';
import { CuadreResult } from '../../api/client';

interface ConfirmarCierreModalProps {
  dialogRef: React.RefObject<HTMLDialogElement | null>;
  totalDeclarado: number;
  cuadreSynced: boolean;
  cuadreResult: CuadreResult | null;
  onConfirmar: () => void;
  onCancelar: () => void;
}

export default function ConfirmarCierreModal({
  dialogRef,
  totalDeclarado,
  cuadreSynced,
  cuadreResult,
  onConfirmar,
  onCancelar,
}: ConfirmarCierreModalProps) {
  return (
    <dialog
      ref={dialogRef}
      aria-modal="true"
      aria-labelledby="confirmar-cierre-titulo"
      onClick={(e) => {
        if (e.target === e.currentTarget) onCancelar();
      }}
      style={{
        padding: 0,
        border: '1px solid var(--color-line)',
        borderRadius: '12px',
        background: 'var(--color-surface)',
        color: 'var(--color-ink)',
      }}
    >
      <div style={{ padding: '1.5rem', maxWidth: '420px' }}>
        <h3 id="confirmar-cierre-titulo" style={{ marginTop: 0 }}>
          Confirmar cierre de turno
        </h3>
        <p>
          Monto declarado: <strong>${totalDeclarado.toLocaleString('es-CL')}</strong>
        </p>
        {cuadreSynced && cuadreResult && (
          <p>
            Diferencia del cuadre: <strong>${cuadreResult.diferencia.toLocaleString('es-CL')}</strong>
          </p>
        )}
        <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end' }}>
          <button
            type="button"
            autoFocus
            onClick={onCancelar}
            style={{
              padding: '0.6rem 1.1rem',
              borderRadius: '8px',
              border: '1px solid var(--color-line)',
              background: 'transparent',
              color: 'var(--color-ink)',
              fontWeight: 600,
              cursor: 'pointer',
            }}
          >
            Cancelar
          </button>
          <button
            type="button"
            onClick={onConfirmar}
            style={{
              padding: '0.6rem 1.1rem',
              borderRadius: '8px',
              border: 'none',
              background: 'var(--color-danger)',
              color: 'var(--color-surface)',
              fontWeight: 700,
              cursor: 'pointer',
            }}
          >
            Confirmar cierre
          </button>
        </div>
      </div>
    </dialog>
  );
}
