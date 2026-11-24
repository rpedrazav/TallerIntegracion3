# S2-112 / TI3-743 — E2E de Admin y venta (entorno integrado)

Validación local: **2026-11-15, Windows, 2/2 aprobadas, 0 fallidas, 0 omitidas, exit code 0**. Comando: `npm run test:e2e:integrado` desde `globalmart-frontend`. [Resultado Playwright JSON](resultados.json) (corrida final, con el redondeo CLP ya aplicado). Antes del redondeo se repitió 2 veces seguidas (`--repeat-each 2`): 4/4 aprobadas.

**Aceptación:** el camino principal termina sin errores de API ni totales divergentes. ✅ Se cumplió después de corregir los cuatro defectos que se describen abajo.

## Qué se ejecuta

Electron real (main, preload y renderer productivos) contra **MS-1, MS-2, MS-3, MS-4 y MS-5 en localhost**, PostgreSQL y Kafka de `docker compose`. **No hay mocks**: Playwright no intercepta ninguna solicitud. La prueba falla si cualquier respuesta HTTP es ≥ 400, salvo el 404 esperado de `GET /api/turnos/activo` antes de abrir el turno.

| # | Paso | Verificación | Captura |
|---|---|---|---|
| 1 | `admin@demo.cl` inicia sesión | Redirige a `#/admin` (rol ADMIN del JWT real) | — |
| 2 | ADMIN crea un producto en Catálogo | `POST /products` → 201 y fila visible | [01](capturas/01-admin-producto-creado.png) |
| 3 | `cajero@demo.cl` inicia sesión y abre turno con $20.000 | Sin turno → apertura → carrito vacío | [02](capturas/02-turno-abierto.png) |
| 4 | Agrega el producto ($1.025), inicia venta y cambia la cantidad a 2 | Σ ítems = subtotal; subtotal + IVA = total; importes CLP enteros: $2.050 + IVA 389,5 → **$390** = **$2.440**; la pantalla muestra exactamente los importes de MS-5 | [03](capturas/03-carrito-totales.png) |
| 5 | Cobro en efectivo ($4.000 por $2.440) | Venta COMPLETADA, total igual al del carrito, vuelto $1.560 calculado por MS-5 y mostrado en el modal | [04](capturas/04-cobro-efectivo.png) |
| 6 | `GET /ventas/{id}` | Estado y totales persistidos iguales a los cobrados | — |
| 7 | Cuadre: se declaran $2.440 en denominaciones | `efectivo_esperado` = declarado = $2.440, diferencia $0 | [05](capturas/05-cuadre.png) |
| 8 | Cierra el turno | `POST /api/turnos/cerrar` → 200; luego `GET /api/turnos/activo` → 404 | [06](capturas/06-turno-cerrado.png) |

Código: [spec](../../../globalmart-frontend/e2e/electron/admin-venta.integrated.spec.ts). Reproducción: [README Electron](../../../globalmart-frontend/e2e/electron/README.md#s2-112--ti3-743--admin-y-venta-integrados).

## Fallos encontrados

| # | Defecto | Evidencia | Estado |
|---|---|---|---|
| F1 | **MS-2 escuchaba en `:5125`**, mientras que MS-5 (`appsettings.json`), `run-apis.*`, `smoke-test.sh` y la documentación usan `:5239`. El IVA de MS-5 no tenía a quién llamar. | `curl localhost:5239/health` → sin respuesta; log de MS-2: `Now listening on: http://localhost:5125`. | **Corregido**: `launchSettings.json` de MS-2 vuelve a `:5239`. |
| F2 | **`admin@demo.cl` no existía en entornos ya sembrados**: el seed de MS-1 solo corría si faltaba el tenant, así que las BD creadas antes de que se agregara el ADMIN al seed nunca lo recibían. | `POST /auth/login` admin → 401; tabla `Usuarios` solo tenía `cajero@demo.cl`. | **Corregido**: el seed crea el tenant y cada usuario demo si faltan (idempotente). |
| F3 | **El cuadre contaba el vuelto como efectivo en caja**: `efectivo_esperado` sumaba `Pago.Monto` (monto *recibido*), sin restar el vuelto entregado. La prueba de consola `CobroCuadreTest` fijaba ese valor erróneo (175 en vez de 150). | Venta de $2.380 pagada con $4.000 → `efectivo_esperado` = **4.000** (se esperaban 2.380). | **Corregido**: suma `Monto − Vuelto`; prueba actualizada a 150/50. |
| F4 | **Totales con centavos en CLP**: con precio $1.190 × 2, MS-5 calcula IVA $452,20 y total $2.832,20. No se puede pagar ni cuadrar con denominaciones CLP. | Venta `f487e5a6-…`: `Subtotal 2380.00 · Impuestos 452.20 · Total 2832.20` en `postgres-pos`. | **Corregido**: en tenants con moneda CLP, MS-2 redondea el subtotal de cada línea y su IVA a peso entero (,5 a ,9 sube; ,1 a ,4 baja: `MidpointRounding.AwayFromZero`). La moneda viene de la config del tenant en MS-1. MS-5 ya no recalcula el subtotal de línea y usa el de MS-2, así que Σ líneas = subtotal. Las otras monedas conservan 2 decimales. Pruebas: 5 casos nuevos en `TaxCalculatorService_Tests`; la spec E2E exige $2.050 / $390 / $2.440. |

Otros hallazgos sin impacto en la aceptación:

- **Kong → servicios del host: 503.** `smoke-test.sh` cae al acceso directo. Por eso la spec usa `API_MODE=local` y **no acredita la ruta por Kong**.
- **`CobroCuadreTest` tiene 2 FAIL preexistentes** (`sale.completed` por productor). MS-5 publica por outbox desde S2-007 y esa prueba de consola no está en la solución ni en CI. Hay que actualizarla aparte.
- **La suite Playwright de navegador (`npm run test:e2e`) no se ejecutó**: falta el binario Chromium de Playwright en esta máquina (`npx playwright install chromium`). No forma parte del gate integrado.
- Con CLP entero, un total como $2.832 no se puede pagar exacto con la moneda más chica de la UI ($10). El redondeo del efectivo a la decena no forma parte de esta tarea. [NO VERIFICADO] si se requiere.
- El cuadre de MS-5 **no incluye el fondo inicial**. Coincide con la etiqueta de la UI «Efectivo esperado (ventas)», así que la spec declara solo las ventas.

## Alcance y límites

- Acredita MS-1 (login real, roles), MS-3 (alta de producto), MS-5 (turno, venta, cobro, cuadre, cierre) y el IVA de MS-2 a través de MS-5, todo con JWT firmado.
- **No acredita** Kong, el despliegue en el clúster, la entrega a Kafka ni el descuento de stock en MS-4 (S2-096..S2-100), tarjeta ni pesaje (S2-113).
- Usa la BD de desarrollo local: cada corrida crea un producto `E2E S2-112 <timestamp>` y un turno del cajero demo. Si una corrida anterior dejó el turno abierto, la spec lo cierra por API antes de empezar.
- Trazas y perfiles quedan en `globalmart-frontend/test-results/electron/`, ignorados por Git. Las trazas pueden contener el JWT de prueba, así que no se publican.

## Verificaciones de regresión

- `npm run typecheck`: exit 0.
- `npm run test:e2e:electron` (mock, excluye `@integrado`): 2/2.
- `GlobalMart.Tests`: 52/52 (incluye los 5 casos CLP).
- `dotnet build GlobalMartOS.sln`: 0 errores.
- `CobroCuadreTest`: los checks de cobro y cuadre pasan (8/8); los 2 FAIL de `sale.completed` son preexistentes.
