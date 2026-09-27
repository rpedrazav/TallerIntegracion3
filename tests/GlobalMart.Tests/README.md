# GlobalMart.Tests — Tests Unitarios

Proyecto de tests unitarios para los microservicios de **GlobalMart OS** usando **xUnit**, **Moq** y **FluentAssertions**.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) instalado
- No se necesita Docker ni base de datos (los tests usan mocks)

## Comandos

### Restaurar dependencias

```bash
dotnet restore tests/GlobalMart.Tests/GlobalMart.Tests.csproj
```

### Compilar el proyecto de tests

```bash
dotnet build tests/GlobalMart.Tests/GlobalMart.Tests.csproj
```

### Ejecutar todos los tests

```bash
dotnet test tests/GlobalMart.Tests/ --verbosity normal
```

### Ejecutar un test específico por nombre

```bash
dotnet test tests/GlobalMart.Tests/ --filter "Login_ConCredencialesCorrectas_Retorna200ConToken"
```

### Ejecutar solo los tests de una clase

```bash
dotnet test tests/GlobalMart.Tests/ --filter "FullyQualifiedName~AuthController_LoginTests"
dotnet test tests/GlobalMart.Tests/ --filter "FullyQualifiedName~AuthService_RoleTests"
```

> **Nota Linux:** Si `dotnet` no está en tu PATH, agrégalo con:
> ```bash
> export PATH="$HOME/.dotnet:$PATH"
> ```
> O agrégalo permanentemente en tu `~/.bashrc`.

## Estructura del Proyecto

```
tests/GlobalMart.Tests/
├── GlobalMart.Tests.csproj            # Proyecto xUnit con Moq y FluentAssertions
├── README.md                          # Este archivo
├── MS1_TenantIdentity/
│   ├── AuthController_LoginTests.cs   # Tests de login (TI3-206, TI3-207, TI3-208)
│   └── AuthService_RoleTests.cs       # Tests de roles RBAC (TI3-209)
└── MS2_TaxCompliance/
    └── TaxCalculatorService_Tests.cs  # Tests de cálculo IVA (TI3-210, TI3-211, TI3-212)
```

## Tests Implementados

### AuthController_LoginTests (7 tests)

| Test | Tarea | Qué verifica |
|------|-------|--------------|
| `Login_ConCredencialesCorrectas_Retorna200ConToken` | TI3-206 | Login OK → HTTP 200 + JWT con user_id |
| `Login_ConCredencialesCorrectas_RetornaUsuarioConEmail` | TI3-206 | Login OK → respuesta incluye email y active_role |
| `Login_ConContrasenaIncorrecta_Retorna401` | TI3-207 | Contraseña mala → HTTP 401 |
| `Login_ConContrasenaIncorrecta_MensajeNoRevelaDetalles` | TI3-207 | Mensaje genérico, no revela si el usuario existe |
| `Login_ConUsuarioInexistente_Retorna401MensajeGenerico` | TI3-207 | Email inexistente → mismo 401 genérico |
| `Login_ConEmailDeOtroTenant_Retorna401` | TI3-208 | Multi-tenant: email de otro tenant → 401 |
| `Login_ConEmailDeOtroTenant_NoRevelaExistenciaEnOtroTenant` | TI3-208 | No revela que el usuario existe en otro tenant |

### AuthService_RoleTests (6 tests)

| Test | Tarea | Qué verifica |
|------|-------|--------------|
| `Usuario_ConMultiplesRoles_GuardaYRecuperaCorrectamente` | TI3-209 | Modelo con 2 roles: se guardan y leen bien |
| `Usuario_SinRoles_TieneColeccionVacia` | TI3-209 | Colección de roles vacía por defecto |
| `Usuario_ConUnSoloRol_RecuperaCorrectamente` | TI3-209 | Un solo rol se recupera correctamente |
| `AuthService_ValidateCredentials_RetornaUsuarioConRolesIntactos` | TI3-209 | AuthService devuelve usuario con roles cargados |
| `JwtService_GenerateToken_IncluyeRolesEnElToken` | TI3-209 | JWT contiene claims `roles` y `active_role` correctos |
| `JwtService_GenerateToken_ConUnSoloRol_ActiveRoleCorrecto` | TI3-209 | Un solo rol → active_role = ese rol |

### TaxCalculatorService_Tests (12 tests)

| Test | Tarea | Qué verifica |
|------|-------|--------------|
| `Calculate_IVA19_ConDosItems_SubtotalCorrecto` | TI3-210 | Subtotal = 13.000 |
| `Calculate_IVA19_ConDosItems_IvaCorrecto` | TI3-210 | IVA 19% = 2.470 |
| `Calculate_IVA19_ConDosItems_TotalCorrecto` | TI3-210 | Total = 15.470 |
| `Calculate_IVA19_ConDosItems_DesglosePorItem` | TI3-210 | Desglose item por item |
| `Calculate_IVA21_Subtotal10000_IvaCorrecto` | TI3-211 | IVA 21% de 10.000 = 2.100, total = 12.100 |
| `Calculate_IVA21_ConMultiplesItems_TotalCorrecto` | TI3-211 | Múltiples items con IVA 21% |
| `Calculate_IVA0_ProductoExento_IvaEsCero` | TI3-212 | IVA 0% → subtotal = total, IVA = 0 |
| `Calculate_IVA0_MultipleItems_SubtotalIgualTotal` | TI3-212 | Múltiples items exentos |
| `Calculate_ItemConFlagExento_IvaEsCeroParaEseItem` | TI3-212 | Canasta mixta: exento + gravado |
| `Calculate_ConListaVacia_RetornaCeros` | — | Lista vacía → ceros |
| `Calculate_ConItemsNull_LanzaArgumentNullException` | — | Null → excepción |
| `Calculate_ConPorcentajeNegativo_LanzaArgumentOutOfRangeException` | — | % negativo → excepción |

## Cobertura de Código (TI3-213)

### Generar reporte de cobertura

```bash
dotnet test tests/GlobalMart.Tests/ --collect:"XPlat Code Coverage" --results-directory tests/GlobalMart.Tests/TestResults
```

### Cobertura actual

| Clase | Cobertura |
|-------|-----------|
| `AuthService` | 100% |
| `AuthController` | 100% |
| `JwtService` | 100% |
| `TaxCalculatorService` | 93.5% |

## Tecnologías

| Paquete | Versión | Para qué se usa |
|---------|---------|-----------------|
| **xUnit** | 2.9.2 | Framework de tests |
| **Moq** | 4.20.72 | Mocking de interfaces (IAuthService, IJwtService, etc.) |
| **FluentAssertions** | 6.12.2 | Aserciones legibles (`.Should().Be(...)`) |
| **Microsoft.NET.Test.Sdk** | 17.11.1 | Runner de tests para `dotnet test` |
