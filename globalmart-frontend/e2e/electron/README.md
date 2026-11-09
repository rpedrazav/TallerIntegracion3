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

## S2-055 / TI3-686 — Tarjeta con backend y simulador reales

Desde la raíz del repositorio, con Docker Desktop iniciado:

```powershell
docker run --detach --rm --name globalmart-s2055-postgres -e POSTGRES_PASSWORD=s2055_test_only -e POSTGRES_DB=s2055 -p 127.0.0.1:55455:5432 postgres:16-alpine
docker exec globalmart-s2055-postgres pg_isready -U postgres -d s2055
$env:GLOBALMART_TEST_POSTGRES='Host=127.0.0.1;Port=55455;Database=s2055;Username=postgres;Password=s2055_test_only'
cd globalmart-frontend
npm ci
npm run test:e2e:card
# Al terminar:
docker stop globalmart-s2055-postgres
```

Requisitos adicionales: .NET 8 SDK, PostgreSQL disponible y puerto `127.0.0.1:4199` libre. Si `pg_isready` aún no informa disponibilidad, repetir antes de ejecutar las pruebas. No ejecutar simultáneamente dos instancias de esta suite. El contenedor es exclusivo de pruebas; no se monta un volumen ni se usa la base del desarrollador. Las credenciales mostradas son únicamente de ese contenedor local desechable.

`run-card.cjs` compila Electron y habilita `GLOBALMART_ELECTRON_E2E=1` para los dos casos xUnit `Plan=S2-055`. Sin esa variable los casos se omiten en la suite backend ordinaria, que no dispone necesariamente de escritorio. Una vez habilitados, dependencias ausentes producen fallo, no omisión. La ejecución gráfica en CI está [NO VERIFICADA]; este comando fue validado localmente en Windows.

Cada caso realiza login, abre turno, agrega un producto, selecciona tarjeta, crea venta y confirma cobro mediante controles reales. `ElectronCardE2ETests` aloja MS-5 mediante WebApplicationFactory y un puente HTTP local; no intercepta ni inventa sus respuestas. Usa JWT firmado y validado por MS-5, PostgreSQL migrado en un esquema único y el proceso real `Docker/simulator/server.js`. Limpia el esquema, cierra Electron y detiene el simulador al terminar, también ante fallo.

Identidad, catálogo e IVA son fixtures controladas: **no acredita MS-1/MS-2/MS-3 ni Kong desplegados**. La UI no expone selección de escenario; un handler exclusivo de prueba selecciona `sim-approved-v1` o `sim-declined-v1` en la solicitud al simulador, preservando tenant, venta, monto y clave. La respuesta proviene del simulador real y se compara con venta, pago, intento y evento. No se modifica código productivo ni se transmiten datos bancarios.

El evento se verifica en el **outbox durable**, con publicación deshabilitada. No acredita entrega Kafka ni descuento de inventario. Aprobación: COMPLETADA, un pago TARJETA con referencia coincidente y un `sale.completed` con IDs/importes coincidentes. Rechazo: PENDIENTE, intento RECHAZADO, cero pagos y cero eventos. Ambos exigen exactamente una autorización externa.

Evidencia por ejecución: `test-results/S2-055/<uuid>/<escenario>/` contiene tres capturas, `ui-result.json`, `verification.json`, `trace.zip` y perfil aislado. Los perfiles y trazas permanecen locales (las trazas pueden contener el JWT de prueba); publicar solo capturas y verificaciones sin tokens. Resultados xUnit: `docs/qa/S2-055/S2-055.trx`. Ver [reporte verificado](../../../docs/qa/S2-055/reporte.md).
