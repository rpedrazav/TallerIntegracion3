# S2-024 — Electron POS local automatizado

Requisitos: Node.js 22, npm y sesión gráfica de Windows. Desde `globalmart-frontend`:

```powershell
npm ci
npm run ci:local
```

`ci:local` ejecuta TypeScript, compila main/preload/renderer desde `src/` y arranca Electron mediante Playwright. Para repetir solo E2E: `npm run test:e2e:electron`. No requiere Chrome, Playwright browsers, Docker, servicios .NET, `.env`, credenciales personales ni secretos de CI. La instalación inicial requiere acceso al registro npm y a la descarga de Electron. En Linux requiere un display (por ejemplo Xvfb) y dependencias de Electron; esa plataforma no está validada aquí.

La configuración separada `playwright.electron.config.ts` no altera las pruebas de navegador existentes. El workflow `.github/workflows/electron-e2e.yml` usa Windows y ejecuta el mismo comando local, con timeout y subida de evidencias incluso si falla.

## Cobertura y límites

- Login por formulario con `cashier@example.test` / `dummy-e2e-only`, tenant ficticio y token exclusivo del mock.
- Ausencia de turno (404), redirección a apertura, envío de fondo inicial y sucursal del token, confirmación y entrada al carrito vacío.
- Uso del JWT simulado en peticiones protegidas; sesión real por IPC/electron-store conservada tras recargar.
- Login rechazado (401) sin sesión ni acceso al carrito.
- Perfil nuevo por prueba dentro de `test-results/electron`, sin leer ni modificar el perfil del desarrollador. Estos perfiles son descartables y están ignorados por Git.

Las APIs se interceptan en Playwright con estado nuevo por prueba y métodos/rutas explícitos. Electron bloquea destinos HTTP/HTTPS distintos del origen simulado; Playwright aborta y hace fallar las solicitudes sin mock. `127.0.0.1:4199` es solo un destino interceptado: no se abre un servidor ni se requiere ese puerto libre. No se inyecta `window.api`; se usa el preload real.

Este esqueleto acredita navegación de Electron con APIs simuladas. No acredita autenticación criptográfica, aislamiento RN-01 de backend, persistencia de ventas, cobro ni Kafka. Para aislamiento real de ventas consultar `docs/qa/S2-023/reporte.md`; las pruebas existentes de carrito/cobro permanecen separadas.

## Diagnóstico

Reporte: `npx playwright show-report playwright-report/electron`.
En fallos se conserva `trace.zip` y `failure.png` en la carpeta de la prueba bajo `test-results/electron`. Abrir una traza con `npx playwright show-trace <ruta/trace.zip>`.
Los perfiles no se publican como artefactos de CI. Nunca reutilizar los tokens dummy contra un backend real.
