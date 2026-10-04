---
id: testing
tipo: infra
titulo: Estrategia de Testing — GlobalMart OS
estado: parcial
fuentes: [src/POSCartService/tests/, src/TaxComplianceService/tests/, .github/workflows/ci.yml]
verificado_contra_codigo: true
ultima_revision: 2026-10-03
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
- Tests E2E Playwright: **SIN IMPLEMENTAR**

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
| Sin tests Playwright E2E | Frontend sin verificación automatizada |
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
