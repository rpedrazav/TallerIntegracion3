# Defensa del Sprint 2 — GlobalMart OS

**Corte:** 30 de octubre de 2026. **Último commit de `dev`:** [`f0e6f31`](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7), del 27 de octubre a las 22:31:56, UTC−03:00. El usuario confirmó que la defensa corresponde al **Sprint 2 hasta el último commit disponible**.

## 1. Resumen global del sprint

El Sprint 2 concentra el trabajo en convertir el POS en un flujo persistido y recuperable: catálogo y precio autoritativos, impuestos desde el servidor, cobro efectivo y tarjeta simulada, publicación durable de eventos, captura de peso y administración. El plan revisado define **120 tareas y 120 horas de capacidad estimada**: seis integrantes × cinco horas semanales × cuatro semanas. No hay evidencia suficiente para transformar esa capacidad en horas realmente trabajadas.

La fuente principal de alcance es [plan_sprint2_revisado.xlsx](../../tareas/plan_sprint2_revisado.xlsx), incorporada por [`f913592`](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592). Sus hojas `Resumen` y `Sprint 2` contienen metas, asignaciones, entregables y aceptación. La hoja `Revisión original` explica tareas reformuladas o diferidas. [sprint2_youtrack_resultado.json](../../tareas/sprint2_youtrack_resultado.json) aporta correspondencias S2/YouTrack, **no un estado actualizado del tablero**.

| Meta del plan | Capacidad | Resultado respaldado al corte | Límite de la conclusión |
|---|---:|---|---|
| G1 — Administración | 15 h | Baja lógica de productos; edición/baja de usuarios; rutas y formularios; cierre con bootstrap, coherencia de `Usuarios`, roles propios e inventario administrativo | No se acredita revocación inmediata de JWT; auditoría y reactivación UI siguen pendientes. Una suite Admin de Martín permanece en otra rama |
| G2 — Carrito y venta funcional | 30 h | Venta persistida en MS-5; precio de MS-3; IVA de MS-2; efectivo; recuperación de carrito; outbox transaccional y worker | No se expone anulación HTTP. El consumidor tiene limitaciones de atomicidad/offsets y está desactivado en el despliegue documentado; no se cierra toda la aceptación hasta stock |
| G3 — Cobro con tarjeta | 25 h | Pasarela simulada; reserva durable; aprobación/rechazo; bloqueo ante incertidumbre; consulta y reconciliación del mismo intento | No hay banco real, captura física ni reembolso completo. El E2E Electron de Daniel está fuera de dev y desactiva publicación Kafka |
| G4 — Balanza | 20 h | Contrato kg; mínimo 10 g y resolución 1 g; simulador; adaptador serial abstracto; IPC; confirmación y respaldo manual; reglas backend | No se validó equipo físico. No está resuelta la correspondencia real de `UomBaseId` con kg. Correcciones runtime de Daniel están en rama |
| G5 — Terminal POS | 15 h | Contrato y máquina de estados; simulador; IPC limitado; sincronización con MS-5; timeout determinista | Cancelación backend no soportada; hardware físico [NO VERIFICADO]. Ampliación de pruebas y helpers single-flight de Martín fuera de dev |
| H — Salud técnica | 15 h | Rutas, configuración de APIs, Compose, typecheck CI, fixtures, reportes, scripts y grafo | No se acredita un release completo ni un porcentaje global de cobertura. Hay contradicciones documentales y evidencia con distintos límites |

**No se declara un porcentaje de cumplimiento del sprint.** La presencia de código relacionado con una tarea no basta para cerrar su criterio de aceptación. Tampoco se atribuye 80% de cobertura: no se encontró un reporte final que sostenga esa cifra.

### El flujo que todos deben poder explicar

1. El cajero inicia sesión y el backend valida JWT. El tenant procede del token.
2. El POS verifica turno abierto; MS-5 vuelve a comprobarlo al crear la venta y obtiene la sucursal del turno.
3. MS-5 consulta producto/precio en MS-3 y cálculo fiscal en MS-2. Persiste datos y totales autoritativos.
4. La UI modifica ítems mediante API y recupera la venta del servidor. En pesables confirma una lectura válida o un peso manual identificado.
5. Efectivo valida el monto y calcula vuelto. Tarjeta reserva un intento antes de autorizar, conserva identidad y diferencia rechazo de incertidumbre.
6. Una aprobación confirma localmente venta, pago, intento cuando corresponda y outbox. El worker publica después.
7. El descuento de inventario es otro tramo: **no se debe afirmar entrega o efecto exactamente una vez de extremo a extremo** por la sola existencia del outbox o de una tabla de deduplicación.

Fuentes decisivas: [VentasController](../../src/POSCartService/Controllers/VentasController.cs), [VentaRepository](../../src/POSCartService/Repositories/VentaRepository.cs), [OutboxPublisherService](../../src/POSCartService/Services/OutboxPublisherService.cs), [KafkaConsumerService](../../src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs) y [synchronizeCardPayment](../../globalmart-frontend/src/payments/synchronizeCardPayment.ts).

### Evidencia de pruebas y cómo citarla

| Evidencia versionada | Resultado informado | Qué permite sostener |
|---|---|---|
| [Merge de pagos S2-035/S2-049](../qa/S2-035/merge.md) | 54/54 backend y 7/7 UI | Integración de reserva y recuperación. UI con APIs controladas; no despliegue Kubernetes completo |
| [S2-052](../qa/S2-052/reporte.md) | 2/2 local | Rechazo/timeout HTTP con PostgreSQL y simulador Node. Remoto bloqueado antes del cobro por catálogo |
| [S2-053](../qa/S2-053/reporte.md) | 3/3 | Autorización única bajo duplicado aprobado o pendiente; simulador con memoria de proceso |
| [S2-065](../qa/S2-065/reporte.md) | 84/84 local | Fronteras pesables, corrección de creación sin peso y regresión; no valida hardware |
| [S2-055 en 3148c9a](https://github.com/rpedrazav/TallerIntegracion3/blob/3148c9a/docs/qa/S2-055/reporte.md) | 2/2 | Electron real con MS-5/PG/simulador; identidad/catálogo/IVA controlados; sin Kafka; fuera de dev |
| [Cierre administrativo en f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31/docs/context/estado-actual.md) | 334/334 .NET, 5/5 UI, typecheck y package informados | Registro de cierre en commit/grafo y pruebas existentes. Esta revisión no reejecuta esas corridas ni encontró un log independiente nuevo que agregue a esa afirmación |

**Esta tarea es un análisis documental y de código.** No ejecutó nuevamente .NET, Electron, Docker ni pruebas contra el clúster. Los resultados históricos se citan como tales. No se suman distintas regresiones que repiten casos para inventar un total de pruebas únicas. Una prueba de modelo EF, una documental y una de navegador con HTTP controlado no se presentan como integración física o remota.

### Hallazgos que afectan la defensa

- `sprint-actual.md` conserva Sprint 1 con revisión de septiembre, aunque el plan y los commits del período dicen Sprint 2. Hay secciones antiguas dentro del grafo que contradicen otras más recientes.
- La documentación del outbox describe decisiones y funciones futuras junto a implementaciones. El reproceso manual no se da por implementado.
- `ReservarIntentoPagoAsync` vigente usa un bloqueo de fila `FOR UPDATE` en una transacción breve; no debe describirse únicamente a partir de una sección histórica que dice aislamiento serializable.
- El consumidor MS-4 guarda descuentos y marcador por pasos y confirma offset incluso ante fallo. La afirmación documental “exactly-once processing” excede lo que ese código demuestra.
- `IntentoPagoPersistenceTests` del primer commit inspecciona modelo, propiedades y filtros: sus cuatro casos no demuestran por sí solos una carrera de autorizaciones reales.
- El smoke de correlación usa `RecordingGatewayClient`. Sus mensajes finales fijos no prueban publicación Kafka ni un despliegue real.
- `RealTerminalAdapter` de `devices/` devuelve error de capacidad no soportada. El modo `real` de la fábrica de `payments/` se relaciona con MS-5; no acredita pinpad bancario físico.
- Se evita repetir afirmaciones de certificación PCI presentes en algunos comentarios. El repositorio permite comprobar contratos sin PAN/CVV y referencias opacas, no una certificación integral.
- Un commit titulado `prueba` (`4d9fd64`) solo agrega líneas vacías y un encabezado vacío. Se inventaría, pero no cuenta como test implementado.

Estos hallazgos se incorporan a las preguntas y respuestas. No se modificó el código del producto ni el Grafo de Contexto: este entregable no altera endpoints, arquitectura o estado funcional; registra una lectura del corte y sus inconsistencias.

