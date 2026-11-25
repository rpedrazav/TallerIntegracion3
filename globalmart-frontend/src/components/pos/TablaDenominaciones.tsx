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
      <div className="denom-header">
        <div>
          <h2 className="denom-title">Desglose de Efectivo en Gaveta</h2>
          <span className="denom-subtitle">
            Indica la cantidad física de cada billete y moneda encontrada.
          </span>
        </div>
        <button
          type="button"
          onClick={onReset}
          className="denom-reset-btn"
          title="Reiniciar todos los contadores a 0"
        >
          🔄 Limpiar Conteo
        </button>
      </div>

      <div className="denom-table-wrap">
        <table className="denom-table">
          <thead>
            <tr className="denom-thead-row">
              <th className="denom-th">Tipo</th>
              <th className="denom-th">Denominación</th>
              <th className="denom-th denom-th--center">Cantidad</th>
              <th className="denom-th denom-th--right">Subtotal</th>
            </tr>
          </thead>
          <tbody>
            {DENOMINACIONES.map((den, index) => {
              const qty = cantidades[den.id] || 0;
              const subtotal = den.valor * qty;

              return (
                <tr
                  key={den.id}
                  className={`denom-row ${index % 2 === 0 ? 'denom-row--even' : 'denom-row--odd'}`}
                >
                  <td className="denom-td">
                    <span className={`denom-badge denom-badge--${den.tipo}`}>
                      <span aria-hidden="true">{den.tipo === 'billete' ? '💵' : '🪙'}</span>
                      <span>{den.tipo}</span>
                    </span>
                  </td>

                  <td className="denom-td denom-td--label">{den.label}</td>

                  <td className="denom-td denom-td--center">
                    <div className="denom-qty-controls">
                      <button
                        type="button"
                        onClick={() => onStepChange(den.id, -1)}
                        className="denom-step-btn"
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
                        className={`denom-qty-input${qty > 0 ? ' denom-qty-input--active' : ''}`}
                      />

                      <button
                        type="button"
                        onClick={() => onStepChange(den.id, 1)}
                        className="denom-step-btn"
                      >
                        +
                      </button>
                    </div>
                  </td>

                  <td className={`denom-td denom-td--subtotal${subtotal > 0 ? ' denom-td--subtotal-active' : ''}`}>
                    ${subtotal.toLocaleString('es-CL')}
                  </td>
                </tr>
              );
            })}
          </tbody>
          <tfoot>
            <tr className="denom-tfoot-row denom-tfoot-row--divider">
              <td colSpan={2} className="denom-tfoot-label">
                💵 Subtotal Billetes:
              </td>
              <td colSpan={2} className="denom-tfoot-value">
                ${totalBilletes.toLocaleString('es-CL')}
              </td>
            </tr>
            <tr className="denom-tfoot-row">
              <td colSpan={2} className="denom-tfoot-label">
                🪙 Subtotal Monedas:
              </td>
              <td colSpan={2} className="denom-tfoot-value">
                ${totalMonedas.toLocaleString('es-CL')}
              </td>
            </tr>
            <tr className="denom-tfoot-total-row">
              <td colSpan={2} className="denom-tfoot-total-label">
                TOTAL DECLARADO EN CAJA:
              </td>
              <td colSpan={2} className="denom-tfoot-total-value">
                ${totalDeclarado.toLocaleString('es-CL')}
              </td>
            </tr>
          </tfoot>
        </table>
      </div>
    </>
  );
}
