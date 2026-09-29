---
id: electron-ipc
tipo: frontend
titulo: Electron IPC — Comunicación Main/Renderer
estado: parcial
fuentes: [globalmart-frontend/src/preload.ts, globalmart-frontend/src/index.ts]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [estructura]
publica: []
consume: []
reglas: []
---
# Electron IPC — Comunicación Main/Renderer

> El preload.ts expone APIs seguras del proceso principal (Node.js) al renderer (React). Actualmente el preload existe pero con contenido NO VERIFICADO en detalle.

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

**Contenido no verificado en detalle.** Ver `globalmart-frontend/src/preload.ts`.

## Handlers IPC diseñados (de diagramas)

Según `diagramas/rodrigo/R1_screen_flow_electron.drawio.png`:
- IPC para abrir drawer/cajón de dinero
- IPC para SerialPort (balanza, impresora)
- IPC para gestión de ventanas

**Estado:** NO VERIFICADO si están implementados en el código actual.

## Hardware por IPC (PLANIFICADO)

```typescript
// Diseño esperado (NO en código actual):
// main process
ipcMain.handle('balanza:leer-peso', async () => {
  const puerto = new SerialPort({ path: 'COM3', baudRate: 9600 });
  // ... leer peso
});

// renderer (vía contextBridge)
const peso = await window.electronAPI.balanza.leerPeso();
```

## Conexiones
- Estructura: [[estructura]]
- Hardware: [[hardware]]
- Auth: [[auth-y-roles]]

## Fuentes
- `globalmart-frontend/src/preload.ts`
- `globalmart-frontend/src/index.ts`
- `diagramas/rodrigo/R1_screen_flow_electron.drawio.png`
