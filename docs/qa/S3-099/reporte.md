# S3-099 — Reproducir y listar las fallas del inventario

Fecha: 2026-11-27. Rama: `dev`. Commit base: `d986c8ba4d89bd4d9218b221d2e01946a7f36e05`.
Meta: Funcionalidad de punta a punta.

## Objetivo y aceptación

Recorrer Admin Inventario (listado, ajuste), recepción, lotes por vencer y venta → descuento de stock en Docker Compose y en el clúster, con dos tenants.

Entrega: lista de fallas con pasos, resultado esperado y real, registrada en YouTrack.
Aceptación: cada falla tiene responsable dentro de S3-100..S3-103 o queda marcada como fuera de alcance.

**Estado: diagnóstico local realizado; cierre pendiente de recorrido remoto desbloqueado y registro en YouTrack.** No se corrigió código productivo. No se administró el clúster.

## Entorno y alcance real

- Infraestructura Docker existente: PostgreSQL, Kafka, Kong. APIs levantadas con `run-apis.sh` en Development.
- Ambos tenants autenticados realmente: Demo (`aaaaaaaa-0000-0000-0000-000000000001`) y Don Pepe (`aaaaaaaa-0000-0000-0000-000000000002`). Sucursales `cccccccc-0000-0000-0000-000000000001` y `cccccccc-0000-0000-0000-000000000002`.
- Renderer React real en Chromium, servido por `e2e/serve.cjs`; puente de sesión Electron simulado con JWT real. No es una ejecución de Electron empaquetado.
- Para recorrer la UI se reenviaron las solicitudes a las APIs locales reales mediante Playwright: Identidad 5124, Catálogo 5203, Inventario 5205, POS 5000. No se simularon respuestas ni persistencia. **Este recorrido omite Kong**, que devolvió 503.
- Verificación remota limitada a login y TLS; no se enviaron ventas ni ajustes al clúster.
- Las credenciales de demo se usan solo en los scripts; no se guardaron JWT en la evidencia.

## Resultado de la ejecución

| Comprobación | Demo | Don Pepe |
|---|---|---|
| Login local, catálogo y sucursales | HTTP 200; catálogo existente de 3 productos | HTTP 200; 8 productos de Daniel |
| Stock inicial de sucursal activa | HTTP 200, vacío | HTTP 200, vacío |
| Ajuste desde UI | Confirmado y persistido: 10 | Confirmado y persistido: 10 |
| Recepción desde UI | HTTP 201; agrega 2; stock 12 | HTTP 201; agrega 2; stock 12 |
| Lote por vencer vía API | Lote de 2 recuperado | Lote de 2 recuperado |
| Lote por vencer visible en UI | No existe tabla de lotes | No existe tabla de lotes |
| Consulta inicial a sucursal ajena | HTTP 200, lista vacía | HTTP 200, lista vacía |
| Venta y cobro efectivo por API | HTTP 201/200, COMPLETADA | HTTP 201/200, COMPLETADA |
| Stock 6 segundos después del cobro | Sigue 12; esperado 11 | Sigue 12; esperado 11 |
| Cierre del turno creado por la prueba | HTTP 200 | HTTP 200 |
| Stock mediante Kong local | HTTP 503: name resolution failed | Igual |
| Login remoto con contrato actual | HTTP 400: exige TenantId | Igual |

`probar-api.mjs`, `probar-ui.cjs` y `probar-ventas.mjs` terminaron con exit code 0. El script UI verificó con aserciones ajuste, recepción, stock 12 y lote 2 en ambos tenants. Los otros scripts son sondas que registran respuestas, **no suites con todos los casos aprobados**. El no descuento es un defecto observado, no un éxito de prueba.

## Fallas y asignación

### INV-01 — Stock agregado sin lotes rechazado

- Responsable: Sebastián, **S3-100**.
- Pasos de reproducción a aislar: crear stock 10 sin lotes; completar venta de 1; procesar su `sale.completed`.
- Esperado: stock 9, movimiento sin lote y evento confirmado.
- Actual confirmado por inspección: `StockRepository.Descontar` retorna null cuando la suma de lotes es menor que la cantidad; con cero lotes rechaza aunque el agregado alcance.
- Fuente: `src/WarehouseInventoryService/Repositories/StockRepository.cs`, método `Descontar`, condición `lotes.Sum(l => l.Cantidad) < cantidad`.
- **[NO VERIFICADO] reproducción aislada extremo a extremo**: el consumidor local está bloqueado previamente por INV-07. No confundir ese evento sin stock con este caso de stock suficiente sin lotes. S3-100 debe incluir la prueba aislada.

### INV-02 — No hay entrega HTTP cuando Kafka está desactivado

- Responsable: Sebastián, **S3-101**.
- Pasos a repetir en entorno habilitado: Kafka desactivado, vender 1 con stock suficiente, consultar inventario.
- Esperado según S3-101: outbox entrega el mismo evento al endpoint interno autenticado; descuento idempotente por tenant.
- Actual confirmado por código: `OutboxPublisherService` depende de `IKafkaProducerService` y publica exclusivamente por ese transporte. MS-4 omite su consumidor cuando `Kafka:Enabled=false`; no existe la entrega alternativa requerida.
- Fuentes: `src/POSCartService/Services/OutboxPublisherService.cs`, `src/POSCartService/Program.cs`, `src/WarehouseInventoryService/Program.cs`.
- **[NO VERIFICADO] efecto de una venta nueva en clúster**: login/TLS bloquean el recorrido. La ausencia de Kafka en clúster procede del plan, no de una inspección del clúster realizada aquí.

### INV-03 — Lotes recibidos no aparecen en Admin Inventario

- Responsable: Sebastián, **S3-102**. Reproducido en ambos tenants.
- Pasos: entrar como ADMIN; ajustar stock a 10; recibir 2 con vencimiento a 7 días y ubicación `QA S3-099`; consultar `GET /api/lotes?porVencer=30&sucursal_id=<sucursal>`.
- Esperado: tabla con producto, cantidad y vencimiento, refrescada tras recepción.
- Actual: API devuelve lote 2 y UI confirma recepción/stock 12, pero solo muestra la tabla de stock y formularios; no hay tabla ni consulta de lotes.
- Evidencia: `ui-resultados.json`, `inventario-demo.png`, `inventario-donpepe.png`. Fuente: `globalmart-frontend/src/pages/AdminInventario.tsx`.

### INV-04 — Seed limitado a Demo y a sucursal incorrecta

- Responsable: Sebastián, **S3-103**. Reproducido.
- Pasos: login ADMIN Don Pepe; `POST http://localhost:5205/seed` con su JWT. Consultar stock de la sucursal activa en ambos tenants.
- Esperado: seed de productos y sucursal propios; inventarios distintos y visibles.
- Actual: Don Pepe recibe 403. Demo tiene 30 stocks asociados a `bbbbbbbb-0000-0000-0000-000000000001`, pero Identidad devuelve `cccccccc-0000-0000-0000-000000000001`; la lista inicial de esta última está vacía.
- La BD local Demo contiene además un catálogo antiguo de 3 productos con UUID distintos a los 30 IDs fijos del seed. Esto describe los datos locales, no invalida el trabajo de Daniel ni demuestra un problema de su seed en una BD limpia.
- Fuentes: `SeedController.RunSeed`, `SeedData.DemoSucursalId`, `SeedData.ProductoIds`. Evidencia: `api-resultados.json`, `entorno.txt`.
- S3-103 debe usar catálogo/sucursal del tenant autenticado y evitar duplicar o sobrescribir datos existentes.

### INV-05 — Kong local no resuelve el upstream

- **Fuera de alcance S3-100..103**. Derivar a infraestructura/rutas con el scrum master.
- Pasos: con APIs locales respondiendo, pedir `/api/stock?sucursal_id=...` por `http://localhost:8000` con JWT válido de cada tenant.
- Esperado: misma respuesta que la API directa.
- Actual: 503 `name resolution failed`; API directa 200. No se modificó Kong ni se afirmó la causa DNS concreta.
- Evidencia: `api-resultados.json`. Impide validar el recorrido local normal completo por gateway.

### INV-06 — Despliegue remoto incompatible con login actual y TLS de inventario

- **Fuera de alcance S3-100..103**. Derivar a despliegue/Ingress con el scrum master.
- Pasos: POST login remoto con `{tenantCodigo,email,password}` para Demo y Don Pepe; acceder a salud de `inventory-rpedraza.dev.censei.cl` usando validación TLS normal.
- Esperado: contrato actual de login y certificado válido para inventario.
- Actual: ambos logins responden 400, `TenantId` obligatorio; Inventario falla con `ERR_TLS_CERT_ALTNAME_INVALID` (curl también reportó código 60).
- Evidencia: `api-resultados.json`. No se desactivó TLS ni se alteró el clúster. Confirmar despliegue con el equipo antes de repetir.

### INV-07 — Evento histórico sin stock bloquea el consumidor local

- **Fuera del arreglo específico de S3-100**: S3-100 exige seguir reintentando cuando el stock realmente es insuficiente. Requiere saneamiento autorizado de fixtures/entorno, no saltar eventos silenciosamente.
- Pasos: iniciar APIs contra Kafka/BD locales existentes; observar offset 40 de `sale.completed`; completar las ventas diagnósticas y revisar stock.
- Esperado de las nuevas ventas: stock 12 → 11 tras procesar sus eventos.
- Actual: ambas ventas completadas y eventos publicados por outbox; stock sigue 12. Se repite offset 40 para producto `22222222-2222-2222-2222-222222222222`, sucursal `3e9f309a-06f8-4082-878a-fd2ab5975403`, evento `f4be0d5d-0863-4e74-ba0f-fbf2b523ff40`. La consulta de BD no encuentra stock para ese producto.
- Evidencia: `venta-resultados.json`, `eventos.log`, `entorno.txt`. La asociación del bloqueo con las ventas pendientes es una inferencia consistente con los registros; no se manipuló el offset para aislarla.
- No se borraron mensajes, datos ni grupos de consumidores.

### INV-08 — Healthcheck PostgreSQL mal formado

- **Fuera de alcance S3-100..103**. Infraestructura local.
- Pasos: inspeccionar healthcheck de `postgres-warehouse` y probar consulta SQL.
- Esperado: `healthy` cuando PostgreSQL admite consultas.
- Actual: `unhealthy`; `pg_isready: error: too many command-line arguments (first is "warehouse_db")`. PostgreSQL sí admite consultas y las operaciones anteriores persistieron.
- Evidencia: `entorno.txt`. No atribuir los 503 de Kong a este indicador sin comprobarlos.

## Reproducción y artefactos

Desde la raíz del repositorio, con infraestructura/API locales disponibles:

```bash
node docs/qa/S3-099/probar-api.mjs
# En otra terminal, renderer de diagnóstico:
cd globalmart-frontend
node e2e/serve.cjs
# Volver a la raíz para los siguientes comandos:
node docs/qa/S3-099/probar-ui.cjs
node docs/qa/S3-099/probar-ventas.mjs
```

**Los dos últimos scripts modifican datos locales de demo.** Usarlos solo en una BD de diagnóstico: UI fija stock a 10 y recibe 2; ventas crea/cobra una venta por tenant. No son idempotentes y una nueva ejecución puede alterar lotes/ventas previas. `probar-api.mjs` intenta el seed de Don Pepe y podría sembrar datos después de implementar S3-103.

Esta ejecución dejó dos recepciones, ajustes y ventas locales de diagnóstico. Se cerraron únicamente los dos turnos creados por la prueba; no se borró historial. Los eventos pendientes pueden descontar stock más adelante si se desbloquea el consumidor. Los scripts conservan estos datos para inspección.

- `api-resultados.json`: estados HTTP iniciales, datos propios y fallos remotos.
- `ui-resultados.json`: stock/lotes y texto visible después de ajuste y recepción.
- `venta-resultados.json`: creación, cobro, stock posterior y cierre.
- `eventos.log`: extracto del publicador y reintentos del consumidor, sin JWT.
- `entorno.txt`: consultas SQL y healthcheck.
- Capturas `inventario-demo.png` y `inventario-donpepe.png`.

## Pendientes para cerrar

1. Registrar INV-01..08 en YouTrack, relacionando INV-01..04 a S3-100..103 y marcando INV-05..08 fuera de alcance. Se solicitó el enlace de S3-099; aún no hay registro verificable ni IDs de incidencias. Este reporte contiene los textos listos para copiar.
2. Repetir recorrido normal por Kong al resolver INV-05 y recorrido remoto con ambos tenants al resolver INV-06.
3. Aislar venta sin lotes con stock suficiente al implementar S3-100, sin confundirla con el evento histórico sin stock.

No se ejecutó la batería completa ni typecheck: no cambió código productivo. No se declara aprobación total ni aislamiento exhaustivo; la consulta ajena vacía se hizo antes de cargar estos fixtures. No se actualizó el grafo de implementación porque no cambió el comportamiento del producto.
