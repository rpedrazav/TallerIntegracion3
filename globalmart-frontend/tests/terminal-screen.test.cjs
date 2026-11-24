const { readFileSync } = require('node:fs');
const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');

test('pantalla de terminal POS cumple requerimientos de diseño S2-106', () => {
  const modalPath = path.resolve(__dirname, '../src/components/pos/PaymentModal.tsx');
  const source = readFileSync(modalPath, 'utf8');

  // 1. Sin pedir número de tarjeta (no PAN, CVV, PIN, expiration date)
  // No debe existir ningún input para datos sensibles de la tarjeta
  const cardInputPattern = /<input[^>]*?(?:pan|cvv|card.?number|numero.?tarjeta|fecha.?expiracion|pin)[^>]*?>/i;
  assert.equal(cardInputPattern.test(source), false, 'No deben existir inputs de datos sensibles de tarjeta');

  // Tampoco debe pedir PAN en el texto de labels ni placeholders
  assert.doesNotMatch(source, /placeholder=".*?(?:número de tarjeta|tarjeta|cvv|pin).*?"/i);

  // 2. Mostrar monto desde MS-5
  assert.match(source, /Monto desde MS-5:/, 'Debe identificar explícitamente el monto proveniente de MS-5');
  assert.match(source, /money\(total\)/, 'Debe renderizar el total de la venta provisto por MS-5');

  // 3. Estado de conexión
  assert.match(source, /Estado de conexión:/, 'Debe mostrar el estado de conexión del terminal');
  assert.match(source, /payment-modal__status-dot/, 'Debe contar con indicador visual de estado de conexión');
  assert.match(source, /terminalConnected \? 'Conectado' : 'Desconectado'/, 'Debe reflejar estado de conexión dinámico');

  // 4. Estado de lectura
  assert.match(source, /Lectura de tarjeta:/, 'Debe mostrar la etapa de lectura de tarjeta');
  assert.match(source, /Listo para iniciar lectura/, 'Debe contemplar estado inicial de lectura');

  // 5. Estado de autorización
  assert.match(source, /Autorización:/, 'Debe contemplar estado de autorización');
  assert.match(source, /Enviando a pasarela simulada/, 'Debe reflejar procesamiento de autorización');

  // 6. Estado de rechazo
  assert.match(source, /Cobro rechazado/, 'Debe reflejar resultado de cobro rechazado');
  assert.match(source, /Rechazada/, 'Debe indicar autorización rechazada');

  // 7. Estado de cancelación
  assert.match(source, /Intento cancelado/, 'Debe reflejar resultado de cancelación');
  assert.match(source, /onCancelAttempt/, 'Debe soportar callback para cancelar el intento de cobro');
  assert.match(source, /Cancelar intento/, 'Debe ofrecer botón de cancelación de intento');

  // 8. Identificación clara del resultado y referencia opaca
  assert.match(source, /Referencia opaca:\s*<code>\{reference\}<\/code>/, 'El cajero debe identificar claramente la referencia opaca');
});

test('estilos de pantalla de terminal POS definen panel, conexión y estados', () => {
  const cssPath = path.resolve(__dirname, '../src/components/pos/PaymentModal.css');
  const cssSource = readFileSync(cssPath, 'utf8');

  assert.match(cssSource, /\.payment-modal__terminal-panel/, 'Debe definir clase para panel de terminal');
  assert.match(cssSource, /\.payment-modal__status-dot--online/, 'Debe definir clase de conexión online');
  assert.match(cssSource, /\.payment-modal__status-dot--offline/, 'Debe definir clase de conexión offline');
  assert.match(cssSource, /\.payment-modal__terminal-steps/, 'Debe definir contenedor de etapas del terminal');
  assert.match(cssSource, /\.payment-modal__security-notice/, 'Debe definir estilo para aviso de seguridad sin PAN');
});
