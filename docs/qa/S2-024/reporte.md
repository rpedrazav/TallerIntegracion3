# S2-024 / TI3-655 — Entorno local automatizado Electron POS

Fecha: 2026-10-18. Entorno verificado: Windows, Node 22.19.0, dependencias instaladas del lockfile.

## Resultado

`npm run ci:local` terminó con código 0: TypeScript aprobado, compilación de main/preload/renderer aprobada y **2 pruebas aprobadas en 5,9 s**:

1. Login con credenciales ficticias → ausencia de turno (404) → apertura por formulario con fondo 20000 y sucursal del JWT simulado → POS vacío. Verifica Authorization, cuerpo del login/apertura, una sola apertura y persistencia real del token mediante IPC tras recarga.
2. Login rechazado (401): muestra error, conserva token nulo y no presenta carrito. El perfil es nuevo e independiente del caso anterior.

Se corrigió una carrera detectada durante validación: `firstWindow()` podía seleccionar DevTools abierto por el main real. La fixture ahora espera y selecciona la ventana `file:` de la aplicación. También se verificó que el bloqueo de red preserve la interceptación del origen simulado. La ejecución necesitó permiso para abrir Electron fuera del entorno restringido del agente.

## Reproducción

Desde `globalmart-frontend`, ejecutar `npm ci` y `npm run ci:local`. No se requieren datos personales, `.env`, servicios backend ni cuentas externas. [Guía completa](../../../globalmart-frontend/e2e/electron/README.md).

Reporte local regenerable en `globalmart-frontend/playwright-report/electron/index.html`; fallos conservan captura y traza en `globalmart-frontend/test-results/electron`. Son salidas ignoradas por Git. El workflow `.github/workflows/electron-e2e.yml` conserva informes siete días sin publicar perfiles. Ejecución en GitHub Actions [NO VERIFICADO]; el comando equivalente se verificó localmente.

## Alcance

Se cumple el esqueleto y arranque autónomo solicitado para login, turno y entrada a POS. Se ejecutan main, preload, electron-store y router reales; las APIs son simuladas por caso y el token dummy no constituye validación criptográfica. Las peticiones inesperadas fallan en lugar de acceder a servicios remotos.

No certifica venta/cobro funcional, persistencia PostgreSQL, eventos Kafka ni aislamiento multi-tenant de backend. Las pruebas RN-01 reales están documentadas en [S2-023](../S2-023/reporte.md); las suites existentes de carrito y cobro en navegador se mantienen separadas.
