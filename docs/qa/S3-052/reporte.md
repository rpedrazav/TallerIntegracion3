# S3-052 / TI3-803 — Actualizar imágenes de MS-1 a MS-5 en el clúster

Fecha: 2026-11-24. Sprint 3 (Semana 11). Meta: Entrega en clúster y `.exe`.

## Objetivo y Criterios de Aceptación

- **Objetivo:** Publicar y desplegar las imágenes con los cambios de las Semanas 9 y 10 usando los manifiestos preparados por Diego (`TI3-758`).
- **Entrega verificable:** Pods actualizados con tags explícitos (`s2-20261125-40379da`).
- **Aceptación:** Cada MS responde `/health` y el login funciona contra el clúster.

## Tag Explícito Desplegado

Todas las imágenes fueron publicadas en Docker Hub por `.github/workflows/docker-build.yml` desde `dev` (`40379da`) con tag trazable **`s2-20261125-40379da`** y aplicadas en el namespace `student-rpedraza`, alineando también los manifiestos `k8s/manifests/all-services.yaml` y `k8s/manifests/warehouse-inventory.yaml`:

| Microservicio | Deployment / Pod activo | Tag anterior | Tag desplegado | Estado | `/health` |
|---|---|---|---|---|---|
| **MS-1 Identity** | `tenantidentityservice-7f598fb84d-p5hk4` | `admin-20261027-roles2` | `rpedrazav/tenantidentityservice:s2-20261125-40379da` | `Running` (`1/1`, 0 restarts) | `https://auth-rpedraza.dev.censei.cl/health` → **200 Healthy** |
| **MS-2 Tax** | `taxcomplianceservice-74867c7bd7-xzjdm` | `latest` | `rpedrazav/taxcomplianceservice:s2-20261125-40379da` | `Running` (`1/1`, 0 restarts) | `http://taxcomplianceservice/health` (interno) → **200 Healthy** |
| **MS-3 Catalog** | `catalogpricingservice-5845bdfc7f-wv7pd` | `latest` | `rpedrazav/catalogpricingservice:s2-20261125-40379da` | `Running` (`1/1`, 0 restarts) | `https://catalog-rpedraza.dev.censei.cl/health` → **200 Healthy** |
| **MS-4 Warehouse** | `warehouseinventoryservice-5ff8c8f6d8-z7dtc` | `admin-20261027-2` | `rpedrazav/warehouseinventoryservice:s2-20261125-40379da` | `Running` (`1/1`, 0 restarts) | `http://warehouseinventoryservice/health` e Ingress → **200 Healthy** *(ver nota TLS abajo)* |
| **MS-5 POS & Cart** | `poscartservice-65747cfbdb-n4vjj` | `latest` | `rpedrazav/poscartservice:s2-20261125-40379da` | `Running` (`1/1`, 0 restarts) | `https://pos-rpedraza.dev.censei.cl/health` → **200 Healthy** |

## Verificación de Login contra el Clúster

- `POST https://auth-rpedraza.dev.censei.cl/api/auth/login` (`cajero@demo.cl`, tenant `aaaaaaaa-0000-0000-0000-000000000001`) → **HTTP 200** (`activeRole: CAJERO`, JWT válido emitido).

## Incidencias Operativas Resueltas Durante el Despliegue

1. **Cuota de pods `12/12` en `student-provisioner-quota`:** `poscartservice` tenía 3 réplicas activas y existía un pod fallido huérfano (`uct-portal-tunnel-20261105`). Se eliminó el pod fallido y se escaló `poscartservice` a `1` réplica (según `all-services.yaml`), liberando cuota a `10/12` pods.
2. **Límite de memoria y seed de desarrollo en `tenantidentityservice`:**
   - El Deployment vivo tenía `limits.memory=160Mi` (provocando `Exit Code 139` al arrancar); se ajustó a `256Mi` conforme a `all-services.yaml`.
   - En modo `ASPNETCORE_ENVIRONMENT=Development`, el seed en `Program.cs:158` fallaba con `23505 duplicate key value violates unique constraint "IX_Usuarios_TenantId_Email"` porque la BD del clúster ya tenía usuarios sembrados con otro UUID. Se cambió a `ASPNETCORE_ENVIRONMENT=Production` (acorde a `all-services.yaml`), completando el rollout limpio sin reiniciar ningún pod `postgres-*`.
3. **Nota TLS en `inventory-rpedraza.dev.censei.cl`:** el certificado `globalmart-dev-tls` no incluye el SAN de `inventory-rpedraza.dev.censei.cl` (PowerShell rechaza el TLS estricto); tanto internamente (`http://warehouseinventoryservice/health`) como vía Ingress (`curl -sk https://inventory-rpedraza.dev.censei.cl/health`) MS-4 responde **HTTP 200 Healthy**.
