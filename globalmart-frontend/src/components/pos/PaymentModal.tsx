import React, { useEffect, useRef, useState } from 'react';
import type { TerminalState } from '../../payments/terminalStateMachine';
import './PaymentModal.css';

export type PaymentMethod = 'cash' | 'card';
export type PaymentState = TerminalState;

interface PaymentModalProps {
  total: number;
  saleMethod: PaymentMethod;
  state: PaymentState;
  reference?: string | null;
  change?: number | null;
  errorMessage?: string;
  shortfall?: number | null;
  terminalConnected?: boolean;
  onClose: () => void;
  onConfirm?: (method: PaymentMethod, cashReceived?: number) => void;
  onChangeMethod?: (method: PaymentMethod) => void;
  onCashInputChange?: () => void;
  onVerify?: () => void;
  onRetry?: () => void;
  onCancelAttempt?: () => void;
}

const money = (value: number) => `$ ${value.toLocaleString('es-CL')}`;

const stateText: Record<Exclude<PaymentState, 'available'>, { title: string; detail: string; symbol: string }> = {
  processing: {
    title: 'Procesando cobro',
    detail: 'Esperando la respuesta. Mantén esta ventana abierta y evita iniciar otro cobro.',
    symbol: '…',
  },
  approved: {
    title: 'Cobro aprobado',
    detail: 'La venta fue confirmada. Ya puedes continuar con la siguiente venta.',
    symbol: '✓',
  },
  declined: {
    title: 'Cobro rechazado',
    detail: 'La venta sigue pendiente. Revisa el motivo antes de decidir el siguiente paso.',
    symbol: '×',
  },
  uncertain: {
    title: 'Resultado incierto',
    detail: 'No se pudo confirmar el resultado. Verifica el estado de la venta antes de intentar cobrar de nuevo.',
    symbol: '?',
  },
  cancelled: {
    title: 'Intento cancelado',
    detail: 'El intento de cobro fue cancelado.',
    symbol: '—',
  },
};

export default function PaymentModal({
  total,
  saleMethod,
  state,
  reference,
  change,
  errorMessage,
  shortfall,
  terminalConnected = true,
  onClose,
  onConfirm,
  onChangeMethod,
  onCashInputChange,
  onVerify,
  onRetry,
  onCancelAttempt,
}: PaymentModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [method, setMethod] = useState<PaymentMethod>(saleMethod);
  const [cashInput, setCashInput] = useState('');
  const cashReceived = Number(cashInput);
  const cashIsValid = cashInput.trim() !== '' && Number.isFinite(cashReceived) && cashReceived > 0;
  const methodMatchesSale = method === saleMethod;
  const canConfirm = state === 'available' && methodMatchesSale && !!onConfirm && (method === 'card' || cashIsValid);

  useEffect(() => {
    const dialog = dialogRef.current;
    dialog?.showModal();
    return () => { if (dialog?.open) dialog.close(); };
  }, []);

  const handleCancel = (event: React.SyntheticEvent<HTMLDialogElement>) => {
    event.preventDefault();
    if (state !== 'processing') onClose();
  };

  return (
    <dialog ref={dialogRef} className="payment-modal" aria-labelledby="payment-modal-title" aria-describedby="payment-modal-description" onCancel={handleCancel}>
      <div className="payment-modal__header">
        <div>
          <p className="payment-modal__eyebrow">Punto de venta · Cobro</p>
          <h2 id="payment-modal-title">{state === 'available' ? 'Confirmar cobro' : stateText[state].title}</h2>
        </div>
        {state !== 'processing' && <button type="button" className="payment-modal__close" onClick={onClose} aria-label="Cerrar cobro">×</button>}
      </div>

      <div className="payment-modal__total">
        <span>Total de la venta</span>
        <strong>{money(total)}</strong>
      </div>

      {state === 'available' ? (
        <>
          <p id="payment-modal-description" className="payment-modal__description">Elige cómo se realizará el cobro.</p>
          <div className="payment-modal__methods" role="group" aria-label="Método de pago">
            <button type="button" className={method === 'cash' ? 'is-selected' : ''} aria-pressed={method === 'cash'} onClick={() => setMethod('cash')}>Efectivo</button>
            <button type="button" className={method === 'card' ? 'is-selected' : ''} aria-pressed={method === 'card'} onClick={() => setMethod('card')}>Tarjeta</button>
          </div>

          {method === 'cash' ? (
            <div className="payment-modal__fields">
              <label htmlFor="cash-received">Monto recibido</label>
              <input id="cash-received" type="number" min="0.01" step="0.01" inputMode="decimal" value={cashInput} onChange={(event) => { setCashInput(event.target.value); onCashInputChange?.(); }} placeholder="Ingresa el efectivo recibido" />
              {cashIsValid && <p>{cashReceived >= total ? 'Vuelto estimado:' : 'Faltan (estimado):'} <strong>{money(Math.abs(cashReceived - total))}</strong></p>}
              {cashInput !== '' && !cashIsValid && <p className="payment-modal__error" role="alert">Ingresa un monto mayor que cero.</p>}
            </div>
          ) : (
            <div className="payment-modal__card-info payment-modal__terminal-panel" aria-label="Panel de terminal POS">
              <div className="payment-modal__terminal-status-bar">
                <div className="payment-modal__terminal-connection">
                  <span className={`payment-modal__status-dot ${terminalConnected ? 'payment-modal__status-dot--online' : 'payment-modal__status-dot--offline'}`} aria-hidden="true" />
                  <span className="payment-modal__status-text">Estado de conexión: <strong>{terminalConnected ? 'Conectado' : 'Desconectado'}</strong></span>
                </div>
                <span className="payment-modal__mode-badge">Terminal POS (Simulador)</span>
              </div>
              <div className="payment-modal__terminal-steps">
                <div className="payment-modal__step-row">
                  <span className="payment-modal__step-label">Monto desde MS-5:</span>
                  <span className="payment-modal__step-value">{money(total)}</span>
                </div>
                <div className="payment-modal__step-row">
                  <span className="payment-modal__step-label">Estado de lectura:</span>
                  <span className="payment-modal__step-value">Listo para iniciar lectura</span>
                </div>
                <div className="payment-modal__step-row">
                  <span className="payment-modal__step-label">Autorización:</span>
                  <span className="payment-modal__step-value">Pasarela simulada</span>
                </div>
              </div>
              <strong>Cobro mediante pasarela simulada</strong>
              <p>El POS enviará el monto de la venta. La respuesta mostrará únicamente una referencia opaca; aquí no se solicitan datos de tarjeta.</p>
              <div className="payment-modal__security-notice">
                <span>🔒 Operación segura: no se solicita número de tarjeta ni datos sensibles.</span>
              </div>
            </div>
          )}

          {!methodMatchesSale && <p className="payment-modal__notice" role="status">Esta venta se inició con {saleMethod === 'cash' ? 'efectivo' : 'tarjeta'}. Cambia el método de la venta antes de cobrar.</p>}
          {!onConfirm && <p className="payment-modal__notice" role="status">La interfaz de cobro está definida; el envío del pago desde POS aún no está conectado.</p>}
          {errorMessage && <p className="payment-modal__error" role="alert">{errorMessage}{shortfall != null && ` Faltan según MS-5: ${money(shortfall)}.`}</p>}
        </>
      ) : (
        <div id="payment-modal-description" className={`payment-modal__result payment-modal__result--${state}`} role="status" aria-live="polite">
          {method === 'card' && (
            <div className="payment-modal__terminal-status-bar">
              <div className="payment-modal__terminal-connection">
                <span className={`payment-modal__status-dot ${terminalConnected ? 'payment-modal__status-dot--online' : 'payment-modal__status-dot--offline'}`} aria-hidden="true" />
                <span className="payment-modal__status-text">Estado de conexión: <strong>{terminalConnected ? 'Conectado' : 'Desconectado'}</strong></span>
              </div>
              <span className="payment-modal__mode-badge">Terminal POS</span>
            </div>
          )}
          <span className="payment-modal__symbol" aria-hidden="true">{stateText[state].symbol}</span>
          <p>{stateText[state].detail}</p>
          {method === 'card' && (
            <div className="payment-modal__terminal-steps">
              <div className="payment-modal__step-row">
                <span className="payment-modal__step-label">Monto desde MS-5:</span>
                <span className="payment-modal__step-value">{money(total)}</span>
              </div>
              {state === 'processing' && (
                <>
                  <div className="payment-modal__step-row">
                    <span className="payment-modal__step-label">Lectura de tarjeta:</span>
                    <span className="payment-modal__step-value">Leyendo dispositivo en terminal...</span>
                  </div>
                  <div className="payment-modal__step-row">
                    <span className="payment-modal__step-label">Autorización:</span>
                    <span className="payment-modal__step-value">Enviando a pasarela simulada</span>
                  </div>
                </>
              )}
              {state === 'approved' && (
                <div className="payment-modal__step-row">
                  <span className="payment-modal__step-label">Autorización:</span>
                  <span className="payment-modal__step-value">Aprobada</span>
                </div>
              )}
              {state === 'declined' && (
                <div className="payment-modal__step-row">
                  <span className="payment-modal__step-label">Autorización:</span>
                  <span className="payment-modal__step-value">Rechazada</span>
                </div>
              )}
              {state === 'cancelled' && (
                <div className="payment-modal__step-row">
                  <span className="payment-modal__step-label">Estado terminal:</span>
                  <span className="payment-modal__step-value">Cancelado</span>
                </div>
              )}
              {state === 'uncertain' && (
                <div className="payment-modal__step-row">
                  <span className="payment-modal__step-label">Estado terminal:</span>
                  <span className="payment-modal__step-value">Incierto (pendiente de verificación)</span>
                </div>
              )}
            </div>
          )}
          {state === 'approved' && method === 'cash' && change != null && <p className="payment-modal__change">Vuelto calculado por MS-5: <strong>{money(change)}</strong></p>}
          {errorMessage && <p>{errorMessage}</p>}
          {method === 'card' && reference && <p className="payment-modal__reference">Referencia opaca: <code>{reference}</code></p>}
        </div>
      )}

      {state === 'declined' && <div className="payment-modal__methods" role="group" aria-label="Cambiar método tras rechazo">
        <button type="button" className={method === 'cash' ? 'is-selected' : ''} aria-pressed={method === 'cash'} onClick={() => setMethod('cash')}>Efectivo</button>
        <button type="button" className={method === 'card' ? 'is-selected' : ''} aria-pressed={method === 'card'} onClick={() => setMethod('card')}>Tarjeta</button>
      </div>}

      <div className="payment-modal__actions">
        {state === 'available' && (
          <>
            <button type="button" className="payment-modal__secondary" onClick={onClose}>Volver al carrito</button>
            {methodMatchesSale ? <button type="button" className="payment-modal__primary" disabled={!canConfirm} onClick={() => onConfirm?.(method, method === 'cash' ? cashReceived : undefined)}>Confirmar {method === 'cash' ? 'efectivo' : 'tarjeta'}</button> :
              <button type="button" className="payment-modal__primary" disabled={!onChangeMethod} onClick={() => onChangeMethod?.(method)}>Cambiar a {method === 'cash' ? 'efectivo' : 'tarjeta'}</button>}
          </>
        )}
        {state === 'declined' && !methodMatchesSale && onChangeMethod && <button type="button" className="payment-modal__primary" onClick={() => onChangeMethod(method)}>Cambiar a {method === 'cash' ? 'efectivo' : 'tarjeta'}</button>}
        {state === 'declined' && methodMatchesSale && method === 'card' && onRetry && <button type="button" className="payment-modal__primary" onClick={onRetry}>Reintentar tarjeta</button>}
        {state === 'processing' && (
          <div className="payment-modal__processing-actions">
            <span className="payment-modal__waiting">Procesando…</span>
            {onCancelAttempt && (
              <button type="button" className="payment-modal__secondary" onClick={onCancelAttempt}>
                Cancelar intento
              </button>
            )}
          </div>
        )}
        {state === 'uncertain' && onVerify && <button type="button" className="payment-modal__primary" onClick={onVerify}>Verificar venta</button>}
        {state !== 'available' && state !== 'processing' && <button type="button" className="payment-modal__secondary" onClick={onClose}>Cerrar</button>}
      </div>
    </dialog>
  );
}
