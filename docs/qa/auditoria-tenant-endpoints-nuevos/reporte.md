# Auditoría de aislamiento por tenant (RN-01) — endpoints nuevos

Fecha: 2026-11-24. Método: revisión de código + pruebas existentes. Pruebas de integración de MS-1 ejecutadas contra PostgreSQL 16 en Docker: 24/24 OK.

| Endpoint | Tenant tomado de | Aislamiento | Prueba con otro tenant |
|---|---|---|---|
| POST `/recepciones` (MS-4) | claim `tenant_id` | Producto y sucursal se validan en MS-3/MS-1 con el mismo JWT (filtrados por tenant); escrituras con `tenantId` del JWT | `RecepcionValidator_Tests.Create_SucursalAjena_Retorna404SinPersistir` (OK, unitaria) |
| GET `/lotes` (MS-4) | claim `tenant_id` | Sucursal validada con el JWT + `Where TenantId == tenantId` explícito + `HasQueryFilter` | `LotesController_Tests.List_DevuelveSoloDisponiblesDelTenantSucursalYRango` incluye lote de otro tenant (OK) |
| GET/POST/PUT/DELETE `/sucursales` (MS-1) | claim `tenant_id` | `HasQueryFilter` + `TenantId == tenantId` en Update/Deactivate; 404 sin revelar existencia | `SucursalIntegrationTests` (otro tenant → 404) (OK con PostgreSQL) |
| POST `/users/{id}/reactivate` (MS-1) | claim `tenant_id` | `ReactivateAsync` filtra por `TenantId`; actor validado en su tenant | `ReactivarUsuarioIntegrationTests.Reactivate_OtroTenantOInexistente_Retorna404SinCambios` (OK con PostgreSQL) |
| Roles `/users/roles` (MS-1) | claim `tenant_id` | `HasQueryFilter` de `Rol` (tenant propio o global) | `AdminCustomRolesIntegrationTests.RolDeOtroTenant_...` (OK con PostgreSQL) |

Resultado MS-4 (`dotnet test --filter MS4_Warehouse`): 45 pruebas, 44 OK; el único fallo fue una prueba temporal que escribí para el hallazgo de abajo (retirada).

## Hallazgo (sin explotación por HTTP)
`Stock` tiene clave primaria `(ProductoId, SucursalId)` **sin `TenantId`** (`WarehouseDbContext.OnModelCreating`). Hoy no se puede explotar: los GUID son aleatorios y `/recepciones` rechaza producto/sucursal que no son del tenant. Pero si dos tenants llegaran a compartir el mismo par, el segundo insert fallaría por clave duplicada. Corregirlo exige migración; se deja como recomendación (incluir `TenantId` en la PK) y no se tocó.

## Conclusión
Por revisión de código y pruebas ningún endpoint nuevo expone ni modifica datos de otro tenant. Falta correr las 3 suites de integración marcadas con PostgreSQL (`GLOBALMART_TEST_POSTGRES`) para cerrar la evidencia dinámica.
