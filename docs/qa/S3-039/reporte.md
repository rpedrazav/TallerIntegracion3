# S3-039 — Simplificar totales en VentaService

Fecha: 2026-11-24. Rama: `dev`. Meta: Funcionalidad de punta a punta.

## Objetivo y criterio de aceptación

- Concentrar subtotal, impuestos y total en un único método usado por creación, agregado y edición de ítems.
- Las pruebas de venta, IVA y pesables pasan sin cambios.

## Cambio y alcance

El código vigente ya delega el cálculo fiscal a MS-2. La duplicación dentro de
`VentaService` era la asignación de los tres importes, no una fórmula fiscal.
`AplicarTotales` concentra esa asignación y se utiliza en `CrearAsync`,
`AgregarItemAsync`, `ModificarCantidadItemAsync` y `EliminarItemAsync`.
Conserva los resultados autoritativos de MS-2, incluidos sus redondeos; no vuelve
a multiplicar precios ni a calcular IVA. Las interfaces y pruebas quedan intactas.

El recorrido HTTP no cambia: frontend → Controller → Service → Repository → BD.
El controlador sigue consultando MS-3/MS-2 antes de persistir. La simplificación
reduce cuatro bloques de asignación a uno; no reduce los saltos del flujo.
No se modifica el grafo porque es un refactor interno sin cambio de comportamiento.

## Qué se probó

- Creación, agregado y edición de ventas con importes autoritativos.
- Pesables, redondeo fiscal y fronteras de peso.
- Aislamiento entre tenants y ausencia de mutaciones ante solicitudes rechazadas.
- Cálculo de IVA de MS-2.

## Resultado

- Línea base de integración: **115/115 aprobadas**, 0 omitidas, exit code 0.
- IVA de MS-2: **17/17 aprobadas**, exit code 0.
- Integración posterior: **115/115 aprobadas**, 0 omitidas, exit code 0.
- Mismos casos antes y después; ningún archivo de pruebas fue modificado.
- Duración de integración: base 9 min 47 s; posterior 9 min 54 s (sin compilación).
- Artefactos: [base.trx](base.trx), [despues.trx](despues.trx), [iva.trx](iva.trx).
- `git diff --check`: aprobado.

## Cómo reproducir

Desde la raíz del repositorio, con PostgreSQL local disponible. Las credenciales
siguientes corresponden al Compose de desarrollo, no al clúster:

```bash
export ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=5436;Database=pos_db;Username=admin;Password=admin_password'
export GLOBALMART_TEST_POSTGRES="$ConnectionStrings__DefaultConnection"
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --no-restore -m:1 --filter 'FullyQualifiedName~VentaIntegrationTests|FullyQualifiedName~ModificarPesoIntegrationTests|FullyQualifiedName~PesableTaxRoundingTests|FullyQualifiedName~VentaTenantIsolationTests' --logger 'trx;LogFileName=despues.trx' --results-directory /tmp/s3-039-despues
dotnet test tests/GlobalMart.Tests/GlobalMart.Tests.csproj -m:1 --filter 'FullyQualifiedName~TaxCalculatorService_Tests' --logger 'trx;LogFileName=iva.trx' --results-directory /tmp/s3-039-tax
```

La línea base usó el mismo filtro antes del refactor, con salida `base.trx`.
Los TRX conservan nombres, resultados y duración de cada caso.
No se suman las dos corridas de integración como casos distintos.

## Límites

Pruebas locales con PostgreSQL y clientes HTTP controlados por las fixtures.
No se verifica despliegue, interfaz Electron ni entrega Kafka. No cambia el
contrato HTTP, las reglas de peso, los permisos ni la autoridad fiscal de MS-2.

## Explicación para la reunión

“Unifiqué en VentaService la actualización del subtotal, IVA y total. Crear,
agregar, editar y eliminar ítems usan el mismo método, conservando los importes
y redondeos que ya entrega MS-2.”
