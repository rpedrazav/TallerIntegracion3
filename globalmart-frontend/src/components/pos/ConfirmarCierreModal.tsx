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
      className="confirmar-cierre-dialog"
    >
      <div className="confirmar-cierre-content">
        <h3 id="confirmar-cierre-titulo" className="confirmar-cierre-title">
          Confirmar cierre de turno
        </h3>
        <p>
          Monto declarado: <strong className="num">${totalDeclarado.toLocaleString('es-CL')}</strong>
        </p>
        {cuadreSynced && cuadreResult && (
          <p>
            Diferencia del cuadre: <strong className="num">${cuadreResult.diferencia.toLocaleString('es-CL')}</strong>
          </p>
        )}
        <div className="confirmar-cierre-actions">
          <button
            type="button"
            autoFocus
            onClick={onCancelar}
            className="confirmar-cierre-btn-cancel"
          >
            Cancelar
          </button>
          <button
            type="button"
            onClick={onConfirmar}
            className="confirmar-cierre-btn-confirm"
          >
            Confirmar cierre
          </button>
        </div>
      </div>
    </dialog>
  );
}
