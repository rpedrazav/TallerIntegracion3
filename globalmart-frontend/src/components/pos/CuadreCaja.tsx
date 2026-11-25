import React from 'react';
import { CuadreResult } from '../../api/client';

interface CuadreCajaProps {
  efectivoEsperado: number | null;
  totalDeclarado: number;
  diferenciaCalculada: number | null;
  isCalculatingCuadre: boolean;
  cuadreSynced: boolean;
  cuadreResult: CuadreResult | null;
  noTurnoOpen: boolean;
  onCalcularCuadre: () => void;
}

export default function CuadreCaja({
  efectivoEsperado,
  totalDeclarado,
  diferenciaCalculada,
  isCalculatingCuadre,
  cuadreSynced,
  cuadreResult,
  noTurnoOpen,
  onCalcularCuadre,
}: CuadreCajaProps) {
  return (
    <div className="cuadre-box">
      <div className="cuadre-header">
        <div>
          <h3 className="cuadre-title">Arqueo y Comparación de Cuadre</h3>
          <div className="cuadre-subtitle">
            Verifica la diferencia entre el efectivo esperado por ventas y lo contado físicamente.
          </div>
        </div>

        <button
          type="button"
          onClick={onCalcularCuadre}
          disabled={isCalculatingCuadre || noTurnoOpen}
          className="cuadre-btn"
        >
          {isCalculatingCuadre ? 'Calculando...' : '🔍 Calcular Cuadre'}
        </button>
      </div>

      {efectivoEsperado !== null && (
        <div className="cuadre-results">
          <div className="cuadre-grid">
            <div className="cuadre-metric">
              <div className="cuadre-metric-label">Efectivo Esperado (Ventas)</div>
              <div className="cuadre-metric-value">
                ${efectivoEsperado.toLocaleString('es-CL')}
              </div>
              <div className="cuadre-metric-note">
                Vía MS-5 (pagos efectivo completados; excluye ventas anuladas)
              </div>
            </div>

            <div className="cuadre-metric">
              <div className="cuadre-metric-label">Efectivo Declarado (Gaveta)</div>
              <div className="cuadre-metric-value cuadre-metric-value--primary">
                ${totalDeclarado.toLocaleString('es-CL')}
              </div>
              <div className="cuadre-metric-note">
                Suma automática de billetes + monedas
              </div>
            </div>

            {(() => {
              const diff = diferenciaCalculada ?? 0;
              const isExact = diff === 0;
              const isSobrante = diff > 0;
              return (
                <div className={`cuadre-metric ${isExact ? 'cuadre-metric--exact' : 'cuadre-metric--diff'}`}>
                  <div className="cuadre-metric-label">
                    {isExact
                      ? '✅ Diferencia (Cuadre Exacto)'
                      : isSobrante
                      ? '⚠️ Diferencia (Sobrante)'
                      : '⚠️ Diferencia (Faltante)'}
                  </div>
                  <div className="cuadre-metric-value">
                    {isSobrante ? '+' : ''}${diff.toLocaleString('es-CL')}
                  </div>
                  <div className="cuadre-metric-note">
                    {isExact
                      ? 'La gaveta coincide exactamente con las ventas registradas'
                      : isSobrante
                      ? 'Hay más efectivo del esperado — revisar conteo'
                      : 'Falta efectivo respecto a las ventas — revisar conteo'}
                  </div>
                  {!isExact && (
                    <div className="cuadre-discrepancy-note">
                      Discrepancia detectada: se recomienda revisar el conteo antes de cerrar el turno.
                    </div>
                  )}
                </div>
              );
            })()}
          </div>

          <div className="cuadre-sync-status">
            {cuadreSynced ? (
              <span className="cuadre-sync-ok">
                ✓ Cuadre verificado y sincronizado con el microservicio POS (MS-5)
                {cuadreResult?.turno_id ? ` (Turno: ${cuadreResult.turno_id.slice(0, 8)}...)` : ''}.
              </span>
            ) : (
              <span>
                ℹ️ Conteo actualizado localmente. Puedes presionar <strong>&ldquo;Calcular Cuadre&rdquo;</strong> para re-validar con el servidor.
              </span>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
