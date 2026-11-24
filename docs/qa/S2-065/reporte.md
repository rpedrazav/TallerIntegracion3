# S2-065 — Fronteras de peso y tenant

Fecha: 2026-10-26.

**Resultado final: 84/84 pruebas Release aprobadas, sin fallos ni omisiones: nueve casos nuevos con matrices internas y 75 de regresión.**

## Hallazgo reproducido y corrección

La ejecución inicial [diagnostico.trx](diagnostico.trx) aprobó 7 de 8 casos y falló en crear una venta pesable sin peso: `POST /ventas` devolvía 201 con `cantidad=1`, `peso_kg=null`, subtotal 12.35 e IVA 2.35. La prueba esperaba 400 sin mutación. La ejecución se detuvo en el primer borde fallido de creación; no se atribuye evidencia inicial a los demás bordes de esa operación.

Se extrajeron las reglas de agregar/editar a `ReglasCantidadItem`, utilizada ahora por crear, agregar y editar. Crear exige peso para pesables, mínimo 0.010 kg, incrementos de 0.001 kg, cantidad coincidente si se aporta y rechazo de peso en no pesables. `ItemVentaRequest.Cantidad` es nullable para distinguir omisión de un 1 explícito; omisión/null en productos no pesables conserva default 1. Editar no pesables sigue exigiendo cantidad positiva.

No se cambian precios, tasas, migraciones ni permisos. El precio y el tipo proceden de catálogo; edición conserva el precio snapshot según S2-064. Las reglas se aplican antes de persistir o solicitar cálculo fiscal.

## Matriz y evidencia

[PesoFronterasTenantTests.cs](../../../tests/GlobalMart.IntegrationTests/PesoFronterasTenantTests.cs) agrega nueve casos xUnit con matrices internas, trait `Plan=S2-065`:

| Grupo | Operaciones | Resultado esperado |
|---|---|---|
| Peso ausente/null, cero, negativo | Crear, agregar, editar | 400, sin mutaciones en ninguno de los tenants |
| 0.009 kg y 0.7505 kg | Crear, agregar, editar | 400; no redondear el peso para aceptarlo |
| Cantidad 1, 0 o -1 con peso 0.750 | Crear, agregar, editar | 400 por contradicción |
| `peso_g:750` sin `peso_kg`, texto `750 g` o decimal con coma | Crear, agregar, editar | 400: no convertir ni inferir unidad implícitamente |
| Producto no pesable con peso | Crear, agregar, editar | 400 sin cambio de unidad |
| 0.010, 0.011, 0.7500 y 1.234 kg | Crear, agregar, editar | Aceptación, precio MS-3, cantidad/peso e importes persistidos coherentes |
| Producto/venta/ítem de otro tenant | Crear, agregar, editar venta ajena y editar ítem ajeno en venta propia | 404, sin exponer nombre ni modificar snapshots |
| Canasta válida seguida de ítem inválido | Crear | 400; no se guarda una venta parcial |

Los ataques se ejecutan en ambos sentidos (A→B, B→A), incluyendo `tenant_id` falsificado en cuerpo y encabezado `X-Tenant-ID`. Los controles positivos prueban que el dueño puede agregar el mismo producto rechazado al atacante y que cada atacante sí puede operar sobre su propio producto.

Cada rechazo compara snapshots de ambas bases lógicas de venta: IDs, tenant, cajero, sucursal, turno, estado, subtotal, IVA, total, método, fecha, ítems y pagos. Se comprueba ausencia de publicación Kafka mediante el recorder existente. Los positivos conservan intacto al otro tenant y verifican respuesta, precio con cuatro decimales y persistencia con contextos nuevos.

Precio de prueba obtenido de MS-3: 12.3456 por kg; tasa controlada 19%:

| Peso kg | Subtotal | IVA | Total |
|---|---|---|---|
| 0.010 | 0.12 | 0.02 | 0.14 |
| 0.011 | 0.14 | 0.03 | 0.17 |
| 0.7500 | 9.26 | 1.76 | 11.02 |
| 1.234 | 15.23 | 2.89 | 18.12 |

La regresión incluye empates de redondeo de subtotal/IVA y canastas mixtas S2-063, validación S2-062, edición S2-064, creación de ventas y aislamiento general RN-01. La evidencia posterior está en [S2-065.trx](S2-065.trx).

## Entorno y límites

Los nueve casos nuevos usan HTTP de MS-5 y **MS-3 real** (middleware JWT, controller, servicio y repositorio) conectados mediante TestServer. PostgreSQL 16 temporal contiene esquemas exclusivos por prueba y tenant. La fixture crea tablas desde el modelo EF, no ejecuta migraciones en esos esquemas. Otras pruebas de regresión sí usan PostgreSQL migrado.

MS-2 se sustituye en transporte con un handler que invoca el calculador productivo con tasa 19%; no se reimplementa su fórmula. No se prueba configuración fiscal real de MS-1, Electron, broker Kafka ni despliegue remoto.

“Unidad incorrecta” acredita incompatibilidad pesable/unitario y entradas no canónicas en MS-5. La conversión `750 g → 0.750 kg` de una balanza/simulador y la resolución de `UomBaseId` siguen pendientes: no hay adaptador de balanza ni correspondencia verificable de UUID a kg. `peso_kg:750` significa 750 kg, no detecta un cliente que etiquetó gramos erróneamente. No se declara cumplida esa aceptación externa ni se inventa un máximo de balanza.

## Reproducción

```bash
docker run --rm -d --name globalmart-s2065-postgres \
  -e POSTGRES_PASSWORD=s2065_test -e POSTGRES_DB=s2065 \
  -p 127.0.0.1:55465:5432 postgres:16
docker exec globalmart-s2065-postgres pg_isready -U postgres -d s2065
export ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=55465;Database=s2065;Username=postgres;Password=s2065_test'
export GLOBALMART_TEST_POSTGRES="$ConnectionStrings__DefaultConnection"
dotnet test tests/GlobalMart.IntegrationTests --configuration Release \
  --filter 'FullyQualifiedName~VentaIntegrationTests|FullyQualifiedName~CrearVentaRequestValidatorTests|FullyQualifiedName~PesableTaxRoundingTests|FullyQualifiedName~VentaTenantIsolationTests' \
  --logger 'trx;LogFileName=S2-065.trx' --results-directory docs/qa/S2-065
docker stop globalmart-s2065-postgres
```

Para ejecutar solo los nueve casos nuevos, usar `--filter 'Plan=S2-065'`. Contraseña exclusiva de pruebas desechables. Compilación aprobada con advertencias XML preexistentes en `ProductoController`. No se ejecutó toda la solución.
