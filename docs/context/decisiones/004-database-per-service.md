---
id: 004-database-per-service
tipo: decision
titulo: "ADR-004: Base de datos independiente por microservicio"
estado: implementado
fuentes: [GlobalMart_ContextMaster.md#sec19, Docker/docker-compose.yml]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
depende_de: []
publica: []
consume: []
reglas: [RNF-01]
---
# ADR-004: Base de datos independiente por microservicio

**Estado:** APROBADA

## Decisión

Cada microservicio tiene su propia instancia de PostgreSQL. Están completamente prohibidos los JOINs entre bases de datos de diferentes servicios.

## Justificación

- Permite que cada servicio evolucione su esquema independientemente
- Sin acoplamiento de datos entre servicios
- Si un servicio cae, los demás siguen operando
- Facilita la escalabilidad horizontal de cada servicio individualmente

## Consecuencias

- 8 instancias PostgreSQL en Docker (8 contenedores separados con volúmenes separados)
- Los datos cruzados se obtienen vía API REST (ej: MS-5 llama MS-3 para precio de producto)
- Los datos cruzados asíncronos se obtienen vía Kafka (ej: MS-4 recibe sale.completed de MS-5)
- Un "JOIN" que antes era una query SQL ahora requiere múltiples llamadas HTTP o un evento

## Ejemplo de anti-patrón (PROHIBIDO)

```sql
-- PROHIBIDO: acceder a la tabla de MS-1 desde MS-5
SELECT u.nombre, v.total
FROM tenant_db.usuarios u  -- ← base de MS-1
JOIN pos_db.ventas v ON v.cajero_id = u.id  -- ← base de MS-5
```

## Ejemplo correcto

```csharp
// MS-5 guarda el nombre del cajero como snapshot al crear la venta
var venta = new Venta {
    CajeroId = cajeroId,
    // nombre del cajero se obtiene del JWT claim o de una llamada MS-1 previa
};
```

## Conexiones
- Docker: [[docker-compose]]
- Stack: [[stack]]
- Multi-tenant: [[multi-tenant]]

## Fuentes
- `GlobalMart_ContextMaster.md` §19
- `Docker/docker-compose.yml` (8 servicios postgres-*)
