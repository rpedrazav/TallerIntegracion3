# GlobalMart OS — Guía para agentes de IA

> Archivo canónico para cualquier agente (Codex, Claude Code, Copilot u otro). `CLAUDE.md` lo importa: **edita aquí, no allá**.

**Sistema:** POS + ERP multi-tenant para minimarkets.
**Contexto:** proyecto académico de Ingeniería Civil Informática (UCT, Taller de Integración 3). Las integraciones externas no disponibles legalmente se simulan.
**Stack:** 8 microservicios ASP.NET Core 8 · PostgreSQL por servicio · Kafka · Kong · Electron + React + TypeScript.

---

## 1. Cómo trabajar: mínimo código, flujos fáciles de seguir

El profesor pidió explícitamente simplificar: al recorrer un flujo en la defensa, el código «da muchas vueltas». Por eso:

1. **Usa siempre la skill `ponytail`** en cualquier tarea de código ([SKILLS.md](SKILLS.md)). Escribe el cambio completo más pequeño posible.
2. **Simplifica de paso.** Toda tarea de Backend/Frontend cumple este criterio de aceptación:
   > Los archivos tocados quedan sin código muerto ni duplicado, y el flujo se puede seguir desde el botón hasta la BD sin desvíos.
3. **Misma arquitectura.** No elimines microservicios, Kong, Kafka/outbox, base de datos por servicio ni la capa Controller → Service → Repository. Se simplifica *dentro* de esa estructura.
4. **Sin dependencias nuevas** salvo que la tarea lo exija y no exista alternativa en el proyecto o en la plataforma.
5. **No toques el clúster** (pods, manifiestos aplicados, imágenes desplegadas). Lo ejecuta el scrum master (usuario YouTrack `admin`).
6. **Referencia la tarea** en el commit: `S3-012: descripción breve` (o `S2-0xx` para tareas arrastradas).

Un flujo bien hecho se explica en unos 5 saltos:
`botón (página .tsx) → handler → función de api/client.ts → Controller → Service → DbContext`.
Si necesitas pasar por archivos de configuración, interceptores o capas que no aportan, eso es una vuelta: elimínala.

---

## 2. Grafo de contexto

Antes de una tarea, consulta el grafo en vez de leer todo el repositorio:

1. [`docs/context/estado-actual.md`](docs/context/estado-actual.md): qué está implementado y qué no.
2. El nodo del servicio en [`docs/context/servicios/`](docs/context/servicios/).
3. [`docs/context/index.md`](docs/context/index.md) para dominio, eventos Kafka, frontend o infraestructura.
4. Plan del sprint: [`docs/context/planificacion/sprint-actual.md`](docs/context/planificacion/sprint-actual.md) y [`tareas/plan_sprint3.xlsx`](tareas/plan_sprint3.xlsx) (criterios de aceptación por ID).

**Si cambias código, actualiza el grafo en la misma tarea** con la skill `grafo-contexto`. Resumen de la regla:
- Actualiza solo los nodos afectados (servicio, dominio, evento, infraestructura o frontend) y `estado-actual.md` si cambian endpoints, eventos, reglas o estado.
- Actualiza `ultima_revision` en cada nodo modificado.
- `IMPLEMENTADO`/`PARCIAL`/`PLANIFICADO` solo con evidencia en código, pruebas o configuración. Lo no confirmado va como `[NO VERIFICADO]`.
- Un cambio interno que no altera comportamiento no requiere tocar el grafo.

---

## 3. Reglas de dominio obligatorias

| # | Regla | Descripción |
|---|---|---|
| RN-01 | **Aislamiento multi-tenant** | Toda consulta o mutación filtra por `tenant_id` del JWT (TenantMiddleware + `HasQueryFilter`). Nunca se acepta el tenant desde el body. |
| RN-02 | **Pagos por pasarela** | Los pagos con tarjeta nunca se procesan localmente; siempre pasan por la pasarela simulada. Sin PAN/CVV en el cliente. |
| RN-06 | **Turno abierto** | Solo se crean ventas si el cajero tiene un turno ABIERTO. |
| RN-07 | **JWT** | Todo endpoint protegido exige JWT válido con claims de usuario y tenant. |
| — | **Precios del servidor** | Precio, impuestos y totales los calcula el backend (MS-3 y MS-2); el renderer nunca es autoridad. |
| — | **Sin datos inventados** | Lo que no esté confirmado en el código se documenta como `[NO VERIFICADO]`. |

Estas reglas **nunca** se recortan al simplificar.

---

## 4. Mapa rápido del código

| Área | Ruta |
|---|---|
| MS-1 Identity (login, usuarios, sucursales) | `src/TenantIdentityService/` |
| MS-2 Tax (IVA, comprobantes) | `src/TaxComplianceService/` |
| MS-3 Catalog (productos) | `src/CatalogPricingService/` |
| MS-4 Inventory (stock, lotes, consumidor Kafka) | `src/WarehouseInventoryService/` |
| MS-5 POS (turnos, ventas, cobros, outbox) | `src/POSCartService/` |
| MS-6/7/8 | Solo modelos; sin endpoints (fuera del alcance actual) |
| Frontend Electron + React | `globalmart-frontend/src/` (`pages/`, `components/`, `api/client.ts`, `config/api.ts`) |
| Pruebas .NET | `tests/GlobalMart.Tests/`, `tests/GlobalMart.IntegrationTests/` |
| Evidencia QA | `docs/qa/<ID>/reporte.md` |

Convenciones de C# y patrones existentes: [`docs/context/proyecto/convenciones-codigo.md`](docs/context/proyecto/convenciones-codigo.md). Guía visual del frontend: [`docs/context/frontend/guia-estilo.md`](docs/context/frontend/guia-estilo.md).

---

## 5. Comandos

```bash
# Infraestructura local (PostgreSQL, Kafka, Kong, Grafana)
docker compose -f Docker/docker-compose.yml up -d

# Backend
dotnet build GlobalMartOS.sln
dotnet test GlobalMartOS.sln
# Pruebas de integración con PostgreSQL real: definir GLOBALMART_TEST_POSTGRES

# Frontend
cd globalmart-frontend
npm install
npm start               # app Electron en desarrollo
npm run typecheck       # obligatorio antes de dar por terminada una tarea de frontend
npm run test:e2e        # Playwright
npm run ci:local        # typecheck + E2E Electron
npm run make            # instalador Setup.exe (Electron Forge)
```

Configuración y seed local: [`docs/context/proyecto/como-ejecutar.md`](docs/context/proyecto/como-ejecutar.md).

---

## 6. Antes de dar una tarea por terminada

- [ ] Cumple el criterio de aceptación de su ID en `tareas/plan_sprint3.xlsx`.
- [ ] Cumple el criterio de simplificación (sección 1).
- [ ] `dotnet test` del servicio tocado y/o `npm run typecheck` pasan.
- [ ] Lógica nueva no trivial tiene al menos una prueba.
- [ ] Grafo actualizado si cambió comportamiento (skill `grafo-contexto`).
- [ ] Si se generó evidencia de pruebas, está en `docs/qa/<ID>/reporte.md` (skill `reporte-qa`).

---

## 7. Sprint actual

**Sprint 3** (Semanas 9–12, de miércoles a martes 23:59). Defensa: **2-dic-2026**. Congelamiento de código: **domingo 29-nov**; después solo se corrigen bugs. Detalle en [`sprint-actual.md`](docs/context/planificacion/sprint-actual.md).
