# S2-063 — Cálculo pesable autoritativo

Fecha: 2026-10-24.

**42/42 pruebas aprobadas en Release, sin fallos ni omisiones: 10 nuevas de S2-063 y 32 de regresión.**

## Comportamiento

MS-5 toma `PrecioBase` de MS-3 y el peso validado por S2-062 como cantidad facturable. No usa precio, subtotal, IVA o total enviados por el renderer. Para productos `EsPesoVariable`, el precio se interpreta por kilogramo según S2-061. Crear/agregar rechazan precio de catálogo negativo.

Se corrigieron dos diferencias que podían alterar los importes:

1. MS-5 guardaba el precio con dos decimales, aunque MS-3 admite cuatro. Ahora el snapshot es `numeric(20,4)`: mantiene cuatro decimales y los 16 dígitos enteros del esquema anterior. Una nueva petición que recalcula el carrito conserva el precio original.
2. MS-2 calculaba con fracciones de centavo sin redondear. Ahora redondea subtotal e IVA por línea a dos decimales con `MidpointRounding.ToEven`; los totales se obtienen sumando las líneas. El subtotal MS-5 utiliza la misma regla explícita.

Ejemplos antes de sumar otros ítems:

| Precio/kg | Peso kg | Tasa | Subtotal | IVA | Total |
|---|---|---|---|---|---|
| 2000 | 0.750 | 19% | 1500.00 | 285.00 | 1785.00 |
| 12.3456 | 0.750 | 19% | 9.26 | 1.76 | 11.02 |
| 12.3456 | 0.500 | 21% | 6.17 | 1.30 | 7.47 |
| 1 | 0.125 | 19% | 0.12 | 0.02 | 0.14 |
| 1 | 0.135 | 19% | 0.14 | 0.03 | 0.17 |

El IVA se calcula sobre el subtotal de línea ya redondeado. Dos líneas con subtotal 0.14 e IVA 0.03 cada una producen subtotal 0.28, IVA 0.06 y total 0.34; no se recalcula el IVA sobre el agregado. Se mantienen exenciones y tasas por ítem/tenant.

## Evidencia y alcance de pruebas

- [VentaIntegrationTests.cs](../../../tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs): siete casos S2-063 mediante HTTP de MS-5, JWT y PostgreSQL temporal con migraciones. Incluyen precio falsificado e importes/tasa del renderer, peso mínimo, precio cero, cuatro decimales, empates y tasas 0/19/21. Comparan respuesta, snapshot del precio, cantidad/peso, subtotal/IVA/total de venta y recarga HTTP. Agregan después otro producto para recalcular una canasta mixta usando el precio persistido.
- El transporte de MS-3 utiliza un handler de catálogo controlado. El handler de MS-2 deserializa la petición y ejecuta el **TaxCalculatorService productivo**; no reimplementa su fórmula. Las tasas de prueba están controladas y se verifica que MS-5 no envía un override fiscal. No ejecuta el host HTTP de MS-2 ni consulta MS-1.
- [PesableTaxRoundingTests.cs](../../../tests/GlobalMart.IntegrationTests/PesableTaxRoundingTests.cs): tres pruebas directas del calculador productivo para suma de líneas fraccionarias, canasta exenta/gravada con tasa por ítem y empates del IVA.
- Regresión: 20 casos S2-062, cinco pruebas previas de ventas y siete de validación de creación.
- [S2-063.trx](S2-063.trx): resultado de la ejecución enfocada.
- Regresión manual TC-01 del servicio fiscal: subtotal 13000, IVA 2470, total 15470; aprobada.
- EF Core, configuración Release: sin cambios de modelo pendientes después de la migración. La primera comprobación con `--no-build` sobre Debug usó un binario anterior; se repitió con el Release recién compilado.
- Compilación aprobada con advertencias XML preexistentes de `ProductoController`; sin errores.

## Migración y compatibilidad

`20261024142752_PreserveItemPricePrecision` amplía exclusivamente `ItemsVenta.PrecioUnitario` de `numeric(18,2)` a `numeric(20,4)`. No cambia la precisión monetaria de subtotal/IVA/total ni recupera decimales ya perdidos en ventas antiguas. El rollback a dos decimales puede redondear precios nuevos; no debe usarse esperando conservar cuatro decimales.

La política de redondeo de MS-2 también afecta a comprobantes que usan el mismo calculador. Deben desplegarse la migración, MS-5 y MS-2 actualizados juntos para obtener el comportamiento verificado. No se alteran tasas ni datos de catálogo.

La correspondencia de `UomBaseId` con kg sigue [NO VERIFICADO]: el catálogo no ofrece una definición de unidades resoluble. Esta tarea usa la convención de precio por kg de S2-061 para `EsPesoVariable`; no acredita validación de la unidad UUID. Tampoco implementa balanza, conversión del simulador ni las validaciones completas de peso al crear/modificar. No se ejecutó la solución completa ni un despliegue remoto.

## Reproducción

Base exclusiva de pruebas:

```bash
docker run --rm -d --name globalmart-s2063-postgres \
  -e POSTGRES_PASSWORD=s2063_test -e POSTGRES_DB=s2063 \
  -p 127.0.0.1:55463:5432 postgres:16
docker exec globalmart-s2063-postgres pg_isready -U postgres -d s2063
export ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=55463;Database=s2063;Username=postgres;Password=s2063_test'
dotnet test tests/GlobalMart.IntegrationTests --configuration Release \
  --filter 'FullyQualifiedName~VentaIntegrationTests|FullyQualifiedName~CrearVentaRequestValidatorTests|FullyQualifiedName~PesableTaxRoundingTests' \
  --logger 'trx;LogFileName=S2-063.trx' --results-directory docs/qa/S2-063
dotnet run --project src/TaxComplianceService/tests/TaxComplianceService.ManualTest --configuration Release
dotnet ef migrations has-pending-model-changes --project src/POSCartService --configuration Release
docker stop globalmart-s2063-postgres
```

La contraseña es desechable y exclusiva de pruebas.
