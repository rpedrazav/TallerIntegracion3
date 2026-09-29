---
id: requerimientos-no-funcionales
tipo: dominio
titulo: Requerimientos No Funcionales (MoSCoW) — GlobalMart OS
estado: parcial
fuentes: [GlobalMart_ContextMaster.md#sec9, src/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: [vision]
publica: []
consume: []
reglas: []
---
# Requerimientos No Funcionales (MoSCoW) — GlobalMart OS

## MUST

| ID | Requerimiento | Estado |
|----|---------------|--------|
| RNF-01 | **Aislamiento de datos:** `tenant_id` discriminador en cada tabla. Middleware lo inyecta en cada query. | [IMPLEMENTADO] |
| RNF-02 | **Rendimiento POS:** escaneo → carrito < 2 segundos. | [NO MEDIDO] — sin tests de performance |
| RNF-03 | **Disponibilidad:** >= 99.5% (~3.6h downtime/mes). | [NO MEDIDO] — sin SLA implementado |
| RNF-04 | **Seguridad:** TLS 1.3 en tránsito. AES-256 en reposo. Passwords con BCrypt. | [PARCIAL] — BCrypt implementado; TLS y AES-256 en reposo NO VERIFICADO |

## SHOULD

| ID | Requerimiento | Estado |
|----|---------------|--------|
| RNF-05 | **Auditoría:** log inmutable de operaciones críticas (login, venta, anulación) con user_id, tenant_id, active_role, timestamp. | [PLANIFICADO] — logs básicos solo en desarrollo |
| RNF-06 | **Multiplataforma:** Electron funciona en Windows, macOS, Linux sin cambios. | [PARCIAL] — Electron configurado, no verificado en macOS/Linux |
| RNF-07 | **Cobertura de tests:** >= 80% en lógica crítica (MS-1, MS-2, MS-5). | [PLANIFICADO] — solo tests manuales/smoke |

## COULD

| ID | Requerimiento | Estado |
|----|---------------|--------|
| RNF-08 | **i18n:** soporte ES, EN, PT configurables por tenant. | [PLANIFICADO] — campo idioma en config, sin i18n en frontend |
| RNF-09 | **Modo offline parcial:** POS procesa ventas en efectivo sin internet. | [PLANIFICADO] |

## Conexiones
- Estado actual: [[estado-actual]]
- Testing: [[testing]]
- Stack: [[stack]]

## Fuentes
- `GlobalMart_ContextMaster.md` §9
