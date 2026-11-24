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
├── MS2_TaxCompliance/
│   └── TaxCalculatorService_Tests.cs  # Tests de cálculo IVA (TI3-210, TI3-211, TI3-212)
└── MS4_Warehouse/
    ├── KafkaConsumer_IdempotenciaTests.cs      # Tests de idempotencia consumer Kafka (TI3-252)
    ├── SaleEventProcessor_MovimientoStockTests.cs # Tests de log estructurado y caso borde (TI3-253, TI3-254)
    ├── StockController_Tests.cs                # Tests de endpoints HTTP y multi-tenant (TI3-255)
    └── SeedData_Tests.cs                       # Tests de script de seed de 30 productos y stock (TI3-256)
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

### KafkaConsumer_IdempotenciaTests (6 tests)

| Test | Tarea | Qué verifica |
|------|-------|--------------|
| `ProcesarEvento_PrimeraVez_DescuentaStockYRegistraEvento` | TI3-252 | Evento nuevo → descuenta stock y registra en EventosKafkaProcesados |
| `ProcesarEvento_MismoEventIdDosVeces_SegundaVezEsDescartada` | TI3-252 | Mismo event_id dos veces → segunda se descarta, descuenta stock 1 sola vez |
| `ProcesarEvento_MismoEventIdTresVeces_SoloUnaEjecucion` | TI3-252 | Tres veces el mismo evento → exactamente 1 ejecución |
| `ProcesarEvento_DosEventosDistintos_AmbosSeProcesanCorrectamente` | TI3-252 | Distintos event_ids → ambos se procesan y descuentan stock |
| `ProcesarEvento_PayloadInvalido_RetornaFalseSinRegistrar` | TI3-252 | JSON inválido → retorna false sin registrar |
| `ProcesarEvento_EventoSinItems_RetornaFalseSinRegistrar` | TI3-252 | Evento sin items → no procesa ni registra |

### SaleEventProcessor_MovimientoStockTests (3 tests)

| Test | Tarea | Qué verifica |
|------|-------|--------------|
| `ProcesarEvento_ConStockExistente_RegistraMovimientoStockConDetalle` | TI3-253 | Log estructurado y registro en `movimientos_stock` con tipo VENTA, stock anterior y nuevo |
| `ProcesarEvento_SinStockPrevio_LlamaCrearYDescontarYRegistraMovimiento` | TI3-254 | Producto sin stock previo → llama `CrearYDescontar`, crea stock negativo (discrepancia) |
| `ProcesarEvento_MultiplesItems_RegistraTodosLosMovimientos` | TI3-253 | Venta con múltiples items → registra un movimiento por cada item |

### StockController_Tests (6 tests)

| Test | Tarea | Qué verifica |
|------|-------|--------------|
| `GetByProducto_ProductoExiste_Retorna200ConStock` | TI3-255 | Consulta stock de producto existente → HTTP 200 con objeto Stock |
| `GetByProducto_ProductoNoExiste_Retorna404` | TI3-255 | Producto inexistente en sucursal → HTTP 404 Not Found |
| `GetByProducto_SucursalVacia_Retorna400` | TI3-255 | `sucursal_id` vacío (Guid.Empty) → HTTP 400 Bad Request |
| `GetByProducto_SinTenantClaim_Retorna401` | TI3-255 | Request sin claim `tenant_id` en JWT → HTTP 401 Unauthorized |
| `GetAll_RetornaListaDeStocksDelTenant` | TI3-255 | Listado general de stock del tenant → HTTP 200 con lista |
| `GetAll_ConFiltroSucursal_RetornaSoloDeEsaSucursal` | TI3-255 | Filtrado por query param `sucursal_id` → retorna solo de esa sucursal |

### SeedData_Tests (4 tests)

| Test | Tarea | Qué verifica |
|------|-------|--------------|
| `CatalogSeedData_InsertaExactamente30ProductosConCategoriasYPrecios` | TI3-256 | Inserta exactamente 30 productos, 4 categorías (Frutas, Carnes, Lácteos, Snacks) con códigos de barras únicos y precios |
| `CatalogSeedData_EjecutarDosVeces_EsIdempotenteNoDuplica` | TI3-256 | La ejecución reiterada del seed no duplica registros |
| `WarehouseSeedData_InsertaStockParaLos30Productos` | TI3-256 | Inserta stock inicial (50 unidades) para cada uno de los 30 productos en MS-4 |
| `WarehouseSeedData_EjecutarDosVeces_EsIdempotente` | TI3-256 | El seed de stock es idempotente |

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
