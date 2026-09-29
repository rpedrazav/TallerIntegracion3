---
id: 005-electron-frontend
tipo: decision
titulo: "ADR-005: Electron + React como frontend Desktop"
estado: implementado
fuentes: [GlobalMart_ContextMaster.md#sec19, globalmart-frontend/package.json]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: []
publica: []
consume: []
reglas: [RNF-06, RNF-09]
---
# ADR-005: Electron + React como frontend Desktop

**Estado:** APROBADA

## Decisión

Usar **Electron + React + TypeScript** para el frontend del POS. No web app (browser).

## Justificación

| Criterio | Electron | Web (browser) |
|----------|----------|---------------|
| Acceso a hardware (Serial/USB) | ✅ Node.js nativo | ❌ Web Serial API (limitada) |
| Modo offline | ✅ electron-store local | ❌ Service Worker limitado |
| Instalación en mostrador | ✅ Ejecutable nativo | ❌ Browser sin gestión |
| Impresora ESC/POS | ✅ Node.js raw | ❌ Solo via web bluetooth |
| Cajon de dinero | ✅ SerialPort | ❌ No disponible |
| Actualizaciones automáticas | ✅ electron-updater | ✅ |

El punto crítico es el **acceso al hardware** del mostrador (balanza, impresora, cajón de dinero), que requiere acceso a puertos nativos del OS.

## Consecuencias

- Paquete descargable de Electron Forge para Windows/macOS/Linux (RNF-06)
- El proceso main de Electron maneja todos los IPC de hardware
- La comunicación con APIs backend sigue siendo HTTP (axios en renderer)
- Se necesita gestionar la distribución de actualizaciones vía electron-updater

## Conexiones
- Frontend: [[estructura]], [[electron-ipc]]
- Hardware: [[hardware]]

## Fuentes
- `GlobalMart_ContextMaster.md` §19
- `globalmart-frontend/forge.config.ts`
