---
id: electron-ipc
tipo: frontend
titulo: Electron IPC — Comunicación Main/Renderer
estado: parcial
fuentes: [globalmart-frontend/src/preload.ts, globalmart-frontend/src/index.ts, globalmart-frontend/src/types/global.d.ts, globalmart-frontend/src/pages/Pos.tsx, globalmart-frontend/src/hardware/scale-contract.ts, globalmart-frontend/src/hardware/simulated-scale-adapter.ts, globalmart-frontend/src/hardware/serial-scale-adapter.ts, globalmart-frontend/src/hardware/scale-adapter-factory.ts, globalmart-frontend/src/hardware/scale-ipc.ts, globalmart-frontend/src/hardware/scale-cart.ts, globalmart-frontend/tests/scale-contract.test.cjs, globalmart-frontend/tests/simulated-scale-adapter.test.cjs, globalmart-frontend/tests/serial-scale-adapter.test.cjs, globalmart-frontend/tests/scale-ipc.test.cjs, globalmart-frontend/tests/scale-main-preload.integration.test.cjs]
verificado_contra_codigo: true
ultima_revision: 2026-10-25
depende_de: [estructura]
publica: []
consume: []
reglas: []
---
# Electron IPC — Comunicación Main/Renderer

> `preload.ts` expone APIs seleccionadas del proceso principal (Node.js) al renderer (React). El contrato, el adaptador simulado y el IPC mínimo de balanza están implementados con una whitelist cerrada.

## Arquitectura Electron

```
Main Process (Node.js)
  ↕ IPC (ipcMain / ipcRenderer / contextBridge)
Renderer Process (React/TypeScript)
  ↕ HTTP/axios
Backend APIs (MS-1, MS-3, MS-5)
```

## Preload.ts

El archivo `src/preload.ts` existe y actúa como bridge de seguridad entre el proceso principal y el renderer. Expone selectivamente las APIs de Node.js que el renderer puede usar.

Actualmente expone `ping`, `getToken`, `setToken`, `logout` y el namespace tipado `scale`. No expone Node.js, `ipcRenderer`, puertos seriales ni un método de invocación genérico. La ventana declara explícitamente `contextIsolation: true`, `nodeIntegration: false` y `sandbox: true`.

## Handlers IPC diseñados (de diagramas)

Según `diagramas/rodrigo/R1_screen_flow_electron.drawio.png`:
- IPC para abrir drawer/cajón de dinero
- IPC para SerialPort (balanza, impresora)
- IPC para gestión de ventanas

**Estado:** el IPC de autenticación y los cuatro handlers mínimos de balanza están implementados. Cajón, impresora, gestión de ventanas y acceso serial continúan planificados.

## Contrato de balanza por IPC (S2-066)

`ScaleRendererApi` define únicamente operaciones de alto nivel: conectar, desconectar, consultar estado y leer peso estable. No contiene ruta de puerto, baud rate ni objetos de `serialport`. `SimulatedScaleAdapter` y la estructura `SerialScaleAdapter` implementan el mismo contrato; Electron main selecciona el driver por configuración y S2-068 conecta sus métodos con `ipcMain` y `contextBridge`. El transporte/parser físico sigue deshabilitado y [NO VERIFICADO].

```typescript
// Implementado en preload: delegación exclusiva por canales constantes.
const scaleApi: ScaleRendererApi = {
  connect: () => ipcRenderer.invoke('scale:connect'),
  disconnect: () => ipcRenderer.invoke('scale:disconnect'),
  readStableWeight: (options) => ipcRenderer.invoke('scale:read-stable', options),
  getState: () => ipcRenderer.invoke('scale:get-state'),
};
```

## Whitelist y validación (S2-068)

`scale-ipc.ts` es la fuente única de los canales `scale:connect`, `scale:disconnect`, `scale:read-stable` y `scale:get-state`. Main registra exactamente esos cuatro handlers y preload solo publica funciones cerradas que invocan esas constantes.

El único payload permitido corresponde a `readStableWeight`: objeto opcional con `timeoutMs` y `stabilityWindowMs`, ambos enteros entre 0 y 60.000 ms. Cualquier ruta de puerto, comando serial, propiedad desconocida, arreglo o valor fuera de rango se rechaza antes de alcanzar el adaptador.

S2-069 no cambia esta superficie: puerto, baud rate y parser se resuelven únicamente en main. Cambiar entre simulador y serial no requiere modificar preload, IPC, tipos globales ni componentes React.

## Prueba integrada de main/preload (S2-070)

`scale-main-preload.integration.test.cjs` enlaza el simulador y los handlers reales de main con la API real publicada por preload y el filtro `readStableWeightForCart`. Cubre lectura estable, dato corrupto, desconexión y reconexión; únicamente las lecturas estables, positivas y válidas llegan al colector que representa el carrito. El POS consume el mismo filtro mediante `window.api.scale`, sin acceder a `ipcRenderer` ni a configuración serial.

Esta verificación se ejecuta sin hardware y con Electron simulado en memoria. Puerto, driver, parser y dispositivo físicos permanecen [NO VERIFICADOS].

## Conexiones
- Estructura: [[estructura]]
- Hardware: [[hardware]]
- Auth: [[auth-y-roles]]

## Fuentes
- `globalmart-frontend/src/preload.ts`
- `globalmart-frontend/src/index.ts`
- `globalmart-frontend/src/hardware/scale-contract.ts`
- `globalmart-frontend/src/hardware/simulated-scale-adapter.ts`
- `globalmart-frontend/src/hardware/serial-scale-adapter.ts`
- `globalmart-frontend/src/hardware/scale-adapter-factory.ts`
- `globalmart-frontend/src/hardware/scale-ipc.ts`
- `globalmart-frontend/src/hardware/scale-cart.ts`
- `globalmart-frontend/src/pages/Pos.tsx`
- `globalmart-frontend/src/types/global.d.ts`
- `globalmart-frontend/tests/scale-contract.test.cjs`
- `globalmart-frontend/tests/simulated-scale-adapter.test.cjs`
- `globalmart-frontend/tests/serial-scale-adapter.test.cjs`
- `globalmart-frontend/tests/scale-ipc.test.cjs`
- `globalmart-frontend/tests/scale-main-preload.integration.test.cjs`
- `diagramas/rodrigo/R1_screen_flow_electron.drawio.png`
