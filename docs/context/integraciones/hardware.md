---
id: hardware
tipo: integracion
titulo: Integración con Hardware — Balanza y Terminal POS
estado: planificado
fuentes: [GlobalMart_ContextMaster.md#sec12, diagramas/rodrigo/]
verificado_contra_codigo: false
ultima_revision: 2026-10-23
depende_de: [ms5-pos, electron-ipc]
publica: []
consume: []
reglas: [RF-15]
---
# Integración con Hardware — Balanza y Terminal POS

> Dispositivos físicos en el mostrador del minimarket. La integración ocurre en el proceso principal de Electron (Node.js) y se expone al renderer vía IPC. **Estado: PLANIFICADO** — sin implementación en el código actual.

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

## Terminal POS

Dispositivo compuesto que agrupa varios periféricos:

| Componente | Protocolo | Función |
|-----------|-----------|---------|
| Lector de tarjetas (NFC/chip/mag) | USB/Bluetooth | Lee datos de tarjeta → envía a MS-5 → Pasarela |
| Cajón de dinero | Serial/USB (comando específico fabricante) | Se abre automáticamente al cobrar en efectivo |
| Impresora de tickets | ESC/POS protocol | Imprime recibo al finalizar venta |
| Display para cliente | Serial/USB | Muestra el total antes de que el cliente pague |

## Implementación en Electron (diseño)

```typescript
// Diseño esperado en main process:
import { SerialPort } from 'serialport';

// Balanza
ipcMain.handle('balanza:conectar', (_, puerto: string) => {
  const serial = new SerialPort({ path: puerto, baudRate: 9600 });
  // ...
});

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
