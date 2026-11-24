# S3-009 — Eliminar código muerto del flujo de caja en MS-5

Fecha: 2026-11-16. Meta: Funcionalidad de punta a punta.

## Objetivo y criterio de aceptación

Quitar los candidatos inventariados en S3-001 (Semana 9) que no rompan pruebas ni rutas usadas por el frontend. Aceptación: `dotnet test` de MS-5 pasa igual que la línea base.

## Cambios

- Eliminados `VentaService.RegistrarIntentoPagoAsync`, su alias en repositorio y las firmas/fakes asociados: no tenían llamadas productivas.
- Eliminado el wrapper `VentaService.ReservarIntentoPagoAsync`. **Se conserva la reserva del repositorio**, utilizada por `IniciarIntentoPagoAsync` para la idempotencia.
- Eliminado el parámetro ignorado `kafkaProducer` de `VentaService`; actualizados constructores y pruebas. La publicación sigue a cargo del worker de outbox.
- Eliminados `VentaRepository.GetByCajeroAsync` y su firma/fake: sin consumidores.
- Conservado `/turnos`: además del contrato de prefijos, varias pruebas de integración llaman esa ruta. Eliminarla excedería una limpieza sin ruptura de contratos.
- Unificada la extracción de claims en `Controllers/PosClaims.cs`, conservando prioridades, validaciones y respuestas de cada acción.
- Unificada la construcción de `sale.completed` en `CrearOutboxVentaCompletada`; efectivo y tarjeta conservan sus operaciones transaccionales independientes.
- Actualizados los dos ejecutables manuales antiguos para comprobar el outbox persistido en lugar de esperar publicación directa desde `VentaService`.

No se modificaron endpoints, reglas ni payloads; por tratarse de una refactorización interna no corresponde cambiar el grafo de contexto.

## Flujo comprobado por lectura de código

Cierre de turno: `CerrarTurnoPage.handleCerrarTurno` → `posApi.closeShift` → `TurnosController.Cerrar` → `TurnoService.Cerrar` → `TurnoRepository.Cerrar` → `PosCartDbContext.SaveChangesAsync`.

Son seis puntos antes y después, agrupando botón y handler. La mejora elimina métodos alternativos sin uso y duplicaciones; no se atribuye una reducción ficticia de capas. La extracción de claims es una validación compartida, no una nueva capa de negocio.

## Qué se probó

La selección reproducible de 29 clases está en [filtro.txt](filtro.txt): creación y edición de ventas, pesos, efectivo, tarjeta, reversas, permisos y aislamiento tenant, turnos, persistencia de intentos, outbox y contratos de eventos. Incluye pruebas de transacciones con PostgreSQL real y pruebas con fixtures/simuladores.

## Cómo reproducir

Desde la raíz del repositorio, con PostgreSQL de pruebas disponible:

```bash
export GLOBALMART_TEST_POSTGRES='Host=127.0.0.1;Port=55499;Database=s3009;Username=postgres;Password=s3009'
export ConnectionStrings__DefaultConnection="$GLOBALMART_TEST_POSTGRES"
dotnet test tests/GlobalMart.IntegrationTests --configuration Release --no-restore --filter "$(cat docs/qa/S3-009/filtro.txt)" --logger 'trx;LogFileName=final.trx' --results-directory docs/qa/S3-009
dotnet run --project src/POSCartService/tests/POSCartService.CobroCuadreTest --configuration Release --no-restore
dotnet run --project src/POSCartService/tests/POSCartService.AgregarItemTest --configuration Release --no-restore
```

## Resultado

- Línea base: 267 aprobadas, 0 fallidas y 1 omitida, exit code 0. Evidencia: [base.trx](base.trx).
- Verificación final de integración: **267 aprobadas, 0 fallidas, 1 omitida; exit code 0**, igual que la línea base. Duración: 36 min 8 s. Evidencia: [final.trx](final.trx) y [final.log](final.log). La primera ejecución final se interrumpió con MSB4166 sin generar TRX; la ejecución exitosa usó `--no-build --no-restore` sobre la compilación final completada. La causa de la diferencia de duración respecto de la línea base no está confirmada.
- CobroCuadre: 10/10 comprobaciones aprobadas, exit code 0. Evidencia: [cobro-cuadre.log](cobro-cuadre.log).
- AgregarItem: 93/93 comprobaciones aprobadas, exit code 0. Evidencia: [agregar-item.log](agregar-item.log).
- Los ejecutables manuales tenían supuestos obsoletos previos: CobroCuadre fallaba en dos comprobaciones de publicación directa; AgregarItem se detenía al iniciar una transacción con InMemory. Se ajustaron para comprobar outbox; la atomicidad se comprueba con PostgreSQL, no con InMemory.

## Límites

La prueba Electron requiere `GLOBALMART_ELECTRON_E2E=1` y no forma parte de esta ejecución. No se verifica la UI desplegada ni el clúster. Los fixtures de catálogo/IVA y la pasarela simulada no equivalen a servicios externos reales. No se declara entrega exactamente una vez de extremo a extremo ni un porcentaje de cobertura.

## Explicación para el equipo

“Limpié el flujo de caja de MS-5 quitando métodos que nadie utilizaba y un parámetro Kafka obsoleto. Compartí las validaciones repetidas de identidad y la construcción del evento de venta, manteniendo las rutas y reglas actuales. Comparé las pruebas antes y después para verificar que la limpieza no cambiara el comportamiento.”
