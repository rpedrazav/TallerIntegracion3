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
    <div
      style={{
        background: 'var(--color-bg)',
        border: '1px solid var(--color-line)',
        borderRadius: '12px',
        padding: '1.5rem',
        marginBottom: '2rem',
      }}
    >
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1rem',
          flexWrap: 'wrap',
          gap: '0.5rem',
        }}
      >
        <div>
          <h3 style={{ margin: 0, fontSize: '1.15rem', fontWeight: 700, color: 'var(--color-ink)' }}>
            Arqueo y Comparación de Cuadre
          </h3>
          <div style={{ fontSize: '0.85rem', color: 'var(--color-ink-soft)' }}>
            Verifica la diferencia entre el efectivo esperado por ventas y lo contado físicamente.
          </div>
        </div>

        <button
          type="button"
          onClick={onCalcularCuadre}
          disabled={isCalculatingCuadre || noTurnoOpen}
          style={{
            background: 'var(--color-primary)',
            color: 'var(--color-surface)',
            border: 'none',
            padding: '0.65rem 1.25rem',
            borderRadius: '8px',
            fontWeight: 700,
            fontSize: '0.9rem',
            cursor: isCalculatingCuadre || noTurnoOpen ? 'not-allowed' : 'pointer',
          }}
        >
          {isCalculatingCuadre ? 'Calculando...' : '🔍 Calcular Cuadre'}
        </button>
      </div>

      {efectivoEsperado !== null && (
        <div style={{ marginTop: '1rem' }}>
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
              gap: '1rem',
            }}
          >
            {/* Efectivo Esperado */}
            <div
              style={{
                background: 'var(--color-surface)',
                border: '1px solid var(--color-line)',
                borderRadius: '8px',
                padding: '1rem',
              }}
            >
              <div style={{ fontSize: '0.75rem', textTransform: 'uppercase', color: 'var(--color-ink-soft)', fontWeight: 600 }}>
                Efectivo Esperado (Ventas)
              </div>
              <div style={{ fontSize: '1.35rem', fontWeight: 800, color: 'var(--color-ink)', marginTop: '0.25rem' }}>
                ${efectivoEsperado.toLocaleString('es-CL')}
              </div>
              <div style={{ fontSize: '0.75rem', color: 'var(--color-ink-soft)', marginTop: '0.25rem' }}>
                Vía MS-5 (pagos efectivo completados)
              </div>
            </div>

            {/* Efectivo Declarado */}
            <div
              style={{
                background: 'var(--color-surface)',
                border: '1px solid var(--color-line)',
                borderRadius: '8px',
                padding: '1rem',
              }}
            >
              <div style={{ fontSize: '0.75rem', textTransform: 'uppercase', color: 'var(--color-ink-soft)', fontWeight: 600 }}>
                Efectivo Declarado (Gaveta)
              </div>
              <div style={{ fontSize: '1.35rem', fontWeight: 800, color: 'var(--color-primary)', marginTop: '0.25rem' }}>
                ${totalDeclarado.toLocaleString('es-CL')}
              </div>
              <div style={{ fontSize: '0.75rem', color: 'var(--color-ink-soft)', marginTop: '0.25rem' }}>
                Suma automática de billetes + monedas
              </div>
            </div>

            {/* Diferencia */}
            {(() => {
              const diff = diferenciaCalculada ?? 0;
              const isExact = diff === 0;
              const isSobrante = diff > 0;
              const hasDiscrepancy = !isExact;
              return (
                <div
                  style={{
                    background: isExact ? 'var(--color-success-bg)' : 'var(--color-danger-bg)',
                    border: `1px solid ${isExact ? 'var(--color-success)' : 'var(--color-danger)'}`,
                    borderRadius: '8px',
                    padding: '1rem',
                  }}
                >
                  <div
                    style={{
                      fontSize: '0.75rem',
                      textTransform: 'uppercase',
                      fontWeight: 700,
                      color: isExact ? 'var(--color-success)' : 'var(--color-danger)',
                    }}
                  >
                    {isExact
                      ? '✅ Diferencia (Cuadre Exacto)'
                      : isSobrante
                      ? '⚠️ Diferencia (Sobrante)'
                      : '⚠️ Diferencia (Faltante)'}
                  </div>
                  <div
                    style={{
                      fontSize: '1.5rem',
                      fontWeight: 800,
                      marginTop: '0.25rem',
                      color: isExact ? 'var(--color-success)' : 'var(--color-danger)',
                    }}
                  >
                    {isSobrante ? '+' : ''}${diff.toLocaleString('es-CL')}
                  </div>
                  <div style={{ fontSize: '0.75rem', marginTop: '0.25rem', color: isExact ? 'var(--color-success)' : 'var(--color-danger)' }}>
                    {isExact
                      ? 'La gaveta coincide exactamente con las ventas registradas'
                      : isSobrante
                      ? 'Hay más efectivo del esperado — revisar conteo'
                      : 'Falta efectivo respecto a las ventas — revisar conteo'}
                  </div>
                  {hasDiscrepancy && (
                    <div style={{ marginTop: '0.5rem', padding: '0.4rem 0.6rem', background: 'var(--color-danger-bg)', borderRadius: '6px', fontSize: '0.75rem', color: 'var(--color-danger)', fontWeight: 600 }}>
                      Discrepancia detectada: se recomienda revisar el conteo antes de cerrar el turno.
                    </div>
                  )}
                </div>
              );
            })()}
          </div>

          <div style={{ marginTop: '0.75rem', fontSize: '0.8rem', color: 'var(--color-ink-soft)', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            {cuadreSynced ? (
              <span style={{ color: 'var(--color-success)', fontWeight: 600 }}>
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
