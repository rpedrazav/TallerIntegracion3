/**
 * Test de Smoke Automatizado de Simuladores de Hardware para Frontend Electron (S2-089).
 * Verifica el ciclo de vida de la Balanza y del Terminal POS simulados,
 * incluyendo estados Aprobado, Rechazado y Cancelación, sin requerir periféricos físicos.
 * También valida las restricciones de seguridad y adaptadores (S2-087 / S2-088).
 */

import { DeviceManager } from '../src/devices/DeviceManager';
import { RealTerminalAdapter } from '../src/devices/adapters/RealTerminalAdapter';
import { SimulatedAction } from '../src/devices/types';

let failures = 0;

function assert(condition: boolean, message: string) {
  if (condition) {
    console.log(`  ✓ ${message}`);
  } else {
    console.error(`  ✗ FALLO: ${message}`);
    failures++;
  }
}

async function runSmokeTests() {
  console.log('=================================================================');
  console.log('  GlobalMart OS — Smoke de Simuladores de Hardware (S2-089)');
  console.log('=================================================================');
  console.log('');

  const deviceManager = DeviceManager.getInstance();
  const terminal = deviceManager.getTerminal();
  const scale = deviceManager.getScale();

  // 1. Verificación de configuración inicial
  console.log('[1/5] Verificando inicio de adaptadores en modo MOCK (S2-087)...');
  const config = deviceManager.getConfig();
  assert(config.terminalMode === 'mock', 'Terminal configurado en modo mock por defecto');
  assert(config.scaleMode === 'mock', 'Balanza configurada en modo mock por defecto');

  // 2. Pruebas de Balanza Simulada
  console.log('\n[2/5] Probando Balanza Simulada (lectura, pesaje y tara)...');
  const lecturaInicial = await scale.obtenerPeso();
  assert(lecturaInicial.pesoKg === 0, 'Lectura inicial de balanza es 0.000 kg');
  assert(lecturaInicial.estable === true, 'Lectura inicial es estable');

  const lecturaPesaje = await scale.simularPeso(2.450);
  assert(lecturaPesaje.pesoKg === 2.450, 'Simulación de peso registró 2.450 kg correctamente');

  const taraResult = await scale.tarar();
  assert(taraResult.exito === true, 'Tara ejecutada exitosamente');
  const lecturaPostTara = await scale.obtenerPeso();
  assert(lecturaPostTara.pesoKg === 0, 'Peso neto tras tarar es 0.000 kg');

  // 3. Pruebas de Terminal POS Simulado: Aprobación
  console.log('\n[3/5] Probando Terminal POS Simulado: Flujo de Aprobación (S2-086 / S2-089)...');
  const estadoInicial = await terminal.obtenerEstado();
  assert(estadoInicial.estado === 'IDLE', 'Terminal inicia en estado IDLE');

  const inicioAprob = await terminal.iniciar({ monto: 12500, moneda: 'CLP', correlationId: 'corr-smoke-aprob-01' });
  assert(inicioAprob.exito === true, 'Operación de cobro iniciada exitosamente');
  assert(inicioAprob.estado === 'ESPERANDO_TARJETA', 'Terminal pasa a estado ESPERANDO_TARJETA');

  const autoAprob = await terminal.autorizar('APROBAR');
  assert(autoAprob.exito === true, 'Cobro simulado aprobado exitosamente');
  assert(autoAprob.estado === 'APROBADO', 'Terminal pasa a estado APROBADO');
  assert(typeof autoAprob.tokenTransaccion === 'string' && autoAprob.tokenTransaccion.startsWith('tok_'), 'Se emitió tokenTransaccion opaco');
  assert(typeof autoAprob.codigoAutorizacion === 'string' && autoAprob.codigoAutorizacion.startsWith('AUTH-'), 'Se generó código de autorización');

  // 4. Pruebas de Terminal POS Simulado: Rechazo y Cancelación
  console.log('\n[4/5] Probando Terminal POS Simulado: Rechazo y Cancelación (S2-089)...');
  const inicioRechazo = await terminal.iniciar({ monto: 8990, correlationId: 'corr-smoke-rechazo-02' });
  assert(inicioRechazo.exito === true, 'Segunda operación de cobro iniciada');

  const autoRechazo = await terminal.autorizar('RECHAZAR');
  assert(autoRechazo.exito === false, 'Cobro rechazado simulado retorna exito: false');
  assert(autoRechazo.estado === 'RECHAZADO', 'Terminal pasa a estado RECHAZADO');
  assert(autoRechazo.tokenTransaccion === undefined, 'No se genera token en caso de rechazo');

  const inicioCancel = await terminal.iniciar({ monto: 3500 });
  const cancelResult = await terminal.cancelar();
  assert(cancelResult.exito === true, 'Cancelación de operación exitosa');
  assert(cancelResult.estado === 'CANCELADO', 'Terminal pasa a estado CANCELADO');

  // 5. Pruebas de Seguridad y Adaptador Real Reservado (S2-087 / S2-088)
  console.log('\n[5/5] Validando seguridad de IPC y proveedor real reservado (S2-087 / S2-088)...');
  const realAdapter = new RealTerminalAdapter();
  const realResult = await realAdapter.iniciar({ monto: 1000 });
  assert(realResult.exito === false, 'Adaptador real no permite ejecución no homologada');
  assert(realResult.mensaje?.includes('no se declara compatibilidad con lectores no probados') === true,
    'Criterio S2-086 cumplido: Adaptador real rechaza lectores de tarjeta no probados');

  console.log('');
  if (failures === 0) {
    console.log('✓ Todos los tests de smoke de simuladores finalizaron con ÉXITO.');
    process.exit(0);
  } else {
    console.error(`✗ Se encontraron ${failures} fallos en el smoke test.`);
    process.exit(1);
  }
}

runSmokeTests().catch((err) => {
  console.error('Error fatal durante la ejecución del smoke test:', err);
  process.exit(1);
});
