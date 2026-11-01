---
id: index
tipo: indice
titulo: Índice del Grafo de Conocimiento — GlobalMart OS
estado: vigente
fuentes: [docs/context/_reports/inventario.md, GlobalMart_ContextMaster.md]
verificado_contra_codigo: true
ultima_revision: 2026-10-23
depende_de: []
publica: []
consume: []
reglas: []
---
# Índice del Grafo de Conocimiento — GlobalMart OS

> Hub central de navegación. Lee este archivo primero, luego sigue los `[[enlaces]]` solo a los nodos relevantes para tu tarea. **No cargues todos los nodos.**

## Instrucciones de uso

1. Lee `[[estado-actual]]` para saber qué está implementado vs planificado.
2. Navega al nodo de la carpeta que corresponde a tu tarea.
3. Sigue los `[[enlaces]]` dentro de cada nodo para profundizar.
4. Si modificas código, actualiza `ultima_revision` en el nodo afectado.

---

## Mapa de nodos por carpeta

### Punto de entrada (raíz)
| Archivo | Contenido |
|---------|-----------|
| `CLAUDE.md` / `AGENTS.md` | Briefing corto + instrucciones para IAs |
| `docs/context/index.md` | **Este archivo** |
| `docs/context/estado-actual.md` | Matriz de estado real por microservicio |

### `proyecto/` — Base del sistema
| Nodo | Cuándo leerlo |
|------|---------------|
| `[[vision]]` | Para entender el problema y la solución |
| `[[arquitectura]]` | Para entender cómo se conectan los servicios |
| `[[stack]]` | Antes de agregar dependencias o tecnologías |
| `[[glosario]]` | Cuando encuentres un término del dominio desconocido |
| `[[como-ejecutar]]` | Para levantar el entorno local |
| `[[convenciones-codigo]]` | Antes de escribir código nuevo |

### `decisiones/` — Arquitectura y diseño
| Nodo | Cuándo leerlo |
|------|---------------|
| `[[001-kafka-vs-rabbitmq]]` | Si propones cambiar la mensajería |
| `[[002-kong-vs-yarp]]` | Si propones cambiar el API Gateway |
| `[[003-efcore-unico]]` | Si propones agregar Dapper u otro ORM |
| `[[004-database-per-service]]` | Si propones compartir bases de datos |
| `[[005-electron-frontend]]` | Si propones migrar a web |
| `[[006-moscow-prioridades]]` | Para entender qué es Must/Should/Could/Won't |

### `dominio/` — Reglas y modelo de negocio
| Nodo | Cuándo leerlo |
|------|---------------|
| `[[actores]]` | Para entender quién usa el sistema |
| `[[reglas-negocio]]` | Antes de modificar lógica de negocio |
| `[[requerimientos-funcionales]]` | Para saber qué debe hacer el sistema |
| `[[requerimientos-no-funcionales]]` | Para restricciones de calidad |
| `[[rbac-multirol]]` | Para todo lo relacionado con roles y permisos |
| `[[multi-tenant]]` | Para lógica de aislamiento de datos |
| [peso-variable](dominio/peso-variable.md) | Contrato S2-061: kg, precisión, mínimo, cantidad y precio de ítems pesables |
| `[[fefo]]` | Para inventario de perecederos |
| `[[costo-landed]]` | Para cálculo de costos de importación |
| `[[fiscal-por-pais]]` | Para lógica fiscal (SII, AFIP, IRS) |

### `servicios/` — Microservicios
| Nodo | Cuándo leerlo |
|------|---------------|
| `[[ms1-identity]]` | Auth, usuarios, JWT, config de tenant |
| `[[ms2-tax]]` | Cálculo de impuestos, DTE |
| `[[ms3-catalog]]` | Productos, categorías, precios |
| `[[ms4-inventory]]` | Stock, FEFO, Kafka consumer |
| `[[ms5-pos]]` | Turnos, carrito, cobro, ventas |
| `[[ms6-supply-chain]]` | Órdenes de compra, proveedores (PLANIFICADO) |
| `[[ms7-analytics]]` | Dashboards, reportes (PLANIFICADO) |
| `[[ms8-loyalty]]` | Puntos, tiers, cupones (PLANIFICADO) |

### `eventos/` — Kafka
| Nodo | Cuándo leerlo |
|------|---------------|
| `[[kafka-topics]]` | Para cualquier cambio en eventos Kafka |

### `frontend/` — App Electron + React
| Nodo | Cuándo leerlo |
|------|---------------|
| `[[estructura]]` | Estructura del proyecto frontend |
| `[[pantallas]]` | Pantallas implementadas |
| `[[guia-estilo]]` | Sistema visual aprobado, tokens y reglas de accesibilidad |
| `[[electron-ipc]]` | Comunicación Electron main/renderer |
| `[[auth-y-roles]]` | Manejo de JWT en el frontend |

### `integraciones/` — Sistemas externos
| Nodo | Cuándo leerlo |
|------|---------------|
| `[[hardware]]` | Balanza, Terminal POS |
| `[[fiscal]]` | SII, AFIP, IRS |
| `[[pasarela-pago]]` | Stripe, Transbank, MercadoPago |
| `[[transporte-3pl]]` | Couriers y tracking |
| `[[mensajeria]]` | Twilio, SendGrid, Firebase |
| `[[tipos-de-cambio]]` | Fixer.io, OpenExchangeRates |

### `infraestructura/` — DevOps
| Nodo | Cuándo leerlo |
|------|---------------|
| `[[docker-compose]]` | Para modificar el entorno de desarrollo |
| `[[kong]]` | Para agregar rutas al API Gateway |
| `[[ci-cd]]` | Para entender el pipeline de CI |
| `[[testing]]` | Para escribir o entender tests |

### `planificacion/` — Sprints y roadmap

Para tareas con ID `S2-xxx`, consultar primero la carpeta [tareas/](../../tareas/), ubicada en la raíz del repositorio. Ruta local confirmada por Daniel Prado: `C:\Users\dp877\Desktop\Universidad\Semestre 6\TallerIntegra3\TallerIntegracion3\tareas`.

- [plan_sprint2_revisado.xlsx](../../tareas/plan_sprint2_revisado.xlsx): hoja `Resumen` con las cinco metas y límites físicos; hoja `Sprint 2` con ID, entregable, aceptación y asignación. Es la fuente consultada para S2-021.
- [sprint2_youtrack_resultado.json](../../tareas/sprint2_youtrack_resultado.json): correspondencia de IDs del plan con YouTrack (S2-021 → TI3-652).
- [Matriz S2-021](../../diagramas/matriz/S2-021-matriz-aceptacion.md): escenarios, resultados esperados, dependencias y separación entre validación simulada y física. Define pruebas pendientes; no certifica su ejecución.

Los archivos de planificación describen objetivos. Contrastar su estado con `estado-actual.md` y evidencia de código/pruebas antes de declarar una funcionalidad implementada.

| Nodo | Cuándo leerlo |
|------|---------------|
| `[[sprint-actual]]` | Estado del sprint en curso |
| `[[sprints-anteriores]]` | Historial de sprints completados |
| `[[roadmap]]` | Plan de sprints 1-8 |
| `[[backlog]]` | User stories y tareas técnicas |

### `diagramas/` — Diagramas del sistema
| Nodo | Cuándo leerlo |
|------|---------------|
| `[[indice-diagramas]]` | Para encontrar un diagrama específico |

---

## Conexiones
- Estado real: [[estado-actual]]
- Frontend: [[estructura]], [[pantallas]], [[guia-estilo]]
- Servicios principales: [[ms1-identity]], [[ms5-pos]], [[ms4-inventory]]
- Eventos: [[kafka-topics]]
- Infraestructura: [[docker-compose]], [[kong]]

## Fuentes
- `docs/context/_reports/inventario.md`
- `GlobalMart_ContextMaster.md`
