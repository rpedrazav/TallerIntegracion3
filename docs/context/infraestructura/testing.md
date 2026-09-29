---
id: testing
tipo: infra
titulo: Estrategia de Testing — GlobalMart OS
estado: parcial
fuentes: [src/POSCartService/tests/, src/TaxComplianceService/tests/, .github/workflows/ci.yml]
verificado_contra_codigo: true
ultima_revision: 2026-09-28
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

**Ninguno de los "tests" es xUnit con assertions reales.** Son console apps que hacen requests HTTP y muestran resultados en consola.

## Objetivo de tests (RNF-07)

- ≥ 80% de cobertura en lógica crítica de MS-1, MS-2, MS-5 → **NO CUMPLIDO**
- Tests de integración: flujo completo login → turno → venta → cierre → **SIN IMPLEMENTAR**
- Tests E2E Playwright: **SIN IMPLEMENTAR**

## Stack de testing configurado (pero sin usar)

En las dependencias del proyecto:
- **xUnit** — framework de tests unitarios
- **FluentAssertions** — assertions expresivas
- **Moq** — mocking
- **WebApplicationFactory** — tests de integración ASP.NET Core

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
