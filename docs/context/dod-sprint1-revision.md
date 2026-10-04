---
id: dod-sprint1-revision
tipo: auditoria
titulo: Revisión del DoD del Sprint 1
estado: completada
verificado_contra_codigo: true
ultima_revision: 2026-10-03
---
# Revisión del DoD del Sprint 1

## Veredicto ejecutivo

El DoD del Sprint 1 no se cumple al 100%. Existen flujos de integración funcionales y un pipeline de CI configurado, pero permanecen brechas en cobertura de endpoints, tests unitarios, cobertura porcentual, flujo E2E, aislamiento multi-tenant probado y composición completa del entorno Docker.

## Evaluación de los siete puntos

| # | Criterio | Estado |
|---|---|---|
| 1 | Todos los endpoints del sprint pasan tests de integración | **NO CUMPLE** |
| 2 | Cobertura de tests unitarios >= 80% en MS-1, MS-2 y MS-5 | **NO CUMPLE** |
| 3 | `docker compose up` levanta el entorno completo desde cero | **CUMPLE PARCIALMENTE** |
| 4 | El flujo end-to-end funciona en la app Electron | **NO CUMPLE** |
| 5 | `sale.completed` se publica en Kafka y MS-4 descuenta el stock | **CUMPLE PARCIALMENTE** |
| 6 | El aislamiento multi-tenant está validado con pruebas | **NO CUMPLE** |
| 7 | GitHub Actions ejecuta build y tests en cada push a `main` | **CUMPLE** |

## Hallazgos técnicos

### 1. Tests de integración de endpoints

Existe [GlobalMart.IntegrationTests](../../tests/GlobalMart.IntegrationTests), con 7 clases y 9 tests activos que cubren flujos de MS-1, MS-4 y MS-5. No existe un proyecto `GlobalMart.UnitTests` y MS-2/MS-3 no tienen cobertura de integración equivalente para todos sus endpoints. No se encontraron reportes persistidos de ejecución.

### 2. Cobertura unitaria

No se encontraron proyectos unitarios, `coverlet`, `ReportGenerator`, `XPlat Code Coverage`, umbrales ni reportes versionados. El 80% no está demostrado.

### 3. Docker Compose

[Docker/docker-compose.yml](../../Docker/docker-compose.yml) contiene 8 PostgreSQL, Zookeeper, Kafka, inicialización de topics, Kafdrop, Kong, Prometheus, Grafana y un contenedor auxiliar de frontend. No define los ocho microservicios ASP.NET como servicios Docker, por lo que el entorno completo no se levanta desde cero con Compose.

### 4. Flujo Electron E2E

[globalmart-frontend](../../globalmart-frontend) tiene integración manual parcial para login y turnos. No se encontraron Playwright, Cypress ni archivos de pruebas E2E. `Pos.tsx` usa datos mockeados y el flujo de venta/cobro no está conectado completamente al backend real.

### 5. Kafka y descuento de stock

[KafkaPublishIntegrationTests.cs](../../tests/GlobalMart.IntegrationTests/KafkaPublishIntegrationTests.cs) valida la publicación real de `sale.completed`. [StockConsumerIntegrationTests.cs](../../tests/GlobalMart.IntegrationTests/StockConsumerIntegrationTests.cs) publica un evento real, prepara stock inicial y verifica mediante polling HTTP que MS-4 descuenta la cantidad. La implementación está cubierta, pero no hay artefactos históricos de ejecución versionados para certificar el criterio de forma independiente.

### 6. Aislamiento multi-tenant

Los DbContext y middlewares implementan filtros y propagación de `tenant_id` en MS-1, MS-4 y MS-5. Sin embargo, no existe un test que cree datos de dos tenants y demuestre que tenant A no puede consultar datos de tenant B.

### 7. GitHub Actions

[.github/workflows/ci.yml](../../.github/workflows/ci.yml) se activa en pushes a `main` y `dev`, ejecuta restore, build y `dotnet test`, y levanta infraestructura Docker antes de probar. No genera métricas de cobertura.

## Conclusión

El Sprint 1 tiene una base de integración funcional para los flujos críticos de autenticación, turnos, ventas, cobro, Kafka y descuento de stock. Para cerrar completamente el DoD faltan principalmente tests unitarios con cobertura medible, pruebas explícitas de aislamiento multi-tenant, automatización E2E del frontend y la incorporación de los microservicios ASP.NET al entorno Compose.
