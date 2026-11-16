# S3-001 — Inventario de código muerto en el flujo de caja MS-5

Fecha: 2026-11-16. Meta: M2 — Funcionalidad de punta a punta.

## Objetivo y aceptación

Recorrer VentasController, VentaService y TurnosController y entregar candidatos con archivo, método y motivo comprobable: sin llamadas, ruta duplicada o rama inalcanzable. Fuente: `tareas/plan_sprint3.xlsx`, S3-001.

**Alcance: inventario estático, sin eliminación de código ni cambios de comportamiento.** No se modificaron archivos productivos; la limpieza corresponde a la tarea de implementación posterior. No se considera muerto un endpoint solo porque no tenga botón.

## Lista breve de candidatos

| Candidato | Archivo / método | Evidencia y alcance de una eliminación posterior |
|---|---|---|
| 1. Método sin llamadas | [VentaService.cs](../../../src/POSCartService/Services/VentaService.cs), `RegistrarIntentoPagoAsync`, líneas 358–359 | No tiene llamadas desde controllers, frontend, scripts ni pruebas del repositorio. Solo declara y reenvía a un alias del repositorio que llama a `ActualizarIntentoPagoAsync`. Eliminar también la firma en IVentaService, alias en VentaRepository/IVentaRepository y miembro del fake de PaymentEventSequenceContractTests; conservar ActualizarIntentoPagoAsync, que sí usa el controller. |
| 2. Método del servicio sin llamadas | [VentaService.cs](../../../src/POSCartService/Services/VentaService.cs), `ReservarIntentoPagoAsync`, líneas 354–355 | No hay llamadas a esta entrada de **VentaService**. El controller usa `IniciarIntentoPagoAsync` (VentasController:950), que llega al método homónimo del repositorio. Eliminar solo la entrada del servicio y su firma en IVentaService. **Conservar VentaRepository.ReservarIntentoPagoAsync:** lo invoca IniciarIntentoPagoAsync en VentaRepository:181 y protege la reserva del cobro. |
| 3. Dependencia sin efecto | [VentaService.cs](../../../src/POSCartService/Services/VentaService.cs), constructor, líneas 16–29: parámetro `kafkaProducer` y descarte `_ = kafkaProducer` | El parámetro se recibe y se descarta; no se guarda ni se usa para publicar. El constructor explica que quedó por compatibilidad tras migrar a outbox. Hay cuatro invocaciones de pruebas que lo pasan: OutboxBrokerRecoveryIntegrationTests:37, VentaOutboxTransactionTests:25, POSCartService.CobroCuadreTest/Program:27 y POSCartService.AgregarItemTest/Program:693 (posicional). Retirar el parámetro exige actualizar esas llamadas y revisar sus aserciones. **No eliminar IKafkaProducerService ni su registro:** el publisher sí los necesita. |

Evidencia de referencias: [referencias.txt](referencias.txt). Ausencia de llamadas significa ausencia **en el repositorio revisado**; consumidores externos o reflexión fuera de él: [NO VERIFICADO].

## Resultado de los otros dos archivos y ramas revisadas

- **VentasController:** no se confirmó una acción pública eliminable. `Crear`, ítems, consulta por ID/turno, `Cobrar`, `CobrarTarjeta`, `ConsultarIntentoTarjeta` y `CambiarMetodoPago` tienen consumidores en `globalmart-frontend/src/api/client.ts` (líneas 176–235). Anular y ConsultarReversa están cubiertos por las suites S2-095; que aún no haya UI no los convierte en código muerto.
- **TurnosController:** sus cuatro acciones están activas: Abrir, Activo, Cerrar y Cuadre corresponden a `openShift`, `activeShift`, `closeShift` y `balanceShift` en `client.ts` (156–174). Cuadre consulta; Cerrar modifica el turno. No son rutas duplicadas.
- **Aliases `/api` y sin `/api`: conservar.** El cliente usa `/api/turnos` y `/ventas`; Kong configura `/api/turnos` y `/api/ventas` con `strip_path: false`. Las pruebas ejercitan ambas variantes. No se demostró una ruta duplicada prescindible.
- **Ramas null de dependencias opcionales:** no se clasifican como inalcanzables. Program registra las dependencias, pero hay construcción manual en pruebas (por ejemplo `new VentaService(repository)` y controllers sin pasarela). Antes de hacerlas obligatorias hay que migrar esos constructores. Las guardias de JWT, tenant, turno, método, incertidumbre y estado tampoco son código muerto.

## Duplicación activa: simplificar, no borrar el comportamiento

1. `VentaService.CompletarAsync` (109–142) y `CompletarTarjetaAsync` (311–344) construyen el mismo tipo de snapshot SaleCompletedEvent/OutboxMessage. Ambos se usan. Candidato a compartir la construcción conservando las transacciones distintas de efectivo y tarjeta.
2. `TurnosController.Abrir`, `Activo`, `Cerrar` y `Cuadre` repiten extracción/validación de claims (30–39, 67–76, 91–100 y 131–140). Podría quedar una sola función privada, manteniendo claims admitidos y respuestas HTTP. No quitar la validación.

## Recorrido para la defensa

- **Efectivo:** `Pos.tsx` (`PaymentModal.onConfirm` → `handlePayment` → `handleCashPayment`, líneas 754/610/522) → `posApi.chargeCash` (`client.ts`:217) → `VentasController.Cobrar` → `VentaService.CompletarAsync` → `VentaRepository.CompletarConOutboxAsync` → PostgreSQL. Después, el worker publica el outbox; el cobro no publica directamente.
- **Abrir turno:** `AbrirTurnoPage.handleSubmit` (73/92) → `posApi.openShift` → `TurnosController.Abrir` → `TurnoService.Abrir` → `TurnoRepository.Abrir` → PostgreSQL.
- **Cerrar turno:** `CerrarTurnoPage` (242) → `posApi.closeShift` → `TurnosController.Cerrar` → `TurnoService.Cerrar` → `TurnoRepository.Cerrar` → PostgreSQL. El cuadre previo llama a balanceShift/Cuadre/CalcularCuadreAsync/GetEfectivoTurnoAsync.

No se cambió el número de saltos en esta tarea: se identificaron entradas sobrantes fuera del camino activo, respetando Controller → Service → Repository.

## Verificación y reproducción

Revisión estática de los tres archivos, interfaces/repositorios relacionados, llamadas del frontend, constructores de pruebas, Program y rutas Kong. Búsquedas representativas:

```bash
rg -n 'RegistrarIntentoPagoAsync|ReservarIntentoPagoAsync|kafkaProducer:' src tests globalmart-frontend scripts
rg -n 'new (Pos.Services.)?VentaService' --glob '*.cs'
rg -n 'ventas|turnos' globalmart-frontend/src/api/client.ts Docker/config/kong.yaml
```

Resultado: tres candidatos delimitados (dos métodos sin llamadas y un parámetro descartado), dos duplicaciones activas y ninguna ruta demostrada como prescindible. No se ejecutaron pruebas ni build: solo se agregaron documentos. No se cambiaron endpoints, reglas, grafo funcional, configuración ni cluster.

## Guion breve

“Revisé el flujo de caja de MS-5 y encontré dos métodos del servicio sin llamadas y un parámetro antiguo de Kafka que ya no hace nada. Documenté dónde están y qué habría que actualizar al eliminarlos. También comprobé que las rutas de turnos y las validaciones sí se usan, para evitar borrar algo necesario.”
