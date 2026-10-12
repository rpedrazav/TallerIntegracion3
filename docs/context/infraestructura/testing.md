---
id: testing
tipo: infra
titulo: Estrategia de Testing — GlobalMart OS
estado: parcial
fuentes: [src/POSCartService/tests/, src/TaxComplianceService/tests/, .github/workflows/ci.yml]
verificado_contra_codigo: true
ultima_revision: 2026-10-12
depende_de: [stack]
publica: []
consume: []
reglas: [RNF-07]
---
# Estrategia de Testing — GlobalMart OS

> Estado real de los tests. La cobertura actual es muy baja respecto al objetivo RNF-07 (≥80%).

## Tests existentes (verificados en código)

| Proyecto | Tipo | Estado |
|---------|------|--------|
| `POSCartService.AgregarItemTest` | Console app (smoke test) | [IMPLEMENTADO] |
| `POSCartService.ManualTest` | Console app (smoke test) | [IMPLEMENTADO] |
| `TaxComplianceService.ManualTest` | Console app (smoke test) | [IMPLEMENTADO] |
| `CatalogPricingService/tests/tests_manual_producto.http` | HTTP manual test | [IMPLEMENTADO] |
| `GlobalMart.IntegrationTests` | xUnit + WebApplicationFactory | [IMPLEMENTADO] |

Los smoke tests existentes son console apps que hacen requests HTTP y muestran resultados en consola. `GlobalMart.IntegrationTests` agrega assertions xUnit reales sobre los flujos de MS-1 y MS-5.

## Objetivo de tests (RNF-07)

- ≥ 80% de cobertura en lógica crítica de MS-1, MS-2, MS-5 → **NO CUMPLIDO**
- Tests de integración: apertura de turno y flujo turno → venta → ítems → IVA → **PARCIALMENTE IMPLEMENTADO**
- Tests Playwright de renderer: **PARCIAL**, S2-025 ejecutada con 1 aprobado y 2 fallidos; integración completa de venta pendiente.

## Stack de testing configurado

En las dependencias del proyecto:
- **xUnit** — framework de tests unitarios
- **FluentAssertions** — assertions expresivas
- **Moq** — mocking
- **WebApplicationFactory** — tests de integración ASP.NET Core

Los flujos de venta y cobro usan handlers HTTP en memoria para simular respuestas deterministas de MS-2 y MS-3 mientras ejecutan el pipeline real de MS-5. La publicación de `sale.completed` cuenta además con pruebas de integración que cubren productor real, consumidor real de MS-4 y descuento de stock verificado por API mediante polling. El cuadre de caja también se verifica mediante HTTP con una venta y pago efectivo persistidos.

## Cómo ejecutar los smoke tests

```bash
# Agregar ítem a venta (requiere MS-5 corriendo)
dotnet run --project src/POSCartService/tests/POSCartService.AgregarItemTest

# Test manual de tax
dotnet run --project src/TaxComplianceService/tests/TaxComplianceService.ManualTest
```

## Tests de CI

El pipeline en GitHub Actions ejecuta `dotnet test GlobalMartOS.sln --configuration Release`. Los console apps de smoke testing compilan y terminan (exit code 0) sin ejecutar casos de prueba reales.

## Brechas críticas

| Brecha | Impacto |
|--------|---------|
| Sin tests unitarios xUnit | RNF-07 no cumplido |
| Sin tests de integración | Regresiones no detectadas |
| Regresión Playwright S2-025 fallida | POS sin respuesta MS-5 y carrito compartido entre sesiones |
| Sin coverage report en CI | No se puede medir cobertura |

## Recomendaciones para agregar tests

```bash
# Crear test de integración para VentaService
dotnet new xunit -n POSCartService.Tests -o src/POSCartService/tests/POSCartService.Tests

# Agregar a la solución
dotnet sln GlobalMartOS.sln add src/POSCartService/tests/POSCartService.Tests/POSCartService.Tests.csproj
```

## Conexiones
- CI/CD: [[ci-cd]]
- Stack: [[stack]]

## Fuentes
- `src/POSCartService/tests/POSCartService.AgregarItemTest/Program.cs`
- `src/TaxComplianceService/tests/TaxComplianceService.ManualTest/Program.cs`
- `.github/workflows/ci.yml`

## S2-022 — Fixtures de dos tenants y turnos

[IMPLEMENTADO] Utilidad reutilizable en [Fixtures/README.md](../../../tests/GlobalMart.IntegrationTests/Fixtures/README.md) con esquemas PostgreSQL exclusivos por ejecución, usuarios CAJERO, sucursales, productos, turnos abiertos/cerrados, ventas pendientes/completadas e ítems/pagos efectivos para dos tenants. Limpieza idempotente por tenant y disposición de los esquemas propios. Compilación Release aprobada; 2/2 pruebas aprobadas el 2026-10-12, incluida la de integración contra PostgreSQL 16 desechable en Docker (filtrado por tenant, limpieza idempotente que conserva al otro tenant y eliminación de esquemas verificada). Corregido el seed del rol CAJERO, que ya existe tras crear las tablas. No cambia endpoints ni acredita E2E de frontend.

## S2-025 — Regresión de carrito

[PARCIAL] Suite Playwright sobre el renderer real de POS, con catálogo y puente de sesión controlados. Ejecución 2026-10-12: 1 aprobado (agregar, cantidad, eliminar y recargar), 2 fallidos (sin respuesta de ventas MS-5 y persistencia del carrito al cambiar sesión de tenant). Pos usa `pos_cart` sin partición por tenant y calcula IVA local al 19%. Igualdad con totales de MS-5 y aislamiento HTTP de backend [NO VERIFICADO]. No acredita E2E completo ni venta funcional. Reporte y capturas: [S2-025](../../qa/S2-025/reporte.md).
