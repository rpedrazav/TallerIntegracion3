---
id: sprints-anteriores
tipo: planificacion
titulo: Sprints Anteriores — Trabajo Completado
estado: vigente
fuentes: [git log, src/, tareas/plan_sprint2_revisado.xlsx, docs/defensa-sprint2/01-resumen.md, docs/context/estado-actual.md]
verificado_contra_codigo: false
ultima_revision: 2026-11-14
depende_de: []
publica: []
consume: []
reglas: []
---
# Sprints Anteriores — Trabajo Completado

> Resumen de los sprints cerrados. La numeración sigue el calendario del proyecto: Sprint 1 = Semanas 1–4, Sprint 2 = Semanas 5–8. Una versión anterior de este nodo usaba una numeración inferida del git log (Sprint 2 «POS Core», Sprint 3 «Inventario + Kafka»); esa numeración no corresponde al calendario y quedó reemplazada.

## Sprint 1 — Fundamentos, Backend Core y POS Base [COMPLETADO]

**Semanas:** 1 a 4.

### Entregables verificados en código
- ✅ Docker Compose completo: 8 PostgreSQL, Zookeeper, Kafka, Kong DB-less, Prometheus, Grafana (TI3-113..114)
- ✅ Kong configurado con JWT, CORS y rate-limiting; pipeline de GitHub Actions
- ✅ MS-1: `POST /auth/login` con JWT; UsuarioController con CRUD y asignación de roles (TI3-179); TenantConfigController (TI3-180..181)
- ✅ MS-2: TaxController con cálculo de IVA
- ✅ MS-3: CRUD de productos, categorías jerárquicas e índice trigram `pg_trgm` (TI3-143)
- ✅ MS-4: StockController, TenantMiddleware y KafkaConsumerService de `sale.completed` con `EventosKafkaProcesados` (TI3-183..188)
- ✅ MS-5: TurnosController y VentasController (crear venta, agregar, modificar y eliminar ítems; TI3-191..194); tests AgregarItem (TI3-195)
- 🟡 Frontend: Electron + React + TypeScript, Login funcional y POS base

### Pendientes que pasaron al Sprint 2
- Publicación de `sale.completed` desde MS-5 → resuelta en Sprint 2 mediante outbox transaccional y worker.
- Endpoints HTTP de cobro y anulación → cobro resuelto en Sprint 2; anulación: backend implementado en la Semana 9 del Sprint 3 (S2-092).
- Conectar `Pos.tsx` a MS-3 y MS-5 reales → resuelto en Sprint 2.

---

## Sprint 2 — POS Avanzado, Cobro con Tarjeta y Hardware Simulado [COMPLETADO — Semana 8 trasladada]

**Semanas:** 5 a 8. **Corte de defensa:** 2026-10-30 (último commit de `dev`: `f0e6f31`, 2026-10-27). Integración de ramas: 2026-11-01.
**Plan:** [plan_sprint2_revisado.xlsx](../../../tareas/plan_sprint2_revisado.xlsx), 120 tareas / 120 h (6 personas × 5 h × 4 semanas). Mapeo a YouTrack: `S2-001`..`S2-120` → `TI3-632`..`TI3-751`.

### Resultado por meta (resumen de la defensa)
| Meta | Resultado respaldado | Límite |
|---|---|---|
| G1 Administración | Baja lógica de productos; edición/baja de usuarios; roles propios; sucursales; inventario administrativo; bootstrap de tenant y ADMIN | Sin revocación inmediata de JWT; auditoría y reactivación UI pendientes |
| G2 Carrito y venta | Venta persistida en MS-5; precio de MS-3; IVA de MS-2; efectivo; recuperación de carrito; outbox y worker | Anulación HTTP no expuesta; consumidor MS-4 sin atomicidad completa y desactivado en clúster |
| G3 Tarjeta | Pasarela simulada; reserva durable del intento; aprobación/rechazo; bloqueo ante incertidumbre; reconciliación | Sin banco real ni reembolso |
| G4 Balanza | Contrato kg (mínimo 10 g, resolución 1 g); simulador; adaptador serial abstracto; IPC; reglas backend | Sin equipo físico; `UomBaseId` ↔ kg sin resolver |
| G5 Terminal POS | Contrato y máquina de estados; simulador; IPC limitado; sincronización con MS-5 | Cancelación backend no soportada; hardware físico [NO VERIFICADO] |
| H Salud técnica | Rutas Kong, configuración de APIs, Compose, typecheck en CI, fixtures, reportes QA y grafo | Sin release completo ni porcentaje global de cobertura acreditado |

No se declara porcentaje de cumplimiento del sprint (ver `docs/defensa-sprint2/01-resumen.md`).

### Semana 8 no ejecutada
Por exigencia del profesor, las 30 tareas de la Semana 8 (`S2-091`..`S2-120`, `TI3-722`..`TI3-751`, 30 h) no se realizaron. Se trasladan a la Semana 9 del Sprint 3. Detalle en [[sprint-actual]].

---

## Conexiones
- Sprint actual: [[sprint-actual]]
- Roadmap: [[roadmap]]
- Estado real: [[estado-actual]]

## Fuentes
- `git log` (2026-09-28) y `src/` para el Sprint 1
- `tareas/plan_sprint2_revisado.xlsx`, `tareas/sprint2_youtrack_resultado.json`
- `docs/defensa-sprint2/01-resumen.md`
- Aclaración del equipo (Daniel Prado, 2026-11-14)
