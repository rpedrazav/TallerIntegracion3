# S2-064 — Actualizar cantidad pesable con recálculo

Fecha: 2026-10-24.

**75/75 pruebas aprobadas en Release, sin fallos ni omisiones:** 23 nuevas S2-064, 42 de regresión de peso/cálculo/ventas y 10 de aislamiento de ventas entre tenants.

## Cambio

`PUT /ventas/{id}/items/{itemId}` (y alias `/api`) consulta `EsPesoVariable` de MS-3 para el producto del ítem existente, propagando el token. El cliente no determina la clasificación. Se conservan los controles de JWT, tenant, venta PENDIENTE y pertenencia del ítem a la venta.

- Pesable: `peso_kg` obligatorio, mínimo 0.010 kg, incrementos de 0.001 kg. Cantidad omitida/null se deriva del peso; si tiene valor debe coincidir. Un cero decimal adicional sin fracción de gramo es válido.
- No pesable: `cantidad` obligatoria y positiva; `peso_kg` solo puede ser null/ausente. No se convierte a kilogramos ni se introduce una restricción nueva de cantidades enteras.
- El subtotal usa el precio snapshot del ítem, no el precio actualizado del catálogo ni el del request. MS-2 recalcula la canasta completa con la política de redondeo de S2-063.
- No se elimina ni se crea otro ítem. Repetir el mismo PUT mantiene los mismos identificadores e importes.

`ModificarCantidadItemRequest.Cantidad` es nullable para distinguir omisión de un valor explícito. Un request vacío deja de ser válido; el validador exige cantidad si no hay peso, y el controlador aplica la regla específica del catálogo.

## Errores sin modificación

Validación: 400. Producto ausente en catálogo: 404; inactivo: 400. Error de catálogo: 502. Fallo o ausencia de resultado fiscal: 503. La consulta de tipo y el cálculo fiscal ocurren antes de modificar ítems o totales. Como ahora se consulta catálogo, una indisponibilidad de MS-3 impide editar; no se adivina el tipo a partir de los campos enviados.

El tipo procede del catálogo actual; todavía no existe snapshot persistido de `EsPesoVariable`. El precio sí es snapshot y se conserva aunque catálogo cambie. No se cambia el esquema, el contrato de creación de venta ni el simulador.

## Pruebas

[ModificarPesoIntegrationTests.cs](../../../tests/GlobalMart.IntegrationTests/ModificarPesoIntegrationTests.cs), clase parcial `VentaIntegrationTests`, trait `Plan=S2-064`:

- 23 casos nuevos: 6 aceptaciones y 17 rechazos/fallos externos.
- Pesable: cambio a 0.500, mínimo 0.010, cantidad coincidente con 1.234 y ceros adicionales en 0.7500.
- Rechazos de peso ausente/null/cero/negativo, inferior al mínimo, fracción de gramo y cantidades contradictorias.
- Unitarios: cambios de cantidad válidos, peso incompatible y cantidades ausentes/null/cero/negativas.
- Fallo de MS-2 para ambos tipos; fallo y producto inexistente en MS-3.
- Antes/después se consulta PostgreSQL con contextos nuevos sin tracking. Ante rechazo se comparan ítems, IDs, cantidades, peso, precio, subtotal, estado, impuestos y total. Las validaciones y fallos de catálogo no llaman a MS-2.
- Para aceptaciones se conserva el ítem vecino, identidad del ítem editado y su precio original (12.3456 para pesables), aunque catálogo cambie a 9999 y el renderer envíe otro precio/total. Se verifican cantidad/peso, subtotal y totales del carrito mixto; se repite el PUT y se compara el snapshot completo.

Pruebas HTTP con JWT y PostgreSQL 16 temporal migrado. Catálogo controlado mediante handler; MS-2 usa el calculador productivo mediante handler HTTP. No ejecuta los hosts externos de MS-3/MS-2, Electron ni una balanza.

Regresión: suites de venta, S2-062, S2-063, validador de creación y aislamiento de ventas por tenant. Evidencia: [S2-064.trx](S2-064.trx).

## Validación final

Tras revisar el DTO, se recompilaron y ejecutaron nuevamente los casos de edición S2-064 y cálculo S2-063: **33/33 aprobados**, sin fallos ni omisiones. [Evidencia final](validacion-final.trx). La cantidad nullable se limita al request de edición; los DTOs fiscales conservan su contrato anterior.

## Reproducción

Usar una base exclusiva de pruebas:

```bash
docker run --rm -d --name globalmart-s2064-postgres \
  -e POSTGRES_PASSWORD=s2064_test -e POSTGRES_DB=s2064 \
  -p 127.0.0.1:55464:5432 postgres:16
docker exec globalmart-s2064-postgres pg_isready -U postgres -d s2064
export ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=55464;Database=s2064;Username=postgres;Password=s2064_test'
export GLOBALMART_TEST_POSTGRES="$ConnectionStrings__DefaultConnection"
dotnet test tests/GlobalMart.IntegrationTests --configuration Release \
  --filter 'FullyQualifiedName~VentaIntegrationTests|FullyQualifiedName~CrearVentaRequestValidatorTests|FullyQualifiedName~PesableTaxRoundingTests|FullyQualifiedName~VentaTenantIsolationTests' \
  --logger 'trx;LogFileName=S2-064.trx' --results-directory docs/qa/S2-064
docker stop globalmart-s2064-postgres
```

Contraseña desechable de pruebas. La compilación presentó advertencias XML preexistentes en `ProductoController`; no hubo errores de compilación. No se ejecutó toda la solución ni se validó un despliegue remoto.
