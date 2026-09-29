---
id: cobertura-ids
tipo: reporte
titulo: Cobertura de IDs — Trazabilidad ContextMaster vs Grafo
estado: vigente
fuentes: [GlobalMart_ContextMaster.md, docs/context/]
verificado_contra_codigo: true
ultima_revision: 2026-09-29
---
# Cobertura de IDs — Trazabilidad ContextMaster vs Grafo

> Generado automáticamente comparando cada ID del GlobalMart_ContextMaster.md contra los nodos del grafo en docs/context/.

**Resumen de Trazabilidad:** 256/256 IDs cubiertos en los nodos del grafo.

## Reglas de Negocio (RN)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| RN-01 | [[006-moscow-prioridades]], [[arquitectura]], [[ms1-identity]], [[ms3-catalog]] (+2 mas) | ✅ |
| RN-02 | [[estado-actual]], [[ms5-pos]], [[pasarela-pago]], [[reglas-negocio]] | ✅ |
| RN-03 | [[estado-actual]], [[fiscal]], [[fiscal-por-pais]], [[ms2-tax]] (+2 mas) | ✅ |
| RN-04 | [[estado-actual]], [[kafka-topics]], [[ms4-inventory]], [[reglas-negocio]] | ✅ |
| RN-05 | [[estado-actual]], [[fefo]], [[ms4-inventory]], [[reglas-negocio]] | ✅ |
| RN-06 | [[glosario]], [[ms5-pos]], [[reglas-negocio]] | ✅ |
| RN-07 | [[auth-y-roles]], [[kong]], [[ms1-identity]], [[rbac-multirol]] (+1 mas) | ✅ |
| RN-08 | [[auth-y-roles]], [[ms1-identity]], [[rbac-multirol]], [[reglas-negocio]] | ✅ |
| RN-09 | [[costo-landed]], [[ms6-supply-chain]], [[reglas-negocio]] | ✅ |
| RN-10 | [[reglas-negocio]] | ✅ |
| RN-11 | [[ms3-catalog]], [[reglas-negocio]], [[roadmap]] | ✅ |
| RN-12 | [[ms1-identity]], [[rbac-multirol]], [[reglas-negocio]] | ✅ |
| RN-13 | [[ms6-supply-chain]], [[reglas-negocio]], [[roadmap]] | ✅ |
| RN-14 | [[reglas-negocio]] | ✅ |
| RN-15 | [[ms8-loyalty]], [[reglas-negocio]] | ✅ |
| RN-16 | [[reglas-negocio]], [[tipos-de-cambio]] | ✅ |
| RN-17 | [[ms8-loyalty]], [[reglas-negocio]] | ✅ |
| RN-18 | [[reglas-negocio]] | ✅ |
| RN-19 | [[reglas-negocio]] | ✅ |
| RN-20 | [[reglas-negocio]], [[vision]] | ✅ |
| RN-21 | [[reglas-negocio]] | ✅ |
| RN-22 | [[reglas-negocio]] | ✅ |

## Requerimientos Funcionales (RF)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| RF-01 | [[006-moscow-prioridades]], [[estado-actual]], [[rbac-multirol]], [[requerimientos-funcionales]] | ✅ |
| RF-02 | [[ms5-pos]], [[requerimientos-funcionales]] | ✅ |
| RF-03 | [[ms5-pos]], [[requerimientos-funcionales]] | ✅ |
| RF-04 | [[estado-actual]], [[ms3-catalog]], [[ms5-pos]], [[requerimientos-funcionales]] | ✅ |
| RF-05 | [[estado-actual]], [[ms2-tax]], [[ms5-pos]], [[requerimientos-funcionales]] | ✅ |
| RF-06 | [[estado-actual]], [[ms5-pos]], [[requerimientos-funcionales]] | ✅ |
| RF-07 | [[estado-actual]], [[ms5-pos]], [[pasarela-pago]], [[requerimientos-funcionales]] | ✅ |
| RF-08 | [[estado-actual]], [[fiscal]], [[fiscal-por-pais]], [[ms2-tax]] (+1 mas) | ✅ |
| RF-09 | [[estado-actual]], [[kafka-topics]], [[ms4-inventory]], [[requerimientos-funcionales]] | ✅ |
| RF-10 | [[estado-actual]], [[fefo]], [[ms4-inventory]], [[requerimientos-funcionales]] | ✅ |
| RF-11 | [[ms3-catalog]], [[requerimientos-funcionales]] | ✅ |
| RF-12 | [[requerimientos-funcionales]] | ✅ |
| RF-13 | [[rbac-multirol]], [[requerimientos-funcionales]] | ✅ |
| RF-14 | [[multi-tenant]], [[requerimientos-funcionales]] | ✅ |
| RF-15 | [[hardware]], [[requerimientos-funcionales]] | ✅ |
| RF-16 | [[requerimientos-funcionales]] | ✅ |
| RF-17 | [[ms7-analytics]], [[requerimientos-funcionales]] | ✅ |
| RF-18 | [[costo-landed]], [[ms6-supply-chain]], [[requerimientos-funcionales]] | ✅ |
| RF-19 | [[estado-actual]], [[requerimientos-funcionales]] | ✅ |
| RF-20 | [[requerimientos-funcionales]] | ✅ |
| RF-21 | [[ms3-catalog]], [[requerimientos-funcionales]] | ✅ |
| RF-22 | [[estado-actual]], [[requerimientos-funcionales]] | ✅ |
| RF-23 | [[ms8-loyalty]], [[requerimientos-funcionales]] | ✅ |
| RF-24 | [[requerimientos-funcionales]], [[tipos-de-cambio]] | ✅ |
| RF-25 | [[mensajeria]], [[ms7-analytics]], [[requerimientos-funcionales]] | ✅ |
| RF-26 | [[ms7-analytics]], [[requerimientos-funcionales]] | ✅ |
| RF-27 | [[requerimientos-funcionales]], [[transporte-3pl]] | ✅ |
| RF-28 | [[requerimientos-funcionales]] | ✅ |
| RF-29 | [[requerimientos-funcionales]], [[vision]] | ✅ |
| RF-30 | [[requerimientos-funcionales]], [[vision]] | ✅ |
| RF-31 | [[requerimientos-funcionales]], [[vision]] | ✅ |
| RF-32 | [[requerimientos-funcionales]], [[vision]] | ✅ |

## Requerimientos No Funcionales (RNF)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| RNF-01 | [[004-database-per-service]], [[006-moscow-prioridades]], [[arquitectura]], [[multi-tenant]] (+1 mas) | ✅ |
| RNF-02 | [[requerimientos-no-funcionales]] | ✅ |
| RNF-03 | [[requerimientos-no-funcionales]] | ✅ |
| RNF-04 | [[requerimientos-no-funcionales]] | ✅ |
| RNF-05 | [[ms1-identity]], [[rbac-multirol]], [[requerimientos-no-funcionales]] | ✅ |
| RNF-06 | [[005-electron-frontend]], [[estructura]], [[requerimientos-no-funcionales]] | ✅ |
| RNF-07 | [[requerimientos-no-funcionales]], [[roadmap]], [[testing]] | ✅ |
| RNF-08 | [[requerimientos-no-funcionales]] | ✅ |
| RNF-09 | [[005-electron-frontend]], [[requerimientos-no-funcionales]] | ✅ |

## Casos de Uso TI (MS-1)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| TI-01 | [[ms1-identity]], [[rbac-multirol]] | ✅ |
| TI-02 | [[ms1-identity]] | ✅ |
| TI-03 | [[estado-actual]], [[ms1-identity]] | ✅ |
| TI-04 | [[ms1-identity]] | ✅ |
| TI-05 | [[ms1-identity]] | ✅ |
| TI-06 | [[ms1-identity]], [[rbac-multirol]] | ✅ |
| TI-07 | [[ms1-identity]] | ✅ |
| TI-08 | [[ms1-identity]] | ✅ |
| TI-09 | [[ms1-identity]] | ✅ |
| TI-10 | [[ms1-identity]] | ✅ |
| TI-11 | [[estado-actual]], [[ms1-identity]], [[multi-tenant]] | ✅ |
| TI-14 | [[ms1-identity]] | ✅ |
| TI-15 | [[ms1-identity]], [[tipos-de-cambio]] | ✅ |
| TI-16 | [[ms1-identity]], [[tipos-de-cambio]] | ✅ |
| TI-17 | [[ms1-identity]], [[tipos-de-cambio]] | ✅ |
| TI-18 | [[ms1-identity]], [[tipos-de-cambio]] | ✅ |
| TI-19 | [[ms1-identity]], [[tipos-de-cambio]] | ✅ |
| TI-20 | [[ms1-identity]], [[rbac-multirol]] | ✅ |
| TI-21 | [[auth-y-roles]], [[ms1-identity]], [[rbac-multirol]] | ✅ |
| TI-22 | [[auth-y-roles]], [[ms1-identity]], [[rbac-multirol]] | ✅ |
| TI-23 | [[ms1-identity]], [[rbac-multirol]] | ✅ |

## Casos de Uso TC (MS-2)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| TC-01 | [[ms2-tax]] | ✅ |
| TC-02 | [[ms2-tax]] | ✅ |
| TC-03 | [[ms2-tax]] | ✅ |
| TC-04 | [[ms2-tax]] | ✅ |
| TC-05 | [[fiscal-por-pais]], [[ms2-tax]] | ✅ |
| TC-06 | [[fiscal]], [[ms2-tax]] | ✅ |
| TC-07 | [[fiscal]], [[ms2-tax]] | ✅ |
| TC-08 | [[estado-actual]], [[fiscal]], [[ms2-tax]] | ✅ |
| TC-09 | [[fiscal]], [[ms2-tax]] | ✅ |
| TC-10 | [[fiscal]], [[ms2-tax]] | ✅ |
| TC-11 | [[ms2-tax]] | ✅ |
| TC-12 | [[ms2-tax]] | ✅ |

## Casos de Uso CP (MS-3)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| CP-01 | [[ms3-catalog]] | ✅ |
| CP-02 | [[ms3-catalog]] | ✅ |
| CP-03 | [[ms3-catalog]] | ✅ |
| CP-04 | [[ms3-catalog]] | ✅ |
| CP-05 | [[ms3-catalog]] | ✅ |
| CP-06 | [[ms3-catalog]] | ✅ |
| CP-07 | [[ms3-catalog]] | ✅ |
| CP-08 | [[ms3-catalog]] | ✅ |
| CP-09 | [[hardware]], [[ms3-catalog]] | ✅ |
| CP-10 | [[ms3-catalog]] | ✅ |
| CP-11 | [[ms3-catalog]] | ✅ |
| CP-12 | [[ms3-catalog]] | ✅ |
| CP-13 | [[ms3-catalog]] | ✅ |
| CP-14 | [[ms3-catalog]] | ✅ |
| CP-15 | [[ms3-catalog]] | ✅ |

## Casos de Uso WI (MS-4)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| WI-01 | [[ms4-inventory]] | ✅ |
| WI-02 | [[ms4-inventory]] | ✅ |
| WI-03 | [[ms4-inventory]] | ✅ |
| WI-04 | [[estado-actual]], [[fefo]], [[ms4-inventory]] | ✅ |
| WI-05 | [[fefo]], [[ms4-inventory]] | ✅ |
| WI-06 | [[fefo]], [[ms4-inventory]] | ✅ |
| WI-07 | [[fefo]], [[ms4-inventory]] | ✅ |
| WI-08 | [[fefo]], [[ms4-inventory]] | ✅ |
| WI-09 | [[ms4-inventory]] | ✅ |
| WI-10 | [[fefo]], [[ms4-inventory]] | ✅ |
| WI-11 | [[ms4-inventory]] | ✅ |
| WI-12 | [[ms4-inventory]] | ✅ |
| WI-13 | [[ms4-inventory]] | ✅ |
| WI-14 | [[ms4-inventory]] | ✅ |
| WI-15 | [[ms4-inventory]] | ✅ |

## Casos de Uso PC (MS-5)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| PC-01 | [[ms5-pos]] | ✅ |
| PC-02 | [[ms5-pos]] | ✅ |
| PC-03 | [[ms5-pos]] | ✅ |
| PC-04 | [[ms5-pos]] | ✅ |
| PC-05 | [[ms5-pos]] | ✅ |
| PC-06 | [[ms5-pos]] | ✅ |
| PC-07 | [[ms5-pos]] | ✅ |
| PC-08 | [[ms5-pos]] | ✅ |
| PC-09 | [[ms5-pos]] | ✅ |
| PC-10 | [[hardware]], [[ms5-pos]] | ✅ |
| PC-11 | [[ms5-pos]] | ✅ |
| PC-12 | [[ms5-pos]] | ✅ |
| PC-13 | [[ms5-pos]] | ✅ |
| PC-14 | [[ms5-pos]] | ✅ |
| PC-15 | [[ms5-pos]] | ✅ |
| PC-16 | [[ms5-pos]] | ✅ |
| PC-17 | [[ms5-pos]], [[pasarela-pago]] | ✅ |
| PC-18 | [[ms5-pos]], [[pasarela-pago]] | ✅ |
| PC-19 | [[ms5-pos]] | ✅ |
| PC-20 | [[ms5-pos]] | ✅ |
| PC-21 | [[ms5-pos]] | ✅ |
| PC-22 | [[ms5-pos]] | ✅ |
| PC-23 | [[ms5-pos]] | ✅ |
| PC-24 | [[estado-actual]], [[ms5-pos]] | ✅ |
| PC-25 | [[ms5-pos]] | ✅ |
| PC-26 | [[hardware]], [[ms5-pos]] | ✅ |
| PC-27 | [[estado-actual]], [[ms5-pos]], [[pasarela-pago]] | ✅ |
| PC-28 | [[ms5-pos]], [[pasarela-pago]] | ✅ |
| PC-29 | [[ms5-pos]] | ✅ |
| PC-30 | [[ms5-pos]] | ✅ |
| PC-31 | [[ms5-pos]] | ✅ |
| PC-32 | [[hardware]], [[ms5-pos]] | ✅ |
| PC-33 | [[hardware]], [[ms5-pos]] | ✅ |
| PC-34 | [[hardware]], [[ms5-pos]] | ✅ |
| PC-35 | [[ms5-pos]] | ✅ |
| PC-36 | [[ms5-pos]] | ✅ |
| PC-37 | [[ms5-pos]] | ✅ |

## Casos de Uso SC (MS-6)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| SC-01 | [[ms6-supply-chain]] | ✅ |
| SC-02 | [[ms6-supply-chain]] | ✅ |
| SC-03 | [[ms6-supply-chain]] | ✅ |
| SC-04 | [[ms6-supply-chain]] | ✅ |
| SC-05 | [[ms6-supply-chain]] | ✅ |
| SC-06 | [[costo-landed]], [[ms6-supply-chain]] | ✅ |
| SC-07 | [[ms6-supply-chain]] | ✅ |
| SC-08 | [[ms6-supply-chain]] | ✅ |
| SC-09 | [[ms6-supply-chain]] | ✅ |
| SC-10 | [[ms6-supply-chain]] | ✅ |
| SC-11 | [[ms6-supply-chain]] | ✅ |
| SC-12 | [[ms6-supply-chain]] | ✅ |
| SC-14 | [[ms6-supply-chain]] | ✅ |
| SC-15 | [[ms6-supply-chain]] | ✅ |
| SC-16 | [[ms6-supply-chain]] | ✅ |
| SC-18 | [[ms6-supply-chain]], [[transporte-3pl]] | ✅ |
| SC-19 | [[ms6-supply-chain]], [[transporte-3pl]] | ✅ |
| SC-20 | [[ms6-supply-chain]], [[transporte-3pl]] | ✅ |
| SC-21 | [[ms6-supply-chain]] | ✅ |
| SC-22 | [[ms6-supply-chain]] | ✅ |
| SC-23 | [[ms6-supply-chain]] | ✅ |
| SC-24 | [[ms6-supply-chain]] | ✅ |

## Casos de Uso AN (MS-7)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| AN-01 | [[ms7-analytics]] | ✅ |
| AN-02 | [[ms7-analytics]] | ✅ |
| AN-03 | [[ms7-analytics]] | ✅ |
| AN-04 | [[ms7-analytics]] | ✅ |
| AN-05 | [[ms7-analytics]] | ✅ |
| AN-06 | [[ms7-analytics]] | ✅ |
| AN-07 | [[ms7-analytics]] | ✅ |
| AN-08 | [[ms7-analytics]] | ✅ |
| AN-09 | [[ms7-analytics]] | ✅ |
| AN-10 | [[ms7-analytics]] | ✅ |
| AN-11 | [[ms7-analytics]] | ✅ |
| AN-12 | [[ms7-analytics]] | ✅ |
| AN-13 | [[ms7-analytics]] | ✅ |
| AN-14 | [[ms7-analytics]] | ✅ |
| AN-15 | [[mensajeria]], [[ms7-analytics]] | ✅ |
| AN-16 | [[mensajeria]], [[ms7-analytics]] | ✅ |
| AN-17 | [[mensajeria]], [[ms7-analytics]] | ✅ |
| AN-18 | [[mensajeria]], [[ms7-analytics]] | ✅ |
| AN-19 | [[mensajeria]], [[ms7-analytics]] | ✅ |
| AN-20 | [[ms7-analytics]] | ✅ |
| AN-21 | [[ms7-analytics]] | ✅ |
| AN-22 | [[ms7-analytics]] | ✅ |
| AN-23 | [[ms7-analytics]] | ✅ |
| AN-24 | [[ms7-analytics]] | ✅ |

## Casos de Uso LC (MS-8)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| LC-01 | [[ms8-loyalty]] | ✅ |
| LC-02 | [[ms8-loyalty]] | ✅ |
| LC-03 | [[ms8-loyalty]] | ✅ |
| LC-04 | [[ms8-loyalty]] | ✅ |
| LC-05 | [[ms8-loyalty]] | ✅ |
| LC-06 | [[ms8-loyalty]] | ✅ |
| LC-07 | [[ms8-loyalty]] | ✅ |
| LC-08 | [[ms8-loyalty]] | ✅ |
| LC-09 | [[ms8-loyalty]] | ✅ |
| LC-10 | [[ms8-loyalty]] | ✅ |
| LC-11 | [[ms8-loyalty]] | ✅ |
| LC-12 | [[ms8-loyalty]] | ✅ |
| LC-13 | [[ms8-loyalty]] | ✅ |
| LC-14 | [[ms8-loyalty]] | ✅ |
| LC-15 | [[ms8-loyalty]] | ✅ |
| LC-16 | [[ms8-loyalty]] | ✅ |
| LC-17 | [[ms8-loyalty]] | ✅ |

## User Stories Sprint 1 (US)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| US-01 | [[backlog]] | ✅ |
| US-02 | [[backlog]] | ✅ |
| US-03 | [[backlog]] | ✅ |
| US-04 | [[backlog]] | ✅ |
| US-05 | [[backlog]] | ✅ |
| US-06 | [[backlog]] | ✅ |
| US-07 | [[backlog]] | ✅ |
| US-08 | [[backlog]] | ✅ |
| US-09 | [[backlog]] | ✅ |
| US-10 | [[backlog]] | ✅ |
| US-11 | [[backlog]] | ✅ |

## Tareas Técnicas Sprint 1 (TT)

| ID | Nodos donde aparece | Estado |
|---|---|---|
| TT-01 | [[backlog]] | ✅ |
| TT-02 | [[backlog]] | ✅ |
| TT-03 | [[backlog]] | ✅ |
| TT-04 | [[backlog]] | ✅ |
| TT-05 | [[backlog]] | ✅ |
| TT-06 | [[backlog]] | ✅ |
| TT-07 | [[backlog]] | ✅ |
| TT-08 | [[backlog]] | ✅ |
| TT-09 | [[backlog]] | ✅ |
| TT-10 | [[backlog]] | ✅ |
| TT-11 | [[backlog]] | ✅ |
| TT-12 | [[backlog]] | ✅ |
| TT-13 | [[backlog]] | ✅ |
| TT-14 | [[backlog]] | ✅ |
| TT-15 | [[backlog]] | ✅ |
| TT-16 | [[backlog]] | ✅ |
| TT-17 | [[backlog]] | ✅ |
| TT-18 | [[backlog]] | ✅ |
| TT-19 | [[backlog]] | ✅ |
