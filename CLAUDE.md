# GlobalMart OS — Briefing para IA

**Sistema:** POS + ERP multi-tenant para minimarkets  
**Stack:** 8 microservicios ASP.NET Core 8 · PostgreSQL por servicio · Kafka · Kong · Electron + React + TypeScript  
**Rama activa:** `docs/context-graph` (documentación) — **nunca modificar `dev` o `main`**  
**Fecha de este documento:** 2026-09-28

---

## Qué hacer ANTES de cualquier tarea

1. Leer [`docs/context/estado-actual.md`](docs/context/estado-actual.md) → qué está implementado vs planificado
2. Leer el nodo de servicio relevante en [`docs/context/servicios/`](docs/context/servicios/)
3. Si vas a tocar Kafka: leer [`docs/context/eventos/kafka-topics.md`](docs/context/eventos/kafka-topics.md)
4. Si vas a tocar auth/roles: leer [`docs/context/dominio/rbac-multirol.md`](docs/context/dominio/rbac-multirol.md)

Hub completo: [`docs/context/index.md`](docs/context/index.md)

---

## Reglas inviolables

| # | Regla |
|---|-------|
| 1 | Nunca modificar `src/`, `globalmart-frontend/`, `Docker/`, `.github/`, `*.sln`, `*.props`, `*.bat` |
| 2 | Solo trabajar en rama `docs/context-graph`. No hacer push a `dev` ni `main` |
| 3 | No inventar datos — usar `[NO VERIFICADO]` si no está en el código |
| 4 | No copiar secrets (connection strings con contraseñas, API keys) |
| 5 | Todo query debe filtrar por `tenant_id` (RN-01) |
| 6 | Pagos con tarjeta NUNCA localmente, siempre por pasarela (RN-02) |
| 7 | Solo crear ventas si hay turno abierto (RN-06) |

---

## Brechas críticas actuales (2026-09-28)

1. **MS-5 NO publica `sale.completed`** → stock nunca se descuenta automáticamente
2. **MS-2 NO emite DTE** → ventas sin documento tributario
3. **Sin pasarela de pago** → cobro con tarjeta no implementado
4. **Frontend POS tiene datos hardcodeados** → sin integración real con APIs

---

## Credentials de prueba (solo dev)

- URL MS-1: `http://localhost:5124/auth/login`
- Email: `cajero@demo.cl` | Password: `demo1234`
- TenantId: `aaaaaaaa-0000-0000-0000-000000000001`

---

## Sprint actual

Sprint 1 — Ver [`docs/context/planificacion/sprint-actual.md`](docs/context/planificacion/sprint-actual.md)
