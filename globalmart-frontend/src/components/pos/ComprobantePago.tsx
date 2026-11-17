import React from 'react';
import type { VentaPendiente } from '../../api/client';
import { buildSafeReceiptData } from './receiptData';
import './ComprobantePago.css';

export interface ComprobantePagoProps {
  venta: VentaPendiente;
  reference?: string | null;
  change?: number | null;
  cashReceived?: number | null;
  onPrint?: () => void;
}

const money = (value: number) => `$ ${value.toLocaleString('es-CL')}`;

export default function ComprobantePago({
  venta,
  reference,
  change,
  cashReceived,
  onPrint,
}: ComprobantePagoProps) {
  const receipt = buildSafeReceiptData(venta, { reference, change, cashReceived });

  const handlePrint = () => {
    if (onPrint) {
      onPrint();
    } else if (typeof window !== 'undefined' && typeof window.print === 'function') {
      window.print();
    }
  };

  const fechaVenta = receipt.fechaIso
    ? new Date(receipt.fechaIso).toLocaleString('es-CL')
    : new Date().toLocaleString('es-CL');

  return (
    <div className="comprobante-wrapper" aria-label="Comprobante de pago seguro">
      <div className="comprobante-ticket">
        {/* Rótulo destacado como simulado (S2-108) */}
        <div className="comprobante-ticket__header">
          <p className="comprobante-ticket__title">GLOBALMART OS</p>
          <div className="comprobante-ticket__simulated-badge" role="status">
            {receipt.rotuloSimulado}
          </div>
          <div className="comprobante-ticket__dte-notice">
            {receipt.avisoNoTributario}
          </div>
        </div>

        {/* Metadatos autoritativos de la venta (GET /ventas/{id}) */}
        <div className="comprobante-ticket__meta">
          <div className="comprobante-ticket__row">
            <span>Venta:</span>
            <strong>#{receipt.ventaCodigoCorto}</strong>
          </div>
          <div className="comprobante-ticket__row">
            <span>Fecha:</span>
            <span>{fechaVenta}</span>
          </div>
          <div className="comprobante-ticket__row">
            <span>Estado MS-5:</span>
            <strong>{receipt.estado}</strong>
          </div>
        </div>

        {/* Detalle de ítems de la venta */}
        {receipt.items.length > 0 && (
          <table className="comprobante-ticket__items-table" aria-label="Detalle de productos">
            <thead>
              <tr>
                <th>Ítem</th>
                <th style={{ textAlign: 'center' }}>Cant</th>
                <th style={{ textAlign: 'right' }}>Total</th>
              </tr>
            </thead>
            <tbody>
              {receipt.items.map((item) => (
                <tr key={item.id}>
                  <td>
                    {item.nombre}
                    {item.pesoKg != null && (
                      <span style={{ fontSize: '0.7rem', display: 'block', color: 'var(--color-ink-soft)' }}>
                        ({item.pesoKg.toFixed(3)} kg)
                      </span>
                    )}
                  </td>
                  <td style={{ textAlign: 'center' }}>
                    {item.pesoKg != null ? `${item.pesoKg.toFixed(3)}kg` : item.cantidad}
                  </td>
                  <td style={{ textAlign: 'right' }}>{money(item.subtotal)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        {/* Montos autoritativos de MS-5 */}
        <div className="comprobante-ticket__totals">
          <div className="comprobante-ticket__row">
            <span>Subtotal (MS-5):</span>
            <span>{money(receipt.subtotal)}</span>
          </div>
          <div className="comprobante-ticket__row">
            <span>Impuestos (MS-5):</span>
            <span>{money(receipt.impuestos)}</span>
          </div>
          <div className="comprobante-ticket__row comprobante-ticket__total-final">
            <span>TOTAL:</span>
            <strong>{money(receipt.total)}</strong>
          </div>
        </div>

        {/* Información de pago seguro sin datos sensibles (S2-108) */}
        <div className="comprobante-ticket__payment-info">
          <div className="comprobante-ticket__row">
            <span>Método de pago:</span>
            <strong>{receipt.metodoPago}</strong>
          </div>

          {receipt.esTarjeta ? (
            <>
              {receipt.referenciaOpaca && (
                <div className="comprobante-ticket__row">
                  <span>Referencia opaca:</span>
                  <code>{receipt.referenciaOpaca}</code>
                </div>
              )}
              <div className="comprobante-ticket__row" style={{ fontSize: '0.75rem', color: 'var(--color-ink-soft)' }}>
                <span>Seguridad:</span>
                <span>Sin datos sensibles almacenados (PCI-DSS)</span>
              </div>
            </>
          ) : (
            <>
              {receipt.montoRecibido != null && (
                <div className="comprobante-ticket__row">
                  <span>Monto recibido:</span>
                  <span>{money(receipt.montoRecibido)}</span>
                </div>
              )}
              {receipt.vuelto != null && (
                <div className="comprobante-ticket__row">
                  <span>Vuelto MS-5:</span>
                  <span>{money(receipt.vuelto)}</span>
                </div>
              )}
            </>
          )}

          {/* Sin inventar folio DTE (S2-108) */}
          <div className="comprobante-ticket__row" style={{ fontSize: '0.75rem', color: 'var(--color-ink-soft)' }}>
            <span>Folio DTE:</span>
            <span>{receipt.folioDte}</span>
          </div>
        </div>

        {/* Pie de comprobante */}
        <div className="comprobante-ticket__footer">
          <p style={{ margin: 0 }}>Montos coincidentes con MS-5 · Sin captura de datos sensibles</p>
        </div>
      </div>

      <div className="comprobante-ticket__actions">
        <button
          type="button"
          className="comprobante-ticket__print-btn"
          onClick={handlePrint}
          aria-label="Imprimir comprobante"
        >
          Imprimir comprobante
        </button>
      </div>
    </div>
  );
}
