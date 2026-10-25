---
id: electron-ipc
tipo: frontend
titulo: Electron IPC — Comunicación Main/Renderer
estado: parcial
fuentes: [globalmart-frontend/src/preload.ts, globalmart-frontend/src/index.ts, globalmart-frontend/src/hardware/scale-contract.ts, globalmart-frontend/tests/scale-contract.test.cjs]
verificado_contra_codigo: true
ultima_revision: 2026-10-24
depende_de: [estructura]
publica: []
consume: []
reglas: []
---
# Electron IPC — Comunicación Main/Renderer

> `preload.ts` expone APIs seleccionadas del proceso principal (Node.js) al renderer (React). El contrato de balanza está definido, pero todavía no se publica ni tiene handlers IPC.

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

Actualmente expone `ping`, `getToken`, `setToken` y `logout`. No expone Node.js, puertos seriales ni una API de balanza.

## Handlers IPC diseñados (de diagramas)

Según `diagramas/rodrigo/R1_screen_flow_electron.drawio.png`:
- IPC para abrir drawer/cajón de dinero
- IPC para SerialPort (balanza, impresora)
- IPC para gestión de ventanas

**Estado:** el IPC de autenticación está implementado; los handlers de hardware siguen planificados.

## Contrato de balanza por IPC (S2-066)

`ScaleRendererApi` define únicamente operaciones de alto nivel: conectar, desconectar, consultar estado y leer peso estable. No contiene ruta de puerto, baud rate ni objetos de `serialport`. El adaptador físico y el simulador deberán implementar `ScaleAdapter` en Electron main; una tarea posterior conectará sus métodos con `ipcMain` y `contextBridge`.

```typescript
// Diseño futuro del preload: delegación exclusiva por IPC.
const scaleApi: ScaleRendererApi = {
  connect: () => ipcRenderer.invoke('scale:connect'),
  disconnect: () => ipcRenderer.invoke('scale:disconnect'),
  readStableWeight: (options) => ipcRenderer.invoke('scale:read-stable', options),
  getState: () => ipcRenderer.invoke('scale:get-state'),
};
```

## Conexiones
- Estructura: [[estructura]]
- Hardware: [[hardware]]
- Auth: [[auth-y-roles]]

## Fuentes
- `globalmart-frontend/src/preload.ts`
- `globalmart-frontend/src/index.ts`
- `globalmart-frontend/src/hardware/scale-contract.ts`
- `globalmart-frontend/tests/scale-contract.test.cjs`
- `diagramas/rodrigo/R1_screen_flow_electron.drawio.png`
