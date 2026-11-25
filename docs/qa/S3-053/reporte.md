# S3-053 — Desplegar MS-4 con recepciones y lotes

Fecha: 2026-11-24. Sprint 3 (Semana 11). Meta: Entrega en clúster y `.exe`.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Aplicar migraciones y desplegar MS-4 con los endpoints nuevos en `warehouse_db`.
- **Entrega verificable:** MS-4 actualizado en el clúster (`rpedrazav/warehouseinventoryservice:s2-20261125-40379da`).
- **Aceptación:** Recepción y consulta de lotes responden en el clúster.
- **Hallazgo o necesidad:** MS-4 corre con base propia (`warehouse_db`) dentro de la instancia PostgreSQL del POS (`postgres-poscartservice`) por la cuota de PVC.

## Despliegue y Migraciones en `warehouse_db`

- **Pod activo:** `warehouseinventoryservice-5ff8c8f6d8-z7dtc` (`Running 1/1`, `0` restarts, tag `s2-20261125-40379da`, conectado a `warehouse_db` vía Secret `globalmart-warehouse-config`).
- **Migraciones aplicadas verificadas en `postgres-poscartservice` → `warehouse_db` (`__EFMigrationsHistory`):**
  1. `20260913033948_InitialCreate_Warehouse`
  2. `20260927195722_AddEventosKafkaProcesados`
  3. `20261118181528_AddSucursalToLote` *(aplicada automáticamente al arrancar el pod actualizado: `ALTER TABLE lotes ADD sucursal_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'`)*
- **Tablas activas en `warehouse_db`:** `__EFMigrationsHistory`, `eventos_kafka_procesados`, `lotes` (con `sucursal_id`), `movimientos_stock`, `stock`.

## Verificación en Vivo de Endpoints en el Clúster

Pruebas ejecutadas contra `https://inventory-rpedraza.dev.censei.cl` (y verificadas internamente contra `http://warehouseinventoryservice`):

| # | Endpoint | Autenticación | HTTP | Resultado |
|---|---|---|---:|---|
| 1 | `GET /health` | Sin token | **200** | `Healthy` |
| 2 | `POST /api/recepciones` | Sin token | **401** | Ruta activa y protegida por JWT |
| 3 | `POST /recepciones` | Sin token | **401** | Alias sin `/api` activo y protegido por JWT |
| 4 | `GET /lotes?porVencer=30&sucursal_id=11141053-…` | Sin token | **401** | Ruta activa y protegida por JWT |
| 5 | `POST /api/recepciones` | JWT real `cajero@demo.cl` (`CAJERO`) | **403** | JWT validado por MS-4 y RBAC `[Authorize(Roles = "ADMIN,REPONEDOR")]` aplicado |
| 6 | `GET /lotes?porVencer=30&sucursal_id=11141053-…` | JWT real `cajero@demo.cl` (`CAJERO`) | **403** | JWT validado por MS-4 y RBAC `[Authorize(Roles = "ADMIN,REPONEDOR")]` aplicado |

## Hallazgos Registrados

1. **Prefijo `/api` en `LotesController`:** `RecepcionesController` declara `[Route("recepciones")]` y `[Route("api/recepciones")]`, mientras que `LotesController` declara únicamente `[Route("lotes")]`. Si el frontend llama `buildServiceUrl('inventory', '/lotes')` (que produce `/api/lotes`), conviene añadir `[Route("api/lotes")]` a `LotesController`.
2. **Certificado TLS de `inventory-rpedraza.dev.censei.cl`:** el Secret `globalmart-dev-tls` no incluye el SAN de `inventory-rpedraza.dev.censei.cl`; el Ingress enruta correctamente al pod (`200`/`401`/`403` con `-k`), pero clientes TLS estrictos rechazarán el certificado hasta reemitirlo con el host incluido.
3. **Prueba `201`/`200` con rol `ADMIN`:** la cuenta `admin@demo.cl` del clúster usa una contraseña distinta a `demo1234`, por lo que la validación con escritura de un lote real requiere las credenciales vigentes de `admin@demo.cl` o `cajero2@demo.cl`.
