# Cobertura de IDs — Trazabilidad ContextMaster vs Grafo

> Verifica que cada ID de requerimiento, caso de uso, regla de negocio y sprint del ContextMaster está cubierto en al menos un nodo del grafo.

**Fecha de revisión:** 2026-09-28

## Reglas de Negocio (RN-01..22)

| ID | Cubierto en | Estado |
|----|------------|--------|
| RN-01 | [[reglas-negocio]], [[multi-tenant]], [[ms1-identity]] | ✅ |
| RN-02 | [[reglas-negocio]], [[pasarela-pago]], [[ms5-pos]] | ✅ |
| RN-03 | [[reglas-negocio]], [[fiscal-por-pais]], [[ms2-tax]] | ✅ |
| RN-04 | [[reglas-negocio]], [[kafka-topics]], [[ms4-inventory]] | ✅ |
| RN-05 | [[reglas-negocio]], [[fefo]], [[ms4-inventory]] | ✅ |
| RN-06 | [[reglas-negocio]], [[ms5-pos]] | ✅ |
| RN-07 | [[reglas-negocio]], [[rbac-multirol]] | ✅ |
| RN-08 | [[reglas-negocio]], [[rbac-multirol]], discrepancia D-04 | ✅ |
| RN-09 | [[reglas-negocio]], [[costo-landed]], [[ms6-supply-chain]] | ✅ |
| RN-10 | [[reglas-negocio]], [[ms4-inventory]] | ✅ |
| RN-11 | [[reglas-negocio]], [[ms3-catalog]] | ✅ |
| RN-12 | [[reglas-negocio]], [[rbac-multirol]] | ✅ |
| RN-13 | [[reglas-negocio]], [[ms6-supply-chain]] | ✅ |
| RN-14 | [[reglas-negocio]], [[ms5-pos]] | ✅ |
| RN-15 | [[reglas-negocio]], [[ms8-loyalty]] | ✅ |
| RN-16 | [[reglas-negocio]], [[tipos-de-cambio]] | ✅ |
| RN-17 | [[reglas-negocio]], [[ms8-loyalty]] | ✅ |
| RN-18 | [[reglas-negocio]], [[ms5-pos]] | ✅ |
| RN-19..22 | [[reglas-negocio]] (WON'T) | ✅ |

## Requerimientos Funcionales (RF-01..32)

| Rango | Cubierto en | Estado |
|-------|------------|--------|
| RF-01..08 | [[requerimientos-funcionales]], [[ms1-identity]], [[ms5-pos]], [[ms2-tax]] | ✅ |
| RF-09..14 | [[requerimientos-funcionales]], [[ms4-inventory]], [[ms3-catalog]], [[ms1-identity]] | ✅ |
| RF-15..22 | [[requerimientos-funcionales]], [[hardware]], [[ms7-analytics]], [[ms6-supply-chain]] | ✅ |
| RF-23..32 | [[requerimientos-funcionales]], [[ms8-loyalty]], [[mensajeria]] | ✅ |

## Casos de Uso TI (MS-1) — TI-01..23

| Rango | Cubierto en |
|-------|------------|
| TI-01..10 | [[ms1-identity]] |
| TI-11..14 | [[ms1-identity]] (planificados) |
| TI-15..19 | [[ms1-identity]], [[tipos-de-cambio]] |
| TI-20..23 | [[ms1-identity]], [[rbac-multirol]] |

## Casos de Uso TC (MS-2) — TC-01..12

| Rango | Cubierto en |
|-------|------------|
| TC-01..05 | [[ms2-tax]] |
| TC-06..12 | [[ms2-tax]], [[fiscal-por-pais]] |

## Casos de Uso CP (MS-3) — CP-01..15

| Rango | Cubierto en |
|-------|------------|
| CP-01..10 | [[ms3-catalog]] |
| CP-11..15 | [[ms3-catalog]] |

## Casos de Uso WI (MS-4) — WI-01..15

| Rango | Cubierto en |
|-------|------------|
| WI-01..05 | [[ms4-inventory]], [[fefo]] |
| WI-06..15 | [[ms4-inventory]] |

## Casos de Uso PC (MS-5) — PC-01..37

| Rango | Cubierto en |
|-------|------------|
| PC-01..20 | [[ms5-pos]] |
| PC-21..37 | [[ms5-pos]], [[hardware]], [[pasarela-pago]] |

## Casos de Uso SC (MS-6) — SC-01..24

| Rango | Cubierto en |
|-------|------------|
| SC-01..24 | [[ms6-supply-chain]], [[transporte-3pl]], [[costo-landed]] |

## Casos de Uso AN (MS-7) — AN-01..24

| Rango | Cubierto en |
|-------|------------|
| AN-01..24 | [[ms7-analytics]], [[mensajeria]] |

## Casos de Uso LC (MS-8) — LC-01..17

| Rango | Cubierto en |
|-------|------------|
| LC-01..17 | [[ms8-loyalty]] |

## Requerimientos No Funcionales (RNF-01..09)

| ID | Cubierto en |
|----|------------|
| RNF-01 | [[requerimientos-no-funcionales]], [[multi-tenant]] |
| RNF-02..04 | [[requerimientos-no-funcionales]] |
| RNF-05 | [[requerimientos-no-funcionales]] |
| RNF-06 | [[requerimientos-no-funcionales]], [[005-electron-frontend]] |
| RNF-07 | [[requerimientos-no-funcionales]], [[testing]] |
| RNF-08..09 | [[requerimientos-no-funcionales]] |

## Decisiones Arquitectónicas (del ContextMaster §19)

| Decisión | Nodo |
|----------|------|
| Kafka sobre RabbitMQ | [[001-kafka-vs-rabbitmq]] |
| Kong como API Gateway | [[002-kong-vs-yarp]] |
| EF Core sin Dapper | [[003-efcore-unico]] |
| DB por servicio | [[004-database-per-service]] |
| Electron para frontend | [[005-electron-frontend]] |
| MoSCoW para priorizar | [[006-moscow-prioridades]] |

## Cobertura total estimada

- Reglas de negocio: **22/22** (100%)
- Requerimientos funcionales: **32/32** (100%)
- Requerimientos no funcionales: **9/9** (100%)
- Casos de uso documentados: **~150/150** (100% referenciados, no todos detallados)
- Decisiones arquitectónicas: **6/6** (100%)
