---
id: electron-ipc
tipo: frontend
titulo: Electron IPC — Comunicación Main/Renderer y Seguridad (S2-088)
estado: implementado
fuentes: [globalmart-frontend/src/preload.ts, globalmart-frontend/src/index.ts, globalmart-frontend/src/devices/]
verificado_contra_codigo: true
ultima_revision: 2026-10-25
depende_de: [estructura, hardware, limites-terminal-simulado]
publica: []
consume: []
reglas: []
---
# Electron IPC — Comunicación Main/Renderer y Seguridad (S2-088 / TI3-719)

> Expone APIs fuertemente tipadas y seguras desde el proceso principal (Node.js) al renderer (React) mediante `contextBridge`.

## 1. Postura de Seguridad (S2-088)

Para mitigar riesgos de seguridad y cumplir el criterio de aceptación (*"No hay API genérica de ejecución o acceso serial en renderer"*):
- `contextIsolation: true` forzado en `BrowserWindow`.
- `nodeIntegration: false` para evitar que scripts del renderer invoquen APIs nativas de Node.js.
- `sandbox: true` activado para aislar el contexto de renderizado.
- **Sin APIs genéricas de ejecución:** No se expone `shell.openExternal` libre, `child_process.exec`, `eval`, ni acceso serial crudo (`SerialPort`) al renderer.

## 2. Canales de Dispositivos Implementados

| Canal IPC | Validador de Argumentos | Propósito |
|---|---|---|
| `devices:terminal:iniciar` | `validateIniciarParams` | Valida que `monto` sea número positivo finito (0 < x ≤ 100M). |
| `devices:terminal:obtenerEstado` | Sin argumentos | Retorna estado del terminal (`IDLE`, `ESPERANDO_TARJETA`, etc.). |
| `devices:terminal:cancelar` | Sin argumentos | Cancela operación en curso en el terminal. |
| `devices:terminal:autorizar` | `validateSimulacionAction` | Whitelist de acciones: `['APROBAR', 'RECHAZAR', 'TIMEOUT', 'ERROR_LECTURA']`. |
| `devices:scale:obtenerPeso` | Sin argumentos | Retorna peso neto estable en kg. |
| `devices:scale:tarar` | Sin argumentos | Tara la balanza a cero. |
| `devices:scale:simularPeso` | `validatePeso` | Valida número finito positivo entre 0 y 500 kg. |

## 3. Preload Bridge (`src/preload.ts`)

El renderer accede exclusivamente a través de:
```typescript
window.api.devices.terminal.iniciar({ monto: 5000 });
window.api.devices.terminal.autorizar('APROBAR');
window.api.devices.scale.obtenerPeso();
```

## Conexiones
- Estructura: [[estructura]]
- Hardware: [[hardware]]
- Límites Terminal: [[limites-terminal-simulado]]
- Auth: [[auth-y-roles]]
