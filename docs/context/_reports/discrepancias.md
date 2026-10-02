---
id: discrepancias
tipo: reporte
titulo: Discrepancias — Código Real vs ContextMaster
estado: vigente
fuentes: [src/, GlobalMart_ContextMaster.md, docs/context/_reports/inventario.md]
verificado_contra_codigo: true
ultima_revision: 2026-09-29
---
# Discrepancias — Código Real vs ContextMaster

> Diferencias entre lo documentado en `GlobalMart_ContextMaster.md` y lo encontrado en el código real. Todas verificadas con análisis de código fuente.

## Discrepancias Críticas (Errores de alto impacto)

### D-01 — `POST /auth/refresh` no existe en código

- **ContextMaster dice:** MS-1 tiene `POST /auth/refresh` para renovar el JWT
- **Código real:** Solo `AuthController.cs` con `POST /auth/login`. No hay RefreshController ni acción Refresh.
- **Impacto:** Sesiones expiradas sin solución. Usuario debe hacer login nuevamente.
- **Estado:** [NO VERIFICADO] — puede ser planificado o eliminado

### D-02 — `GET /fx/rates` no existe en código

- **ContextMaster dice:** MS-1 tiene `GET /fx/rates` y `POST /fx/convert`
- **Código real:** Solo 3 controllers en MS-1: Auth, Usuario, TenantConfig. Sin FX.
- **Impacto:** Tipos de cambio no disponibles. MS-6 (Costo Landed) bloqueado.
- **Estado:** [PLANIFICADO] — no implementado

### D-03 — `GET /tenants/{id}/sucursales` no existe en código

- **ContextMaster dice:** MS-1 gestiona sucursales con ese endpoint
- **Código real:** TenantConfigController solo maneja config (país, moneda, IVA). Sin endpoint de sucursales.
- **Impacto:** `sucursal_id` en JWT es un campo libre sin validación en MS-1
- **Estado:** [PLANIFICADO]

### D-04 — `sucursal_id` falta en JWT

- **ContextMaster dice (RN-08):** JWT debe incluir `sucursal_id` obligatoriamente
- **Código real:** JWT generado por JwtService incluye `sub`, `tenant_id`, `roles[]`, `active_role`, `exp`. **Sin `sucursal_id`.**
- **Impacto:** RN-08 parcialmente incumplido. MS-5 obtiene sucursal_id del body del request, no del JWT.
- **Estado:** [DISCREPANCIA — código no coincide con diseño]

### D-05 — MS-5 NO publica `sale.completed` a Kafka

- **ContextMaster dice:** Al completar una venta, MS-5 publica `sale.completed` → MS-4, MS-7, MS-8
- **Código real:** `VentaService.CompletarAsync` solo cambia el estado a `COMPLETADA` y llama al repositorio. Sin ninguna llamada a Kafka producer.
- **Impacto:** MS-4 (Warehouse) tiene el consumer listo pero nunca recibe eventos. Stock nunca se descuenta automáticamente.
- **Estado:** [CRÍTICO — cadena Kafka rota]

### D-06 — MS-2 NO emite DTE

- **ContextMaster dice:** MS-2 tiene endpoints `POST /dte/solicitar-folio`, `POST /dte/emitir`, `GET /dte/{id}/estado`
- **Código real:** Solo existe `POST /tax/calculate`. Sin DTE, sin folios, sin integración SII.
- **Impacto:** RN-03 incumplido — toda venta completada carece de documento tributario
- **Estado:** [PLANIFICADO]

## Discrepancias Importantes

### D-07 — Duración y avance del Sprint 1

- **ContextMaster dice:** Documentaba un alcance de Sprint 1 muy preliminar centrado en setup.
- **Realidad aclarada por el equipo:** El proyecto sigue formalmente en **Sprint 1** (sprints de 4 semanas, de miércoles a miércoles, en la UCT; actualmente semana 3). Sin embargo, el equipo avanzó tareas backend (TI3-179..195) de Kafka, multi-tenant y ventas completas antes de cerrar el sprint.
- **Impacto:** El backlog original del Sprint 1 en ContextMaster subestimaba el progreso real que se alcanzaría en las primeras 3 semanas.

### D-08 — Endpoints de MS-2 faltantes en ContextMaster

- **ContextMaster NO documenta** que MS-2 llama a MS-1 para obtener PorcentajeIva
- **Código real:** `TenantConfigClient.GetTenantConfigAsync()` hace GET a MS-1 en cada cálculo de tax
- **Impacto:** Dependencia no documentada. Si MS-1 cae, MS-2 no puede calcular IVA.

### D-09 — Frontend llama sin Kong

- **Diseño:** Frontend → Kong `:8000` → Microservicios
- **Código real (Login.tsx):** `POST http://127.0.0.1:5124/auth/login` — directo a puerto de dev de MS-1
- **Impacto:** Rate-limiting y JWT validation de Kong no se aplican al frontend real

### D-10 — POS sin integración de API

- **ContextMaster:** POS Screen muestra carrito con productos reales del backend
- **Código real (Pos.tsx):** Datos hardcodeados (`useState<ProductItem[]>([{ name: 'Coca Cola 2L', ... }])`)
- **Impacto:** POS no llama a MS-3 para productos ni a MS-5 para crear ventas. Botón cobrar muestra alert.

### D-11 — ContextMaster dice 9 topics Kafka; docker-compose tiene 17

- **ContextMaster:** Documenta 9 topics de Kafka
- **Código real (docker-compose):** 17 topics aprovisionados en `KAFKA_INIT_TOPICS`
- **Impacto:** ContextMaster está desactualizado respecto a los topics del sistema

### D-12 — MS-7 y MS-8 sin controllers en código

- **ContextMaster:** Documenta AN-01..24 (24 casos de uso de MS-7) y LC-01..17 (17 de MS-8)
- **Código real:** Solo modelos y DbContext. Sin ningún controller, service ni Kafka handler.
- **Impacto:** Todos los casos de uso documentados son PLANIFICADOS, no implementados

## Discrepancias Menores

### D-13 — Doble prefijo en TenantConfigController

- **ContextMaster:** Documenta el endpoint como `GET /api/v1/tenants/{id}/config`
- **Código real:** El controller registra AMBAS rutas: `/api/v1/tenants` Y `/tenants`
- **Impacto menor:** Ambas rutas funcionan, pero la documentación es incompleta

### D-14 — Tests documentados vs tests reales

- **ContextMaster dice:** "RNF-07: >= 80% cobertura en MS-1, MS-2, MS-5"
- **Código real:** Solo console apps de smoke testing. Cobertura estimada: < 5%

### D-15 — Endpoints HTTP de cobro y anulación ausentes en VentasController

- **ContextMaster dice:** MS-5 expone `POST /ventas/{id}/cobrar` y `POST /ventas/{id}/anular`
- **Código real:** `VentaService.cs` implementa `CompletarAsync` y `AnularAsync`, pero `VentasController.cs` aún no tiene las acciones HTTP mapeadas (solo tiene `POST /ventas`, items CRUD y GET).
- **Impacto:** Las operaciones de completar y anular no son invocables vía HTTP todavía.

## Resumen

| Gravedad | Cantidad |
|----------|---------|
| 🔴 Crítica | 6 (D-01 a D-06) |
| 🟠 Importante | 7 (D-07 a D-12, D-15) |
| 🟡 Menor | 2 (D-13, D-14) |
| **Total** | **15** |
