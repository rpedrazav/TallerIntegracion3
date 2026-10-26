---
id: hardware
tipo: integracion
titulo: Integración con Hardware — Balanza y Terminal POS
estado: parcial
fuentes: [GlobalMart_ContextMaster.md#sec12, diagramas/rodrigo/, globalmart-frontend/src/index.ts, globalmart-frontend/src/preload.ts, globalmart-frontend/src/hardware/scale-contract.ts, globalmart-frontend/src/hardware/simulated-scale-adapter.ts, globalmart-frontend/src/hardware/serial-scale-adapter.ts, globalmart-frontend/src/hardware/scale-adapter-factory.ts, globalmart-frontend/src/hardware/scale-ipc.ts, globalmart-frontend/tests/scale-contract.test.cjs, globalmart-frontend/tests/simulated-scale-adapter.test.cjs, globalmart-frontend/tests/serial-scale-adapter.test.cjs, globalmart-frontend/tests/scale-ipc.test.cjs]
verificado_contra_codigo: true
ultima_revision: 2026-10-25
depende_de: [ms5-pos, electron-ipc]
publica: []
consume: []
reglas: [RF-15]
---
# Integración con Hardware — Balanza y Terminal POS

> Dispositivos físicos en el mostrador del minimarket. La integración ocurre en el proceso principal de Electron (Node.js) y se expone al renderer vía IPC. **Estado: PARCIAL** — contrato, simulador, IPC y estructura serial intercambiable implementados; la integración visual, el transporte/parser de un modelo real y la compatibilidad física siguen pendientes.

## Balanza Física

**Protocolo:** Puerto serial (COM) o USB.

**Flujo diseñado:**
```
1. Cajero escanea producto de peso variable (frutas, carnes)
2. Coloca el producto en la balanza
3. El dispositivo o simulador entrega valor y unidad explícita (g o kg)
4. El adaptador convierte g / 1000 a kg; kg se conserva sin reconvertir
5. Se validan mínimo y precisión; IPC entrega peso canónico al renderer
6. Renderer envía peso_kg a MS-5; solo muestra una estimación del precio
7. MS-5 valida el ítem y calcula con precio autoritativo por kg de MS-3
```

**En el modelo de datos:** `ItemVenta.PesoKg` (decimal?) ya existe en MS-5 para este caso.

**Si balanza no responde:** el cajero ingresa el peso manualmente (fallback de UI).

El [contrato S2-061](../dominio/peso-variable.md) define mínimo de 0.010 kg, precisión de 0.001 kg y rechazo de lecturas sin unidad. El simulador aplica el mínimo y la precisión, recibe unidad explícita `g`/`kg` y normaliza a kg. La integración física sigue pendiente.

## S2-066 / TI3-697 — Interfaz de balanza [IMPLEMENTADO]

`globalmart-frontend/src/hardware/scale-contract.ts` define un contrato común para el adaptador físico y el simulador:

- `ScaleAdapter`: `connect`, `disconnect`, `readStableWeight` y `getState`.
- Estados de conexión: `disconnected`, `connecting`, `connected`, `disconnecting` y `error`.
- Estados de lectura: `idle`, `waiting_for_stability`, `stable` y `error`.
- Errores tipados y cerrados mediante `ScaleErrorCode`, con indicador `retryable`.
- `StableWeightReading`: lectura estable normalizada a `kg`, con fecha ISO y `stable: true`.
- `ScaleOperationResult<T>`: resultado discriminado de éxito o error que incluye el estado vigente.
- `ScaleRendererApi`: superficie serializable que un preload futuro podrá publicar por `contextBridge`.

La configuración física del puerto se inyectará al construir el adaptador en Electron main. No forma parte de `ScaleRendererApi`, por lo que el renderer no recibe ruta serial, baud rate ni handle del dispositivo. La prueba estática `scale-contract.test.cjs` valida los estados y prohíbe `serialport`, `navigator.serial` y creación de `SerialPort` en el contrato y renderer. Typecheck y prueba aprobados el 2026-10-24.

## S2-067 / TI3-698 — Adaptador de balanza simulada [IMPLEMENTADO]

`SimulatedScaleAdapter` implementa `ScaleAdapter` sin acceder a hardware y consume una secuencia determinista de escenarios configurables:

- `stable`: recibe valor con unidad explícita `g` o `kg`, convierte a kg y devuelve una `StableWeightReading`.
- `disconnected`: cambia el estado de conexión a `disconnected` y devuelve `device_unavailable`.
- `error`: devuelve un código, mensaje y condición de reintento controlados sin desconectar necesariamente el adaptador.

Una lectura sin conexión devuelve `not_connected`; valores no finitos, menores que `0.010 kg` o con fracciones inferiores a 1 g devuelven `invalid_reading`. Las respuestas conservan el mismo contrato serializable que usará la UI cuando S2-068 agregue el bridge IPC.

`scale-adapter-factory.ts` selecciona el driver mediante `GLOBALMART_SCALE_ADAPTER=simulator|serial`. El valor por defecto es `simulator`; seleccionar `serial` exige inyectar el proveedor futuro y falla de forma explícita mientras no exista. La configuración y el adaptador permanecen en Electron main y no se exponen al renderer.

Typecheck y pruebas del contrato/simulador aprobados el 2026-10-24. Las pruebas cubren selección por configuración, conversión `750 g → 0.750 kg`, lectura sin conexión, desconexión durante lectura, error controlado y peso inválido. El IPC se implementó en S2-068; su consumo visual desde el POS permanece pendiente.

## S2-068 / TI3-699 — IPC mínimo y validado [IMPLEMENTADO]

Electron main crea el adaptador seleccionado por configuración y registra una whitelist cerrada de cuatro canales:

| Canal | Operación autorizada | Payload del renderer |
|---|---|---|
| `scale:connect` | Conectar el adaptador | Ninguno |
| `scale:disconnect` | Desconectar el adaptador | Ninguno |
| `scale:read-stable` | Leer peso estable | Solo `timeoutMs` y `stabilityWindowMs`, enteros de 0 a 60.000 ms |
| `scale:get-state` | Consultar estado | Ninguno |

`preload.ts` expone esas operaciones mediante `window.api.scale`, tipado como `ScaleRendererApi`. No expone `ipcRenderer`, un método `invoke` genérico, rutas de puerto, baud rate ni comandos seriales. El renderer mantiene `contextIsolation: true`, `nodeIntegration: false` y `sandbox: true`. El handler de lectura rechaza objetos no válidos, propiedades desconocidas y valores fuera de rango antes de delegar al adaptador.

Typecheck y pruebas dirigidas aprobados el 2026-10-25. La prueba verifica la whitelist exacta, la validación del payload, el aislamiento del renderer y la superficie real publicada por `contextBridge`. No acredita todavía un componente React consumiendo la balanza ni compatibilidad con hardware físico.

## S2-069 / TI3-700 — Adaptador serial intercambiable [IMPLEMENTADO SIN HARDWARE]

`serial-scale-adapter.ts` implementa `ScaleAdapter` detrás de dos puertos internos de Electron main:

- `SerialScaleTransport`: abre/cierra el puerto y entrega una trama, sin imponer biblioteca serial.
- `SerialScaleParser`: traduce una trama específica del dispositivo a valor, unidad `g`/`kg` y señal de estabilidad.

La configuración privada contiene `portPath`, `baudRate` y `parserId`. Se obtiene en main mediante `GLOBALMART_SCALE_SERIAL_PORT`, `GLOBALMART_SCALE_SERIAL_BAUD_RATE` y `GLOBALMART_SCALE_SERIAL_PARSER`; nunca cruza IPC ni forma parte de `ScaleRendererApi`. El adaptador normaliza la lectura válida a kg y traduce timeout, dispositivo no disponible, trama inválida e inestabilidad al contrato común, sin devolver la trama ni detalles del puerto al renderer.

El driver por defecto continúa siendo `GLOBALMART_SCALE_ADAPTER=simulator`. Elegir `serial` exige las tres configuraciones y un proveedor de transporte/parser inyectado; como el repositorio no incorpora `serialport` ni un parser de fabricante, main falla explícitamente indicando que el proveedor está deshabilitado. Esto evita afirmar compatibilidad con un modelo desconocido.

Typecheck y pruebas dirigidas aprobados el 2026-10-25. Las pruebas usan dobles de transporte/parser, comprueban conversión `750 g → 0.750 kg`, conexión/lectura/desconexión, errores neutrales y selección por configuración. Funcionamiento con puerto, balanza y protocolo reales: [NO VERIFICADO]. React conserva `window.api.scale` sin cambios.

## Terminal POS

Dispositivo compuesto que agrupa varios periféricos:

| Componente | Protocolo | Función |
|-----------|-----------|---------|
| Lector de tarjetas (NFC/chip/mag) | USB/Bluetooth | Lee datos de tarjeta → envía a MS-5 → Pasarela |
| Cajón de dinero | Serial/USB (comando específico fabricante) | Se abre automáticamente al cobrar en efectivo |
| Impresora de tickets | ESC/POS protocol | Imprime recibo al finalizar venta |
| Display para cliente | Serial/USB | Muestra el total antes de que el cliente pague |

## IPC implementado y transporte serial futuro

```typescript
// Implementado en Electron main mediante una whitelist centralizada:
ipcMain.handle('scale:connect', () => scaleAdapter.connect());
ipcMain.handle('scale:disconnect', () => scaleAdapter.disconnect());
ipcMain.handle('scale:read-stable', (_, options) =>
  scaleAdapter.readStableWeight(parseStableWeightReadOptions(options)));
ipcMain.handle('scale:get-state', () => scaleAdapter.getState());

// Futuro: solo un transporte inyectado en main podrá importar SerialPort.
// Cajón de dinero
ipcMain.handle('cajon:abrir', () => {
  // Enviar comando ESC/POS al terminal
});

// Impresora
ipcMain.handle('impresora:imprimir', (_, contenido: string) => {
  // Enviar ESC/POS al puerto de impresora
});
```

## Límites del Terminal Simulado (S2-086)

Ver la especificación exhaustiva en [[limites-terminal-simulado]]:
- Matriz de operaciones: `iniciar`, `cancelar`, `autorizar`, `obtenerEstado`.
- Desacoplamiento total de pasarela bancaria externa.
- Regla de aceptación: No declarar compatibilidad con marcas físicas sin verificación en hardware real.

## Configuración de Adaptadores y Reserva de Hardware Real (S2-087)

En `globalmart-frontend/src/devices/`:
- `DeviceManager`: Singleton que orquesta la selección de adaptadores para balanza y terminal según variables de entorno (`GLOBALMART_TERMINAL_ADAPTER`, `GLOBALMART_SCALE_ADAPTER`).
- **Adaptadores simulados por defecto:** `MockTerminalAdapter` y `MockScaleAdapter` inician de forma inmediata sin solicitar permisos de sistema ni requerir drivers.
- **Adaptadores físicos reservados:** `RealTerminalAdapter` está implementado como clase de extensión segura; rechaza cualquier ejecución activa devolviendo error controlado (`DEVICE_NOT_CERTIFIED`) para impedir invocaciones a periféricos no homologados.

## Seguridad de Canales IPC (S2-088)

- Endurecimiento de `BrowserWindow` con `contextIsolation: true`, `nodeIntegration: false` y `sandbox: true`.
- Validación estricta de tipos y rangos numéricos en `index.ts` antes de invocar los adaptadores (`validateIniciarParams`, `validateSimulacionAction`, `validatePeso`).
- Prohibición absoluta de APIs genéricas de ejecución (`eval`, `child_process`, `exec`) o handles directos al sistema operativo.

## Smoke Automatizado de Simuladores (S2-089)

- Script `scripts/smoke-simuladores-frontend.sh` y suite `globalmart-frontend/tests/smoke-simulators.ts`.
- Valida lectura de balanza (cero inicial, tara y lectura estable) y operaciones de terminal (inicio, aprobación con token opaco, rechazo y cancelación limpia).
- Ejecución 100% libre de hardware en entornos CI / headless.

## Casos de uso afectados

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| CP-09 | Capturar Peso de Balanza | [PARCIAL: adaptadores e IPC, widget en POS] |
| PC-10 | Integrar Peso de Balanza | [PARCIAL: simulador integrado en POS] |
| PC-26 | Leer Tarjeta en Terminal | [PARCIAL: simulador integrado y smoke S2-089] |
| PC-32 | Abrir Cajón de Dinero | [PLANIFICADO] |
| PC-33 | Imprimir Recibo | [PLANIFICADO] |
| PC-34 | Mostrar Total en Display | [PLANIFICADO] |

## Conexiones
- Frontend: [[electron-ipc]]
- POS backend: [[ms5-pos]]
- Pasarela de pago: [[pasarela-pago]]
- Límites terminal: [[limites-terminal-simulado]]

## Fuentes
- `GlobalMart_ContextMaster.md` §12
- `diagramas/rodrigo/R1_screen_flow_electron.drawio.png`
- `globalmart-frontend/src/hardware/scale-contract.ts`
- `globalmart-frontend/src/hardware/simulated-scale-adapter.ts`
- `globalmart-frontend/src/hardware/serial-scale-adapter.ts`
- `globalmart-frontend/src/hardware/scale-adapter-factory.ts`
- `globalmart-frontend/src/hardware/scale-ipc.ts`
- `globalmart-frontend/src/index.ts`
- `globalmart-frontend/src/preload.ts`
- `globalmart-frontend/tests/scale-contract.test.cjs`
- `globalmart-frontend/tests/simulated-scale-adapter.test.cjs`
- `globalmart-frontend/tests/serial-scale-adapter.test.cjs`
- `globalmart-frontend/tests/scale-ipc.test.cjs`
- `tareas/sprint2_youtrack_resultado.json` (`S2-066` → `TI3-697`)
- `tareas/sprint2_youtrack_resultado.json` (`S2-067` → `TI3-698`)
- `tareas/sprint2_youtrack_resultado.json` (`S2-068` → `TI3-699`)
- `tareas/sprint2_youtrack_resultado.json` (`S2-069` → `TI3-700`)
