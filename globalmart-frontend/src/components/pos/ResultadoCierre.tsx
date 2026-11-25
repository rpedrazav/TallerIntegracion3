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
    <div className="resultado-cierre" role="status">
      <div className="resultado-cierre-title">
        <span aria-hidden="true">✅</span>
        <span>{successMsg}</span>
      </div>
      <p className="resultado-cierre-text">
        El turno ha finalizado correctamente. La gaveta ha sido registrada con un monto total declarado de{' '}
        <strong className="num">${totalDeclarado.toLocaleString('es-CL')}</strong>.
      </p>
      <div className="resultado-cierre-actions">
        <button
          type="button"
          onClick={onAbrirTurno}
          className="resultado-cierre-btn-primary"
        >
          Abrir Nuevo Turno →
        </button>
        <button
          type="button"
          onClick={onVolverPos}
          className="resultado-cierre-btn-secondary"
        >
          Volver al POS
        </button>
      </div>
    </div>
  );
}
