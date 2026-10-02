---
id: 003-efcore-unico
tipo: decision
titulo: "ADR-003: EF Core 8 sin Dapper"
estado: implementado
fuentes: [GlobalMart_ContextMaster.md#sec19, src/]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: []
publica: []
consume: []
reglas: []
---
# ADR-003: EF Core 8 sin Dapper

**Estado:** APROBADA

## Decisión

Usar **Entity Framework Core 8** como único ORM. No introducir Dapper u otros micro-ORMs para consultas específicas.

## Justificación

- EF Core 8 soporta migraciones automáticas, útil para evolucionar el esquema por servicio
- EF Core 8 soporta filtros globales de query (`HasQueryFilter`) — fundamental para el patrón multi-tenant
- Curva de aprendizaje uniforme en el equipo
- Para búsquedas complejas: usar `FromSqlRaw` con PostgreSQL funciones (ej: trigram) desde EF Core

## Consecuencias

- Los repositorios usan `IQueryable<T>` con LINQ
- El índice trigram (`pg_trgm`) se crea en migración y se usa desde `context.Set<Producto>().Where(p => EF.Functions.ILike(p.Nombre, $"%{query}%"))`
- Sin SQL crudo innecesario — toda la lógica en C# con LINQ

## Conexiones
- Stack: [[stack]]
- Multi-tenant: [[multi-tenant]]

## Fuentes
- `GlobalMart_ContextMaster.md` §19
