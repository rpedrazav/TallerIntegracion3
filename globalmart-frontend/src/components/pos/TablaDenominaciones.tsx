import React from 'react';

export interface DenominacionConfig {
  id: string;
  valor: number;
  label: string;
  tipo: 'billete' | 'moneda';
}

export const DENOMINACIONES: DenominacionConfig[] = [
  { id: 'b_20000', valor: 20000, label: '$20.000', tipo: 'billete' },
  { id: 'b_10000', valor: 10000, label: '$10.000', tipo: 'billete' },
  { id: 'b_5000',  valor: 5000,  label: '$5.000',  tipo: 'billete' },
  { id: 'b_2000',  valor: 2000,  label: '$2.000',  tipo: 'billete' },
  { id: 'b_1000',  valor: 1000,  label: '$1.000',  tipo: 'billete' },
  { id: 'm_500',   valor: 500,   label: '$500',    tipo: 'moneda' },
  { id: 'm_100',   valor: 100,   label: '$100',    tipo: 'moneda' },
  { id: 'm_50',    valor: 50,    label: '$50',     tipo: 'moneda' },
  { id: 'm_10',    valor: 10,    label: '$10',     tipo: 'moneda' },
];

interface TablaDenominacionesProps {
  cantidades: Record<string, number>;
  onCantidadChange: (id: string, value: string) => void;
  onStepChange: (id: string, delta: number) => void;
  onReset: () => void;
  totalBilletes: number;
  totalMonedas: number;
  totalDeclarado: number;
}

export default function TablaDenominaciones({
  cantidades,
  onCantidadChange,
  onStepChange,
  onReset,
  totalBilletes,
  totalMonedas,
  totalDeclarado,
}: TablaDenominacionesProps) {
  return (
    <>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1.25rem',
          flexWrap: 'wrap',
          gap: '1rem',
        }}
      >
        <div>
          <h2 style={{ margin: 0, fontSize: '1.25rem', fontWeight: 700, color: 'var(--color-ink)' }}>
            Desglose de Efectivo en Gaveta
          </h2>
          <span style={{ fontSize: '0.85rem', color: 'var(--color-ink-soft)' }}>
            Indica la cantidad física de cada billete y moneda encontrada.
          </span>
        </div>
        <button
          type="button"
          onClick={onReset}
          style={{
            background: 'var(--color-bg)',
            color: 'var(--color-ink-soft)',
            border: '1px solid var(--color-line)',
            padding: '0.5rem 1rem',
            borderRadius: '8px',
            fontSize: '0.85rem',
            fontWeight: 600,
            cursor: 'pointer',
          }}
          title="Reiniciar todos los contadores a 0"
        >
          🔄 Limpiar Conteo
        </button>
      </div>

      <div
        style={{
          overflowX: 'auto',
          border: '1px solid var(--color-line)',
          borderRadius: '12px',
          marginBottom: '2rem',
        }}
      >
        <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
          <thead>
            <tr style={{ background: 'var(--color-bg)', borderBottom: '2px solid var(--color-line)', color: 'var(--color-ink-soft)' }}>
              <th style={{ padding: '0.875rem 1.25rem', fontSize: '0.85rem', fontWeight: 700, textTransform: 'uppercase' }}>
                Tipo
              </th>
              <th style={{ padding: '0.875rem 1.25rem', fontSize: '0.85rem', fontWeight: 700, textTransform: 'uppercase' }}>
                Denominación
              </th>
              <th
                style={{
                  padding: '0.875rem 1.25rem',
                  fontSize: '0.85rem',
                  fontWeight: 700,
                  textTransform: 'uppercase',
                  width: '240px',
                  textAlign: 'center',
                }}
              >
                Cantidad
              </th>
              <th
                style={{
                  padding: '0.875rem 1.25rem',
                  fontSize: '0.85rem',
                  fontWeight: 700,
                  textTransform: 'uppercase',
                  textAlign: 'right',
                }}
              >
                Subtotal
              </th>
            </tr>
          </thead>
          <tbody>
            {DENOMINACIONES.map((den, index) => {
              const qty = cantidades[den.id] || 0;
              const subtotal = den.valor * qty;
              const isEven = index % 2 === 0;

              return (
                <tr
                  key={den.id}
                  style={{
                    backgroundColor: isEven ? 'var(--color-surface)' : 'var(--color-bg)',
                    borderBottom: '1px solid var(--color-bg)',
                  }}
                >
                  <td style={{ padding: '0.875rem 1.25rem' }}>
                    <span
                      style={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: '0.4rem',
                        padding: '0.25rem 0.6rem',
                        borderRadius: '9999px',
                        fontSize: '0.75rem',
                        fontWeight: 700,
                        background: den.tipo === 'billete' ? 'var(--color-primary-bg)' : 'var(--color-warning-bg)',
                        color: den.tipo === 'billete' ? 'var(--color-primary)' : 'var(--color-warning-text)',
                      }}
                    >
                      <span>{den.tipo === 'billete' ? '💵' : '🪙'}</span>
                      <span style={{ textTransform: 'capitalize' }}>{den.tipo}</span>
                    </span>
                  </td>

                  <td style={{ padding: '0.875rem 1.25rem', fontWeight: 700, fontSize: '1.05rem', color: 'var(--color-ink)' }}>
                    {den.label}
                  </td>

                  <td style={{ padding: '0.875rem 1.25rem', textAlign: 'center' }}>
                    <div style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}>
                      <button
                        type="button"
                        onClick={() => onStepChange(den.id, -1)}
                        style={{
                          width: '32px',
                          height: '34px',
                          borderRadius: '6px',
                          border: '1px solid var(--color-line)',
                          background: 'var(--color-bg)',
                          fontWeight: 'bold',
                          fontSize: '1rem',
                          color: 'var(--color-ink)',
                          cursor: 'pointer',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                        }}
                      >
                        -
                      </button>

                      <input
                        id={`input-denominacion-${den.id}`}
                        type="number"
                        min="0"
                        step="1"
                        value={qty === 0 ? '' : qty}
                        onChange={(e) => onCantidadChange(den.id, e.target.value)}
                        placeholder="0"
                        style={{
                          width: '90px',
                          padding: '0.45rem 0.5rem',
                          textAlign: 'center',
                          fontSize: '1rem',
                          fontWeight: 700,
                          borderRadius: '6px',
                          border: '1px solid var(--color-line)',
                          outline: 'none',
                          color: 'var(--color-ink)',
                          backgroundColor: qty > 0 ? 'var(--color-success-bg)' : 'var(--color-surface)',
                          borderColor: qty > 0 ? 'var(--color-success)' : 'var(--color-line)',
                        }}
                      />

                      <button
                        type="button"
                        onClick={() => onStepChange(den.id, 1)}
                        style={{
                          width: '32px',
                          height: '34px',
                          borderRadius: '6px',
                          border: '1px solid var(--color-line)',
                          background: 'var(--color-bg)',
                          fontWeight: 'bold',
                          fontSize: '1rem',
                          color: 'var(--color-ink)',
                          cursor: 'pointer',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                        }}
                      >
                        +
                      </button>
                    </div>
                  </td>

                  <td style={{ padding: '0.875rem 1.25rem', textAlign: 'right', fontWeight: 700, color: subtotal > 0 ? 'var(--color-primary)' : 'var(--color-muted)' }}>
                    ${subtotal.toLocaleString('es-CL')}
                  </td>
                </tr>
              );
            })}
          </tbody>
          <tfoot>
            <tr style={{ background: 'var(--color-bg)', borderTop: '2px solid var(--color-line)' }}>
              <td colSpan={2} style={{ padding: '0.75rem 1.25rem', fontWeight: 600, color: 'var(--color-ink-soft)' }}>
                💵 Subtotal Billetes:
              </td>
              <td colSpan={2} style={{ padding: '0.75rem 1.25rem', textAlign: 'right', fontWeight: 700, color: 'var(--color-ink)' }}>
                ${totalBilletes.toLocaleString('es-CL')}
              </td>
            </tr>
            <tr style={{ background: 'var(--color-bg)' }}>
              <td colSpan={2} style={{ padding: '0.75rem 1.25rem', fontWeight: 600, color: 'var(--color-ink-soft)' }}>
                🪙 Subtotal Monedas:
              </td>
              <td colSpan={2} style={{ padding: '0.75rem 1.25rem', textAlign: 'right', fontWeight: 700, color: 'var(--color-ink)' }}>
                ${totalMonedas.toLocaleString('es-CL')}
              </td>
            </tr>
            <tr style={{ background: 'var(--color-primary-bg)', borderTop: '2px solid var(--color-primary)' }}>
              <td colSpan={2} style={{ padding: '1rem 1.25rem', fontWeight: 800, fontSize: '1.15rem', color: 'var(--color-primary)' }}>
                TOTAL DECLARADO EN CAJA:
              </td>
              <td colSpan={2} style={{ padding: '1rem 1.25rem', textAlign: 'right', fontWeight: 800, fontSize: '1.45rem', color: 'var(--color-primary)' }}>
                ${totalDeclarado.toLocaleString('es-CL')}
              </td>
            </tr>
          </tfoot>
        </table>
      </div>
    </>
  );
}
