---
id: hardware
tipo: integracion
titulo: Integración con Hardware — Balanza y Terminal POS
estado: parcial
fuentes: [GlobalMart_ContextMaster.md#sec12, diagramas/rodrigo/, globalmart-frontend/src/hardware/scale-contract.ts, globalmart-frontend/tests/scale-contract.test.cjs]
verificado_contra_codigo: true
ultima_revision: 2026-10-24
depende_de: [ms5-pos, electron-ipc]
publica: []
consume: []
reglas: [RF-15]
---
# Integración con Hardware — Balanza y Terminal POS

> Dispositivos físicos en el mostrador del minimarket. La integración ocurre en el proceso principal de Electron (Node.js) y se expone al renderer vía IPC. **Estado: PARCIAL** — el contrato tipado de balanza está implementado; adaptador, simulador e IPC siguen planificados.

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

El [contrato S2-061](../dominio/peso-variable.md) define mínimo de 0.010 kg, precisión de 0.001 kg y rechazo de lecturas sin unidad. Estas son reglas del simulador; la integración física y la conversión del adaptador siguen pendientes.

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

## Terminal POS

Dispositivo compuesto que agrupa varios periféricos:

| Componente | Protocolo | Función |
|-----------|-----------|---------|
| Lector de tarjetas (NFC/chip/mag) | USB/Bluetooth | Lee datos de tarjeta → envía a MS-5 → Pasarela |
| Cajón de dinero | Serial/USB (comando específico fabricante) | Se abre automáticamente al cobrar en efectivo |
| Impresora de tickets | ESC/POS protocol | Imprime recibo al finalizar venta |
| Display para cliente | Serial/USB | Muestra el total antes de que el cliente pague |

## Implementación futura en Electron

```typescript
// Solo en Electron main; nunca en el renderer:
import { SerialPort } from 'serialport';
import type { ScaleAdapter } from './hardware/scale-contract';

// La configuración serial se inyecta al crear el adaptador.
// Los handlers IPC delegan en ScaleAdapter y no aceptan una ruta de puerto.
ipcMain.handle('balanza:conectar', () => scaleAdapter.connect());
ipcMain.handle('balanza:desconectar', () => scaleAdapter.disconnect());
ipcMain.handle('balanza:leer-peso-estable', (_, options) =>
  scaleAdapter.readStableWeight(options));

// Cajón de dinero
ipcMain.handle('cajon:abrir', () => {
  // Enviar comando ESC/POS al terminal
});

// Impresora
ipcMain.handle('impresora:imprimir', (_, contenido: string) => {
  // Enviar ESC/POS al puerto de impresora
});
```

## Casos de uso afectados

| ID | Caso de Uso | Estado |
|----|-------------|--------|
| CP-09 | Capturar Peso de Balanza | [PLANIFICADO] |
| PC-10 | Integrar Peso de Balanza | [PLANIFICADO] |
| PC-26 | Leer Tarjeta en Terminal | [PLANIFICADO] |
| PC-32 | Abrir Cajón de Dinero | [PLANIFICADO] |
| PC-33 | Imprimir Recibo | [PLANIFICADO] |
| PC-34 | Mostrar Total en Display | [PLANIFICADO] |

## Conexiones
- Frontend: [[electron-ipc]]
- POS backend: [[ms5-pos]]
- Pasarela de pago: [[pasarela-pago]]

## Fuentes
- `GlobalMart_ContextMaster.md` §12
- `diagramas/rodrigo/R1_screen_flow_electron.drawio.png`
- `globalmart-frontend/src/hardware/scale-contract.ts`
- `globalmart-frontend/tests/scale-contract.test.cjs`
- `tareas/sprint2_youtrack_resultado.json` (`S2-066` → `TI3-697`)
