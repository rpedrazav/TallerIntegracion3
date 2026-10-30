## 3. Análisis de contribuciones individuales

Las identidades se relacionan mediante autores/correos de Git, la columna Persona/Asignado del plan y el mapeo del conversor. Se usan nombres de pila donde no hace falta inferir apellidos. Un bot de un período anterior no se cuenta como integrante del Sprint 2.

| Integrante | Autor Git | Cuenta del plan | Commits de trabajo, incluidos merges | Integrados / fuera de dev | Merges |
|---|---|---|---:|---:|---:|
| Sebastián | `SPereda2005` | `spereda2024` | 19 | 19 / 0 | 4 |
| Nicolás | `nmartinez` | `nmartinez2024` | 15 | 15 / 0 | 0 |
| Martín | `martinxoo` | `msanhueza2024` | 18 | 14 / 4 | 2 |
| Rodrigo | `iignvcioo` | `admin` | 20 | 20 / 0 | 0 |
| Daniel Prado | `Daniel Prado` | `dprado2024` | 18 | 8 / 10 | 1 |
| Diego Leiva Caballero | `Diego Leiva Caballero` | `dleiva2024` | 5 | 5 / 0 | 1 |
| **Total** | | | **95** | **81 / 14** | **8** |

Los dos objetos de stash de Rodrigo se registran aparte. Estas cifras son un inventario, **no una clasificación de rendimiento, esfuerzo o importancia**. Diego agrupa muchas tareas en cuatro commits de trabajo amplios; Daniel tiene aportes relevantes fuera de la rama integrada.

### Sebastián — reglas y persistencia de venta

La evidencia principal está en `VentasController`, `VentaService`, `VentaRepository`, DTOs, `IntentoPago`, `ReglasCantidadItem` y pruebas de venta/pagos/peso. La secuencia va desde S2-001/005, pasando por S2-031/035, hasta S2-061/065. `b200256` documenta IVA implementado antes, y `847ef12` agrega contrato/tests sobre adaptador ya incorporado por Nicolás. El merge `1ba6b37` es relevante por resolver semántica de incertidumbre y reserva con el trabajo de Rodrigo.

**Debe explicar:** autoridad de precio/IVA, reserva antes de HTTP, concurrencia por venta, commit de pago/outbox, kg y redondeo. **Límites:** anulación/reversa/evento `sale.reversed` del bloque S2-091/095 no se acreditan como implementados. La participación en reuniones y esfuerzo personal se confirma con el integrante.

### Nicolás — publicación durable, pasarela y balanza

Quince commits cubren contrato/migración/outbox/worker/recuperación (S2-006/010), adaptador y simulador de pagos/consulta/contrato de secuencia (S2-036/040), y contrato/simulador/IPC/serial abstracto/composición de balanza (S2-066/070). Los archivos permiten separar definición, implementación y validación; `731a23f` usa productor controlado y no Kafka caído físicamente.

**Debe explicar:** identidad estable, transacción local, lease, reintentos, incertidumbre de autorización, frontera serial y contexto de tenant sin HTTP. **Límites:** no se acreditan como resueltas las tareas S2-096/100 de atomicidad, offsets y recuperación del consumidor; el código actual conserva esas brechas. No hay compatibilidad física demostrada.

### Martín — administración y terminal

Sus commits implementan baja lógica de catálogo y lectura activa, tipado/configuración, rutas Admin, edición y baja UI, `TerminalPort`, máquina de estados, simulador, IPC y sincronización con MS-5. `b415c99` arregla infraestructura de test; no agrega funcionalidad de catálogo. `3eb4cb4` y `9b7cb4b` son trabajo exclusivo de rama; los otros dos objetos fuera de dev son merges.

**Debe explicar:** conservación histórica al desactivar, diferencia entre permiso UI/API, confirmación autoritativa de cobro, validación IPC y timeout. **Límites:** la suite Admin de su rama no se confunde con la creada posteriormente en dev por Rodrigo; los helpers single-flight de su último commit no están integrados. Cancelar un pago backend sigue sin soporte.

### Rodrigo — integración POS y cierre administrativo

Además de planificar, centraliza clientes, exige turno, conecta creación/mutaciones/recuperación de venta, modal/efectivo/tarjeta/timeout y captura/confirmación/respaldo manual. El cierre amplía administración, corrige `Usuarios`, agrega inventario por sucursal y roles propios. Modifica backend e infraestructura: no debe reducirse su contribución a colores o pantallas. Los stashes no cuentan como entregas.

**Debe explicar:** estado visual frente a venta durable, recarga y respuestas ambiguas, totales autoritativos, confirmación del peso, permisos heredados y adaptación del despliegue. **Límites:** los tests UI usan HTTP controlado; los resultados remotos son registros históricos separados. El consumidor está apagado donde no hay broker y la revocación inmediata de JWT sigue pendiente.

### Daniel — aceptación, aislamiento y evidencia

Sus ocho commits integrados aportan fixture, matrices, regresión inicial, refuerzo RN-01 y pruebas de pago. En otra rama quedan el entorno/E2E Electron, protocolo físico, plan documental, parser, cálculo entre servicios y fallback. Los tests distinguen componentización, HTTP, PostgreSQL, renderer parcial y Electron. `5850d61` contiene correcciones productivas de finitud/unidad no incorporadas a dev.

**Debe explicar:** controles positivos/negativos, snapshots, limpieza, diferencias entre niveles de test y límites de resultados. **Límites:** `f787762` incluye una guardia que denuncia aceptación manual incompleta en su revisión y no monta UI. El protocolo físico no acredita ejecución; el E2E de tarjeta no publica Kafka. No se adjudica a Daniel toda implementación que sus pruebas verifican.

### Diego — entorno, rutas, CI y observabilidad

Cuatro commits de trabajo agrupan S2-026/030, S2-056/060 y capacidades S2-086/089; el quinto integra cambios de otros. Hay evidencia abundante en `kong.yaml`, Compose, `api.ts`, workflows, scripts, `PaymentGatewayClient`, `DeviceManager`, main y preload. El bajo número de commits no obliga a inventar tareas: cada uno tiene varios cambios concretos para estudiar.

**Debe explicar:** paths y puertos, `strip_path`, modos/overrides de API, typecheck frente a ejecución, healthcheck frente a aceptación, logs y contratos IPC. **Límites:** el smoke de cobro emplea un doble de pasarela y mensajes fijos; no certifica despliegue real ni Kafka. La clase física está reservada. Las políticas de checks requeridos en GitHub no se deducen del workflow.

### Cómo usar las respuestas personales

Las respuestas en primera persona modelan una defensa basada en la autoría declarada en Git; **no sustituyen la comprensión del estudiante ni prueban trabajo sin ayuda**. Cada integrante debe abrir el archivo y explicar el recorrido de datos y errores. Si una pregunta pide una vivencia, las respuestas indican qué confirmar y proponen una estructura: contexto → acción propia → decisión → resultado → evidencia → mejora. No se inventan reuniones, horas, revisiones ni emociones.

Los bancos continúan con 60 preguntas grupales y luego 30 por persona en el orden de esta tabla. Los IDs `G`, `S`, `N`, `M`, `R`, `D` y `L` evitan duplicaciones y permiten localizar las selecciones finales.
