---
id: plan-de-nodos
tipo: reporte
titulo: Plan de Nodos — GlobalMart OS Context Graph
estado: vigente
fuentes: [docs/context/_reports/inventario.md, GlobalMart_ContextMaster.md]
verificado_contra_codigo: true
ultima_revision: 2026-09-29
---
# Plan de Nodos — GlobalMart OS Context Graph
**Fecha:** 2026-09-28  
**Estado:** Aprobado → generación completada

---

## Árbol final de nodos

```
CLAUDE.md                                   (raíz; punto de entrada <60 líneas)
AGENTS.md                                   (raíz; idéntico a CLAUDE.md)
docs/context/
├── index.md                                hub central — todos los nodos + cuándo leerlos
├── estado-actual.md                        matriz servicio × funcionalidad + sprint real
├── proyecto/
│   ├── vision.md                           qué es GlobalMart OS y por qué existe
│   ├── arquitectura.md                     patrón microservicios, flujos sync/async
│   ├── stack.md                            stack tecnológico verificado
│   ├── glosario.md                         términos del dominio (tenant, turno, FEFO, etc.)
│   ├── como-ejecutar.md                    comandos docker compose, build, test, run
│   └── convenciones-codigo.md              convenciones C#, naming, estructura por servicio
├── decisiones/
│   ├── 001-kafka-vs-rabbitmq.md            Decision 1: mantener Kafka
│   ├── 002-kong-vs-yarp.md                 Decision 2: mantener Kong
│   ├── 003-efcore-unico.md                 Decision 3: EF Core 8 sin Dapper
│   ├── 004-database-per-service.md         Decision 4: DB por servicio
│   ├── 005-electron-frontend.md            Decision 5: Electron + React
│   └── 006-moscow-prioridades.md           Decision 6: MoSCoW para alcance
├── dominio/
│   ├── actores.md                          13 actores (5 humanos + 8 sistema/hardware)
│   ├── reglas-negocio.md                   RN-01..22 con etiquetas de estado
│   ├── requerimientos-funcionales.md       RF-01..32 con etiquetas
│   ├── requerimientos-no-funcionales.md    RNF-01..09 con etiquetas
│   ├── rbac-multirol.md                    roles, permisos granulares, JWT estructura
│   ├── multi-tenant.md                     patrón tenant_id, middleware, aislamiento
│   ├── fefo.md                             regla First-Expired-First-Out
│   ├── costo-landed.md                     fórmula y componentes del costo landed
│   └── fiscal-por-pais.md                  SII/AFIP/IRS, tipos de DTE
├── servicios/
│   ├── ms1-identity.md                     MS-1 completo
│   ├── ms2-tax.md                          MS-2 completo
│   ├── ms3-catalog.md                      MS-3 completo
│   ├── ms4-inventory.md                    MS-4 completo
│   ├── ms5-pos.md                          MS-5 completo
│   ├── ms6-supply-chain.md                 MS-6 completo
│   ├── ms7-analytics.md                    MS-7 completo
│   └── ms8-loyalty.md                      MS-8 completo
├── eventos/
│   └── kafka-topics.md                     todos los topics: diseño vs código real
├── frontend/
│   ├── estructura.md                       árbol de archivos, tecnologías
│   ├── pantallas.md                        Login, Pos, Admin (estado real)
│   ├── electron-ipc.md                     handlers IPC (preload.ts)
│   └── auth-y-roles.md                     useAuth hook, electron-store, JWT
├── integraciones/
│   ├── hardware.md                         balanza serial, Terminal POS
│   ├── fiscal.md                           SII, AFIP, IRS (estado planificado)
│   ├── pasarela-pago.md                    Stripe/Transbank/MercadoPago (planificado)
│   ├── transporte-3pl.md                   couriers (planificado)
│   ├── mensajeria.md                       Twilio/SendGrid/Firebase (planificado)
│   └── tipos-de-cambio.md                  Fixer.io/OpenExchangeRates (planificado)
├── infraestructura/
│   ├── docker-compose.md                   15 servicios, puertos, volúmenes
│   ├── kong.md                             rutas configuradas, plugins JWT
│   ├── ci-cd.md                            GitHub Actions workflow
│   └── testing.md                          estrategia de tests, cobertura real
├── diagramas/
│   └── indice-diagramas.md                 9 diagramas de casos de uso + ERDs + secuencias
├── planificacion/
│   ├── sprint-actual.md                    sprint real en curso (Sprint 2/3)
│   ├── sprints-anteriores.md               Sprint 1 completado
│   ├── roadmap.md                          Sprints 1-8 del diseño
│   └── backlog.md                          US-01..11, TT-01..19
└── _reports/
    ├── inventario.md                       ← ya creado (Fase 1)
    ├── plan-de-nodos.md                    ← este archivo
    ├── discrepancias.md                    ← por crear (Fase 6)
    ├── cobertura-ids.md                    ← por crear (Fase 6)
    └── preguntas-abiertas.md               ← por crear (Fase 6)
```

## Decisiones sobre el plan

1. **MS-6, MS-7, MS-8** tienen nodos de servicio aunque estén mayormente PLANIFICADOS — documentar honestamente qué existe vs qué no.
2. **No se creará** `msN-casos-de-uso.md` separado porque los nodos de servicio no superan 250 líneas con los casos de uso incluidos.
3. **`indice-diagramas.md`** referenciará los archivos .mmd y .png existentes en el repo sin copiarlos.
4. **`convenciones-codigo.md`** documentará lo observado en el código (no inventado).
