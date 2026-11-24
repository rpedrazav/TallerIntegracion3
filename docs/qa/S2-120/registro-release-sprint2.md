# Registro de Revisión de Release — Cierre Sprint 2 (S2-120 / TI3-751)

- **Fecha de auditoría:** `2026-11-24`
- **Rama auditada:** `dev` (`drifting_comet_launches_01h37`)
- **Alcance:** Compilación `.NET 8` + Electron/TypeScript, suites unitarias e integración con infraestructura Docker, enrutamiento Kong y controladores, migraciones EF Core, escaneo de secretos/PCI DSS (RN-02) y separación explícita entre metas simuladas e integración física pendiente.
- **Veredicto técnico:** **GO CONDICIONAL PARA CIERRE DE SPRINT 2 (MODALIDAD SIMULADA)** — 100% de builds y suites automatizadas locales aprobadas; metas físicas G4/G5 permanecen explícitamente bloqueadas a la espera de periféricos y protocolos reales.

---

## 1. Matriz de Comprobaciones Ejecutadas

| Dimensión | Comando / Evidencia Ejecutada | Resultado | Estado |
|---|---|---|---|
| **Build Backend (.NET 8 Release)** | `dotnet build GlobalMartOS.sln --configuration Release` | **0 errores**, 14 advertencias preexistentes (comentarios XML CS1570 en MS-3 y analizador xUnit2031) | ✅ APROBADO |
| **Suite Backend xUnit + Infraestructura** | `dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj -c Release` (PostgreSQL 16 x8, Kafka 7.6.1, Node Simulator) | **318 / 318 pruebas aprobadas**, 0 fallidas, 0 omitidas (`1 m 52 s`) | ✅ APROBADO |
| **Typecheck Frontend (TypeScript)** | `cd globalmart-frontend && npm run typecheck` (`tsc --noEmit`) | **0 errores de tipos** | ✅ APROBADO |
| **Pruebas Unitarias Frontend / IPC** | `cd globalmart-frontend && npm run test:unit` (`node --test tests/*.test.cjs`) | **25 / 25 pruebas aprobadas**, 0 fallidas (`~1.1 s`) | ✅ APROBADO |
| **Smoke Simuladores sin Hardware** | `./scripts/smoke-simuladores-frontend.sh` (`npm run smoke:simulators`) | **5 / 5 bloques aprobados** (balanza + terminal mock + bloqueo `RealTerminalAdapter`) | ✅ APROBADO |
| **Smoke Tarjeta y Correlación Logs** | `./scripts/smoke-tarjeta.sh` (`CobroTarjetaCorrelationIntegrationTests`) | **2 / 2 aprobadas** (aprobado, rechazado 422, timeout 504 y 0 PAN/CVV en `[COBRO_CORRELATION]`) | ✅ APROBADO |
| **Validación Kong y Smoke HTTP Integral** | `kong config parse` (`KONG_DATABASE=off`) + `REQUIRE_GATEWAY=true ./scripts/smoke-test.sh` | `parse successful`; Health x6 (`200`), Login `/api/auth/login` (`200`), `/api/products` (`200`), `/api/turnos/activo` (`404`), `/simulator/payments` (`200`) y rechazo PAN (`400 CARD_DATA_FORBIDDEN`) | ✅ APROBADO |
| **Auditoría de Migraciones EF Core** | Inspección de `src/*/Data/Migrations/` + arranque `db.Database.Migrate()` en PostgreSQL 16 limpio | **15 migraciones** distribuidas en los 8 MS aplican sin errores sobre PostgreSQL 16 (detalle en §2) | ✅ APROBADO CON NOTA |
| **Escaneo de Secretos y PCI DSS** | `.gitignore` + `git ls-files` + `git grep` de llaves privadas, tokens cloud y PANs en `src/` | **0 archivos `.env` o `kubeconfig` versionados**, **0 llaves privadas/tokens**, **0 PANs/CVVs** en código productivo | ✅ APROBADO |

---

## 2. Estado de Migraciones EF Core por Microservicio

| Microservicio | Base de Datos | Migraciones Registradas | Aplicación Automática en Arranque | Observaciones |
|---|---|---|---|---|
| **MS-1 Identity** | `tenant_db` (`:5433`) | `20260911165130_InitialCreate`, `20261001192629_AddSucursalZonaHoraria`, `20261001194639_AddSucursalUniqueNombreIndex` | Sí (`Program.cs`) + seed `cajero@demo.cl` y `admin@demo.cl` en `Development` | **Riesgo conocido:** `PUT/DELETE /users/{id}` operan sobre proyección `users`, mientras login/listado/alta usan `Usuarios` (ver §4). |
| **MS-2 Tax** | `tax_db` (`:5437`) | `InitialCreate`, `AddComprobante`, `AddComprobanteSequenceFunction`, `AddComprobanteCorrelativeFunction`, `FixComprobanteCorrelativeSequenceRace` (5 migraciones) | Sí (`Program.cs`) | Secuencia correlativa libre de carrera TOCTOU bajo concurrencia. |
| **MS-3 Catalog** | `catalog_db` (`:5435`) | `20261006212533_InitialCreate` (incluye jerarquía de categorías e índice `pg_trgm`) | Sí (`Program.cs`) | Baja lógica `IsActive=false` sin borrado físico. |
| **MS-4 Warehouse** | `warehouse_db` (`:5438`) | `20260913033948_InitialCreate_Warehouse`, `20260927195722_AddEventosKafkaProcesados` | Vía `dotnet ef database update` / host de test | Se corrigió en cierre S2-116/S2-120 `/health` y alineación de `Jwt:Issuer`/`Jwt:SecretKey` con MS-1. |
| **MS-5 POS & Cart** | `pos_db` (`:5436`) | `InitialCreate`, `AddOutboxMessages`, `AddIntentoPago`, `AddPagoGatewayReference`, `PreserveItemPricePrecision` (5 migraciones) | Sí (`Program.cs`) | Cubre transacción atómica venta + pago + outbox, idempotencia de tarjeta y precisión `numeric(20,4)`. |
| **MS-6 / MS-7 / MS-8** | `supply_db` / `analytics_db` / `loyalty_db` | `InitialCreate` / `InitialAnalyticsSchema` / `InitialLoyaltySchema` (1 c/u) | `[PLANIFICADO]` | Sin endpoints activos en Sprint 2. |

---

## 3. Distinción Explícita: Metas Simuladas vs Integración Física Pendiente

> [!IMPORTANT]
> **Criterio de Aceptación S2-120:** Ningún componente simulado se declara como integración de hardware físico certificada.

| Meta del Sprint 2 | Estado en Modalidad Simulada / Software | Estado en Integración Física Real | Condición para Cierre Físico |
|---|---|---|---|
| **G1: Página Administración** | ✅ **Verificada:** RBAC `AdminRoute`, alta/edición/desactivación lógica de usuarios, configuración numérica de IVA y gestión/baja lógica de productos. | N/A (100% software) | N/A |
| **G2: Carrito y Venta Funcional** | ⚠️ **Parcial avanzada:** Carrito autoritativo en MS-5, precios MS-3, IVA real MS-2 desde creación, cobro efectivo con vuelto, outbox `sale.completed` recuperable y descuento de stock en MS-4. Falta exponer `POST /ventas/{id}/anular` en `VentasController`. | Impresora térmica ESC/POS y apertura eléctrica de cajón portamonedas: **`[PLANIFICADO / PENDIENTE]`** | Disponer de impresora/gaveta ESC/POS y exponer handlers en Electron Main. |
| **G3: Cobro con Tarjeta (RN-02)** | ✅ **Verificada con Simulador:** `POST /ventas/{id}/cobrar-tarjeta`, idempotencia durable en `IntentosPago`, reconciliación `GET /ventas/{id}/intento-tarjeta` tras timeout, referencia opaca en `Pago`, 0 PAN/CVV en red, BD y logs. | Pasarela bancaria comercial productiva (Transbank/Stripe): **`[SIMULADA POR CONTEXTO ACADÉMICO]`** | Contrato comercial de adquirencia y llaves en Secret Manager externo. |
| **G4: Balanza de Mostrador** | ✅ **Verificada con Simulador:** Contrato `ScaleAdapter`, validación S2-061 (`>= 10 g`, precisión `1 g`), `SimulatedScaleAdapter`, esqueleto `SerialScaleAdapter`, IPC aislado (`scale:*`), lectura en `Pos.tsx` y cálculo pesable en MS-5. | **`[BLOQUEO FÍSICO EXPLÍCITO — NO VERIFICADO]`** | Aportar fabricante, modelo, puerto COM/USB, gramática de trama, librería nativa `serialport` y masas patrón ([guia-sustitucion-hardware-real.md](../../context/integraciones/guia-sustitucion-hardware-real.md)). |
| **G5: Terminal POS Mostrador** | ✅ **Verificada con Simulador:** `TerminalPort`, máquina de estados pura, `MockTerminalAdapter`, `Ms5TerminalAdapter`, IPC validado (`devices:terminal:*`) y smoke sin periféricos. | **`[BLOQUEO FÍSICO EXPLÍCITO — NO VERIFICADO]`** | Aportar PINPad certificado EMV/PCI PTS, SDK oficial del adquirente y tarjetas físicas de prueba ([guia-sustitucion-hardware-real.md](../../context/integraciones/guia-sustitucion-hardware-real.md)). |
| **H: Salud Técnica** | ✅ **Verificada:** Rutas duales + Kong funcional (`:8000`), CI bloqueante por etapas (`.NET`, `tsc`, unitarias, integración con Docker), grafo de contexto actualizado y manual de demo reproducible. | Despliegue remoto en clúster `student-rpedraza`: **`[PENDIENTE APLICAR MANIFIESTO ACTUALIZADO]`** | Ejecutar el runbook de [manifiestos-k8s-cluster.md](../../context/infraestructura/manifiestos-k8s-cluster.md) con credenciales de Rodrigo. |

---

## 4. Registro de Riesgos y Fallos / Deudas Remanentes

| ID | Severidad | Componente | Descripción del Riesgo o Remanente | Mitigación / Acción Siguiente |
|---|---|---|---|---|
| **REM-01** | 🟠 Media | MS-5 (`VentasController.cs`) | `VentaService.AnularAsync` existe en capa de servicio, pero el endpoint HTTP `POST /ventas/{id}/anular` aún no está expuesto en el controlador ni conectado a UI. | Exponer acción en `VentasController` + evento compensatorio `sale.reversed` en Sprint 3. |
| **REM-02** | 🟠 Media | MS-1 (`Usuario` vs `users`) | `PUT` y `DELETE /users/{id}` escriben sobre la proyección `users` (`is_active`), mientras que login, alta y listado general siguen leyendo la tabla `Usuarios` (`Activo`). | Unificar ambos repositorios sobre una única entidad migrada en EF Core y preservar `sucursalId` en edición. |
| **REM-03** | 🟠 Media | Clúster K8s (`student-rpedraza`) | El intento previo S2-052 en el clúster remoto devolvió `502` porque `poscartservice` no tenía configuradas `Services__CatalogPricingService__BaseUrl` ni `PasarelaPago__BaseUrl`. Además, los pods PostgreSQL en K8s no tienen `PVC` persistente y el TLS Let's Encrypt vence el `2027-01-16`. | Ya quedó corregido `k8s/manifests/all-services.yaml` y documentado el despliegue de `payment-simulator` y tags inmutables en [manifiestos-k8s-cluster.md](../../context/infraestructura/manifiestos-k8s-cluster.md) para que Rodrigo ejecute `kubectl apply`. |
| **REM-04** | 🟡 Baja | Secretos de Desarrollo (`appsettings.json` / `kong.yaml`) | No existen secretos de producción ni llaves reales en git, pero `appsettings.json`, `kong.yaml` y `all-services.yaml` contienen claves simétricas y credenciales dummy de desarrollo local (`admin_password`, `postgres`, `GlobalMartOS_SuperSecretKey...`). | Aceptable solo para entorno local/académico; en cualquier entorno productivo inyectar `Jwt__Key` y `ConnectionStrings__DefaultConnection` mediante `Kubernetes Secret`. |
| **REM-05** | 🟡 Baja | Frontend (`Pos.tsx`) | El borrador local previo a crear la venta en servidor usa la clave `localStorage['pos_cart']` sin prefijo de `tenant_id`/`user_id` (detectado en S2-025). Una vez iniciada la venta, MS-5 toma autoridad total. | Particionar la clave de borrador como `pos_cart:${tenantId}:${userId}` al extraer `useDraftCart` según [mapeo-responsabilidades-pos-cierre.md](../../context/frontend/mapeo-responsabilidades-pos-cierre.md). |
| **REM-06** | 🟡 Baja | Cobertura Cuantitativa (RNF-07) | Aunque pasan 318 pruebas xUnit + 25 pruebas Node, no hay recolección automatizada con `coverlet` que certifique ≥ 80% de líneas en todos los MS. | Integrar `coverlet.collector` y reporte Cobertura en `.github/workflows/ci.yml` sin atribuir 80% hasta medirlo. |
