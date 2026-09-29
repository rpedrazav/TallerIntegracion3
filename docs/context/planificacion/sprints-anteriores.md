---
id: sprints-anteriores
tipo: planificacion
titulo: Sprints Anteriores — Trabajo Completado
estado: vigente
fuentes: [git log, src/]
verificado_contra_codigo: true
ultima_revision: 2026-09-29
depende_de: []
publica: []
consume: []
reglas: []
---
# Sprints Anteriores — Trabajo Completado

> Resumen de lo completado antes del sprint actual, inferido del git log y el estado del código.

## Sprint 1 — Fundamentos [COMPLETADO] 

**Período estimado:** Agosto - inicios de septiembre 2026

### Entregables verificados en código
- ✅ Docker Compose completo (15 servicios: 8 BD, Kafka, Kong, monitoreo)
- ✅ Kong configurado con JWT, CORS, rate-limiting
- ✅ GitHub Actions CI pipeline completo
- ✅ MS-1: `POST /auth/login` con JWT y prioridad de roles
- ✅ MS-3: CRUD de productos y búsqueda
- ✅ MS-5: Turnos básicos y estructura de ventas

### Evidencia en git
- Setup inicial de repos y branches
- Creación de todos los proyectos .csproj
- Migraciones iniciales de todas las BDs

---

## Sprint 2 — POS Core [COMPLETADO]

**Período estimado:** septiembre 2026 (primera quincena)

### Entregables verificados en código
- ✅ MS-1: UsuarioController con CRUD y assignRoles (TI3-179)
- ✅ MS-1: TenantConfigController con config fiscal (TI3-180, TI3-181)
- ✅ MS-3: CategoriaController con jerarquía (AddCategoriaHierarchy migration)
- ✅ MS-3: Índice trigram pg_trgm para búsqueda (TI3-143 según migration)
- ✅ MS-5: VentasController completo (TI3-191..194)
- ✅ MS-5: TurnosController completo
- ✅ MS-5: VentaService con estados PENDIENTE/COMPLETADA/ANULADA
- ✅ MS-2: TaxController con cálculo de IVA

### Evidencia en git
- Commits TI3-179, 180, 181, 191, 192, 193, 194

---

## Sprint 3 — Inventario + Kafka [EN CURSO]

**Período estimado:** septiembre 2026 (segunda quincena)

### Entregables completados
- ✅ MS-4: StockController (TI3-183)
- ✅ MS-4: TenantMiddleware (TI3-184)
- ✅ MS-4: KafkaConsumerService sale.completed (TI3-185, 186, 187, 188)
- ✅ MS-4: Migración AddEventosKafkaProcesados (TI3-186)
- ✅ MS-5: Tests AgregarItemTest (TI3-195)

### Pendiente en Sprint 3
- 🔴 MS-5: Publicar sale.completed a Kafka
- 🔴 MS-4: Endpoints de recepción y FEFO

## Conexiones
- Sprint actual: [[sprint-actual]]
- Roadmap: [[roadmap]]

## Fuentes
- `git log --oneline -30` (ejecutado 2026-09-28)
