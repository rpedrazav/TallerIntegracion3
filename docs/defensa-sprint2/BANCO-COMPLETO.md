# Banco completo — Defensa del Sprint 2


**240 preguntas con respuestas:** 60 grupales + 6 × 30 personales. Corte dev `f0e6f31`, con aportes exclusivos de otras ramas expresamente identificados.


## Contenido


- [01-resumen.md](01-resumen.md)

- [02-inventario-commits.md](02-inventario-commits.md)

- [03-contribuciones.md](03-contribuciones.md)

- [04-preguntas-grupales.md](04-preguntas-grupales.md)

- [05-sebastian.md](05-sebastian.md)

- [06-nicolas.md](06-nicolas.md)

- [07-martin.md](07-martin.md)

- [08-rodrigo.md](08-rodrigo.md)

- [09-daniel.md](09-daniel.md)

- [10-diego.md](10-diego.md)

- [11-prioridades-de-estudio.md](11-prioridades-de-estudio.md)


---


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



## 2. Inventario de commits revisados

El inventario completo está en [02-inventario-commits.md](02-inventario-commits.md), antes de los bancos. La versión estructurada [inventario-commits.json](inventario-commits.json) conserva SHA completo, fechas, padres, mensaje, todas las rutas modificadas, referencias y observación por objeto.

Se actualizó la información remota con `git fetch origin --prune`. `HEAD` y `origin/dev` coinciden en el SHA de corte. Se enumeraron **97 objetos** del período en las referencias Git disponibles: **95 commits de trabajo/integración y 2 objetos de stash**. De los 95, **81 están integrados en dev y 14 no**; incluyen **87 commits sin merge y 8 merges**. Las fechas observadas van del 11 al 27 de octubre; no se inventa una fecha formal de inicio o término del sprint calendario.

Para cada objeto se reconstruyeron metadatos, padres, pertenencia a dev, archivos y delta respecto del primer padre. Se contrastaron cambios de contratos, funciones, configuración y tests con el código al corte y con reportes. Los artefactos generados, binarios y lockfiles se inventarían como archivos; su presencia no se usa como prueba de ejecución. En merges, el delta puede contener trabajo de varios autores y **no se reasigna toda esa autoría al integrador**.

La búsqueda del conector GitHub para PR accesibles del repositorio actualizados desde el 10 de octubre devolvió cero resultados. No se atribuyen PR ni revisiones no encontrados. Esto no prueba inexistencia histórica de PR o coordinación informal. No se consultó un tablero YouTrack vivo: solo los archivos del repositorio.


### Registro completo

Orden cronológico por fecha de autor y SHA como desempate. `Dev` significa ancestro de `f0e6f31`; `Rama` no está integrado. Las observaciones distinguen trabajo, documentación y límites. Todas las rutas, padres, fechas de autor/committer y referencias están en el JSON adjunto.

| N.º | Commit | Fecha UTC−03 | Autor | Tipo / integración | Archivos | Cambio y lectura para la defensa |
|---:|---|---|---|---|---:|---|
| 1 | [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) | 2026-10-11 17:26 | iignvcioo | Trabajo / Dev | 5 | **Planificacion tareas Sprint 2**. Incorpora plan revisado de 120 tareas/120 horas, exportaciones y conversor para YouTrack. La planificación no demuestra ejecución. |
| 2 | [4a96e91](https://github.com/rpedrazav/TallerIntegracion3/commit/4a96e914b5f01b41eaa948293a0989da7e4bd9bc) | 2026-10-11 17:32 | iignvcioo | Trabajo / Dev | 2 | **correccion errores**. Ajusta Zod y valueAsNumber en TenantConfig; corrige representación numérica del IVA en el formulario. |
| 3 | [00572cf](https://github.com/rpedrazav/TallerIntegracion3/commit/00572cfb5054e43bea900b06b4baef43328b34c7) | 2026-10-12 15:26 | Daniel Prado | Trabajo / Dev | 6 | **Crear fixtures de dos tenants y turnos completado**. Crea fixture reutilizable de dos tenants y limpieza aislada, con pruebas de la infraestructura de test. |
| 4 | [a451b5d](https://github.com/rpedrazav/TallerIntegracion3/commit/a451b5d22e4632ae811e8adcf1814b9de98b20c2) | 2026-10-12 15:59 | Daniel Prado | Trabajo / Dev | 44 | **qa: Ejecutar regresión de carrito y ademas se corrigio un error de la tarea anterior.**. Agrega regresión de renderer y registra 1 aprobado/2 fallidos; no equivale a aceptación del carrito backend. |
| 5 | [a5d998d](https://github.com/rpedrazav/TallerIntegracion3/commit/a5d998dd049ad216bd5c0336523fcba6dc1c76de) | 2026-10-13 12:50 | SPereda2005 | Trabajo / Dev | 6 | **TI3-632**. Relaja datos informativos del request de venta y fija contrato; todavía necesita resolución autoritativa posterior. |
| 6 | [806450b](https://github.com/rpedrazav/TallerIntegracion3/commit/806450bec2bda6cee096bb62cbc02b204d5daae9) | 2026-10-13 13:39 | Daniel Prado | Trabajo / Dev | 2 | **Definir casos de aceptación de las cinco metas**. Define matriz de aceptación de cinco metas y diferencia prueba simulada de física. |
| 7 | [152f39b](https://github.com/rpedrazav/TallerIntegracion3/commit/152f39b00076de93573a261f9721ad1c29eb3c5c) | 2026-10-13 15:12 | SPereda2005 | Trabajo / Dev | 7 | **TI3-633 completada**. Resuelve productos en MS-3 y totales en MS-2 antes de persistir creación; actualiza contrato de servicio y pruebas. |
| 8 | [b062da4](https://github.com/rpedrazav/TallerIntegracion3/commit/b062da44211f0e422962f85396f08e33b2554aad) | 2026-10-13 15:12 | SPereda2005 | Merge / Dev | 2 | **Merge remote-tracking branch 'origin/dev' into dev**. Merge: incorpora la matriz de aceptación; los archivos entrantes no se reasignan como autoría original del integrador. |
| 9 | [566576b](https://github.com/rpedrazav/TallerIntegracion3/commit/566576be9d4f7d020171cb56d549864f4f570a10) | 2026-10-13 15:50 | iignvcioo | Stash — excluido de entregas | 3 | **index on dev: 4a96e91 correccion errores**. Objeto auxiliar del índice de stash; no se contabiliza como entrega. Incluye instantánea de API/HPA/documentación. |
| 10 | [ad0e295](https://github.com/rpedrazav/TallerIntegracion3/commit/ad0e295ccfdd246f5a6f320ed3da289ca35e4a1e) | 2026-10-13 15:50 | iignvcioo | Stash — excluido de entregas | 14 | **On dev: !!GitHub_Desktop<dev>**. Stash de GitHub Desktop con cambios de API y Kubernetes; no es entrega integrada ni prueba de despliegue. |
| 11 | [e1a2691](https://github.com/rpedrazav/TallerIntegracion3/commit/e1a269113ceeef94ed89dd79502b830fc2557ab2) | 2026-10-13 15:54 | iignvcioo | Trabajo / Dev | 9 | **Centralizar clientes API de login, catálogo y turnos**. Centraliza API tipada, JWT, timeout y 401; migra login/turnos/POS sin completar aún la venta persistida. |
| 12 | [b200256](https://github.com/rpedrazav/TallerIntegracion3/commit/b20025693dbd44553a9d74718ec2cd1c3093c0e7) | 2026-10-13 20:58 | SPereda2005 | Trabajo / Dev | 2 | **docs(context): documentar cumplimiento de S2-003 en MS-5 y estado actual**. Actualiza grafo para S2-003; el cálculo se implementó en 152f39b, no se cuenta otra implementación. |
| 13 | [c86909c](https://github.com/rpedrazav/TallerIntegracion3/commit/c86909ca3c944da0c185da90a66e3f2be1881197) | 2026-10-13 21:22 | SPereda2005 | Trabajo / Dev | 5 | **TI3-635 completada: restringir método de cobro efectivo (S2-004)**. Impide cobro EFECTIVO sobre TARJETA/MIXTO en controller y servicio y verifica ausencia de pago. |
| 14 | [cb7b8df](https://github.com/rpedrazav/TallerIntegracion3/commit/cb7b8df333e309d4e8e93e87498aa10e54d02c10) | 2026-10-14 00:45 | martinxoo | Trabajo / Dev | 10 | **TI3-642**. Baja lógica ADMIN por tenant, idempotencia, conservación histórica y confirmación visual de producto. |
| 15 | [b415c99](https://github.com/rpedrazav/TallerIntegracion3/commit/b415c99ceaaaf2859ad6e51880fa5f12cb8db889) | 2026-10-14 00:59 | martinxoo | Trabajo / Dev | 1 | **Solucion de error minimo**. Corrige configuración de conexión y esquema de migraciones de tests de baja, evitando arrancar la factoría base sobre esquema compartido. |
| 16 | [4bb39aa](https://github.com/rpedrazav/TallerIntegracion3/commit/4bb39aa947e6e7586d437b8ab490c1a99c03f02e) | 2026-10-14 01:22 | martinxoo | Trabajo / Dev | 6 | **TI3-643**. Separa consulta activa para venta de consulta administrativa que permite reactivar; amplía prueba de producto inactivo. |
| 17 | [1833981](https://github.com/rpedrazav/TallerIntegracion3/commit/18339813f950b4becfffcef7d046e7907172dff0) | 2026-10-14 11:40 | SPereda2005 | Trabajo / Dev | 3 | **test(ms5): S2-005 — pruebas creación segura y turno abierto (TI3-636)**. Agrega pruebas de precio falsificado y turno inexistente; otros escenarios seguros ya estaban presentes. |
| 18 | [d44e22b](https://github.com/rpedrazav/TallerIntegracion3/commit/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e) | 2026-10-14 15:12 | Daniel Prado | Trabajo / Dev | 17 | **Probar aislamiento de venta y producto**. Agrega aislamiento HTTP y directo de repositorios; refuerza mutaciones por tenant persistido y 404 de producto ajeno. |
| 19 | [ccf7bf3](https://github.com/rpedrazav/TallerIntegracion3/commit/ccf7bf330b2e9ae25d93d689d7926db9f81d0071) | 2026-10-14 18:49 | iignvcioo | Trabajo / Dev | 3 | **Exigir turno activo antes de mostrar el POS**. Consulta turno antes de operar POS, distingue carga, ausencia y fallo recuperable. |
| 20 | [160846e](https://github.com/rpedrazav/TallerIntegracion3/commit/160846eb68bac27937560baabb6a10d4ed545c73) | 2026-10-14 19:24 | iignvcioo | Trabajo / Dev | 5 | **Iniciar venta en MS-5 y recuperar venta pendiente en POS**. Integra creación de venta con respuesta autoritativa y recuperación por identificador desde MS-5. |
| 21 | [f612f9a](https://github.com/rpedrazav/TallerIntegracion3/commit/f612f9aa4a35460ba9ec56d52530fc9fc1bbb962) | 2026-10-14 19:28 | iignvcioo | Trabajo / Dev | 5 | **sincronizar ítems y totales de venta con MS-5**. Envía mutaciones de ítems y refresca venta/totales desde MS-5; reduce autoridad del estado local. |
| 22 | [9dedcc5](https://github.com/rpedrazav/TallerIntegracion3/commit/9dedcc50684998bb0791c214e7947939c0503021) | 2026-10-14 19:51 | iignvcioo | Trabajo / Dev | 4 | **sincronizar eliminación y recuperación de venta pendiente**. Conecta eliminación y recuperación de venta pendiente con feedback ante estado no confirmado. |
| 23 | [054d7d3](https://github.com/rpedrazav/TallerIntegracion3/commit/054d7d382ca6a91967e96dc885b9cfb6b1558c68) | 2026-10-14 22:02 | nmartinez | Trabajo / Dev | 1 | **docs(S2-006): definir contrato sale.completed y reintentos outbox**. Contrato versionado de sale.completed, identidad estable, reintentos y límites de entrega/deduplicación. |
| 24 | [2e546dc](https://github.com/rpedrazav/TallerIntegracion3/commit/2e546dc7110791c030047eacf9e5432ceff6be75) | 2026-10-14 23:05 | nmartinez | Trabajo / Dev | 10 | **feat(S2-007): agregar entidad y migracion outbox en MS-5**. Entidad y migración OutboxMessages con filtros e índices; prueba de migración y unicidad. |
| 25 | [b4f6d20](https://github.com/rpedrazav/TallerIntegracion3/commit/b4f6d20d5d9f6cdc9e85b73124b56e29bc5a7073) | 2026-10-15 00:55 | martinxoo | Trabajo / Dev | 2 | **TI3-645**. Ajusta tipado de entrada/salida del formulario TenantConfig; no implementa un nuevo endpoint fiscal. |
| 26 | [577fbd1](https://github.com/rpedrazav/TallerIntegracion3/commit/577fbd1f3d3cfb3bc792661ae928374eebe04dcc) | 2026-10-15 01:28 | martinxoo | Trabajo / Dev | 4 | **TI3-646**. Agrega pruebas de esquema y UI de configuración por tenant con dependencias controladas. |
| 27 | [7f26d0f](https://github.com/rpedrazav/TallerIntegracion3/commit/7f26d0fec50937cda0bec87a22af6b34dd41814d) | 2026-10-15 01:56 | martinxoo | Trabajo / Dev | 7 | **TI3-672**. Centraliza permisos visuales Admin y navegación; no sustituye autorización backend. |
| 28 | [0602a93](https://github.com/rpedrazav/TallerIntegracion3/commit/0602a936469683c8a1f3409477c6c644aa9fe5c0) | 2026-10-15 11:25 | nmartinez | Trabajo / Dev | 14 | **feat(pos): persistir venta y evento outbox atomicamente**. Guarda venta/pago/outbox en transacción y prueba rollback; elimina publicación directa como requisito del cobro. |
| 29 | [4cf822c](https://github.com/rpedrazav/TallerIntegracion3/commit/4cf822c347c18d987282a31b5babe3e2f3582a31) | 2026-10-15 11:40 | nmartinez | Trabajo / Dev | 17 | **feat(pos): publicar eventos pendientes desde outbox**. Publicador de fondo con reserva, lease, reintento y cancelación; respeta contexto de tenant en procesamiento. |
| 30 | [731a23f](https://github.com/rpedrazav/TallerIntegracion3/commit/731a23fc0f794be061a12882952ad4d003757cac) | 2026-10-15 15:38 | nmartinez | Trabajo / Dev | 6 | **test(pos): validar recuperacion del outbox tras caida**. Prueba cobro y recuperación del outbox con PostgreSQL real y productor controlado; no cae Kafka real. |
| 31 | [0ca5d0f](https://github.com/rpedrazav/TallerIntegracion3/commit/0ca5d0f64e785660b1e288df0d42f7f9da2c8979) | 2026-10-16 14:15 | nmartinez | Trabajo / Dev | 2 | **docs(payments): definir escenarios del simulador**. Documenta escenarios y contrato del simulador; es definición, no nueva ejecución de autorizaciones. |
| 32 | [866e5b1](https://github.com/rpedrazav/TallerIntegracion3/commit/866e5b156356706f02d5ceaadedcd9965c9b7ed5) | 2026-10-16 14:30 | nmartinez | Trabajo / Dev | 11 | **feat(pos): agregar adaptador HTTP de pasarela**. Implementa adaptador HTTP y DTOs de referencias opacas; registra cliente y prueba payload/correlación. |
| 33 | [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) | 2026-10-16 21:10 | Diego Leiva Caballero | Trabajo / Dev | 27 | **TI3-657 (S2-026): Inventariar rutas y puertos reales.**. Agrupa S2-026/027/028: inventario de rutas, alias compatibles con Kong y configuración central de destinos API. |
| 34 | [512f2b7](https://github.com/rpedrazav/TallerIntegracion3/commit/512f2b71419c11dc4925120245a78a8e16d95637) | 2026-10-16 21:32 | Diego Leiva Caballero | Trabajo / Dev | 13 | **TI3-660 : Añadir TypeScript al gate CI**. Agrupa S2-029/030/056: typecheck CI, arranque y configuración de pasarela. No demuestra reglas de protección de rama. |
| 35 | [1ca8853](https://github.com/rpedrazav/TallerIntegracion3/commit/1ca8853ea62c0c1fccf1b95b886d6996e74d0166) | 2026-10-16 21:54 | Diego Leiva Caballero | Trabajo / Dev | 15 | **TI3-688: corporar simulador al entorno local**. Agrupa S2-057/058: proceso simulador, Compose/CI y rutas Kong; los cambios de semántica posteriores pertenecen a otros commits. |
| 36 | [847ef12](https://github.com/rpedrazav/TallerIntegracion3/commit/847ef12c065895f6cbccd9b86dfb4f8fbbf90c94) | 2026-10-17 11:31 | SPereda2005 | Trabajo / Dev | 4 | **Definir contrato de pasarela simulada TI3-662**. Añade pruebas del contrato de pasarela y documentación; IPaymentGatewayClient/DTOs ya proceden de 866e5b1. |
| 37 | [60af3d6](https://github.com/rpedrazav/TallerIntegracion3/commit/60af3d652813fd039f301938d3b616c700485318) | 2026-10-17 12:23 | nmartinez | Trabajo / Dev | 6 | **feat(payments): completar simulador de autorizaciones**. Completa escenarios e idempotencia del simulador y pruebas con proceso Node; estado del simulador en memoria. |
| 38 | [ce97fe3](https://github.com/rpedrazav/TallerIntegracion3/commit/ce97fe3683cc86096a3a2abc8bdd2e630af1c92d) | 2026-10-17 14:02 | SPereda2005 | Trabajo / Dev | 10 | **Agregar persistencia de intento de tarjeta (TI3-663)**. Agrega IntentoPago, migración e índice tenant/clave. Los cuatro tests inspeccionan modelo/contrato; no acreditan por sí solos cobros concurrentes. |
| 39 | [d8851f9](https://github.com/rpedrazav/TallerIntegracion3/commit/d8851f9fe8f40e5514dd6689ac30a35d9eed8f62) | 2026-10-17 14:02 | SPereda2005 | Merge / Dev | 6 | **Merge remote-tracking branch 'origin/dev' into dev**. Merge de la evolución del simulador; diferencia respecto al primer padre no constituye autoría exclusiva. |
| 40 | [1060d53](https://github.com/rpedrazav/TallerIntegracion3/commit/1060d5308c131d7ffe9e73bcd71a497bfda74101) | 2026-10-17 14:04 | nmartinez | Trabajo / Dev | 14 | **feat(payments): evitar reintento inseguro tras timeout**. Consulta el mismo intento tras timeout, añade estado incierto y ruta GET; evita reautorizar automáticamente. |
| 41 | [4a20547](https://github.com/rpedrazav/TallerIntegracion3/commit/4a20547e4261ba810385936c4ca3f5b2b115fe2b) | 2026-10-17 16:32 | Daniel Prado | Trabajo / Dev | 4 | **Definir matriz RN-02 de pagos**. Matriz RN-02 con resultados, efectos y límites; diferencia aceptación documental de cierre E2E. |
| 42 | [31196e3](https://github.com/rpedrazav/TallerIntegracion3/commit/31196e362a866e20af316b820c0f15af1da28e2f) | 2026-10-17 16:47 | SPereda2005 | Trabajo / Dev | 6 | **Crear endpoint cobrar-tarjeta (TI3-664)**. Expone cobrar-tarjeta con guardias y DTOs; la finalización durable completa llega en pasos posteriores. |
| 43 | [186f8d5](https://github.com/rpedrazav/TallerIntegracion3/commit/186f8d583d6a2a0e6d049784fea3885203f3c305) | 2026-10-17 17:10 | SPereda2005 | Trabajo / Dev | 9 | **Persistir aprobacion y rechazo de cobro con tarjeta (TI3-665)**. Integra aprobación/rechazo con persistencia de venta, pago, intento y outbox; amplía pruebas de tarjeta. |
| 44 | [72c1355](https://github.com/rpedrazav/TallerIntegracion3/commit/72c135576af49ffde6f244bc8eba6236527f77c6) | 2026-10-17 21:17 | martinxoo | Trabajo / Dev | 7 | **TI3-673**. Implementa edición de usuario en UI/helper/modal y errores; no sustituye backend de edición preexistente. |
| 45 | [97dc4e7](https://github.com/rpedrazav/TallerIntegracion3/commit/97dc4e768a7fee4ab18133bf962d9d004880da17) | 2026-10-18 00:00 | martinxoo | Trabajo / Dev | 6 | **TI3-674 Implementar desactivación de usuario**. Baja lógica en interfaz con confirmación y estado inactivo; documenta dependencia del listado backend. |
| 46 | [e56597d](https://github.com/rpedrazav/TallerIntegracion3/commit/e56597d569c908301eb6f97274d2ffc89ef36acf) | 2026-10-18 00:15 | martinxoo | Trabajo / Dev | 3 | **TI3-675 Ajustar configuración y rol activo**. Unifica AdminRoute y protege productos; mejora feedback CAJERO sin atribuir autorización al frontend. |
| 47 | [ac583e8](https://github.com/rpedrazav/TallerIntegracion3/commit/ac583e8b6d50ef4b1133e21c87d4a5613a95b488) | 2026-10-18 01:14 | nmartinez | Trabajo / Dev | 13 | **test(payments): validar secuencia de pago y outbox**. Prueba secuencia autorización/pago/evento y agrega referencia de pasarela en Pago con migración. |
| 48 | [32d1fb5](https://github.com/rpedrazav/TallerIntegracion3/commit/32d1fb5a69a4296941fde148642d0dad85326013) | 2026-10-18 12:06 | iignvcioo | Trabajo / Dev | 5 | **definir UX del modal de cobro**. Modal y estados visuales de cobro sin captura de tarjeta; integración funcional llega después. |
| 49 | [12a8c4b](https://github.com/rpedrazav/TallerIntegracion3/commit/12a8c4bb50ed05e56b599f0569fe14210225ba29) | 2026-10-18 12:15 | iignvcioo | Trabajo / Dev | 8 | **conectar cobro en efectivo con MS-5**. Conecta efectivo, vuelto y monto insuficiente; pruebas UI con HTTP controlado. |
| 50 | [1331133](https://github.com/rpedrazav/TallerIntegracion3/commit/1331133587ec9818ab5bc85cb17934cb31ceee56) | 2026-10-18 12:41 | iignvcioo | Trabajo / Dev | 3 | **Arreglo cluster**. Actualiza reglas de Ingress y documentación; configuración no equivale por sí sola a validación remota completa. |
| 51 | [d9f4f55](https://github.com/rpedrazav/TallerIntegracion3/commit/d9f4f55cd922734f5d422104335b278235100c93) | 2026-10-18 12:56 | iignvcioo | Trabajo / Dev | 17 | **Conectar cobro con tarjeta y corregir búsqueda de productos en POS**. Conecta tarjeta y búsqueda, agrega cambio de método de venta pendiente y ajuste de catálogo; pruebas dirigidas. |
| 52 | [f6727db](https://github.com/rpedrazav/TallerIntegracion3/commit/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d) | 2026-10-18 13:10 | iignvcioo | Trabajo / Dev | 16 | **Manejar timeout de tarjeta sin doble cobro**. Reserva durable y recuperación del intento mediante GET, bloqueo de nueva autorización incierta y recuperación UI. |
| 53 | [212cc56](https://github.com/rpedrazav/TallerIntegracion3/commit/212cc56b8ffe0c44dc07650a40b01ebc2996f215) | 2026-10-18 20:38 | SPereda2005 | Trabajo / Dev | 16 | **Probar e implementar cobro seguro e idempotente S2-035 (TI3-666)**. Refuerza reserva previa con bloqueo por venta, clave estricta y pruebas concurrentes/rechazo/timeout. |
| 54 | [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) | 2026-10-18 20:50 | SPereda2005 | Merge / Dev | 43 | **Merge origin/dev: integrar cobro seguro con recuperacion de pagos**. Merge semántico de reserva segura y recuperación; reporte 54/54 backend y 7/7 UI controlada. No duplica autoría de cambios entrantes. |
| 55 | [4d9fd64](https://github.com/rpedrazav/TallerIntegracion3/commit/4d9fd64e31ea6a7da89e1ac969358837a520ad0d) | 2026-10-18 20:57 | Daniel Prado | Trabajo / Rama | 1 | **prueba**. Solo añade líneas vacías y un encabezado vacío a la matriz RN-02; no agrega prueba ni comportamiento pese al título «prueba». |
| 56 | [fac1511](https://github.com/rpedrazav/TallerIntegracion3/commit/fac151121f8fbc3c9a5a43417a9183242ffbbdb5) | 2026-10-18 21:32 | Daniel Prado | Trabajo / Rama | 13 | **Configuracion de E2E para Electron para CI local**. Entorno E2E Electron local con APIs controladas, selección de ventana file y perfil aislado; ejecución CI remota no verificada. |
| 57 | [5dbc672](https://github.com/rpedrazav/TallerIntegracion3/commit/5dbc672881ba388a5e308d706ed3a6e2d7c8c323) | 2026-10-18 21:37 | Daniel Prado | Trabajo / Rama | 2 | **Configuracion de Ci local para Github Actions**. Ajusta workflow Electron y documentación para ejecución local/autohospedada; no demuestra un run remoto exitoso. |
| 58 | [3eb4cb4](https://github.com/rpedrazav/TallerIntegracion3/commit/3eb4cb47955ee8976aaf1c108a95bfcab00bc875) | 2026-10-19 00:57 | martinxoo | Trabajo / Rama | 7 | **TI3-676 Probar flujos Admin en interfaz**. Pruebas Admin de alta/edición/baja/configuración con fixtures HTTP y feedback; rama no integrada, distinta de admin.spec final. |
| 59 | [3148c9a](https://github.com/rpedrazav/TallerIntegracion3/commit/3148c9aa4a19b7a5448bb85107a41c1c060eb37b) | 2026-10-19 12:30 | Daniel Prado | Trabajo / Rama | 20 | **Automatizar flujo E2E tarjeta**. E2E Electron con MS-5/PG/simulador real y dependencias identidad/catálogo/IVA controladas; outbox sin Kafka; rama no integrada. |
| 60 | [25a40f5](https://github.com/rpedrazav/TallerIntegracion3/commit/25a40f52648694c51c2ffa429c065349c3046202) | 2026-10-19 12:44 | Daniel Prado | Trabajo / Dev | 7 | **Probar rechazo y timeout por HTTP**. Dos pruebas HTTP de rechazo/timeout y script; reporte distingue éxito local de bloqueo remoto al crear venta. |
| 61 | [abfd1f0](https://github.com/rpedrazav/TallerIntegracion3/commit/abfd1f0e1050027701674889775d2f28c19e780a) | 2026-10-19 13:44 | Daniel Prado | Trabajo / Dev | 9 | **Probar autorización e idempotencia**. Prueba duplicado aprobado y pendiente y refuerza repetición directa del simulador; registra 3/3 y límites. |
| 62 | [e61d618](https://github.com/rpedrazav/TallerIntegracion3/commit/e61d618cdaa1a9eb7074c94d97ecca3de0c8b148) | 2026-10-23 20:44 | SPereda2005 | Trabajo / Dev | 6 | **Definir contrato de peso variable TI3-692**. Define kg, mínimo/resolución y relación cantidad/peso; unidad UUID real de catálogo pendiente. |
| 63 | [e5141df](https://github.com/rpedrazav/TallerIntegracion3/commit/e5141df2c4a485c2fb34aa2ef22c7df4d51fdc29) | 2026-10-24 11:19 | SPereda2005 | Trabajo / Dev | 8 | **Validar peso en agregar ítem. TI3-693**. Valida agregado pesable antes de impuestos/persistencia y prueba fronteras; no cubre aún todos los caminos. |
| 64 | [ef6a406](https://github.com/rpedrazav/TallerIntegracion3/commit/ef6a40632bc975c961d289a6d41c7a67cc85d56d) | 2026-10-24 12:00 | SPereda2005 | Trabajo / Dev | 16 | **Calcular total pesable en servidor. TI3-694**. Conserva cuatro decimales de precio, redondea subtotal/IVA por línea y agrega pruebas de cálculo. |
| 65 | [bbc5b5f](https://github.com/rpedrazav/TallerIntegracion3/commit/bbc5b5f5df03fec2af2a65e75d11bc6dfc01b291) | 2026-10-24 14:31 | Daniel Prado | Trabajo / Dev | 5 | **probar el aislamento de pago**. Agrega aislamiento del pago S2-054 con evidencia propia y propiedades de build de prueba. |
| 66 | [8c20fee](https://github.com/rpedrazav/TallerIntegracion3/commit/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98) | 2026-10-24 16:17 | Daniel Prado | Trabajo / Rama | 4 | **Preparar protocolo de prueba física**. Protocolo/captura serial física condicionados a equipo y manual; no acredita lectura física y está fuera de dev. |
| 67 | [15dc321](https://github.com/rpedrazav/TallerIntegracion3/commit/15dc32170a5b8cb8a2e006fd7f9f552b0b4e9251) | 2026-10-24 18:07 | SPereda2005 | Trabajo / Dev | 11 | **Actualizar cantidad pesable con recálculo. TI3-695**. Edita peso con reglas y precio snapshot, recálculo fiscal y pruebas de ausencia de mutación ante error. |
| 68 | [7c6145f](https://github.com/rpedrazav/TallerIntegracion3/commit/7c6145fc9abff02e1da50d989db34d78a2fa23d0) | 2026-10-24 21:18 | nmartinez | Trabajo / Dev | 6 | **Definir interfaz de balanza. S2-066**. Define ScaleAdapter/ScaleRendererApi y resultados discriminados; contrato sin driver físico. |
| 69 | [33891a1](https://github.com/rpedrazav/TallerIntegracion3/commit/33891a1510182848eb9f99b1d02f695f669598b1) | 2026-10-24 21:24 | nmartinez | Trabajo / Dev | 8 | **Implementar adaptador de balanza simulada. S2-067**. Simulador de balanza determinista y fábrica, normalización/estabilidad/errores con pruebas. |
| 70 | [0ff7e00](https://github.com/rpedrazav/TallerIntegracion3/commit/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a) | 2026-10-25 02:31 | martinxoo | Trabajo / Dev | 9 | **TI3-702 Definir contrato del terminal de pago**. TerminalPort, máquina de estados y adaptador MS-5; cancelación explícitamente no soportada. |
| 71 | [71847b1](https://github.com/rpedrazav/TallerIntegracion3/commit/71847b18c559111107436474461d18542688b7dc) | 2026-10-25 12:25 | nmartinez | Trabajo / Dev | 9 | **Exponer IPC mínimo de balanza. S2-068**. Cuatro canales IPC de balanza, validación acotada y contextBridge; evita configuración serial desde renderer. |
| 72 | [0afc855](https://github.com/rpedrazav/TallerIntegracion3/commit/0afc855588582f8294644160dff4f418f40b2683) | 2026-10-25 12:33 | nmartinez | Trabajo / Dev | 10 | **Preparar adaptador serial intercambiable. S2-069**. SerialScaleAdapter con transporte/parser inyectados y pruebas; sin protocolo físico ni biblioteca serial concretos. |
| 73 | [352fe7f](https://github.com/rpedrazav/TallerIntegracion3/commit/352fe7fef4b5401ea6a2d7bb449c427c3616d91c) | 2026-10-25 14:31 | nmartinez | Trabajo / Dev | 11 | **Probar IPC y estados simulados. S2-070**. Integra balanza main/preload con filtro de carrito y prueba de estados/corrupción/reconexión simulados. |
| 74 | [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) | 2026-10-25 14:37 | Diego Leiva Caballero | Trabajo / Dev | 26 | **feat(MS-5, frontend): implementar simuladores, logs de correlacion y seguridad IPC (S2-059..S2-089)**. Agrupa correlación, smoke con pasarela doblada, DeviceManager y seguridad IPC; RealTerminalAdapter es reserva sin soporte físico. |
| 75 | [22a5b50](https://github.com/rpedrazav/TallerIntegracion3/commit/22a5b50ab756fc0a9de9ec11d791d00485f8cd77) | 2026-10-25 14:42 | Diego Leiva Caballero | Merge / Dev | 8 | **Merge branch 'dev' of https://github.com/rpedrazav/TallerIntegracion3 into dev**. Merge de cambios de peso/IPC/carrito: revisar resolución; no atribuir todos los archivos entrantes a Diego. |
| 76 | [078bf67](https://github.com/rpedrazav/TallerIntegracion3/commit/078bf67c9d7b4be8fe94498d2042e44799b1e187) | 2026-10-25 15:17 | Daniel Prado | Merge / Rama | 81 | **Merge remote-tracking branch 'origin/dev' into local**. Merge hacia rama local con conflictos de grafo declarados; incorpora cambios compartidos y no está en dev. |
| 77 | [ce542e9](https://github.com/rpedrazav/TallerIntegracion3/commit/ce542e946fc6da1d4f5c11b91acc56d22e141367) | 2026-10-25 18:14 | Daniel Prado | Trabajo / Rama | 3 | **Separar pruebas simuladas y físicas**. Plan de validación simulada/física y tres tests documentales; no ejecuta balanza ni POS. |
| 78 | [5850d61](https://github.com/rpedrazav/TallerIntegracion3/commit/5850d6111d0c5e9086bf6d156cc89fd7c9777dab) | 2026-10-25 19:25 | Daniel Prado | Trabajo / Rama | 8 | **Probar parser y redondeo de peso. Se corrigieron errores del reacho a infinity y de unidades distintas.**. Pruebas parser/redondeo y correcciones runtime de Infinity/unidad; cambios exclusivos de rama, pendientes de incorporar. |
| 79 | [ed50883](https://github.com/rpedrazav/TallerIntegracion3/commit/ed508832009bce8ffd40f13dacc64a3baa40ef73) | 2026-10-26 01:50 | martinxoo | Trabajo / Dev | 10 | **TI3-703 Crear simulador de terminal POS**. SimulatedTerminalAdapter y fábrica mock/real, escenarios deterministas y pruebas del contrato. |
| 80 | [96dc728](https://github.com/rpedrazav/TallerIntegracion3/commit/96dc728f1adf215086f49f01f6271f38a43d9257) | 2026-10-26 02:19 | martinxoo | Trabajo / Dev | 9 | **TI3-704 Exponer IPC limitado del terminal**. IPC limitado del terminal con validación de payload y resultados proyectados; instancia simulada persistente en main. |
| 81 | [7f85701](https://github.com/rpedrazav/TallerIntegracion3/commit/7f85701bac4c9f94c95bd229410574ec37d2734f) | 2026-10-26 11:31 | Daniel Prado | Trabajo / Rama | 4 | **Probar cálculo pesable entre servicios**. Prueba cálculo MS-3/MS-5 HTTP + calculador MS-2 y renderer parcial; 14 casos documentados, sin Electron ni HTTP MS-2. |
| 82 | [f787762](https://github.com/rpedrazav/TallerIntegracion3/commit/f787762c367100e6d7f5d85815d59e697f5330c6) | 2026-10-26 11:42 | Daniel Prado | Trabajo / Rama | 3 | **Probar caída de balanza y modo manual**. Pruebas de caída/reconexión y guardia de aceptación manual incompleta usando callback extraído; no es E2E de UI y no está integrado. |
| 83 | [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) | 2026-10-26 15:31 | SPereda2005 | Trabajo / Dev | 11 | **Probar fronteras de peso y tenant. TI3-696**. Encuentra creación sin peso, centraliza ReglasCantidadItem y prueba fronteras/tenant; reporte 84/84 local. |
| 84 | [9209029](https://github.com/rpedrazav/TallerIntegracion3/commit/9209029a02bd1de391065efd74f7d3bc482d7007) | 2026-10-26 15:44 | SPereda2005 | Merge / Dev | 52 | **Merge remote-tracking branch 'origin/dev' into dev**. Merge de hardware/terminal/observabilidad sobre backend; delta incluye trabajo previo de varios autores. |
| 85 | [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) | 2026-10-26 22:50 | martinxoo | Trabajo / Dev | 6 | **TI3-705 Sincronizar estado con la pasarela**. Sincroniza terminal preliminar con confirmación MS-5 y misma clave; verifica correlación y reconciliación. |
| 86 | [dfa373e](https://github.com/rpedrazav/TallerIntegracion3/commit/dfa373e283140697936c707c9b961bd89a2c9448) | 2026-10-26 23:25 | martinxoo | Merge / Rama | 27 | **Merge branch 'dev' into local**. Merge de dev hacia local con integración de terminal/peso; no pertenece al corte dev analizado. |
| 87 | [de5bf27](https://github.com/rpedrazav/TallerIntegracion3/commit/de5bf2760b1ebfd5613f6483cb594ee3bb02f461) | 2026-10-26 23:47 | martinxoo | Trabajo / Dev | 7 | **Agregar escenario timeout al terminal simulado**. Agrega timeout determinista con transición processing→uncertain y conservación de referencia. |
| 88 | [11abad6](https://github.com/rpedrazav/TallerIntegracion3/commit/11abad6660d1e713b01044d55ea9b6ea90a61fb0) | 2026-10-27 00:26 | martinxoo | Merge / Rama | 7 | **Merge branch 'dev' into local**. Merge hacia local de la evolución de timeout; no nueva implementación independiente del escenario. |
| 89 | [9b7cb4b](https://github.com/rpedrazav/TallerIntegracion3/commit/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1) | 2026-10-27 00:36 | martinxoo | Trabajo / Rama | 8 | **Probar estados del terminal simulado**. Amplía pruebas terminal/IPC/sincronización y helpers single-flight; contribución de rama no integrada. |
| 90 | [4f3c604](https://github.com/rpedrazav/TallerIntegracion3/commit/4f3c60432d01e7146f3957ac79f48d9b07654dc9) | 2026-10-27 09:49 | iignvcioo | Trabajo / Dev | 4 | **S2-076 Diseñar estado de captura de peso**. Panel PesoProducto y estado de captura con origen/unidad y tratamiento distinto para unitarios. |
| 91 | [39b5b2a](https://github.com/rpedrazav/TallerIntegracion3/commit/39b5b2a08291ea2cf299e1a427a3165180ee3cb4) | 2026-10-27 09:49 | iignvcioo | Trabajo / Dev | 3 | **S2-077 Conectar lectura simulada al carrito**. Separa lectura de confirmación; cancelar/error no muta carrito y confirmar agrega/actualiza peso. |
| 92 | [755ba27](https://github.com/rpedrazav/TallerIntegracion3/commit/755ba275391164c675cbc95aaed0eb1fb9c83dbd) | 2026-10-27 09:50 | iignvcioo | Trabajo / Dev | 5 | **S2-078 Implementar ingreso manual de respaldo**. Respaldo manual identificado y validado con mínimo/resolución; no representa hardware físico. |
| 93 | [00fbb39](https://github.com/rpedrazav/TallerIntegracion3/commit/00fbb3981562a122d76fd6c61e2e3104c2f1bb42) | 2026-10-27 09:50 | iignvcioo | Trabajo / Dev | 4 | **S2-079 Mostrar precio e IVA del servidor**. Presenta kg/precio por kg y subtotal estimado, reservando impuestos y total a MS-5. |
| 94 | [4bde899](https://github.com/rpedrazav/TallerIntegracion3/commit/4bde899fce12fc6d005100d7585ec27cfbda8024) | 2026-10-27 09:54 | iignvcioo | Trabajo / Dev | 3 | **S2-080 Probar UX de balanza y respaldo**. Tres casos de UI pesable con HTTP controlado: confirmación, respaldo y unitario; no prueba periférico. |
| 95 | [bfd46b0](https://github.com/rpedrazav/TallerIntegracion3/commit/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1) | 2026-10-27 10:15 | iignvcioo | Trabajo / Dev | 30 | **feat(admin): habilitar gestión de tenant y panel administrativo**. Bootstrap ADMIN, unificación Usuarios, navegación administrativa e inventario validado por sucursal; preparación MS-4. |
| 96 | [dbd2b09](https://github.com/rpedrazav/TallerIntegracion3/commit/dbd2b096d545cd5476821dd3a2baa4f420db2eca) | 2026-10-27 22:16 | iignvcioo | Trabajo / Dev | 18 | **feat(admin): desplegar inventario y verificar flujos**. Despliegue MS-4 con base lógica separada en instancia compartida por cuota, Kafka apagado; paginación y pruebas Admin/stock. |
| 97 | [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) | 2026-10-27 22:31 | iignvcioo | Trabajo / Dev | 19 | **feat(admin): gestionar roles propios por tenant**. Roles propios por tenant con base heredada, migración/JWT/UI y tres pruebas backend nuevas; cierre reporta 334/334 y 5 E2E. |

### Atribución de merges y ramas

Los cinco merges integrados son de Sebastián (cuatro) y Diego (uno). Los tres restantes son de Daniel (uno) y Martín (dos), en ramas no integradas. Los cambios respecto al primer padre se inventarían completos, pero se atribuyen al autor original cuando el historial lo permite. `1ba6b37` tiene además un reporte específico de resolución semántica.

Fuera de dev quedan diez commits de Daniel y cuatro de Martín, incluidos esos tres merges. Los dos stashes son un grupo aparte. La revisión no modifica, fusiona ni descarta ninguna de esas ramas.


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


## 4. Banco de 60 preguntas — Grupal

**Registro:** 60 preguntas; 20 básicas, 25 intermedias, 15 avanzadas. Numeración única en este banco.

Las respuestas describen el código o la revisión enlazada. Los resultados de ejecución son históricos. “Rama, no integrado” identifica evidencia que no forma parte de dev al corte. Las experiencias personales deben ser confirmadas por el integrante.


### Objetivos, planificación y alcance del sprint

<a id="g-01"></a>

**Pregunta N.º:** 1 (G-01)

**Categoría:** Objetivos, planificación y alcance del sprint

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cuáles fueron las cinco metas del Sprint 2 y qué problema concreto resolvía cada una?

**Respuesta modelo:** El plan revisado agrupó administración, carrito y venta funcional, tarjeta por pasarela simulada, balanza mediante adaptador y respaldo manual, y terminal POS simulado. Agregó salud técnica para rutas, CI, pruebas y documentación. Buscaba cerrar un flujo de caja coherente, incluyendo precio e IVA del servidor, recuperación de pagos y persistencia, sin presentar simulación como hardware validado.

**Explicación para estudiar:** Una meta describe un resultado observable; una tarea entrega una parte. Poder abrir una pantalla no demuestra que su meta completa esté aceptada.

**Evidencia del repositorio:** [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) (integrado en dev): [tareas/plan_sprint2_revisado.xlsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/plan_sprint2_revisado.xlsx).

**Posible repregunta:** ¿Qué condición de G2 todavía impide declarar cerrado el flujo completo?

---

<a id="g-02"></a>

**Pregunta N.º:** 2 (G-02)

**Categoría:** Objetivos, planificación y alcance del sprint

**Dificultad:** Básica.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Cómo calcularon las 120 horas de capacidad y qué significa ese número para la defensa?

**Respuesta modelo:** La planilla usa seis personas, cinco horas semanales y cuatro semanas: 120 horas. Distribuye 15 a administración, 30 a carrito, 25 a tarjeta, 20 a balanza, 15 a terminal y 15 a salud técnica. Son estimaciones de planificación; los commits no prueban horas efectivamente trabajadas. Para explicar desviaciones habría que confirmar el registro personal de dedicación.

**Explicación para estudiar:** Capacidad planificada y esfuerzo real son medidas distintas. Ni el número de commits ni las líneas modificadas permiten convertir directamente actividad en horas.

**Evidencia del repositorio:** [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) (integrado en dev): [tareas/plan_sprint2_revisado.xlsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/plan_sprint2_revisado.xlsx).

**Posible repregunta:** ¿Cómo justificarían una tarea que consumió más que su estimación?

---

<a id="g-03"></a>

**Pregunta N.º:** 3 (G-03)

**Categoría:** Objetivos, planificación y alcance del sprint

**Dificultad:** Básica.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Por qué estudiamos este trabajo como Sprint 2 si el grafo todavía dice Sprint 1?

**Respuesta modelo:** El usuario confirmó Sprint 2 y el commit de planificación f913592 incorpora el plan revisado con IDs S2. El nodo sprint-actual conserva una revisión de septiembre y el rótulo Sprint 1. Para esta defensa tomamos el trabajo desde esa planificación hasta f0e6f31 y también las ramas del período, distinguiendo lo integrado. El rótulo antiguo es una inconsistencia documental.

**Explicación para estudiar:** La fecha y la procedencia de una fuente importan. Un documento titulado actual puede quedar desfasado respecto del historial y del código.

**Evidencia del repositorio:** [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) (integrado en dev): [tareas/plan_sprint2_revisado.xlsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/plan_sprint2_revisado.xlsx); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [docs/context/planificacion/sprint-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/docs/context/planificacion/sprint-actual.md).

**Posible repregunta:** ¿Qué fuente utilizarían si una tabla contradice el controlador vigente?

---

<a id="g-04"></a>

**Pregunta N.º:** 4 (G-04)

**Categoría:** Objetivos, planificación y alcance del sprint

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué significaba implementar balanza y terminal dentro de las restricciones académicas?

**Respuesta modelo:** Significaba desarrollar contratos, simuladores, estados de error e integración con el POS sin requerir una pasarela bancaria ni un periférico disponible. El plan condiciona las pruebas físicas a modelo, protocolo y equipo. Por eso podemos demostrar conversión de peso y aprobación simulada, pero la compatibilidad con un dispositivo comercial debe declararse [NO VERIFICADO].

**Explicación para estudiar:** Un simulador verifica la respuesta del software a entradas controladas. No reproduce por sí solo ruido eléctrico, firmware, drivers ni certificación del proveedor.

**Evidencia del repositorio:** [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) (integrado en dev): [tareas/plan_sprint2_revisado.xlsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/plan_sprint2_revisado.xlsx); [0afc855](https://github.com/rpedrazav/TallerIntegracion3/commit/0afc855588582f8294644160dff4f418f40b2683) (integrado en dev): [globalmart-frontend/src/hardware/serial-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/src/hardware/serial-scale-adapter.ts).

**Posible repregunta:** ¿Qué datos necesitarían antes de implementar un parser para una balanza real?

---

<a id="g-05"></a>

**Pregunta N.º:** 5 (G-05)

**Categoría:** Objetivos, planificación y alcance del sprint

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué deudas iniciales motivaron priorizar carrito, precio e IVA antes de nuevas funciones ERP?

**Respuesta modelo:** El plan identifica creación que aceptaba precios del cliente e IVA inicial cero, además de un carrito que aún no garantizaba persistencia autoritativa. S2-002 consulta catálogo y S2-003 calcula impuestos desde la primera creación. Resolver eso protege el monto de la venta y permite que tarjeta, outbox y peso trabajen sobre una base consistente.

**Explicación para estudiar:** Una dependencia funcional debe estabilizarse antes de agregar consumidores. Un pago seguro sobre un total manipulable seguiría cobrando un importe incorrecto.

**Evidencia del repositorio:** [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) (integrado en dev): [tareas/plan_sprint2_revisado.xlsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/plan_sprint2_revisado.xlsx); [152f39b](https://github.com/rpedrazav/TallerIntegracion3/commit/152f39b00076de93573a261f9721ad1c29eb3c5c) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Controllers/VentasController.cs), [tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs); [1833981](https://github.com/rpedrazav/TallerIntegracion3/commit/18339813f950b4becfffcef7d046e7907172dff0) (integrado en dev): [tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/18339813f950b4becfffcef7d046e7907172dff0/tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs).

**Posible repregunta:** ¿Qué prueba comprueba que un precio falsificado no llega al pago?

---

<a id="g-06"></a>

**Pregunta N.º:** 6 (G-06)

**Categoría:** Objetivos, planificación y alcance del sprint

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué funcionalidades del ERP se dejaron fuera y por qué no deberían presentarlas como logros de este sprint?

**Respuesta modelo:** La revisión del plan difiere FEFO completo, DTE, compras y analytics para mantener las cinco metas dentro de la capacidad. Existen modelos y proyectos de servicios futuros, pero eso no acredita endpoints ni flujos operativos. En la defensa debemos presentar esa estructura como base existente y limitar los logros a comportamiento respaldado por implementación y evidencia.

**Explicación para estudiar:** Un proyecto que compila puede contener solo entidades y configuración. El alcance funcional exige una entrada, reglas, persistencia y una salida verificable.

**Evidencia del repositorio:** [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) (integrado en dev): [tareas/plan_sprint2_revisado.xlsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/plan_sprint2_revisado.xlsx); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/docs/context/estado-actual.md).

**Posible repregunta:** ¿Cómo demostrarían que MS-6 tiene estructura pero no un flujo de compras terminado?

---

<a id="g-07"></a>

**Pregunta N.º:** 7 (G-07)

**Categoría:** Objetivos, planificación y alcance del sprint

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo se convirtió el plan en criterios de aceptación comprobables?

**Respuesta modelo:** Daniel agregó la matriz S2-021 con escenarios para las cinco metas y luego la matriz RN-02 para pagos. Esas matrices conectan resultado esperado, dependencias y tipo de prueba. Por ejemplo, rechazo de tarjeta exige conservar venta pendiente y no crear pago ni evento. La matriz orienta la validación; el reporte o prueba ejecutada es la evidencia de cumplimiento.

**Explicación para estudiar:** Un criterio debe permitir decidir aprobado o fallido con observaciones concretas. Escribirlo no equivale a ejecutarlo.

**Evidencia del repositorio:** [806450b](https://github.com/rpedrazav/TallerIntegracion3/commit/806450bec2bda6cee096bb62cbc02b204d5daae9) (integrado en dev): [diagramas/matriz/S2-021-matriz-aceptacion.md](https://github.com/rpedrazav/TallerIntegracion3/blob/806450bec2bda6cee096bb62cbc02b204d5daae9/diagramas/matriz/S2-021-matriz-aceptacion.md); [4a20547](https://github.com/rpedrazav/TallerIntegracion3/commit/4a20547e4261ba810385936c4ca3f5b2b115fe2b) (integrado en dev): [diagramas/matriz/S2-051-matriz-rn02-pagos.md](https://github.com/rpedrazav/TallerIntegracion3/blob/4a20547e4261ba810385936c4ca3f5b2b115fe2b/diagramas/matriz/S2-051-matriz-rn02-pagos.md).

**Posible repregunta:** ¿Qué observaciones necesitarían además del código HTTP 422?

---

<a id="g-08"></a>

**Pregunta N.º:** 8 (G-08)

**Categoría:** Objetivos, planificación y alcance del sprint

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo distinguirían una tarea asignada de una contribución realmente realizada?

**Respuesta modelo:** La asignación se obtiene de la planilla y el mapeo S2/YouTrack; la contribución se reconstruye con autor, diff, archivos y pruebas del commit. Hay trabajo compartido: Nicolás desarrolló el adaptador de pasarela y Sebastián añadió contrato, persistencia y endpoint. Por eso no atribuimos una funcionalidad entera a quien tiene el último commit ni damos por hecha una fila planificada.

**Explicación para estudiar:** Git registra cambios y autoría declarada, no reuniones ni toda la colaboración. La responsabilidad puede estar distribuida entre contrato, implementación, integración y verificación.

**Evidencia del repositorio:** [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) (integrado en dev): [tareas/sprint2_youtrack_resultado.json](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/sprint2_youtrack_resultado.json), [tareas/plan_sprint2_revisado.xlsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/plan_sprint2_revisado.xlsx); [866e5b1](https://github.com/rpedrazav/TallerIntegracion3/commit/866e5b156356706f02d5ceaadedcd9965c9b7ed5) (integrado en dev): [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/866e5b156356706f02d5ceaadedcd9965c9b7ed5/src/POSCartService/Services/PaymentGatewayClient.cs); [847ef12](https://github.com/rpedrazav/TallerIntegracion3/commit/847ef12c065895f6cbccd9b86dfb4f8fbbf90c94) (integrado en dev): [tests/GlobalMart.IntegrationTests/PaymentGatewayClientTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/847ef12c065895f6cbccd9b86dfb4f8fbbf90c94/tests/GlobalMart.IntegrationTests/PaymentGatewayClientTests.cs); [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md).

**Posible repregunta:** ¿Cómo atribuirían la recuperación de pagos entre Rodrigo y Sebastián?

---

<a id="g-09"></a>

**Pregunta N.º:** 9 (G-09)

**Categoría:** Objetivos, planificación y alcance del sprint

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Puede declararse cumplida G2 porque el cobro y el outbox funcionan?

**Respuesta modelo:** No con toda su condición de cierre. El plan incluye anulación total y un flujo hasta stock sin pérdida. En dev no se expone la anulación HTTP y el consumidor de inventario está desactivado en el clúster documentado. El outbox y sus pruebas son avances verificables, pero no sustituyen la aceptación del recorrido completo ni resuelven las limitaciones del consumidor.

**Explicación para estudiar:** La aceptación de una meta compuesta es la conjunción de sus condiciones. Aprobar una parte crítica no convierte automáticamente las restantes en completadas.

**Evidencia del repositorio:** [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) (integrado en dev): [tareas/plan_sprint2_revisado.xlsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/plan_sprint2_revisado.xlsx); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/POSCartService/Controllers/VentasController.cs), [src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs), [k8s/manifests/warehouse-inventory.yaml](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/k8s/manifests/warehouse-inventory.yaml).

**Posible repregunta:** ¿Qué prueba y qué cambio de implementación priorizarían para cerrar esa brecha?

---

<a id="g-10"></a>

**Pregunta N.º:** 10 (G-10)

**Categoría:** Objetivos, planificación y alcance del sprint

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo presentarían los commits de ramas no integradas al evaluar el cierre?

**Respuesta modelo:** Los reconoceríamos como contribuciones verificables, indicando su SHA y rama, pero separados del estado de dev. Por ejemplo, Daniel tiene un E2E Electron de tarjeta y Martín amplía pruebas del terminal en commits no ancestros de f0e6f31. Antes de afirmar que forman parte de la entrega integrada necesitamos incorporarlos, resolver conflictos y repetir la validación pertinente.

**Explicación para estudiar:** Un commit existente y un commit alcanzable desde la rama de entrega son hechos distintos. La relación de ancestros es más fiable que comparar solo fechas.

**Evidencia del repositorio:** [3148c9a](https://github.com/rpedrazav/TallerIntegracion3/commit/3148c9aa4a19b7a5448bb85107a41c1c060eb37b) (rama, no integrado en dev): [docs/qa/S2-055/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/3148c9aa4a19b7a5448bb85107a41c1c060eb37b/docs/qa/S2-055/reporte.md); [9b7cb4b](https://github.com/rpedrazav/TallerIntegracion3/commit/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1) (rama, no integrado en dev): [globalmart-frontend/tests/synchronized-card-payment.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1/globalmart-frontend/tests/synchronized-card-payment.test.cjs); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/docs/context/estado-actual.md).

**Posible repregunta:** ¿Qué comando permite verificar si 9b7cb4b pertenece a dev?

---


### Funcionalidades desarrolladas y funcionamiento del sistema

<a id="g-11"></a>

**Pregunta N.º:** 11 (G-11)

**Categoría:** Funcionalidades desarrolladas y funcionamiento del sistema

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cuál es el recorrido de una venta desde que entra el cajero al POS?

**Respuesta modelo:** La interfaz verifica el turno activo y permite iniciar una venta en MS-5. El backend obtiene tenant y cajero del JWT, sucursal del turno, producto y precio de MS-3 e impuestos de MS-2. Persiste la venta pendiente, devuelve identificadores y totales y permite editar ítems. El cobro confirmado crea pago y evento durable; no basta con agregar productos al borrador.

**Explicación para estudiar:** Borrador visual, venta pendiente y venta completada son estados diferentes. Cada transición debe tener una condición y una fuente de verdad.

**Evidencia del repositorio:** [ccf7bf3](https://github.com/rpedrazav/TallerIntegracion3/commit/ccf7bf330b2e9ae25d93d689d7926db9f81d0071) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/ccf7bf330b2e9ae25d93d689d7926db9f81d0071/globalmart-frontend/src/pages/Pos.tsx); [152f39b](https://github.com/rpedrazav/TallerIntegracion3/commit/152f39b00076de93573a261f9721ad1c29eb3c5c) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Controllers/VentasController.cs); [0602a93](https://github.com/rpedrazav/TallerIntegracion3/commit/0602a936469683c8a1f3409477c6c644aa9fe5c0) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0602a936469683c8a1f3409477c6c644aa9fe5c0/src/POSCartService/Repositories/VentaRepository.cs).

**Posible repregunta:** ¿Qué sucede si el cajero intenta crear una venta sin turno?

---

<a id="g-12"></a>

**Pregunta N.º:** 12 (G-12)

**Categoría:** Funcionalidades desarrolladas y funcionamiento del sistema

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo evita el servidor que un cliente compre un producto a un precio inventado?

**Respuesta modelo:** El cliente aporta producto y cantidad o peso. MS-5 consulta MS-3, toma su nombre y PrecioBase y solicita impuestos a MS-2; el precio recibido no se usa como autoridad. Los tests envían un precio falsificado y comprueban el snapshot persistido. Si un producto no puede resolverse, se rechaza la creación antes de guardar la venta.

**Explicación para estudiar:** Validar formato no basta para confiar en un importe. La autoridad del dato debe estar en el servicio dueño del catálogo y repetirse al construir la operación.

**Evidencia del repositorio:** [152f39b](https://github.com/rpedrazav/TallerIntegracion3/commit/152f39b00076de93573a261f9721ad1c29eb3c5c) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Controllers/VentasController.cs), [src/POSCartService/Services/VentaService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Services/VentaService.cs); [1833981](https://github.com/rpedrazav/TallerIntegracion3/commit/18339813f950b4becfffcef7d046e7907172dff0) (integrado en dev): [tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/18339813f950b4becfffcef7d046e7907172dff0/tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs).

**Posible repregunta:** ¿Qué ocurre con el nombre histórico cuando cambia el catálogo?

---

<a id="g-13"></a>

**Pregunta N.º:** 13 (G-13)

**Categoría:** Funcionalidades desarrolladas y funcionamiento del sistema

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo funcionan el cobro en efectivo y el vuelto?

**Respuesta modelo:** El endpoint de efectivo exige una venta compatible con EFECTIVO y un monto recibido al menos igual al total. Calcula el vuelto como recibido menos total. Venta completada, pago y outbox se guardan conjuntamente. El modal muestra el resultado de MS-5 y conserva la venta si falta dinero; TARJETA y MIXTO se rechazan en este endpoint.

**Explicación para estudiar:** Validar tanto en controlador como en servicio protege entradas HTTP y otros llamadores internos. El vuelto es resultado del servidor, no una confirmación visual.

**Evidencia del repositorio:** [c86909c](https://github.com/rpedrazav/TallerIntegracion3/commit/c86909ca3c944da0c185da90a66e3f2be1881197) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/c86909ca3c944da0c185da90a66e3f2be1881197/src/POSCartService/Controllers/VentasController.cs), [src/POSCartService/Services/VentaService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/c86909ca3c944da0c185da90a66e3f2be1881197/src/POSCartService/Services/VentaService.cs), [tests/GlobalMart.IntegrationTests/CobroIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/c86909ca3c944da0c185da90a66e3f2be1881197/tests/GlobalMart.IntegrationTests/CobroIntegrationTests.cs); [12a8c4b](https://github.com/rpedrazav/TallerIntegracion3/commit/12a8c4bb50ed05e56b599f0569fe14210225ba29) (integrado en dev): [globalmart-frontend/e2e/cash-payment.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/12a8c4bb50ed05e56b599f0569fe14210225ba29/globalmart-frontend/e2e/cash-payment.spec.ts).

**Posible repregunta:** ¿Por qué un método MIXTO no implica que el pago dividido esté implementado?

---

<a id="g-14"></a>

**Pregunta N.º:** 14 (G-14)

**Categoría:** Funcionalidades desarrolladas y funcionamiento del sistema

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué hace una baja de producto y qué pasa con ventas anteriores?

**Respuesta modelo:** DELETE marca IsActive=false y conserva la fila. Requiere ADMIN y tenant correcto; repetir la baja devuelve 204. La consulta activa usada por POS deja de ofrecer el producto, pero los ítems históricos conservan nombre, precio y cantidad como snapshot. La prueba de Martín comprueba que una venta completada sigue consultable después de desactivar su producto.

**Explicación para estudiar:** Baja lógica y eliminación física tienen efectos distintos sobre trazabilidad. La venta histórica debe representar lo vendido entonces, no el estado actual del catálogo.

**Evidencia del repositorio:** [cb7b8df](https://github.com/rpedrazav/TallerIntegracion3/commit/cb7b8df333e309d4e8e93e87498aa10e54d02c10) (integrado en dev): [src/CatalogPricingService/Controllers/ProductoController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/cb7b8df333e309d4e8e93e87498aa10e54d02c10/src/CatalogPricingService/Controllers/ProductoController.cs), [tests/GlobalMart.IntegrationTests/DesactivarProductoIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/cb7b8df333e309d4e8e93e87498aa10e54d02c10/tests/GlobalMart.IntegrationTests/DesactivarProductoIntegrationTests.cs); [4bb39aa](https://github.com/rpedrazav/TallerIntegracion3/commit/4bb39aa947e6e7586d437b8ab490c1a99c03f02e) (integrado en dev): [src/CatalogPricingService/Data/ProductoRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4bb39aa947e6e7586d437b8ab490c1a99c03f02e/src/CatalogPricingService/Data/ProductoRepository.cs), [src/CatalogPricingService/Services/ProductoService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4bb39aa947e6e7586d437b8ab490c1a99c03f02e/src/CatalogPricingService/Services/ProductoService.cs).

**Posible repregunta:** ¿Por qué PUT necesita poder encontrar un producto inactivo?

---

<a id="g-15"></a>

**Pregunta N.º:** 15 (G-15)

**Categoría:** Funcionalidades desarrolladas y funcionamiento del sistema

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué diferencia hay entre tarjeta aprobada, rechazada e incierta?

**Respuesta modelo:** Aprobada permite completar la venta con pago, intento aprobado y outbox. Rechazada conserva la venta pendiente y registra rechazo sin pago ni evento. Un timeout o error ambiguo mantiene el intento pendiente y bloquea una nueva autorización; se consulta el mismo intento para reconciliar. Un error de comunicación no demuestra que la pasarela no haya autorizado.

**Explicación para estudiar:** El estado comercial y el estado de comunicación no son equivalentes. La incertidumbre necesita una recuperación explícita para evitar duplicar cargos.

**Evidencia del repositorio:** [f6727db](https://github.com/rpedrazav/TallerIntegracion3/commit/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d/src/POSCartService/Controllers/VentasController.cs); [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md); [25a40f5](https://github.com/rpedrazav/TallerIntegracion3/commit/25a40f52648694c51c2ffa429c065349c3046202) (integrado en dev): [docs/qa/S2-052/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/25a40f52648694c51c2ffa429c065349c3046202/docs/qa/S2-052/reporte.md).

**Posible repregunta:** ¿Qué debe hacer la interfaz si una consulta sigue devolviendo UNKNOWN?

---

<a id="g-16"></a>

**Pregunta N.º:** 16 (G-16)

**Categoría:** Funcionalidades desarrolladas y funcionamiento del sistema

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo ingresa un producto pesable y qué cantidad se factura?

**Respuesta modelo:** Catálogo determina que es pesable. El flujo obtiene un peso estable, lo presenta y espera confirmación antes de enviar el ítem. En MS-5 el peso canónico es kg, con mínimo 0,010 e incrementos de 0,001; la cantidad facturable coincide con peso_kg. El subtotal usa precio por kg por peso, nunca precio por cantidad por peso.

**Explicación para estudiar:** Unidad, resolución y estabilidad son conceptos distintos. Una lectura estable puede seguir siendo inválida si tiene unidad incorrecta o precisión no admitida.

**Evidencia del repositorio:** [e61d618](https://github.com/rpedrazav/TallerIntegracion3/commit/e61d618cdaa1a9eb7074c94d97ecca3de0c8b148) (integrado en dev): [docs/context/dominio/peso-variable.md](https://github.com/rpedrazav/TallerIntegracion3/blob/e61d618cdaa1a9eb7074c94d97ecca3de0c8b148/docs/context/dominio/peso-variable.md); [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [src/POSCartService/Services/ReglasCantidadItem.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/src/POSCartService/Services/ReglasCantidadItem.cs); [39b5b2a](https://github.com/rpedrazav/TallerIntegracion3/commit/39b5b2a08291ea2cf299e1a427a3165180ee3cb4) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/39b5b2a08291ea2cf299e1a427a3165180ee3cb4/globalmart-frontend/src/pages/Pos.tsx).

**Posible repregunta:** ¿Es válido enviar cantidad 1 y peso_kg 0,750?

---

<a id="g-17"></a>

**Pregunta N.º:** 17 (G-17)

**Categoría:** Funcionalidades desarrolladas y funcionamiento del sistema

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué permite el respaldo manual cuando falla la balanza?

**Respuesta modelo:** El POS ofrece ingreso manual identificado como tal y valida mínimo y precisión antes de enviarlo. El usuario confirma el peso y MS-5 vuelve a validar el contrato. El respaldo permite continuar una captura fallida; no convierte la cifra en una medición física verificada. Las pruebas de interfaz cubren desconexión, valores inválidos y los importes devueltos por el servidor.

**Explicación para estudiar:** La validación de UI mejora la experiencia; la del servidor mantiene la regla frente a clientes alterados. El origen manual debe seguir siendo visible.

**Evidencia del repositorio:** [755ba27](https://github.com/rpedrazav/TallerIntegracion3/commit/755ba275391164c675cbc95aaed0eb1fb9c83dbd) (integrado en dev): [globalmart-frontend/src/hardware/scale-cart.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/755ba275391164c675cbc95aaed0eb1fb9c83dbd/globalmart-frontend/src/hardware/scale-cart.ts), [globalmart-frontend/src/components/pos/PesoProducto.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/755ba275391164c675cbc95aaed0eb1fb9c83dbd/globalmart-frontend/src/components/pos/PesoProducto.tsx); [4bde899](https://github.com/rpedrazav/TallerIntegracion3/commit/4bde899fce12fc6d005100d7585ec27cfbda8024) (integrado en dev): [globalmart-frontend/e2e/weight-cart.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/4bde899fce12fc6d005100d7585ec27cfbda8024/globalmart-frontend/e2e/weight-cart.spec.ts).

**Posible repregunta:** ¿Por qué se rechaza 0,7505 en lugar de redondearlo?

---

<a id="g-18"></a>

**Pregunta N.º:** 18 (G-18)

**Categoría:** Funcionalidades desarrolladas y funcionamiento del sistema

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué cambió en la administración al final del sprint?

**Respuesta modelo:** Se agregó aprovisionamiento explícito de tenant y ADMIN, se unificaron edición y baja con Usuarios, que usa el login, y se amplió la navegación administrativa. También hay stock por sucursal y roles propios del tenant con permisos base. Es una extensión comprobable en código; la revocación inmediata de tokens ya emitidos continúa pendiente.

**Explicación para estudiar:** Corregir la tabla realmente autenticada es más importante que mostrar éxito en el formulario. El acceso posterior debe reflejar el cambio persistido.

**Evidencia del repositorio:** [bfd46b0](https://github.com/rpedrazav/TallerIntegracion3/commit/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1) (integrado en dev): [src/TenantIdentityService/Services/AdminBootstrap.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1/src/TenantIdentityService/Services/AdminBootstrap.cs), [src/TenantIdentityService/Repositories/UsuarioRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1/src/TenantIdentityService/Repositories/UsuarioRepository.cs); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (integrado en dev): [src/TenantIdentityService/Services/JwtService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/TenantIdentityService/Services/JwtService.cs), [tests/GlobalMart.IntegrationTests/AdminCustomRolesIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/tests/GlobalMart.IntegrationTests/AdminCustomRolesIntegrationTests.cs).

**Posible repregunta:** ¿Por qué desactivar una cuenta no invalida automáticamente su JWT vigente?

---

<a id="g-19"></a>

**Pregunta N.º:** 19 (G-19)

**Categoría:** Funcionalidades desarrolladas y funcionamiento del sistema

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué una aprobación del terminal simulado no basta para vaciar el carrito?

**Respuesta modelo:** La aprobación local es preliminar. synchronizeCardPayment exige la misma referencia del intento y consulta o cobra mediante MS-5. Solo considera éxito una respuesta aprobada con la venta esperada en COMPLETADA. Si la respuesta es ambigua, reconcilia la misma clave. Así la interfaz no presenta una venta cobrada solo porque un simulador visual produjo approved.

**Explicación para estudiar:** Hay dos autoridades: el dispositivo informa su interacción y MS-5 confirma el estado de negocio persistido. La correlación une ambos sin confundirlos.

**Evidencia del repositorio:** [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) (integrado en dev): [globalmart-frontend/src/payments/synchronizeCardPayment.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/src/payments/synchronizeCardPayment.ts), [globalmart-frontend/tests/synchronized-card-payment.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/tests/synchronized-card-payment.test.cjs).

**Posible repregunta:** ¿Cómo reaccionaría la función ante una referencia distinta de la solicitada?

---

<a id="g-20"></a>

**Pregunta N.º:** 20 (G-20)

**Categoría:** Funcionalidades desarrolladas y funcionamiento del sistema

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué protege un ajuste de stock entre sucursales y tenants?

**Respuesta modelo:** StockController exige JWT y ADMIN o REPONEDOR, toma tenant del contexto y verifica producto y sucursal mediante sus servicios. Actualiza el stock y registra movimiento en una transacción serializable. Los tests comparan estado y aislamiento. Esto permite ajustes administrativos; no acredita FEFO ni demuestra que el descuento Kafka remoto esté funcionando.

**Explicación para estudiar:** Un identificador válido sintácticamente no prueba pertenencia. La operación debe verificar la relación del producto y la sucursal con el tenant autorizado.

**Evidencia del repositorio:** [bfd46b0](https://github.com/rpedrazav/TallerIntegracion3/commit/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1) (integrado en dev): [src/WarehouseInventoryService/Controllers/StockController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1/src/WarehouseInventoryService/Controllers/StockController.cs); [dbd2b09](https://github.com/rpedrazav/TallerIntegracion3/commit/dbd2b096d545cd5476821dd3a2baa4f420db2eca) (integrado en dev): [tests/GlobalMart.IntegrationTests/AdminStockIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/dbd2b096d545cd5476821dd3a2baa4f420db2eca/tests/GlobalMart.IntegrationTests/AdminStockIntegrationTests.cs).

**Posible repregunta:** ¿Qué debe conservarse si falla la validación remota de la sucursal?

---


### Arquitectura, tecnologías y decisiones técnicas

<a id="g-21"></a>

**Pregunta N.º:** 21 (G-21)

**Categoría:** Arquitectura, tecnologías y decisiones técnicas

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué responsabilidades tienen MS-1, MS-2, MS-3, MS-4 y MS-5 en este sprint?

**Respuesta modelo:** MS-1 identifica usuarios y tenants y administra configuración; MS-2 calcula impuestos; MS-3 resuelve productos y precios; MS-4 mantiene inventario; MS-5 orquesta turnos, venta y cobro. Electron ofrece la interfaz. Los otros tres proyectos forman parte de la arquitectura, pero no debemos presentarlos como módulos funcionalmente terminados por tener entidades y migraciones.

**Explicación para estudiar:** Separar responsabilidad evita que la interfaz o POS dupliquen reglas de catálogo e impuestos. La independencia también introduce dependencias de comunicación que deben probarse.

**Evidencia del repositorio:** [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/docs/context/estado-actual.md); [152f39b](https://github.com/rpedrazav/TallerIntegracion3/commit/152f39b00076de93573a261f9721ad1c29eb3c5c) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Controllers/VentasController.cs).

**Posible repregunta:** ¿Qué servicio debería decidir si un producto sigue vendible?

---

<a id="g-22"></a>

**Pregunta N.º:** 22 (G-22)

**Categoría:** Arquitectura, tecnologías y decisiones técnicas

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo está organizado el código del flujo de ventas?

**Respuesta modelo:** VentasController recibe HTTP, valida contexto y coordina catálogo e impuestos. Los DTOs expresan contratos; VentaService organiza operaciones de negocio y VentaRepository persiste con EF Core. PosCartDbContext configura tablas, relaciones e aislamiento. La interfaz utiliza clientes tipados. Esta separación facilita probar límites, aunque el controlador aún concentra bastante orquestación y podría refactorizarse.

**Explicación para estudiar:** Separar capas no es solo crear carpetas: importa que cada regla tenga una ubicación clara y que los dobles de prueba no sustituyan lo que se quiere verificar.

**Evidencia del repositorio:** [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/POSCartService/Controllers/VentasController.cs), [src/POSCartService/Services/VentaService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/POSCartService/Services/VentaService.cs), [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/POSCartService/Repositories/VentaRepository.cs), [src/POSCartService/Data/PosCartDbContext.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/POSCartService/Data/PosCartDbContext.cs), [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/globalmart-frontend/src/api/client.ts).

**Posible repregunta:** ¿Qué parte extraerían del controlador sin debilitar la validación?

---

<a id="g-23"></a>

**Pregunta N.º:** 23 (G-23)

**Categoría:** Arquitectura, tecnologías y decisiones técnicas

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Para qué usan Kong y cómo se relacionan sus rutas con los controladores?

**Respuesta modelo:** Kong ofrece prefijos de entrada comunes, como /api/ventas, y los enruta al servicio correspondiente. Las rutas conservan el prefijo con strip_path=false, por lo que los controladores deben aceptar los alias /api. Diego inventarió rutas y añadió pruebas de prefijos. Una ruta configurada en Kong no significa que exista un endpoint funcional detrás.

**Explicación para estudiar:** Gateway, dirección interna y ruta del controlador son capas distintas. Un desacuerdo entre ellas puede producir 404 aunque el servicio esté sano.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [Docker/config/kong.yaml](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/Docker/config/kong.yaml), [docs/context/infraestructura/matriz-rutas.md](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/docs/context/infraestructura/matriz-rutas.md), [tests/GlobalMart.IntegrationTests/RoutePrefixIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/tests/GlobalMart.IntegrationTests/RoutePrefixIntegrationTests.cs).

**Posible repregunta:** ¿Por qué una ruta /api/prices no demuestra precios dinámicos implementados?

---

<a id="g-24"></a>

**Pregunta N.º:** 24 (G-24)

**Categoría:** Arquitectura, tecnologías y decisiones técnicas

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué usar un outbox en PostgreSQL para sale.completed?

**Respuesta modelo:** Evita confirmar la venta en la base y perder el evento si Kafka falla inmediatamente después. Se guarda la intención de publicación con venta y pago en una misma transacción local. Otro proceso publica luego y actualiza su estado. Kafka puede estar caído sin borrar un cobro ya confirmado; el evento queda pendiente para recuperación.

**Explicación para estudiar:** No hay una transacción única entre PostgreSQL y Kafka. El outbox mueve la consistencia crítica al almacenamiento local y acepta publicación posterior con posibles reentregas.

**Evidencia del repositorio:** [0602a93](https://github.com/rpedrazav/TallerIntegracion3/commit/0602a936469683c8a1f3409477c6c644aa9fe5c0) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0602a936469683c8a1f3409477c6c644aa9fe5c0/src/POSCartService/Repositories/VentaRepository.cs), [src/POSCartService/Models/OutboxMessage.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0602a936469683c8a1f3409477c6c644aa9fe5c0/src/POSCartService/Models/OutboxMessage.cs); [4cf822c](https://github.com/rpedrazav/TallerIntegracion3/commit/4cf822c347c18d987282a31b5babe3e2f3582a31) (integrado en dev): [src/POSCartService/Services/OutboxPublisherService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Services/OutboxPublisherService.cs).

**Posible repregunta:** ¿Qué ocurre si Kafka recibió el evento pero falló el marcado PUBLICADO?

---

<a id="g-25"></a>

**Pregunta N.º:** 25 (G-25)

**Categoría:** Arquitectura, tecnologías y decisiones técnicas

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué aporta TypeScript y qué no demuestra el gate del frontend?

**Respuesta modelo:** Los contratos tipados detectan incompatibilidades entre clientes, componentes y adaptadores antes de empaquetar. El workflow ejecuta npm ci y tsc --noEmit. Eso no verifica respuestas reales de HTTP, aislamiento de datos ni entradas IPC en tiempo de ejecución; esas fronteras requieren validadores y pruebas. Los ajustes de TenantConfig muestran cómo una discrepancia de tipos afecta formularios concretos.

**Explicación para estudiar:** Los tipos desaparecen en ejecución. Un payload externo sigue siendo desconocido aunque un código TypeScript lo declare con una interfaz.

**Evidencia del repositorio:** [512f2b7](https://github.com/rpedrazav/TallerIntegracion3/commit/512f2b71419c11dc4925120245a78a8e16d95637) (integrado en dev): [.github/workflows/ci.yml](https://github.com/rpedrazav/TallerIntegracion3/blob/512f2b71419c11dc4925120245a78a8e16d95637/.github/workflows/ci.yml); [b4f6d20](https://github.com/rpedrazav/TallerIntegracion3/commit/b4f6d20d5d9f6cdc9e85b73124b56e29bc5a7073) (integrado en dev): [globalmart-frontend/src/components/admin/TenantConfig.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/b4f6d20d5d9f6cdc9e85b73124b56e29bc5a7073/globalmart-frontend/src/components/admin/TenantConfig.tsx); [96dc728](https://github.com/rpedrazav/TallerIntegracion3/commit/96dc728f1adf215086f49f01f6271f38a43d9257) (integrado en dev): [globalmart-frontend/src/payments/terminal-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/96dc728f1adf215086f49f01f6271f38a43d9257/globalmart-frontend/src/payments/terminal-ipc.ts).

**Posible repregunta:** ¿Por qué parseIniciarCobroTerminal recibe unknown?

---

<a id="g-26"></a>

**Pregunta N.º:** 26 (G-26)

**Categoría:** Arquitectura, tecnologías y decisiones técnicas

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo distribuye Electron las responsabilidades de hardware?

**Respuesta modelo:** React corre en renderer, preload expone una API limitada con contextBridge y main contiene adaptadores y handlers. La balanza permite conectar, desconectar, leer peso estable y consultar estado; el terminal expone iniciar y consultar. El renderer no necesita puertos seriales ni comandos arbitrarios. contextIsolation, sandbox y nodeIntegration deshabilitado complementan la validación de payloads.

**Explicación para estudiar:** Un puente pequeño reduce capacidades disponibles a una interfaz comprometida. Los tipos ayudan al programador y las validaciones protegen la frontera de ejecución.

**Evidencia del repositorio:** [71847b1](https://github.com/rpedrazav/TallerIntegracion3/commit/71847b18c559111107436474461d18542688b7dc) (integrado en dev): [globalmart-frontend/src/hardware/scale-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/71847b18c559111107436474461d18542688b7dc/globalmart-frontend/src/hardware/scale-ipc.ts), [globalmart-frontend/src/preload.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/71847b18c559111107436474461d18542688b7dc/globalmart-frontend/src/preload.ts); [96dc728](https://github.com/rpedrazav/TallerIntegracion3/commit/96dc728f1adf215086f49f01f6271f38a43d9257) (integrado en dev): [globalmart-frontend/src/payments/terminal-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/96dc728f1adf215086f49f01f6271f38a43d9257/globalmart-frontend/src/payments/terminal-ipc.ts); [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [globalmart-frontend/src/index.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/index.ts).

**Posible repregunta:** ¿Qué riesgo introduciría exponer ipcRenderer.invoke libremente?

---

<a id="g-27"></a>

**Pregunta N.º:** 27 (G-27)

**Categoría:** Arquitectura, tecnologías y decisiones técnicas

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué el precio pesable conserva cuatro decimales pero el subtotal se redondea a dos?

**Respuesta modelo:** El catálogo ofrece precisión de cuatro decimales, útil en precio por kg. MS-5 conserva ese snapshot en numeric(20,4), multiplica por el peso y redondea el subtotal a dos con ToEven. MS-2 redondea IVA por línea y suma. Redondear el precio primero cambiaría el resultado, y ampliar la columna no recupera decimales históricos ya perdidos.

**Explicación para estudiar:** Precisión de entrada y precisión monetaria final no son lo mismo. El punto donde se redondea forma parte del contrato entre servicios.

**Evidencia del repositorio:** [ef6a406](https://github.com/rpedrazav/TallerIntegracion3/commit/ef6a40632bc975c961d289a6d41c7a67cc85d56d) (integrado en dev): [src/POSCartService/Data/PosCartDbContext.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ef6a40632bc975c961d289a6d41c7a67cc85d56d/src/POSCartService/Data/PosCartDbContext.cs), [src/TaxComplianceService/Services/TaxCalculatorService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ef6a40632bc975c961d289a6d41c7a67cc85d56d/src/TaxComplianceService/Services/TaxCalculatorService.cs), [tests/GlobalMart.IntegrationTests/PesableTaxRoundingTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ef6a40632bc975c961d289a6d41c7a67cc85d56d/tests/GlobalMart.IntegrationTests/PesableTaxRoundingTests.cs).

**Posible repregunta:** ¿Cuánto resulta 12,3456 por 0,750 antes y después del redondeo?

---

<a id="g-28"></a>

**Pregunta N.º:** 28 (G-28)

**Categoría:** Arquitectura, tecnologías y decisiones técnicas

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿La implementación garantiza exactamente una entrega y un descuento de stock?

**Respuesta modelo:** El outbox permite entrega al menos una vez: una caída después de enviar y antes de marcar puede repetir el mismo EventId. El consumidor actual registra eventos procesados, pero guarda descuentos y marcador por pasos y confirma offsets incluso ante error. Por ello no debemos afirmar exactamente una vez de extremo a extremo; falta cerrar atomicidad y recuperación del consumidor.

**Explicación para estudiar:** Deduplicar casos normales no elimina ventanas de fallo. Hay que analizar qué se confirmó en cada sistema antes de una caída y qué se reintentará después.

**Evidencia del repositorio:** [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs); [4cf822c](https://github.com/rpedrazav/TallerIntegracion3/commit/4cf822c347c18d987282a31b5babe3e2f3582a31) (integrado en dev): [src/POSCartService/Services/OutboxPublisherService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Services/OutboxPublisherService.cs); [054d7d3](https://github.com/rpedrazav/TallerIntegracion3/commit/054d7d382ca6a91967e96dc885b9cfb6b1558c68) (integrado en dev): [docs/context/eventos/sale-completed-outbox.md](https://github.com/rpedrazav/TallerIntegracion3/blob/054d7d382ca6a91967e96dc885b9cfb6b1558c68/docs/context/eventos/sale-completed-outbox.md).

**Posible repregunta:** ¿Cómo agruparían descuentos y marcador para reducir esa ventana?

---

<a id="g-29"></a>

**Pregunta N.º:** 29 (G-29)

**Categoría:** Arquitectura, tecnologías y decisiones técnicas

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo impiden que dos instancias de MS-5 autoricen simultáneamente la misma venta?

**Respuesta modelo:** Antes de HTTP, ReservarIntentoPagoAsync bloquea la fila de venta con FOR UPDATE dentro de una transacción breve. Comprueba estado, monto, pagos e intentos no rechazados, y guarda una reserva durable. El índice por tenant y clave protege además claves repetidas. La transacción termina antes de llamar a la pasarela; la reserva sigue bloqueando otra autorización.

**Explicación para estudiar:** Un booleano en React o un lock de proceso no coordina varias instancias. La coordinación compartida debe ocurrir en almacenamiento común y cubrir claves diferentes para la misma venta.

**Evidencia del repositorio:** [212cc56](https://github.com/rpedrazav/TallerIntegracion3/commit/212cc56b8ffe0c44dc07650a40b01ebc2996f215) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/212cc56b8ffe0c44dc07650a40b01ebc2996f215/src/POSCartService/Repositories/VentaRepository.cs); [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/src/POSCartService/Repositories/VentaRepository.cs), [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md).

**Posible repregunta:** ¿Por qué el índice de idempotencia solo no cubre dos claves distintas?

---

<a id="g-30"></a>

**Pregunta N.º:** 30 (G-30)

**Categoría:** Arquitectura, tecnologías y decisiones técnicas

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué diferencia hay entre un adaptador serial intercambiable y compatibilidad con una balanza real?

**Respuesta modelo:** SerialScaleAdapter recibe transporte, parser y configuración por inyección y normaliza a kg. Puede probarse con tramas y errores controlados, pero no implementa por sí solo una biblioteca serial ni un protocolo de fabricante. La fábrica exige dependencias reales para ese modo. Declarar compatibilidad requiere modelo, manual, transporte implementado y pruebas con el equipo.

**Explicación para estudiar:** Una interfaz desacopla el software de una implementación futura; no crea esa implementación. Las pruebas de contrato y las de hardware cubren riesgos diferentes.

**Evidencia del repositorio:** [0afc855](https://github.com/rpedrazav/TallerIntegracion3/commit/0afc855588582f8294644160dff4f418f40b2683) (integrado en dev): [globalmart-frontend/src/hardware/serial-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/src/hardware/serial-scale-adapter.ts), [globalmart-frontend/src/hardware/scale-adapter-factory.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/src/hardware/scale-adapter-factory.ts), [globalmart-frontend/tests/serial-scale-adapter.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/tests/serial-scale-adapter.test.cjs).

**Posible repregunta:** ¿Qué conservarían del adaptador si cambia el protocolo del fabricante?

---


### Integración de módulos y trabajo colaborativo

<a id="g-31"></a>

**Pregunta N.º:** 31 (G-31)

**Categoría:** Integración de módulos y trabajo colaborativo

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué trabajo conectó el carrito de Rodrigo con el backend de Sebastián?

**Respuesta modelo:** Los clientes de frontend comenzaron a enviar producto y cantidad a MS-5, conservar el ID de venta y refrescar los ítems y totales. El backend resolvió datos autoritativos y rechazó entradas inválidas. Así la interfaz dejó de tratar sus cálculos locales como la venta persistida. La recuperación por GET /ventas/{id} permite reconstruir la venta pendiente.

**Explicación para estudiar:** La integración depende de acordar request, respuesta y errores. Que cada lado funcione aisladamente no garantiza que interpreten igual el contrato.

**Evidencia del repositorio:** [152f39b](https://github.com/rpedrazav/TallerIntegracion3/commit/152f39b00076de93573a261f9721ad1c29eb3c5c) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Controllers/VentasController.cs); [160846e](https://github.com/rpedrazav/TallerIntegracion3/commit/160846eb68bac27937560baabb6a10d4ed545c73) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/160846eb68bac27937560baabb6a10d4ed545c73/globalmart-frontend/src/pages/Pos.tsx), [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/160846eb68bac27937560baabb6a10d4ed545c73/globalmart-frontend/src/api/client.ts); [9dedcc5](https://github.com/rpedrazav/TallerIntegracion3/commit/9dedcc50684998bb0791c214e7947939c0503021) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/9dedcc50684998bb0791c214e7947939c0503021/globalmart-frontend/src/pages/Pos.tsx).

**Posible repregunta:** ¿Qué debería mostrar el POS si no puede refrescar después de una mutación?

---

<a id="g-32"></a>

**Pregunta N.º:** 32 (G-32)

**Categoría:** Integración de módulos y trabajo colaborativo

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo se conectaron el trabajo de Nicolás y el de Sebastián en pagos?

**Respuesta modelo:** Nicolás aportó adaptador HTTP, simulador y outbox; Sebastián incorporó intento persistente, guardias del endpoint y finalización del cobro. La secuencia debe reservar, autorizar y después completar localmente con pago y evento. Los tests de contrato y de HTTP comprueban distintos tramos, por lo que ninguno de los dos aportes acredita solo todo el flujo.

**Explicación para estudiar:** Los límites entre módulos deben conservar identidad, monto y estado. Integrar requiere verificar el orden de efectos, además de que las interfaces compilen.

**Evidencia del repositorio:** [866e5b1](https://github.com/rpedrazav/TallerIntegracion3/commit/866e5b156356706f02d5ceaadedcd9965c9b7ed5) (integrado en dev): [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/866e5b156356706f02d5ceaadedcd9965c9b7ed5/src/POSCartService/Services/PaymentGatewayClient.cs); [212cc56](https://github.com/rpedrazav/TallerIntegracion3/commit/212cc56b8ffe0c44dc07650a40b01ebc2996f215) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/212cc56b8ffe0c44dc07650a40b01ebc2996f215/src/POSCartService/Repositories/VentaRepository.cs); [ac583e8](https://github.com/rpedrazav/TallerIntegracion3/commit/ac583e8b6d50ef4b1133e21c87d4a5613a95b488) (integrado en dev): [tests/GlobalMart.IntegrationTests/PaymentEventSequenceContractTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ac583e8b6d50ef4b1133e21c87d4a5613a95b488/tests/GlobalMart.IntegrationTests/PaymentEventSequenceContractTests.cs).

**Posible repregunta:** ¿Quién debe generar la referencia que mantiene unido el ciclo de autorización?

---

<a id="g-33"></a>

**Pregunta N.º:** 33 (G-33)

**Categoría:** Integración de módulos y trabajo colaborativo

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo se repartió la funcionalidad pesable entre backend, adaptador e interfaz?

**Respuesta modelo:** Sebastián definió y unificó reglas en MS-5 y el cálculo; Nicolás implementó contrato, simulador, serial abstracto e IPC; Rodrigo agregó captura, confirmación, respaldo manual y presentación; Daniel agregó pruebas y protocolo físico en su rama. Cada parte tiene evidencia distinta. La defensa grupal debe explicar el recorrido completo sin asignar todas esas capas a una sola persona.

**Explicación para estudiar:** Una función visible suele atravesar varias responsabilidades. La trazabilidad por archivo y commit identifica interfaces donde deben coordinarse las contribuciones.

**Evidencia del repositorio:** [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [src/POSCartService/Services/ReglasCantidadItem.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/src/POSCartService/Services/ReglasCantidadItem.cs); [0afc855](https://github.com/rpedrazav/TallerIntegracion3/commit/0afc855588582f8294644160dff4f418f40b2683) (integrado en dev): [globalmart-frontend/src/hardware/serial-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/src/hardware/serial-scale-adapter.ts); [755ba27](https://github.com/rpedrazav/TallerIntegracion3/commit/755ba275391164c675cbc95aaed0eb1fb9c83dbd) (integrado en dev): [globalmart-frontend/src/hardware/scale-cart.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/755ba275391164c675cbc95aaed0eb1fb9c83dbd/globalmart-frontend/src/hardware/scale-cart.ts); [8c20fee](https://github.com/rpedrazav/TallerIntegracion3/commit/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98) (rama, no integrado en dev): [docs/qa/S2-085/protocolo-balanza-fisica.md](https://github.com/rpedrazav/TallerIntegracion3/blob/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98/docs/qa/S2-085/protocolo-balanza-fisica.md).

**Posible repregunta:** ¿En qué capa se convierte de gramos a kg y dónde se vuelve a validar?

---

<a id="g-34"></a>

**Pregunta N.º:** 34 (G-34)

**Categoría:** Integración de módulos y trabajo colaborativo

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué se resolvió en el merge de cobro seguro y recuperación de pagos?

**Respuesta modelo:** El merge 1ba6b37 conservó la reserva previa de S2-035 y la consulta/reconciliación de S2-049. Unificó el estado ambiguo como PENDIENTE, mantuvo actualización condicional y referencias de pago, y adaptó pruebas a la interfaz combinada. Su reporte registra 54 casos backend y siete de UI con APIs controladas; no acredita el clúster completo.

**Explicación para estudiar:** Resolver un conflicto semántico exige conservar invariantes de ambos cambios. Elegir un archivo entero puede compilar y aun así eliminar una protección.

**Evidencia del repositorio:** [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md), [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/src/POSCartService/Repositories/VentaRepository.cs).

**Posible repregunta:** ¿Qué regresión habría aparecido si se perdía la reserva anterior al POST?

---

<a id="g-35"></a>

**Pregunta N.º:** 35 (G-35)

**Categoría:** Integración de módulos y trabajo colaborativo

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo evita la prueba RN-01 un falso éxito por rechazar todas las solicitudes?

**Respuesta modelo:** Usa dos tenants y controles positivos: el usuario propio debe acceder y el ajeno debe ser rechazado. Compara también snapshots de la base para verificar que una mutación prohibida no alteró datos. Así distingue aislamiento real de una API que responde siempre error. JWT, middleware y repositorios productivos participan en los casos HTTP.

**Explicación para estudiar:** Una prueba negativa necesita un control que demuestre que el recurso existe y funciona para quien corresponde. El status aislado no acredita ausencia de efectos laterales.

**Evidencia del repositorio:** [d44e22b](https://github.com/rpedrazav/TallerIntegracion3/commit/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e) (integrado en dev): [tests/GlobalMart.IntegrationTests/VentaTenantIsolationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/VentaTenantIsolationTests.cs), [tests/GlobalMart.IntegrationTests/ProductoTenantIsolationIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/ProductoTenantIsolationIntegrationTests.cs), [tests/GlobalMart.IntegrationTests/PosRepositoryTenantIsolationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/PosRepositoryTenantIsolationTests.cs).

**Posible repregunta:** ¿Qué demuestra consultar luego con el JWT del tenant B?

---

<a id="g-36"></a>

**Pregunta N.º:** 36 (G-36)

**Categoría:** Integración de módulos y trabajo colaborativo

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué aportó Diego para ejecutar juntos los servicios y simuladores?

**Respuesta modelo:** Inventarió rutas y puertos, agregó alias coherentes con Kong, parametrizó destinos del frontend e incorporó payment-simulator a Compose y CI. También añadió scripts de arranque y smoke. Esos cambios reducen diferencias entre entornos, pero se debe verificar la configuración efectiva: una URL local dentro de un pod apunta al propio pod, no al servicio remoto esperado.

**Explicación para estudiar:** La integración operativa incluye nombres DNS, puertos internos y expuestos y prefijos HTTP. Un fallo puede ocurrir antes de llegar a la regla de negocio.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [docs/context/infraestructura/matriz-rutas.md](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/docs/context/infraestructura/matriz-rutas.md), [globalmart-frontend/src/config/api.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/globalmart-frontend/src/config/api.ts); [1ca8853](https://github.com/rpedrazav/TallerIntegracion3/commit/1ca8853ea62c0c1fccf1b95b886d6996e74d0166) (integrado en dev): [Docker/docker-compose.yml](https://github.com/rpedrazav/TallerIntegracion3/blob/1ca8853ea62c0c1fccf1b95b886d6996e74d0166/Docker/docker-compose.yml); [25a40f5](https://github.com/rpedrazav/TallerIntegracion3/commit/25a40f52648694c51c2ffa429c065349c3046202) (integrado en dev): [docs/qa/S2-052/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/25a40f52648694c51c2ffa429c065349c3046202/docs/qa/S2-052/reporte.md).

**Posible repregunta:** ¿Qué revela el 502 de catálogo documentado en S2-052?

---

<a id="g-37"></a>

**Pregunta N.º:** 37 (G-37)

**Categoría:** Integración de módulos y trabajo colaborativo

**Dificultad:** Intermedia.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Cómo deberían presentar el trabajo colaborativo si Git no registra reuniones ni revisiones?

**Respuesta modelo:** Podemos describir dependencias y merges verificables, como la integración de reserva y recuperación. La frecuencia de reuniones, quién propuso una idea o qué revisión informal ocurrió está [NO VERIFICADO]. Cada integrante debe confirmar un ejemplo real y responder con contexto, acuerdo, acción propia y resultado observable, apoyándolo con el commit o documento correspondiente.

**Explicación para estudiar:** Una evidencia técnica permite reconstruir cambios, pero no atribuir motivaciones ni conversaciones. La defensa gana precisión separando hechos registrados de experiencia personal confirmada.

**Evidencia del repositorio:** [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md); [806450b](https://github.com/rpedrazav/TallerIntegracion3/commit/806450bec2bda6cee096bb62cbc02b204d5daae9) (integrado en dev): [diagramas/matriz/S2-021-matriz-aceptacion.md](https://github.com/rpedrazav/TallerIntegracion3/blob/806450bec2bda6cee096bb62cbc02b204d5daae9/diagramas/matriz/S2-021-matriz-aceptacion.md).

**Posible repregunta:** ¿Qué acuerdo concreto recuerdan sobre el estado incierto y dónde se refleja?

---

<a id="g-38"></a>

**Pregunta N.º:** 38 (G-38)

**Categoría:** Integración de módulos y trabajo colaborativo

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué sucede cuando evoluciona el contrato sale.completed?

**Respuesta modelo:** La versión del esquema, EventId, tenant y datos de venta permiten reconocer el evento y coordinar consumidores. El outbox guarda un snapshot estable que no debe reconstruirse al reintentar. Agregar campos compatibles exige comprobar el deserializador; un cambio incompatible requiere una nueva versión y estrategia de transición. La existencia de un consumidor antiguo no demuestra automáticamente compatibilidad validada.

**Explicación para estudiar:** Versionar hace explícito el contrato distribuido. La tolerancia de JSON no garantiza que nuevos significados se interpreten correctamente.

**Evidencia del repositorio:** [054d7d3](https://github.com/rpedrazav/TallerIntegracion3/commit/054d7d382ca6a91967e96dc885b9cfb6b1558c68) (integrado en dev): [docs/context/eventos/sale-completed-outbox.md](https://github.com/rpedrazav/TallerIntegracion3/blob/054d7d382ca6a91967e96dc885b9cfb6b1558c68/docs/context/eventos/sale-completed-outbox.md); [0602a93](https://github.com/rpedrazav/TallerIntegracion3/commit/0602a936469683c8a1f3409477c6c644aa9fe5c0) (integrado en dev): [src/POSCartService/Messaging/SaleCompletedEvent.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0602a936469683c8a1f3409477c6c644aa9fe5c0/src/POSCartService/Messaging/SaleCompletedEvent.cs); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs).

**Posible repregunta:** ¿Cómo probarían que un consumidor rechaza una versión que no entiende?

---

<a id="g-39"></a>

**Pregunta N.º:** 39 (G-39)

**Categoría:** Integración de módulos y trabajo colaborativo

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué distinguir UI con HTTP simulado, integración backend y E2E Electron?

**Respuesta modelo:** Las pruebas de renderer verifican interacción con respuestas controladas; las backend ejercitan HTTP, JWT y PostgreSQL en diferentes combinaciones; el E2E de Daniel arranca Electron y MS-5 con simulador real, pero mantiene identidad, catálogo e IVA controlados y no publica Kafka. Cada nivel reduce riesgos distintos. No se pueden sumar sus nombres y afirmar un despliegue completo validado.

**Explicación para estudiar:** La frontera sustituida por un doble define el límite de una conclusión. Hay que explicar tanto lo ejecutado como las dependencias controladas.

**Evidencia del repositorio:** [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md); [3148c9a](https://github.com/rpedrazav/TallerIntegracion3/commit/3148c9aa4a19b7a5448bb85107a41c1c060eb37b) (rama, no integrado en dev): [docs/qa/S2-055/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/3148c9aa4a19b7a5448bb85107a41c1c060eb37b/docs/qa/S2-055/reporte.md), [tests/GlobalMart.IntegrationTests/ElectronCardE2ETests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/3148c9aa4a19b7a5448bb85107a41c1c060eb37b/tests/GlobalMart.IntegrationTests/ElectronCardE2ETests.cs).

**Posible repregunta:** ¿Qué falta agregar al E2E de tarjeta para comprobar descuento de stock?

---

<a id="g-40"></a>

**Pregunta N.º:** 40 (G-40)

**Categoría:** Integración de módulos y trabajo colaborativo

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo integrarían los cambios pendientes de Daniel y Martín sin perder avances de dev?

**Respuesta modelo:** Compararíamos los commits con dev, identificaríamos pruebas y correcciones exclusivas, resolveríamos los contratos modificados y ejecutaríamos regresiones relevantes. El test antiguo de fallback inspecciona una estructura de callback que evolucionó con la captura confirmada; no conviene trasladarlo sin revisar su intención. Luego documentaríamos qué evidencia nueva corresponde al resultado integrado, no a la rama histórica.

**Explicación para estudiar:** Una prueba puede volverse obsoleta por cambios estructurales aunque su requisito siga vigente. La integración debe preservar el comportamiento esperado, no copiar ciegamente el mecanismo de prueba.

**Evidencia del repositorio:** [f787762](https://github.com/rpedrazav/TallerIntegracion3/commit/f787762c367100e6d7f5d85815d59e697f5330c6) (rama, no integrado en dev): [tests/GlobalMart.IntegrationTests/s2083-scale-tests.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/f787762c367100e6d7f5d85815d59e697f5330c6/tests/GlobalMart.IntegrationTests/s2083-scale-tests.cjs); [755ba27](https://github.com/rpedrazav/TallerIntegracion3/commit/755ba275391164c675cbc95aaed0eb1fb9c83dbd) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/755ba275391164c675cbc95aaed0eb1fb9c83dbd/globalmart-frontend/src/pages/Pos.tsx); [9b7cb4b](https://github.com/rpedrazav/TallerIntegracion3/commit/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1) (rama, no integrado en dev): [globalmart-frontend/tests/synchronized-card-payment.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1/globalmart-frontend/tests/synchronized-card-payment.test.cjs).

**Posible repregunta:** ¿Cómo reemplazarían la inspección AST del fallback por una comprobación observable?

---


### Problemas, dificultades y soluciones

<a id="g-41"></a>

**Pregunta N.º:** 41 (G-41)

**Categoría:** Problemas, dificultades y soluciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué problema encontró la primera regresión de carrito y qué avance posterior lo abordó?

**Respuesta modelo:** S2-025 registró un caso aprobado y dos fallidos: falta de respuesta de ventas y persistencia local al cambiar tenant. Después se integró el carrito con MS-5 y se incorporaron aislamiento y recuperación. Ese reporte es una línea base histórica, no una descripción automática de dev final; hay que contrastarlo con los commits posteriores y sus pruebas.

**Explicación para estudiar:** Una prueba fallida es información útil para priorizar. Conservarla permite explicar el antes y después sin ocultar el problema ni atribuirle vigencia indefinida.

**Evidencia del repositorio:** [a451b5d](https://github.com/rpedrazav/TallerIntegracion3/commit/a451b5d22e4632ae811e8adcf1814b9de98b20c2) (integrado en dev): [docs/qa/S2-025/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/a451b5d22e4632ae811e8adcf1814b9de98b20c2/docs/qa/S2-025/reporte.md); [160846e](https://github.com/rpedrazav/TallerIntegracion3/commit/160846eb68bac27937560baabb6a10d4ed545c73) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/160846eb68bac27937560baabb6a10d4ed545c73/globalmart-frontend/src/pages/Pos.tsx); [d44e22b](https://github.com/rpedrazav/TallerIntegracion3/commit/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e) (integrado en dev): [tests/GlobalMart.IntegrationTests/VentaTenantIsolationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/VentaTenantIsolationTests.cs).

**Posible repregunta:** ¿Qué diferencia existe entre recuperar una venta por ID y restaurar pos_cart?

---

<a id="g-42"></a>

**Pregunta N.º:** 42 (G-42)

**Categoría:** Problemas, dificultades y soluciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué dificultad apareció en las bajas y ediciones de usuarios?

**Respuesta modelo:** Existía una separación entre la proyección legacy users y Usuarios, utilizada por login. Cambiar la tabla equivocada podía mostrar una baja sin impedir autenticación. El cierre administrativo unificó operaciones sobre Usuarios y actualizó las pruebas. Es un ejemplo de integración donde el éxito HTTP debe contrastarse con el efecto real sobre el flujo posterior.

**Explicación para estudiar:** Dos representaciones de la misma entidad pueden divergir. La prueba decisiva debe recorrer creación, cambio y uso del usuario, además de verificar la fila.

**Evidencia del repositorio:** [bfd46b0](https://github.com/rpedrazav/TallerIntegracion3/commit/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1) (integrado en dev): [src/TenantIdentityService/Repositories/UsuarioRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1/src/TenantIdentityService/Repositories/UsuarioRepository.cs); [dbd2b09](https://github.com/rpedrazav/TallerIntegracion3/commit/dbd2b096d545cd5476821dd3a2baa4f420db2eca) (integrado en dev): [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/dbd2b096d545cd5476821dd3a2baa4f420db2eca/docs/context/estado-actual.md).

**Posible repregunta:** ¿Qué prueba demuestra que la cuenta desactivada no puede iniciar otra sesión?

---

<a id="g-43"></a>

**Pregunta N.º:** 43 (G-43)

**Categoría:** Problemas, dificultades y soluciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué problema de entorno bloqueó una validación remota de pagos?

**Respuesta modelo:** S2-052 no llegó a cobrar porque crear la venta devolvió 502: MS-5 intentaba contactar catálogo en localhost:5203. Las pruebas locales de rechazo y timeout sí tuvieron evidencia con PostgreSQL temporal y simulador real. Se registró el bloqueo remoto sin presentar esos resultados locales como aceptación del clúster.

**Explicación para estudiar:** Antes de atribuir un fallo a pagos hay que localizar la etapa. Un error de dependencia en creación impide sacar conclusiones sobre la autorización posterior.

**Evidencia del repositorio:** [25a40f5](https://github.com/rpedrazav/TallerIntegracion3/commit/25a40f52648694c51c2ffa429c065349c3046202) (integrado en dev): [docs/qa/S2-052/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/25a40f52648694c51c2ffa429c065349c3046202/docs/qa/S2-052/reporte.md); [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [docs/context/infraestructura/matriz-rutas.md](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/docs/context/infraestructura/matriz-rutas.md).

**Posible repregunta:** ¿Qué configuración revisarían dentro del contenedor de MS-5?

---

<a id="g-44"></a>

**Pregunta N.º:** 44 (G-44)

**Categoría:** Problemas, dificultades y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué fallo de validación pesable se encontró y cómo se corrigió?

**Respuesta modelo:** S2-065 detectó que crear una venta pesable sin peso todavía podía responder 201. Las reglas de agregar y editar no garantizaban el mismo control al crear. Se centralizó la validación en ReglasCantidadItem y se aplicó a los tres caminos; el reporte final registra 84 pruebas aprobadas con sus límites de entorno.

**Explicación para estudiar:** Duplicar reglas entre endpoints facilita diferencias accidentales. Una función compartida reduce divergencia, pero cada punto de entrada debe tener una prueba que demuestre que realmente la utiliza.

**Evidencia del repositorio:** [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [src/POSCartService/Services/ReglasCantidadItem.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/src/POSCartService/Services/ReglasCantidadItem.cs), [tests/GlobalMart.IntegrationTests/PesoFronterasTenantTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/tests/GlobalMart.IntegrationTests/PesoFronterasTenantTests.cs), [docs/qa/S2-065/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/docs/qa/S2-065/reporte.md).

**Posible repregunta:** ¿Qué caso de frontera detectaría un redondeo indebido de peso?

---

<a id="g-45"></a>

**Pregunta N.º:** 45 (G-45)

**Categoría:** Problemas, dificultades y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué se corrigió el tratamiento de Infinity y unidades desconocidas en las pruebas de balanza?

**Respuesta modelo:** Daniel encontró en su rama que comprobar solo isNaN no excluía Infinity en MockScaleAdapter y que la normalización debía rechazar unidades diferentes de g y kg en ejecución. Añadió Number.isFinite y una guardia explícita de unidad. Ese commit no es ancestro de dev final; es una corrección verificable pendiente de integración, no una garantía global ya entregada.

**Explicación para estudiar:** Un union type protege código tipado, pero datos externos o JavaScript pueden violarlo. Validar rango y unidad en ejecución evita magnitudes sin significado.

**Evidencia del repositorio:** [5850d61](https://github.com/rpedrazav/TallerIntegracion3/commit/5850d6111d0c5e9086bf6d156cc89fd7c9777dab) (rama, no integrado en dev): [globalmart-frontend/src/devices/adapters/MockScaleAdapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/5850d6111d0c5e9086bf6d156cc89fd7c9777dab/globalmart-frontend/src/devices/adapters/MockScaleAdapter.ts), [globalmart-frontend/src/hardware/simulated-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/5850d6111d0c5e9086bf6d156cc89fd7c9777dab/globalmart-frontend/src/hardware/simulated-scale-adapter.ts), [tests/GlobalMart.IntegrationTests/ScaleParserRoundingIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/5850d6111d0c5e9086bf6d156cc89fd7c9777dab/tests/GlobalMart.IntegrationTests/ScaleParserRoundingIntegrationTests.cs).

**Posible repregunta:** ¿Por qué un número finito con unidad lb tampoco debe aceptarse como kg?

---

<a id="g-46"></a>

**Pregunta N.º:** 46 (G-46)

**Categoría:** Problemas, dificultades y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo se gestionó la falta de capacidad de almacenamiento del clúster?

**Respuesta modelo:** El manifiesto final de MS-4 usa una base warehouse_db independiente en la instancia PostgreSQL existente del POS para respetar la cuota de PVC documentada. Se conservan bases separadas lógicamente, aunque comparten instancia y sus riesgos operativos. Además se deshabilita el consumidor por falta de broker. Es una adaptación del despliegue académico con límites que deben explicarse.

**Explicación para estudiar:** Separación de base y separación de servidor físico no son equivalentes. Compartir instancia reduce recursos, pero acopla disponibilidad, mantenimiento y capacidad.

**Evidencia del repositorio:** [dbd2b09](https://github.com/rpedrazav/TallerIntegracion3/commit/dbd2b096d545cd5476821dd3a2baa4f420db2eca) (integrado en dev): [k8s/manifests/warehouse-inventory.yaml](https://github.com/rpedrazav/TallerIntegracion3/blob/dbd2b096d545cd5476821dd3a2baa4f420db2eca/k8s/manifests/warehouse-inventory.yaml), [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/dbd2b096d545cd5476821dd3a2baa4f420db2eca/docs/context/estado-actual.md).

**Posible repregunta:** ¿Qué cambiarían al disponer de recursos para aislamiento operativo?

---

<a id="g-47"></a>

**Pregunta N.º:** 47 (G-47)

**Categoría:** Problemas, dificultades y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué dificultad plantea conservar el precio histórico al editar peso?

**Respuesta modelo:** Se debe recalcular con el precio snapshot persistido, no reemplazarlo por el precio actual del catálogo. La implementación consulta catálogo para saber si el producto es pesable, pero conserva el precio de la línea. Si catálogo o impuestos falla, debe impedirse la mutación. Queda una dependencia: el tipo pesable no tiene snapshot propio en el ítem.

**Explicación para estudiar:** La venta guarda hechos históricos mientras el catálogo es mutable. Elegir qué atributos se congelan determina reproducibilidad y disponibilidad al editar.

**Evidencia del repositorio:** [15dc321](https://github.com/rpedrazav/TallerIntegracion3/commit/15dc32170a5b8cb8a2e006fd7f9f552b0b4e9251) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/15dc32170a5b8cb8a2e006fd7f9f552b0b4e9251/src/POSCartService/Controllers/VentasController.cs), [tests/GlobalMart.IntegrationTests/ModificarPesoIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/15dc32170a5b8cb8a2e006fd7f9f552b0b4e9251/tests/GlobalMart.IntegrationTests/ModificarPesoIntegrationTests.cs); [ef6a406](https://github.com/rpedrazav/TallerIntegracion3/commit/ef6a40632bc975c961d289a6d41c7a67cc85d56d) (integrado en dev): [src/POSCartService/Data/PosCartDbContext.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ef6a40632bc975c961d289a6d41c7a67cc85d56d/src/POSCartService/Data/PosCartDbContext.cs).

**Posible repregunta:** ¿Qué ocurriría si el catálogo cambia EsPesoVariable después de crear la venta?

---

<a id="g-48"></a>

**Pregunta N.º:** 48 (G-48)

**Categoría:** Problemas, dificultades y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo se recupera una aprobación externa si falla la persistencia local?

**Respuesta modelo:** La reserva ya existe antes de contactar la pasarela. Si la aprobación no puede confirmarse localmente, la transacción revierte pago, estado y outbox, pero queda un intento recuperable. La consulta posterior usa la misma clave y puede completar la venta sin otro POST de autorización. El merge de pagos incluye una prueba de esa recuperación con un solo POST externo.

**Explicación para estudiar:** La llamada externa no participa de la transacción PostgreSQL. Una reserva durable y una consulta idempotente permiten reconciliar dos sistemas que pudieron confirmar en momentos distintos.

**Evidencia del repositorio:** [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md), [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/src/POSCartService/Repositories/VentaRepository.cs), [tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs).

**Posible repregunta:** ¿Por qué sería peligroso borrar el intento tras el rollback?

---

<a id="g-49"></a>

**Pregunta N.º:** 49 (G-49)

**Categoría:** Problemas, dificultades y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Puede el publicador perdido recuperar una reserva sin que dos workers sobrescriban el resultado?

**Respuesta modelo:** OutboxRepository reserva con bloqueo y lease, y las actualizaciones comparan tenant, evento, estado y LeaseToken. Una reserva vencida puede recuperarse, mientras el dueño antiguo pierde derecho a finalizarla. Eso reduce carreras de persistencia, pero no elimina una posible entrega duplicada al broker si el primer envío sí ocurrió. El consumidor debe tolerar reentregas.

**Explicación para estudiar:** Un lease concede propiedad temporal, no una prueba de que ningún efecto externo ocurrió. El token de reserva evita que un propietario obsoleto escriba sobre el nuevo.

**Evidencia del repositorio:** [4cf822c](https://github.com/rpedrazav/TallerIntegracion3/commit/4cf822c347c18d987282a31b5babe3e2f3582a31) (integrado en dev): [src/POSCartService/Repositories/OutboxRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Repositories/OutboxRepository.cs), [src/POSCartService/Services/OutboxPublisherService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Services/OutboxPublisherService.cs).

**Posible repregunta:** ¿Qué diferencia hay entre EventId y LeaseToken?

---

<a id="g-50"></a>

**Pregunta N.º:** 50 (G-50)

**Categoría:** Problemas, dificultades y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué riesgo queda en la recuperación del consumidor de inventario tras un fallo parcial?

**Respuesta modelo:** El consumidor descuenta ítems antes de guardar el marcador y confirma el offset incluso si el procesamiento falla. Una caída entre esos pasos puede dejar stock parcial o perder una oportunidad de reintento. La solución futura debería coordinar descuento y deduplicación en una transacción, y definir reintentos y tratamiento de mensajes fallidos; no basta con la tabla de eventos procesados.

**Explicación para estudiar:** El orden de commit de base y offset determina pérdida o duplicación. Es necesario razonar sobre fallos entre cada efecto y probarlos explícitamente.

**Evidencia del repositorio:** [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs); [054d7d3](https://github.com/rpedrazav/TallerIntegracion3/commit/054d7d382ca6a91967e96dc885b9cfb6b1558c68) (integrado en dev): [docs/context/eventos/sale-completed-outbox.md](https://github.com/rpedrazav/TallerIntegracion3/blob/054d7d382ca6a91967e96dc885b9cfb6b1558c68/docs/context/eventos/sale-completed-outbox.md).

**Posible repregunta:** ¿Qué harían con un evento cuyo segundo producto no puede descontarse?

---


### Progresos, logros, resultados y mejoras futuras

<a id="g-51"></a>

**Pregunta N.º:** 51 (G-51)

**Categoría:** Progresos, logros, resultados y mejoras futuras

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cuál es el avance más visible del POS frente al comienzo del período?

**Respuesta modelo:** Ahora puede representar una venta persistida en MS-5, actualizar y eliminar ítems, mostrar totales del servidor, cobrar efectivo y coordinar tarjeta simulada. Además incorpora captura de peso y respaldo manual. El avance verificable es pasar de interacción local a contratos de negocio conectados, manteniendo explícitas las limitaciones de despliegue y las funciones todavía pendientes.

**Explicación para estudiar:** La mejora debe medirse con un recorrido y sus invariantes, no con cantidad de pantallas. Persistencia y recuperación hacen observable la diferencia.

**Evidencia del repositorio:** [160846e](https://github.com/rpedrazav/TallerIntegracion3/commit/160846eb68bac27937560baabb6a10d4ed545c73) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/160846eb68bac27937560baabb6a10d4ed545c73/globalmart-frontend/src/pages/Pos.tsx); [9dedcc5](https://github.com/rpedrazav/TallerIntegracion3/commit/9dedcc50684998bb0791c214e7947939c0503021) (integrado en dev): [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/9dedcc50684998bb0791c214e7947939c0503021/globalmart-frontend/src/api/client.ts); [4bde899](https://github.com/rpedrazav/TallerIntegracion3/commit/4bde899fce12fc6d005100d7585ec27cfbda8024) (integrado en dev): [globalmart-frontend/e2e/weight-cart.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/4bde899fce12fc6d005100d7585ec27cfbda8024/globalmart-frontend/e2e/weight-cart.spec.ts).

**Posible repregunta:** ¿Qué demostrarían recargando el POS después de crear una venta?

---

<a id="g-52"></a>

**Pregunta N.º:** 52 (G-52)

**Categoría:** Progresos, logros, resultados y mejoras futuras

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué resultados de pruebas pueden citar sin confundirlos con una ejecución de esta revisión?

**Respuesta modelo:** Podemos citar los reportes versionados: por ejemplo 54/54 en el merge de pagos, 3/3 en S2-053 y 84/84 en S2-065. El cierre administrativo documenta 334/334 y cinco E2E. Son resultados históricos con sus entornos y fechas; este banco no vuelve a ejecutar la aplicación ni convierte los mensajes de commit en logs independientes.

**Explicación para estudiar:** Un resultado debe acompañarse de suite, revisión, entorno y límites. No se suman corridas de regresión que repiten los mismos casos para inventar cobertura.

**Evidencia del repositorio:** [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md); [abfd1f0](https://github.com/rpedrazav/TallerIntegracion3/commit/abfd1f0e1050027701674889775d2f28c19e780a) (integrado en dev): [docs/qa/S2-053/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/abfd1f0e1050027701674889775d2f28c19e780a/docs/qa/S2-053/reporte.md); [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [docs/qa/S2-065/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/docs/qa/S2-065/reporte.md); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (integrado en dev): [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/docs/context/estado-actual.md).

**Posible repregunta:** ¿Qué evidencia es más fuerte: un mensaje de commit o un TRX con contexto?

---

<a id="g-53"></a>

**Pregunta N.º:** 53 (G-53)

**Categoría:** Progresos, logros, resultados y mejoras futuras

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué entregables individuales hacen posible una demostración grupal coherente?

**Respuesta modelo:** Sebastián aporta reglas y persistencia del cobro; Nicolás, outbox y adaptadores; Martín, administración y terminal; Rodrigo, integración visual y cierre administrativo; Daniel, fixtures y validación; Diego, rutas, entorno y observabilidad. Son focos reconstruidos desde cambios, no exclusividades rígidas. La demostración debe enlazarlos desde turno hasta resultado y señalar qué pasos usan simulación.

**Explicación para estudiar:** La contribución grupal aparece en las interfaces entre trabajos. Conocer solo el propio archivo impide explicar por qué el recorrido completo es consistente.

**Evidencia del repositorio:** [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md); [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [docs/context/infraestructura/matriz-rutas.md](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/docs/context/infraestructura/matriz-rutas.md); [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) (integrado en dev): [globalmart-frontend/src/payments/synchronizeCardPayment.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/src/payments/synchronizeCardPayment.ts); [00572cf](https://github.com/rpedrazav/TallerIntegracion3/commit/00572cfb5054e43bea900b06b4baef43328b34c7) (integrado en dev): [tests/GlobalMart.IntegrationTests/Fixtures/TwoTenantFixture.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/00572cfb5054e43bea900b06b4baef43328b34c7/tests/GlobalMart.IntegrationTests/Fixtures/TwoTenantFixture.cs).

**Posible repregunta:** ¿Quién puede explicar el recorrido del identificador de intento entre todas las capas?

---

<a id="g-54"></a>

**Pregunta N.º:** 54 (G-54)

**Categoría:** Progresos, logros, resultados y mejoras futuras

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué pendientes deberían reconocer sin minimizar los logros?

**Respuesta modelo:** En dev no hay endpoint HTTP de anulación, ni reembolso, ni pago mixto completo. La revocación inmediata de JWT, la unidad real de catálogo para kg, el reproceso manual de outbox fallido y la validación física siguen pendientes o no verificadas. También falta integrar ciertos commits de pruebas y cerrar la aceptación hasta stock en el entorno desplegado.

**Explicación para estudiar:** Reconocer una brecha precisa demuestra comprensión del alcance. Debe indicarse su impacto y la evidencia que permitiría cerrarla, no solo decir que falta mejorar.

**Evidencia del repositorio:** [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/POSCartService/Controllers/VentasController.cs), [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/docs/context/estado-actual.md), [docs/context/dominio/peso-variable.md](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/docs/context/dominio/peso-variable.md); [4cf822c](https://github.com/rpedrazav/TallerIntegracion3/commit/4cf822c347c18d987282a31b5babe3e2f3582a31) (integrado en dev): [docs/context/eventos/sale-completed-outbox.md](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/docs/context/eventos/sale-completed-outbox.md).

**Posible repregunta:** ¿Cuál de esos pendientes afecta directamente una condición del plan G2?

---

<a id="g-55"></a>

**Pregunta N.º:** 55 (G-55)

**Categoría:** Progresos, logros, resultados y mejoras futuras

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué mejora priorizarían en la documentación después de este análisis?

**Respuesta modelo:** Alinear el nodo sprint-actual y resolver contradicciones internas sobre administración, inventario y pruebas. Por ejemplo, hay apartados que todavía dicen que una función está planificada aunque las secciones recientes y el código la implementan. Actualizaríamos solo los nodos afectados y sus fechas, citando código y evidencia, sin convertir este banco en una modificación del comportamiento.

**Explicación para estudiar:** La documentación es parte del sistema de verificación. Una afirmación vieja puede inducir una defensa incorrecta aunque el código actual sea válido.

**Evidencia del repositorio:** [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/docs/context/estado-actual.md), [docs/context/planificacion/sprint-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/docs/context/planificacion/sprint-actual.md); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (integrado en dev): [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/docs/context/estado-actual.md).

**Posible repregunta:** ¿Cómo impedirían que una nueva tarea deje dos estados contradictorios en el mismo nodo?

---

<a id="g-56"></a>

**Pregunta N.º:** 56 (G-56)

**Categoría:** Progresos, logros, resultados y mejoras futuras

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo mejorarían la aceptación de hardware en el próximo sprint?

**Respuesta modelo:** Primero confirmaríamos modelo, manual, transporte y características de medición; luego implementaríamos el parser específico y ejecutaríamos el protocolo físico con capturas. Reutilizaríamos tramas reales como fixtures sin reemplazar la prueba con equipo. El mínimo de 10 g y la resolución de 1 g son decisiones del simulador, no capacidades físicas demostradas.

**Explicación para estudiar:** Las pruebas reales y las deterministas se complementan: unas descubren comportamiento del dispositivo y las otras preservan regresiones reproducibles.

**Evidencia del repositorio:** [8c20fee](https://github.com/rpedrazav/TallerIntegracion3/commit/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98) (rama, no integrado en dev): [docs/qa/S2-085/protocolo-balanza-fisica.md](https://github.com/rpedrazav/TallerIntegracion3/blob/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98/docs/qa/S2-085/protocolo-balanza-fisica.md), [docs/qa/S2-085/capturar-balanza.ps1](https://github.com/rpedrazav/TallerIntegracion3/blob/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98/docs/qa/S2-085/capturar-balanza.ps1); [0afc855](https://github.com/rpedrazav/TallerIntegracion3/commit/0afc855588582f8294644160dff4f418f40b2683) (integrado en dev): [globalmart-frontend/src/hardware/serial-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/src/hardware/serial-scale-adapter.ts).

**Posible repregunta:** ¿Qué harían si el equipo tiene un mínimo superior al contrato de software?

---

<a id="g-57"></a>

**Pregunta N.º:** 57 (G-57)

**Categoría:** Progresos, logros, resultados y mejoras futuras

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué mejorarían en seguridad de sesión después de la baja lógica?

**Respuesta modelo:** El siguiente login ya puede rechazar usuarios inactivos, pero un JWT firmado y no expirado puede seguir aceptándose. Una mejora sería incorporar revocación o validación de versión de sesión y probar sus efectos distribuidos. Debemos presentarla como propuesta, no como función existente, y evaluar disponibilidad y costo de consultar estado en cada servicio.

**Explicación para estudiar:** JWT autocontenido facilita validación local; revocar antes de expirar introduce estado o ciclos de renovación más cortos. La elección tiene consecuencias operativas.

**Evidencia del repositorio:** [bfd46b0](https://github.com/rpedrazav/TallerIntegracion3/commit/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1) (integrado en dev): [src/TenantIdentityService/Repositories/UsuarioRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1/src/TenantIdentityService/Repositories/UsuarioRepository.cs); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (integrado en dev): [src/TenantIdentityService/Services/JwtService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/TenantIdentityService/Services/JwtService.cs), [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/docs/context/estado-actual.md).

**Posible repregunta:** ¿Qué pasaría con sesiones existentes después de quitar el rol ADMIN?

---

<a id="g-58"></a>

**Pregunta N.º:** 58 (G-58)

**Categoría:** Progresos, logros, resultados y mejoras futuras

**Dificultad:** Intermedia.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Cómo medirían el próximo avance sin usar solo número de commits?

**Respuesta modelo:** Compararíamos criterios de aceptación cerrados con evidencia reproducible: anulación accesible, reintentos seguros, descuento único bajo fallos y escenarios de aislamiento. También registraríamos qué pruebas pasan en la revisión integrada y qué dependencias fueron reales. Las 120 horas siguen siendo capacidad planificada, y la experiencia del equipo sobre esfuerzo o dificultades debe confirmarse con registros personales.

**Explicación para estudiar:** Un indicador útil representa resultado y riesgo reducido. Más actividad puede producir menos funcionalidad aceptada si no se integran los cambios.

**Evidencia del repositorio:** [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) (integrado en dev): [tareas/plan_sprint2_revisado.xlsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/plan_sprint2_revisado.xlsx); [806450b](https://github.com/rpedrazav/TallerIntegracion3/commit/806450bec2bda6cee096bb62cbc02b204d5daae9) (integrado en dev): [diagramas/matriz/S2-021-matriz-aceptacion.md](https://github.com/rpedrazav/TallerIntegracion3/blob/806450bec2bda6cee096bb62cbc02b204d5daae9/diagramas/matriz/S2-021-matriz-aceptacion.md); [abfd1f0](https://github.com/rpedrazav/TallerIntegracion3/commit/abfd1f0e1050027701674889775d2f28c19e780a) (integrado en dev): [docs/qa/S2-053/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/abfd1f0e1050027701674889775d2f28c19e780a/docs/qa/S2-053/reporte.md).

**Posible repregunta:** ¿Qué indicador mostraría mejora real en recuperación de pagos?

---

<a id="g-59"></a>

**Pregunta N.º:** 59 (G-59)

**Categoría:** Progresos, logros, resultados y mejoras futuras

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué experimento diseñarían para demostrar recuperación completa desde cobro hasta inventario?

**Respuesta modelo:** Usaríamos una venta y stock conocidos, Kafka real y consumidores observables. Interrumpiríamos después del commit del cobro, después del envío y durante el descuento; al recuperar, compararíamos pago, EventId, outbox, marcador y stock. La aceptación exigiría un único efecto de negocio y ausencia de pérdida, con evidencia por tenant. Hoy esa conclusión completa no está demostrada.

**Explicación para estudiar:** La prueba debe inyectar fallos en las ventanas entre sistemas, no solo simular un productor que falla antes de enviar. La identidad estable permite correlacionar todas las observaciones.

**Evidencia del repositorio:** [731a23f](https://github.com/rpedrazav/TallerIntegracion3/commit/731a23fc0f794be061a12882952ad4d003757cac) (integrado en dev): [tests/GlobalMart.IntegrationTests/OutboxBrokerRecoveryIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/731a23fc0f794be061a12882952ad4d003757cac/tests/GlobalMart.IntegrationTests/OutboxBrokerRecoveryIntegrationTests.cs); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs); [054d7d3](https://github.com/rpedrazav/TallerIntegracion3/commit/054d7d382ca6a91967e96dc885b9cfb6b1558c68) (integrado en dev): [docs/context/eventos/sale-completed-outbox.md](https://github.com/rpedrazav/TallerIntegracion3/blob/054d7d382ca6a91967e96dc885b9cfb6b1558c68/docs/context/eventos/sale-completed-outbox.md).

**Posible repregunta:** ¿Cómo distinguirían duplicado de transporte de descuento duplicado?

---

<a id="g-60"></a>

**Pregunta N.º:** 60 (G-60)

**Categoría:** Progresos, logros, resultados y mejoras futuras

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** Si tuvieran que defender el sprint en cinco minutos, ¿qué demostrarían y cómo acotarían la conclusión?

**Respuesta modelo:** Mostraríamos turno, venta con precio e IVA del servidor, un pesable confirmado y tarjeta simulada con recuperación o rechazo. Después presentaríamos un caso RN-01 y una evidencia de outbox. Cerraríamos con administración y las brechas de anulación, consumidor y hardware. Elegir ese recorrido es una propuesta basada en riesgos y metas del repositorio, no conocimiento de las intenciones del profesor.

**Explicación para estudiar:** Una defensa breve debe conectar objetivo, implementación, prueba y límite. Esa cadena permite responder repreguntas sin depender de memorizar una lista de tecnologías.

**Evidencia del repositorio:** [806450b](https://github.com/rpedrazav/TallerIntegracion3/commit/806450bec2bda6cee096bb62cbc02b204d5daae9) (integrado en dev): [diagramas/matriz/S2-021-matriz-aceptacion.md](https://github.com/rpedrazav/TallerIntegracion3/blob/806450bec2bda6cee096bb62cbc02b204d5daae9/diagramas/matriz/S2-021-matriz-aceptacion.md); [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md); [4bde899](https://github.com/rpedrazav/TallerIntegracion3/commit/4bde899fce12fc6d005100d7585ec27cfbda8024) (integrado en dev): [globalmart-frontend/e2e/weight-cart.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/4bde899fce12fc6d005100d7585ec27cfbda8024/globalmart-frontend/e2e/weight-cart.spec.ts); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (integrado en dev): [tests/GlobalMart.IntegrationTests/AdminCustomRolesIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/tests/GlobalMart.IntegrationTests/AdminCustomRolesIntegrationTests.cs).

**Posible repregunta:** ¿Qué evidencia alternativa usarían si el clúster no está disponible durante la defensa?

---


## 5. Banco de 30 preguntas — Sebastián

**Registro:** 30 preguntas; 10 básicas, 12 intermedias, 8 avanzadas. Numeración única en este banco.

Las respuestas describen el código o la revisión enlazada. Los resultados de ejecución son históricos. “Rama, no integrado” identifica evidencia que no forma parte de dev al corte. Las experiencias personales deben ser confirmadas por el integrante.


### Tareas, responsabilidades y contribuciones

<a id="s-01"></a>

**Pregunta N.º:** 1 (S-01)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué partes del Sprint 2 puedes atribuir a tus propios commits de MS-5?

**Respuesta modelo:** Mis cambios cubren contrato y creación autoritativa de venta, restricción del cobro efectivo, persistencia y seguridad del intento de tarjeta y reglas de peso variable. Agregué pruebas y participé en el merge de pagos. No atribuiría a mi trabajo toda la pasarela ni todo el frontend: esos componentes tienen aportes de Nicolás y Rodrigo.

**Explicación para estudiar:** La evidencia personal se reconstruye desde diffs y pruebas, no desde el servicio completo donde se trabajó.

**Evidencia del repositorio:** [a5d998d](https://github.com/rpedrazav/TallerIntegracion3/commit/a5d998dd049ad216bd5c0336523fcba6dc1c76de) (integrado en dev): [src/POSCartService/DTOs/CrearVentaRequest.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/a5d998dd049ad216bd5c0336523fcba6dc1c76de/src/POSCartService/DTOs/CrearVentaRequest.cs); [212cc56](https://github.com/rpedrazav/TallerIntegracion3/commit/212cc56b8ffe0c44dc07650a40b01ebc2996f215) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/212cc56b8ffe0c44dc07650a40b01ebc2996f215/src/POSCartService/Repositories/VentaRepository.cs); [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [src/POSCartService/Services/ReglasCantidadItem.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/src/POSCartService/Services/ReglasCantidadItem.cs).

**Posible repregunta:** ¿Qué archivo muestra una contribución tuya en cada una de esas etapas?

---

<a id="s-02"></a>

**Pregunta N.º:** 2 (S-02)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué entregaste en S2-001 y qué todavía dependía de S2-002?

**Respuesta modelo:** S2-001 ajustó el contrato para aceptar producto, cantidad y peso sin exigir que el cliente definiera el precio real. Pero declarar la autoridad del servidor no implementaba por sí solo la consulta a catálogo. Esa resolución y el cálculo inicial con MS-2 llegaron en 152f39b. Debo explicar esa evolución sin atribuir el comportamiento final al primer commit aislado.

**Explicación para estudiar:** Un contrato expresa una obligación; su cumplimiento debe encontrarse en código ejecutable. Leer el comentario y seguir la variable hasta persistencia evita confundir ambos.

**Evidencia del repositorio:** [a5d998d](https://github.com/rpedrazav/TallerIntegracion3/commit/a5d998dd049ad216bd5c0336523fcba6dc1c76de) (integrado en dev): [src/POSCartService/DTOs/CrearVentaRequest.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/a5d998dd049ad216bd5c0336523fcba6dc1c76de/src/POSCartService/DTOs/CrearVentaRequest.cs), [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/a5d998dd049ad216bd5c0336523fcba6dc1c76de/src/POSCartService/Controllers/VentasController.cs); [152f39b](https://github.com/rpedrazav/TallerIntegracion3/commit/152f39b00076de93573a261f9721ad1c29eb3c5c) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Controllers/VentasController.cs).

**Posible repregunta:** ¿Dónde dejó de utilizarse el precio enviado al construir ItemVenta?

---

<a id="s-03"></a>

**Pregunta N.º:** 3 (S-03)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Por qué b200256 es contribución documental y no una segunda implementación de IVA?

**Respuesta modelo:** Ese commit documenta S2-003 en el grafo. La consulta a MS-2 y el uso de subtotal, impuestos y total ya están en 152f39b. Reconocerlo evita contar dos veces el mismo avance: uno implementa el comportamiento y otro registra su estado para el equipo. Ambos son útiles, pero respaldan afirmaciones distintas.

**Explicación para estudiar:** La unidad de avance no siempre coincide con la unidad commit. Un cambio de documentación puede acreditar trazabilidad sin agregar una función nueva.

**Evidencia del repositorio:** [b200256](https://github.com/rpedrazav/TallerIntegracion3/commit/b20025693dbd44553a9d74718ec2cd1c3093c0e7) (integrado en dev): [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/b20025693dbd44553a9d74718ec2cd1c3093c0e7/docs/context/estado-actual.md); [152f39b](https://github.com/rpedrazav/TallerIntegracion3/commit/152f39b00076de93573a261f9721ad1c29eb3c5c) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Controllers/VentasController.cs), [src/POSCartService/Services/VentaService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Services/VentaService.cs).

**Posible repregunta:** ¿Cómo verificarías esa diferencia sin confiar en los títulos de los commits?

---

<a id="s-04"></a>

**Pregunta N.º:** 4 (S-04)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué responsabilidad asumiste al integrar tu cobro seguro con la recuperación de Rodrigo?

**Respuesta modelo:** El merge conservó mi reserva antes de HTTP y la reconciliación por consulta de Rodrigo. Debía evitar dos reservas, mantener condiciones de actualización y adaptar pruebas sin eliminar escenarios. El reporte explica cinco archivos en conflicto y la decisión de conservar PENDIENTE ante incertidumbre. Los acuerdos personales sobre cómo coordinamos esto debo confirmarlos, porque Git no registra la conversación.

**Explicación para estudiar:** Integrar seguridad requiere preservar invariantes de ambas ramas. Que el código compile no demuestra que el flujo mantenga el mismo número de autorizaciones.

**Evidencia del repositorio:** [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md), [tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs).

**Posible repregunta:** ¿Qué prueba demuestra una recuperación posterior sin un segundo POST?

---

<a id="s-05"></a>

**Pregunta N.º:** 5 (S-05)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué tareas de peso variable aparecen en tu tramo final del sprint?

**Respuesta modelo:** Definí el contrato S2-061, validé agregado, preservé precisión y cálculo en servidor, permití editar peso y probé fronteras e aislamiento. S2-065 también corrigió creación sin peso y centralizó reglas. Mi aporte corresponde al contrato y backend; lectura de balanza, IPC y confirmación visual tienen autores y pruebas diferentes.

**Explicación para estudiar:** Un flujo pesable necesita reglas consistentes al crear, agregar y modificar. Cubrir solo uno deja entradas alternativas con comportamiento distinto.

**Evidencia del repositorio:** [e61d618](https://github.com/rpedrazav/TallerIntegracion3/commit/e61d618cdaa1a9eb7074c94d97ecca3de0c8b148) (integrado en dev): [docs/context/dominio/peso-variable.md](https://github.com/rpedrazav/TallerIntegracion3/blob/e61d618cdaa1a9eb7074c94d97ecca3de0c8b148/docs/context/dominio/peso-variable.md); [e5141df](https://github.com/rpedrazav/TallerIntegracion3/commit/e5141df2c4a485c2fb34aa2ef22c7df4d51fdc29) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/e5141df2c4a485c2fb34aa2ef22c7df4d51fdc29/src/POSCartService/Controllers/VentasController.cs); [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [src/POSCartService/Services/ReglasCantidadItem.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/src/POSCartService/Services/ReglasCantidadItem.cs).

**Posible repregunta:** ¿Qué tres endpoints utilizan ReglasCantidadItem?

---


### Funcionalidades implementadas y explicación del código

<a id="s-06"></a>

**Pregunta N.º:** 6 (S-06)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** Explícame cómo tu endpoint de creación obtiene la sucursal y el cajero.

**Respuesta modelo:** El cajero y tenant se extraen del JWT validado. Se consulta su turno abierto y se usa la sucursal de ese turno. No se acepta una sucursal arbitraria del cliente para construir la venta. Si falta turno, se responde conflicto antes de persistir. Esto conecta autenticación con RN-06 y evita que el renderer elija la pertenencia de la operación.

**Explicación para estudiar:** Identidad autenticada y datos del request tienen niveles de confianza distintos. La sucursal es contexto de caja y no un dato comercial libre.

**Evidencia del repositorio:** [152f39b](https://github.com/rpedrazav/TallerIntegracion3/commit/152f39b00076de93573a261f9721ad1c29eb3c5c) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Controllers/VentasController.cs); [1833981](https://github.com/rpedrazav/TallerIntegracion3/commit/18339813f950b4becfffcef7d046e7907172dff0) (integrado en dev): [tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/18339813f950b4becfffcef7d046e7907172dff0/tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs).

**Posible repregunta:** ¿Por qué ocultar el POS sin turno no reemplaza esta validación backend?

---

<a id="s-07"></a>

**Pregunta N.º:** 7 (S-07)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué hace tu código cuando uno de varios productos no existe?

**Respuesta modelo:** Recorre los productos y consulta MS-3 antes de guardar la venta. Si alguno no se encuentra, retorna 404; si recibe un producto explícitamente inactivo, lo rechaza. No debe persistir una venta parcial ni sus ítems. La prueba consulta la base para confirmar ausencia de ventas, además del código de respuesta.

**Explicación para estudiar:** Validar todo antes de persistir evita aceptar parcialmente una solicitud que el cliente esperaba atómica. Hay que comprobar efectos, no solo errores.

**Evidencia del repositorio:** [152f39b](https://github.com/rpedrazav/TallerIntegracion3/commit/152f39b00076de93573a261f9721ad1c29eb3c5c) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Controllers/VentasController.cs), [tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs); [4bb39aa](https://github.com/rpedrazav/TallerIntegracion3/commit/4bb39aa947e6e7586d437b8ab490c1a99c03f02e) (integrado en dev): [src/CatalogPricingService/Data/ProductoRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4bb39aa947e6e7586d437b8ab490c1a99c03f02e/src/CatalogPricingService/Data/ProductoRepository.cs).

**Posible repregunta:** ¿Qué respuesta puede producir MS-3 para un producto desactivado desde el cambio de Martín?

---

<a id="s-08"></a>

**Pregunta N.º:** 8 (S-08)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo llega el IVA correcto a la primera venta sin editar después un ítem?

**Respuesta modelo:** Construyo la lista de líneas con precios de MS-3 y llamo a TaxClient antes de CrearVentaAsync. El servicio recibe los totales calculados por MS-2 y los persiste. Antes se usaba un porcentaje inicial cero. Si impuestos no responde, se devuelve un error y no se confirma una venta con un IVA inventado o provisional.

**Explicación para estudiar:** El orden de operaciones determina qué invariantes existen al guardar. Calcular después puede dejar un estado persistido incorrecto aunque la pantalla lo corrija luego.

**Evidencia del repositorio:** [152f39b](https://github.com/rpedrazav/TallerIntegracion3/commit/152f39b00076de93573a261f9721ad1c29eb3c5c) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Controllers/VentasController.cs), [src/POSCartService/Services/VentaService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Services/VentaService.cs), [tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs).

**Posible repregunta:** ¿Qué diferencia hay entre el handler controlado de una prueba y MS-2 desplegado?

---

<a id="s-09"></a>

**Pregunta N.º:** 9 (S-09)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué validaste EFECTIVO tanto en el controlador como en VentaService?

**Respuesta modelo:** El controlador entrega un error HTTP claro para TARJETA o MIXTO. La guardia del servicio impide que un llamador interno utilice CompletarAsync para registrar efectivo sobre otro método. Así no depende todo de una única entrada HTTP. Las pruebas comprueban que el rechazo no crea pagos y mantiene el estado de la venta.

**Explicación para estudiar:** Las invariantes de negocio deben sobrevivir a nuevos puntos de entrada. Una validación de presentación no protege una invocación directa del servicio.

**Evidencia del repositorio:** [c86909c](https://github.com/rpedrazav/TallerIntegracion3/commit/c86909ca3c944da0c185da90a66e3f2be1881197) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/c86909ca3c944da0c185da90a66e3f2be1881197/src/POSCartService/Controllers/VentasController.cs), [src/POSCartService/Services/VentaService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/c86909ca3c944da0c185da90a66e3f2be1881197/src/POSCartService/Services/VentaService.cs), [tests/GlobalMart.IntegrationTests/CobroIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/c86909ca3c944da0c185da90a66e3f2be1881197/tests/GlobalMart.IntegrationTests/CobroIntegrationTests.cs).

**Posible repregunta:** ¿Sería correcto cambiar el método a efectivo automáticamente para evitar el error?

---

<a id="s-10"></a>

**Pregunta N.º:** 10 (S-10)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué datos persiste IntentoPago y por qué son necesarios?

**Respuesta modelo:** Persiste tenant, venta, clave de idempotencia, monto, moneda, estado y referencias opacas del resultado, además de fechas. No requiere PAN ni CVV. La clave relaciona la reserva con la autorización y la consulta posterior. El índice único por tenant y clave evita duplicados con esa identidad; el control por venta agrega protección frente a otra clave.

**Explicación para estudiar:** Intento y pago no son lo mismo: puede existir un intento pendiente o rechazado sin que haya un pago confirmado.

**Evidencia del repositorio:** [ce97fe3](https://github.com/rpedrazav/TallerIntegracion3/commit/ce97fe3683cc86096a3a2abc8bdd2e630af1c92d) (integrado en dev): [src/POSCartService/Models/IntentoPago.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ce97fe3683cc86096a3a2abc8bdd2e630af1c92d/src/POSCartService/Models/IntentoPago.cs), [src/POSCartService/Data/PosCartDbContext.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ce97fe3683cc86096a3a2abc8bdd2e630af1c92d/src/POSCartService/Data/PosCartDbContext.cs); [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/src/POSCartService/Repositories/VentaRepository.cs).

**Posible repregunta:** ¿Qué estado debe conservarse después de un timeout ambiguo?

---

<a id="s-11"></a>

**Pregunta N.º:** 11 (S-11)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué no basta el índice único de IdempotencyKey para evitar dos cobros?

**Respuesta modelo:** Dos solicitudes pueden usar claves diferentes para la misma venta y ambas cumplir la unicidad. ReservarIntentoPagoAsync bloquea la fila de venta y busca cualquier intento no rechazado, además de pagos y estado. Esa reserva serializa la decisión de autorizar en la base compartida. La llamada HTTP ocurre después de confirmar la reserva, sin mantener abierto el lock durante la red.

**Explicación para estudiar:** La identidad de una petición y la identidad de la operación comercial son dimensiones distintas. Hay que proteger ambas frente a concurrencia.

**Evidencia del repositorio:** [212cc56](https://github.com/rpedrazav/TallerIntegracion3/commit/212cc56b8ffe0c44dc07650a40b01ebc2996f215) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/212cc56b8ffe0c44dc07650a40b01ebc2996f215/src/POSCartService/Repositories/VentaRepository.cs); [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md).

**Posible repregunta:** ¿Qué sucede si dos ventas distintas usan la misma clave dentro del tenant?

---

<a id="s-12"></a>

**Pregunta N.º:** 12 (S-12)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué se confirma atómicamente cuando la pasarela aprueba?

**Respuesta modelo:** CompletarTarjetaConOutboxAsync verifica venta y tenant, cambia a COMPLETADA, agrega Pago, actualiza el intento reservado a APROBADO y agrega el outbox dentro de la transacción local. Si falla una escritura, se revierte ese conjunto. La autorización externa ya pudo ocurrir, por lo que se conserva la posibilidad de reconciliar el intento después.

**Explicación para estudiar:** Atomicidad local no implica rollback de una pasarela externa. La recuperación se diseña para el desfase entre ambos sistemas.

**Evidencia del repositorio:** [186f8d5](https://github.com/rpedrazav/TallerIntegracion3/commit/186f8d583d6a2a0e6d049784fea3885203f3c305) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/186f8d583d6a2a0e6d049784fea3885203f3c305/src/POSCartService/Repositories/VentaRepository.cs); [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md), [tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs).

**Posible repregunta:** ¿Qué evidencia evita confundir rollback local con devolución de dinero?

---

<a id="s-13"></a>

**Pregunta N.º:** 13 (S-13)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo decide ReglasCantidadItem la cantidad facturable de un pesable?

**Respuesta modelo:** Exige peso, mínimo 0,010 y múltiplos de 0,001 kg. Si también llega cantidad, debe ser igual al peso. Devuelve ese peso como cantidad facturable. Para no pesables rechaza peso y exige cantidad positiva, permitiendo el valor por defecto solo donde el llamador lo indica. No redondea un peso inválido para aceptarlo.

**Explicación para estudiar:** Cantidad expresa kg en este contrato pesable. Multiplicarla otra vez por peso duplicaría la magnitud y alteraría el subtotal.

**Evidencia del repositorio:** [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [src/POSCartService/Services/ReglasCantidadItem.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/src/POSCartService/Services/ReglasCantidadItem.cs).

**Posible repregunta:** ¿Cómo se trata 1,2340 frente a 1,2345?

---


### Decisiones técnicas, tecnologías y arquitectura

<a id="s-14"></a>

**Pregunta N.º:** 14 (S-14)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué utilizaste decimal para peso e importes en C#?

**Respuesta modelo:** Las reglas se expresan en incrementos decimales, como 0,001 kg, y los importes necesitan un redondeo definido. decimal permite operar con esa representación sin depender de los errores binarios típicos de float o double para estas magnitudes. Aun así debo fijar escala, rango y momento de redondeo; el tipo por sí solo no define el contrato.

**Explicación para estudiar:** Representación numérica y regla comercial son decisiones complementarias. Elegir decimal no sustituye validar precisión y conservar el snapshot.

**Evidencia del repositorio:** [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [src/POSCartService/Services/ReglasCantidadItem.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/src/POSCartService/Services/ReglasCantidadItem.cs); [ef6a406](https://github.com/rpedrazav/TallerIntegracion3/commit/ef6a40632bc975c961d289a6d41c7a67cc85d56d) (integrado en dev): [src/TaxComplianceService/Services/TaxCalculatorService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ef6a40632bc975c961d289a6d41c7a67cc85d56d/src/TaxComplianceService/Services/TaxCalculatorService.cs).

**Posible repregunta:** ¿Por qué el frontend necesita una tolerancia distinta al comprobar gramos?

---

<a id="s-15"></a>

**Pregunta N.º:** 15 (S-15)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué decisión llevó a migrar PrecioUnitario a numeric(20,4)?

**Respuesta modelo:** El catálogo conserva cuatro decimales, pero la columna anterior podía perderlos. Amplié la escala manteniendo capacidad de parte entera para guardar el precio snapshot antes de multiplicarlo por peso. La migración preserva nuevos valores con precisión; no recupera decimales ya redondeados. Volver a dos decimales puede modificar valores nuevos, por lo que el rollback tiene impacto.

**Explicación para estudiar:** La precisión de una columna afecta cálculos futuros. Una migración aparentemente pequeña puede cambiar fidelidad histórica y reversibilidad de datos.

**Evidencia del repositorio:** [ef6a406](https://github.com/rpedrazav/TallerIntegracion3/commit/ef6a40632bc975c961d289a6d41c7a67cc85d56d) (integrado en dev): [src/POSCartService/Data/PosCartDbContext.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ef6a40632bc975c961d289a6d41c7a67cc85d56d/src/POSCartService/Data/PosCartDbContext.cs), [tests/GlobalMart.IntegrationTests/PesableTaxRoundingTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ef6a40632bc975c961d289a6d41c7a67cc85d56d/tests/GlobalMart.IntegrationTests/PesableTaxRoundingTests.cs).

**Posible repregunta:** ¿Qué resultado espera la prueba para 12,3456 por 0,500?

---

<a id="s-16"></a>

**Pregunta N.º:** 16 (S-16)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué redondear IVA por línea y después sumar, en vez de redondear solo el total?

**Respuesta modelo:** Es la convención implementada en MS-2 para mantener coherencia con subtotales de ítems. Cada subtotal y su IVA se redondean a dos decimales con ToEven; luego se suman. Cambiar el orden puede producir diferencias de centavos. Mi cambio hace explícito ese contrato y lo prueba con casos de empate, sin afirmar que implemente todas las reglas fiscales posibles.

**Explicación para estudiar:** El redondeo no es asociativo respecto de la suma. Dos algoritmos razonables pueden dar resultados distintos si no comparten el mismo punto de redondeo.

**Evidencia del repositorio:** [ef6a406](https://github.com/rpedrazav/TallerIntegracion3/commit/ef6a40632bc975c961d289a6d41c7a67cc85d56d) (integrado en dev): [src/TaxComplianceService/Services/TaxCalculatorService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ef6a40632bc975c961d289a6d41c7a67cc85d56d/src/TaxComplianceService/Services/TaxCalculatorService.cs), [tests/GlobalMart.IntegrationTests/PesableTaxRoundingTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ef6a40632bc975c961d289a6d41c7a67cc85d56d/tests/GlobalMart.IntegrationTests/PesableTaxRoundingTests.cs).

**Posible repregunta:** ¿Por qué 0,125 y 0,135 ilustran ToEven?

---

<a id="s-17"></a>

**Pregunta N.º:** 17 (S-17)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué garantía ofrece la actualización condicional del intento frente a una reconciliación concurrente?

**Respuesta modelo:** La actualización compara identidad, venta, tenant, clave, monto, moneda y estado previo permitido. Si otra operación ya finalizó el intento, no debe sobrescribirla como si siguiera pendiente. Además la reserva se desasocia del tracking para evitar guardar después una copia obsoleta. Esa coordinación protege el resultado persistido aunque lleguen respuestas y consultas en distinto orden.

**Explicación para estudiar:** Un objeto leído antes puede quedar desactualizado. La condición de escritura debe comprobar el estado en la base, no confiar solo en el objeto en memoria.

**Evidencia del repositorio:** [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/src/POSCartService/Repositories/VentaRepository.cs), [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md).

**Posible repregunta:** ¿Qué error debería producir actualizar un intento ya aprobado?

---

<a id="s-18"></a>

**Pregunta N.º:** 18 (S-18)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué editar peso consulta catálogo pero conserva el precio del ítem?

**Respuesta modelo:** Consulta catálogo para determinar el tipo de producto y aplicar la regla correcta. El precio se toma del snapshot ya vendido, evitando que un cambio de catálogo revalorice silenciosamente la línea. Después recalcula impuestos. Reconozco la limitación: EsPesoVariable no está congelado en el ítem, por lo que disponibilidad y cambios de tipo del catálogo aún influyen en la edición.

**Explicación para estudiar:** Congelar algunos atributos y consultar otros es una decisión de consistencia. Debe explicarse qué representa el hecho histórico y qué sigue siendo configuración vigente.

**Evidencia del repositorio:** [15dc321](https://github.com/rpedrazav/TallerIntegracion3/commit/15dc32170a5b8cb8a2e006fd7f9f552b0b4e9251) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/15dc32170a5b8cb8a2e006fd7f9f552b0b4e9251/src/POSCartService/Controllers/VentasController.cs), [tests/GlobalMart.IntegrationTests/ModificarPesoIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/15dc32170a5b8cb8a2e006fd7f9f552b0b4e9251/tests/GlobalMart.IntegrationTests/ModificarPesoIntegrationTests.cs).

**Posible repregunta:** ¿Qué snapshot adicional evaluarías para reducir esa dependencia?

---


### Problemas encontrados y soluciones

<a id="s-19"></a>

**Pregunta N.º:** 19 (S-19)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué fallo concreto revelaron tus pruebas de fronteras de peso?

**Respuesta modelo:** La creación permitía un pesable sin peso aunque agregar y modificar ya tenían controles. El diagnóstico registró un 201 incorrecto. Lo corregí aplicando ReglasCantidadItem también al crear y amplié la matriz con tipo, mínimo, precisión y tenant. El reporte registra 84/84 en la corrida final, con alcance local y sin acreditar el clúster.

**Explicación para estudiar:** Probar distintos puntos de entrada revela reglas que parecen globales pero solo se aplican en ciertos caminos. La regresión debe incluir el caso originalmente fallido.

**Evidencia del repositorio:** [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [docs/qa/S2-065/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/docs/qa/S2-065/reporte.md), [tests/GlobalMart.IntegrationTests/PesoFronterasTenantTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/tests/GlobalMart.IntegrationTests/PesoFronterasTenantTests.cs).

**Posible repregunta:** ¿Qué comprobación demuestra que un 400 no dejó una venta parcial?

---

<a id="s-20"></a>

**Pregunta N.º:** 20 (S-20)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo resolviste el conflicto entre FALLIDO y PENDIENTE para una autorización incierta?

**Respuesta modelo:** La versión inicial de cobro seguro usaba FALLIDO para errores de comunicación. En el merge se conservó PENDIENTE de recuperación, porque todavía no se conoce el resultado comercial. Ambos bloquean una nueva autorización, y la consulta admite registros FALLIDO anteriores. Ajustar las expectativas de pruebas preservó esa compatibilidad sin interpretar timeout como rechazo definitivo.

**Explicación para estudiar:** Un estado debe comunicar qué se sabe, no solo qué excepción ocurrió. La incertidumbre debe conservar la identidad necesaria para recuperar una decisión externa.

**Evidencia del repositorio:** [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md), [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/src/POSCartService/Repositories/VentaRepository.cs).

**Posible repregunta:** ¿Por qué RECHAZADO sí permite un nuevo intento manual?

---

<a id="s-21"></a>

**Pregunta N.º:** 21 (S-21)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo verificaste que un cobro rechazado o inválido no generara efectos indebidos?

**Respuesta modelo:** Los tests comparan estado de venta, intentos, pagos y outbox, y cuentan solicitudes al simulador. Una guardia inválida debe impedir incluso la llamada externa; un rechazo comercial puede registrar el intento rechazado, pero no pago ni evento. Esas comprobaciones distinguen errores antes de autorizar de respuestas negativas de una autorización efectivamente enviada.

**Explicación para estudiar:** El lugar del fallo cambia qué efectos son válidos. Una prueba de seguridad debe observar tanto el límite externo como la persistencia.

**Evidencia del repositorio:** [212cc56](https://github.com/rpedrazav/TallerIntegracion3/commit/212cc56b8ffe0c44dc07650a40b01ebc2996f215) (integrado en dev): [tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/212cc56b8ffe0c44dc07650a40b01ebc2996f215/tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs); [31196e3](https://github.com/rpedrazav/TallerIntegracion3/commit/31196e362a866e20af316b820c0f15af1da28e2f) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/31196e362a866e20af316b820c0f15af1da28e2f/src/POSCartService/Controllers/VentasController.cs).

**Posible repregunta:** ¿Qué esperas si se presenta una venta de otro tenant?

---

<a id="s-22"></a>

**Pregunta N.º:** 22 (S-22)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué riesgo queda cuando la pasarela aprueba pero la base falla y cómo lo demostrarías?

**Respuesta modelo:** El riesgo es repetir el cargo por creer que el rollback local canceló la autorización. La prueba del merge fuerza la falla, conserva la reserva y luego consulta la misma clave para completar localmente. Verifica un solo POST, un pago y un outbox tras recuperación. No se presenta esa consulta como reembolso ni como una segunda venta.

**Explicación para estudiar:** En operaciones distribuidas hay resultados parciales legítimos. La recuperación correcta une esos resultados conservando identidad en vez de reiniciar todo el proceso.

**Evidencia del repositorio:** [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md), [tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs).

**Posible repregunta:** ¿Qué información se perdería si se eliminara la reserva después del error?

---


### Integración con el trabajo de otros integrantes

<a id="s-23"></a>

**Pregunta N.º:** 23 (S-23)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo se integra tu endpoint de tarjeta con el adaptador de Nicolás?

**Respuesta modelo:** El controlador prepara monto de la venta, tenant, venta y RequestId tras superar las guardias y reservar. IPaymentGatewayClient realiza la comunicación y devuelve referencias y estado. Mi código interpreta ese resultado para persistir, mientras el adaptador maneja HTTP y consulta tras timeout. No necesita recibir datos de tarjeta del renderer.

**Explicación para estudiar:** Una interfaz separa transporte y negocio, pero ambos lados deben acordar errores, correlación y estados ambiguos.

**Evidencia del repositorio:** [31196e3](https://github.com/rpedrazav/TallerIntegracion3/commit/31196e362a866e20af316b820c0f15af1da28e2f) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/31196e362a866e20af316b820c0f15af1da28e2f/src/POSCartService/Controllers/VentasController.cs); [866e5b1](https://github.com/rpedrazav/TallerIntegracion3/commit/866e5b156356706f02d5ceaadedcd9965c9b7ed5) (integrado en dev): [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/866e5b156356706f02d5ceaadedcd9965c9b7ed5/src/POSCartService/Services/PaymentGatewayClient.cs), [src/POSCartService/DTOs/PaymentGatewayDtos.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/866e5b156356706f02d5ceaadedcd9965c9b7ed5/src/POSCartService/DTOs/PaymentGatewayDtos.cs).

**Posible repregunta:** ¿Qué campo debe coincidir entre la solicitud y la respuesta?

---

<a id="s-24"></a>

**Pregunta N.º:** 24 (S-24)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué debe respetar el frontend de Rodrigo al consumir tus reglas pesables?

**Respuesta modelo:** Debe enviar kg válidos, omitir o igualar cantidad para un pesable y usar los importes devueltos por MS-5. La UI puede ayudar con conversión y confirmación, pero el backend repite las reglas. Al editar se conserva el snapshot del precio. Si el servidor rechaza el peso, el frontend debe mantener o recuperar el estado real, no mostrar éxito provisional.

**Explicación para estudiar:** Una prevalidación compartida mejora experiencia; el servidor conserva autoridad porque cualquier cliente puede saltarse la interfaz.

**Evidencia del repositorio:** [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [src/POSCartService/Services/ReglasCantidadItem.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/src/POSCartService/Services/ReglasCantidadItem.cs); [00fbb39](https://github.com/rpedrazav/TallerIntegracion3/commit/00fbb3981562a122d76fd6c61e2e3104c2f1bb42) (integrado en dev): [globalmart-frontend/src/components/pos/CartItem.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/00fbb3981562a122d76fd6c61e2e3104c2f1bb42/globalmart-frontend/src/components/pos/CartItem.tsx), [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/00fbb3981562a122d76fd6c61e2e3104c2f1bb42/globalmart-frontend/src/pages/Pos.tsx).

**Posible repregunta:** ¿Cómo distinguirían subtotal estimado de total confirmado en pantalla?

---

<a id="s-25"></a>

**Pregunta N.º:** 25 (S-25)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo ayudan las pruebas RN-01 de Daniel a validar tus cambios en repositorios?

**Respuesta modelo:** Ejercitan accesos propios y ajenos con JWT y PostgreSQL, además de llamadas directas a repositorios para evitar depender únicamente del controlador. Al editar ventas o ítems, la pertenencia se confirma contra datos persistidos. Eso protege mis flujos de cobro y peso de regressiones donde un objeto rastreado o recibido intenta cambiar de tenant.

**Explicación para estudiar:** La seguridad por capas necesita pruebas en cada frontera relevante. Un filtro global de lectura no basta para justificar cualquier escritura.

**Evidencia del repositorio:** [d44e22b](https://github.com/rpedrazav/TallerIntegracion3/commit/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e) (integrado en dev): [tests/GlobalMart.IntegrationTests/PosRepositoryTenantIsolationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/PosRepositoryTenantIsolationTests.cs), [src/POSCartService/Repositories/ItemVentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/src/POSCartService/Repositories/ItemVentaRepository.cs); [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [tests/GlobalMart.IntegrationTests/PesoFronterasTenantTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/tests/GlobalMart.IntegrationTests/PesoFronterasTenantTests.cs).

**Posible repregunta:** ¿Por qué no debe tomarse el tenant del objeto enviado al repositorio?

---

<a id="s-26"></a>

**Pregunta N.º:** 26 (S-26)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué parte de la coherencia de cobro depende del outbox de Nicolás?

**Respuesta modelo:** La finalización utiliza su entidad y patrón para guardar el evento con pago y venta. Mi flujo de tarjeta incorpora el intento aprobado en esa misma transacción. El worker publica después. Esto conecta una aprobación de pasarela con inventario de forma recuperable, aunque no demuestra por sí solo descuento único ni publicación remota.

**Explicación para estudiar:** La coherencia local se compone entre módulos cuando comparten la transacción adecuada. La entrega externa sigue siendo otra etapa con sus propios fallos.

**Evidencia del repositorio:** [186f8d5](https://github.com/rpedrazav/TallerIntegracion3/commit/186f8d583d6a2a0e6d049784fea3885203f3c305) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/186f8d583d6a2a0e6d049784fea3885203f3c305/src/POSCartService/Repositories/VentaRepository.cs); [0602a93](https://github.com/rpedrazav/TallerIntegracion3/commit/0602a936469683c8a1f3409477c6c644aa9fe5c0) (integrado en dev): [src/POSCartService/Models/OutboxMessage.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0602a936469683c8a1f3409477c6c644aa9fe5c0/src/POSCartService/Models/OutboxMessage.cs); [4cf822c](https://github.com/rpedrazav/TallerIntegracion3/commit/4cf822c347c18d987282a31b5babe3e2f3582a31) (integrado en dev): [src/POSCartService/Services/OutboxPublisherService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Services/OutboxPublisherService.cs).

**Posible repregunta:** ¿Por qué no publicar Kafka antes del commit de la venta?

---


### Logros personales, aprendizajes y mejoras

<a id="s-27"></a>

**Pregunta N.º:** 27 (S-27)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Básica.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Qué logro de creación de venta puedes demostrar con un caso breve?

**Respuesta modelo:** Puedo enviar un precio manipulado para un producto conocido y mostrar que la venta conserva el precio del catálogo y el IVA calculado. También puedo mostrar que sin turno no se crea. Esos casos convierten la regla de autoridad del servidor en un resultado observable, sin depender de cómo la UI construya la solicitud.

**Explicación para estudiar:** Un ejemplo de defensa eficaz muestra entrada adversa, respuesta y persistencia. La prueba debe explicar por qué el valor final es confiable.

**Evidencia del repositorio:** [1833981](https://github.com/rpedrazav/TallerIntegracion3/commit/18339813f950b4becfffcef7d046e7907172dff0) (integrado en dev): [tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/18339813f950b4becfffcef7d046e7907172dff0/tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs).

**Posible repregunta:** ¿Qué cambiaría si el IVA del tenant fuera distinto de 19%?

---

<a id="s-28"></a>

**Pregunta N.º:** 28 (S-28)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué aprendizaje técnico puedes demostrar al comparar S2-062 con S2-065?

**Respuesta modelo:** La evolución muestra que validar agregado no garantizaba validar creación. El resultado fue extraer una regla común y probar los tres caminos. Ese aprendizaje es visible en el código; qué parte me resultó más difícil personalmente está [NO VERIFICADO] y debo confirmarla. Puedo explicar el antes, el fallo reproducido, la corrección y su prueba.

**Explicación para estudiar:** Reutilizar reglas reduce divergencia, pero la cobertura debe verificar su uso. La lección es estructural y no depende de inventar una experiencia personal.

**Evidencia del repositorio:** [e5141df](https://github.com/rpedrazav/TallerIntegracion3/commit/e5141df2c4a485c2fb34aa2ef22c7df4d51fdc29) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/e5141df2c4a485c2fb34aa2ef22c7df4d51fdc29/src/POSCartService/Controllers/VentasController.cs); [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [src/POSCartService/Services/ReglasCantidadItem.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/src/POSCartService/Services/ReglasCantidadItem.cs), [docs/qa/S2-065/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/docs/qa/S2-065/reporte.md).

**Posible repregunta:** ¿Qué cambio futuro podría volver a introducir esa divergencia?

---

<a id="s-29"></a>

**Pregunta N.º:** 29 (S-29)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué dificultad personal elegirías para explicar tu proceso de trabajo con cobros concurrentes?

**Respuesta modelo:** Debo confirmar mi experiencia real. Una estructura defendible es: situación concreta, hipótesis sobre el doble cobro, prueba que la contrastó, cambio que hice y resultado. El repositorio permite usar como soporte la reserva por venta y el merge, pero no prueba cuánto tardé, quién me ayudó ni qué herramienta me resultó más útil.

**Explicación para estudiar:** La reflexión personal debe apoyarse en un caso verificable sin convertir deducciones de Git en recuerdos. Separar problema, acción y evidencia evita respuestas vagas.

**Evidencia del repositorio:** [212cc56](https://github.com/rpedrazav/TallerIntegracion3/commit/212cc56b8ffe0c44dc07650a40b01ebc2996f215) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/212cc56b8ffe0c44dc07650a40b01ebc2996f215/src/POSCartService/Repositories/VentaRepository.cs); [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md).

**Posible repregunta:** ¿Qué decisión cambiarías después de haber integrado recuperación y reserva?

---

<a id="s-30"></a>

**Pregunta N.º:** 30 (S-30)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué mejora técnica priorizarías en MS-5 después de este sprint y cómo la aceptarías?

**Respuesta modelo:** Priorizaría cerrar anulación total, porque está en G2 y el controlador no expone el endpoint. Diseñaría autorización, estado permitido y relación con pago, reembolso y evento compensatorio antes de activarlo. La aceptación debe cubrir ventas ajenas, repetición y fallos parciales. Es una propuesta: el servicio AnularAsync existente no acredita ese flujo completo.

**Explicación para estudiar:** Una reversión comercial exige compensar efectos ya confirmados, no solo cambiar un enum. El alcance debe incluir sistemas externos y trazabilidad.

**Evidencia del repositorio:** [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/POSCartService/Controllers/VentasController.cs), [src/POSCartService/Services/VentaService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/POSCartService/Services/VentaService.cs); [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) (integrado en dev): [tareas/plan_sprint2_revisado.xlsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/plan_sprint2_revisado.xlsx).

**Posible repregunta:** ¿Por qué reutilizar simplemente el evento sale.completed sería incorrecto?

---


## 6. Banco de 30 preguntas — Nicolás

**Registro:** 30 preguntas; 10 básicas, 12 intermedias, 8 avanzadas. Numeración única en este banco.

Las respuestas describen el código o la revisión enlazada. Los resultados de ejecución son históricos. “Rama, no integrado” identifica evidencia que no forma parte de dev al corte. Las experiencias personales deben ser confirmadas por el integrante.


### Tareas, responsabilidades y contribuciones

<a id="n-01"></a>

**Pregunta N.º:** 1 (N-01)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué tres líneas de trabajo concentran tus quince commits del Sprint 2?

**Respuesta modelo:** El outbox de sale.completed, la comunicación y simulación de pagos, y la abstracción de balanza con IPC y adaptador serial. Hay commits de contrato, implementación y pruebas en cada línea. No todos equivalen a una funcionalidad independiente: algunos documentan decisiones o validan la recuperación de una función implementada antes.

**Explicación para estudiar:** Contar entregables requiere agrupar cambios relacionados sin borrar su trazabilidad. La autoría se identifica por diff y no por la cantidad de archivos del servicio.

**Evidencia del repositorio:** [731a23f](https://github.com/rpedrazav/TallerIntegracion3/commit/731a23fc0f794be061a12882952ad4d003757cac) (integrado en dev): [tests/GlobalMart.IntegrationTests/OutboxBrokerRecoveryIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/731a23fc0f794be061a12882952ad4d003757cac/tests/GlobalMart.IntegrationTests/OutboxBrokerRecoveryIntegrationTests.cs); [1060d53](https://github.com/rpedrazav/TallerIntegracion3/commit/1060d5308c131d7ffe9e73bcd71a497bfda74101) (integrado en dev): [tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1060d5308c131d7ffe9e73bcd71a497bfda74101/tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs); [352fe7f](https://github.com/rpedrazav/TallerIntegracion3/commit/352fe7fef4b5401ea6a2d7bb449c427c3616d91c) (integrado en dev): [globalmart-frontend/tests/scale-main-preload.integration.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/352fe7fef4b5401ea6a2d7bb449c427c3616d91c/globalmart-frontend/tests/scale-main-preload.integration.test.cjs).

**Posible repregunta:** ¿Qué commit conecta cada línea con una prueba concreta?

---

<a id="n-02"></a>

**Pregunta N.º:** 2 (N-02)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué definiste en S2-006 antes de crear la tabla outbox?

**Respuesta modelo:** Definí identidad estable, versión, contenido del evento, estados y política de reintento. También dejé explícito que se permiten reentregas y que la deduplicación de MS-4 es una dependencia aparte. Ese commit documenta el contrato; la entidad y migración aparecen después, y el worker llega en S2-009.

**Explicación para estudiar:** Acordar el contrato antes permite separar garantía buscada de comportamiento implementado. Los estados futuros no se vuelven reales por estar en una tabla documental.

**Evidencia del repositorio:** [054d7d3](https://github.com/rpedrazav/TallerIntegracion3/commit/054d7d382ca6a91967e96dc885b9cfb6b1558c68) (integrado en dev): [docs/context/eventos/sale-completed-outbox.md](https://github.com/rpedrazav/TallerIntegracion3/blob/054d7d382ca6a91967e96dc885b9cfb6b1558c68/docs/context/eventos/sale-completed-outbox.md); [2e546dc](https://github.com/rpedrazav/TallerIntegracion3/commit/2e546dc7110791c030047eacf9e5432ceff6be75) (integrado en dev): [src/POSCartService/Models/OutboxMessage.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2e546dc7110791c030047eacf9e5432ceff6be75/src/POSCartService/Models/OutboxMessage.cs).

**Posible repregunta:** ¿Qué parte del reproceso sigue planificada aunque aparezca en el documento?

---

<a id="n-03"></a>

**Pregunta N.º:** 3 (N-03)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Qué aportaste a la pasarela y qué parte quedó en los commits de Sebastián?

**Respuesta modelo:** Desarrollé el adaptador HTTP, escenarios y funcionamiento del simulador, consulta tras timeout y pruebas de secuencia con outbox. Sebastián integró guardias, persistencia de IntentoPago y finalización segura del endpoint. Mi adaptador no decide por sí solo cuándo una venta puede cobrarse; debe recibir una solicitud autorizada por las reglas de MS-5.

**Explicación para estudiar:** Separar el transporte del caso de uso facilita pruebas, pero obliga a acordar quién valida y quién persiste cada estado.

**Evidencia del repositorio:** [866e5b1](https://github.com/rpedrazav/TallerIntegracion3/commit/866e5b156356706f02d5ceaadedcd9965c9b7ed5) (integrado en dev): [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/866e5b156356706f02d5ceaadedcd9965c9b7ed5/src/POSCartService/Services/PaymentGatewayClient.cs); [60af3d6](https://github.com/rpedrazav/TallerIntegracion3/commit/60af3d652813fd039f301938d3b616c700485318) (integrado en dev): [Docker/simulator/server.js](https://github.com/rpedrazav/TallerIntegracion3/blob/60af3d652813fd039f301938d3b616c700485318/Docker/simulator/server.js); [212cc56](https://github.com/rpedrazav/TallerIntegracion3/commit/212cc56b8ffe0c44dc07650a40b01ebc2996f215) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/212cc56b8ffe0c44dc07650a40b01ebc2996f215/src/POSCartService/Repositories/VentaRepository.cs).

**Posible repregunta:** ¿Qué guardia de negocio no debería delegarse al simulador?

---

<a id="n-04"></a>

**Pregunta N.º:** 4 (N-04)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué cubre S2-070 además de probar métodos aislados de balanza?

**Respuesta modelo:** Recorre el simulador a través de main y preload hasta el filtro que permite usar el peso en carrito. Incluye datos corruptos, desconexión y reconexión. Es integración de componentes con infraestructura controlada y no certifica hardware. También conecté la representación de kg y las restricciones de edición de líneas pesables.

**Explicación para estudiar:** Una prueba de composición detecta errores de enlace que no aparecen al probar cada adaptador solo. Su entorno controlado sigue limitando las conclusiones físicas.

**Evidencia del repositorio:** [352fe7f](https://github.com/rpedrazav/TallerIntegracion3/commit/352fe7fef4b5401ea6a2d7bb449c427c3616d91c) (integrado en dev): [globalmart-frontend/tests/scale-main-preload.integration.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/352fe7fef4b5401ea6a2d7bb449c427c3616d91c/globalmart-frontend/tests/scale-main-preload.integration.test.cjs), [globalmart-frontend/src/hardware/scale-cart.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/352fe7fef4b5401ea6a2d7bb449c427c3616d91c/globalmart-frontend/src/hardware/scale-cart.ts).

**Posible repregunta:** ¿Qué fallo podría introducir preload aunque el simulador funcione bien?

---

<a id="n-05"></a>

**Pregunta N.º:** 5 (N-05)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué entregaste realmente con el adaptador serial S2-069?

**Respuesta modelo:** Una implementación del contrato ScaleAdapter que recibe transporte y parser inyectables, configuración privada y manejo de errores. No entregué un driver físico universal ni confirmé un modelo de balanza. La fábrica exige dependencias para el modo serial. Esto permite reemplazar la simulación sin cambiar el consumidor, una vez implementado y validado el transporte real.

**Explicación para estudiar:** Preparar una extensión arquitectónica es un avance concreto, distinto de completar la integración externa que la utilizará.

**Evidencia del repositorio:** [0afc855](https://github.com/rpedrazav/TallerIntegracion3/commit/0afc855588582f8294644160dff4f418f40b2683) (integrado en dev): [globalmart-frontend/src/hardware/serial-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/src/hardware/serial-scale-adapter.ts), [globalmart-frontend/src/hardware/scale-adapter-factory.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/src/hardware/scale-adapter-factory.ts), [globalmart-frontend/tests/serial-scale-adapter.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/tests/serial-scale-adapter.test.cjs).

**Posible repregunta:** ¿Qué evidencia faltaría para afirmar compatibilidad con un modelo comercial?

---


### Funcionalidades implementadas y explicación del código

<a id="n-06"></a>

**Pregunta N.º:** 6 (N-06)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué guarda OutboxMessage y por qué guarda el JSON completo?

**Respuesta modelo:** Guarda EventId, tenant, venta, tipo, versión, topic, clave y payload, junto con estado, intentos y fechas de recuperación. El JSON se conserva para reenviar exactamente el snapshot original, sin consultar una venta que pudo cambiar. La identidad no se regenera al reintentar, de modo que el consumidor pueda reconocer el mismo evento lógico.

**Explicación para estudiar:** Evento y estado de publicación son cosas distintas. El contenido comercial permanece estable mientras cambian intentos, lease y diagnóstico.

**Evidencia del repositorio:** [2e546dc](https://github.com/rpedrazav/TallerIntegracion3/commit/2e546dc7110791c030047eacf9e5432ceff6be75) (integrado en dev): [src/POSCartService/Models/OutboxMessage.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2e546dc7110791c030047eacf9e5432ceff6be75/src/POSCartService/Models/OutboxMessage.cs); [0602a93](https://github.com/rpedrazav/TallerIntegracion3/commit/0602a936469683c8a1f3409477c6c644aa9fe5c0) (integrado en dev): [src/POSCartService/Messaging/SaleCompletedEvent.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0602a936469683c8a1f3409477c6c644aa9fe5c0/src/POSCartService/Messaging/SaleCompletedEvent.cs).

**Posible repregunta:** ¿Qué problema causaría reconstruir el payload cada vez?

---

<a id="n-07"></a>

**Pregunta N.º:** 7 (N-07)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo cambió CompletarAsync con tu incorporación del outbox?

**Respuesta modelo:** Preparó un evento y pasó venta, pago y outbox al método transaccional del repositorio. La solicitud ya no necesita publicar directamente a Kafka para confirmar el cobro. Si no puede guardarse el outbox, se revierten también pago y estado. La prueba fuerza la falla de inserción y verifica que no quede una venta completada sin intención durable de publicación.

**Explicación para estudiar:** La atomicidad protege el conjunto de datos local. Guardar primero la venta y luego el evento dejaría una ventana de pérdida.

**Evidencia del repositorio:** [0602a93](https://github.com/rpedrazav/TallerIntegracion3/commit/0602a936469683c8a1f3409477c6c644aa9fe5c0) (integrado en dev): [src/POSCartService/Services/VentaService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0602a936469683c8a1f3409477c6c644aa9fe5c0/src/POSCartService/Services/VentaService.cs), [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0602a936469683c8a1f3409477c6c644aa9fe5c0/src/POSCartService/Repositories/VentaRepository.cs), [tests/GlobalMart.IntegrationTests/VentaOutboxTransactionTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0602a936469683c8a1f3409477c6c644aa9fe5c0/tests/GlobalMart.IntegrationTests/VentaOutboxTransactionTests.cs).

**Posible repregunta:** ¿Por qué un broker caído no debe revertir un cobro ya persistido?

---

<a id="n-08"></a>

**Pregunta N.º:** 8 (N-08)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo selecciona el worker mensajes sin romper el aislamiento por tenant?

**Respuesta modelo:** Enumera internamente tenants con trabajo y procesa cada uno con contexto explícito. ReserveNextAsync obtiene filas elegibles y reserva con bloqueo, estado y lease. Las actualizaciones comparan tenant y evento. No se toma un tenant arbitrario de una solicitud pública para recorrer registros de todos los clientes. El batch limita el trabajo de cada ciclo.

**Explicación para estudiar:** Un proceso de fondo también necesita contexto de aislamiento aunque no atraviese middleware HTTP. La ausencia de request no elimina RN-01.

**Evidencia del repositorio:** [4cf822c](https://github.com/rpedrazav/TallerIntegracion3/commit/4cf822c347c18d987282a31b5babe3e2f3582a31) (integrado en dev): [src/POSCartService/Repositories/OutboxRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Repositories/OutboxRepository.cs), [src/POSCartService/Services/OutboxPublisherService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Services/OutboxPublisherService.cs), [src/POSCartService/Services/OutboxPublisherWorker.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Services/OutboxPublisherWorker.cs).

**Posible repregunta:** ¿Por qué es importante establecer CurrentTenantId antes de reservar?

---

<a id="n-09"></a>

**Pregunta N.º:** 9 (N-09)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo funciona tu política de reintentos del publicador?

**Respuesta modelo:** Ante fallo se conserva la fila y se calcula una espera exponencial con jitter, usando base y máximo configurables. Se persiste NextAttemptAt y se cuentan intentos. Al alcanzar el límite, la fila pasa a FALLIDO. PUBLICADO solo se marca tras retorno satisfactorio del productor. El reproceso manual de fallidos todavía necesita una solución operativa.

**Explicación para estudiar:** El backoff evita insistencia inmediata y el jitter dispersa reintentos simultáneos. Persistir el calendario impide que un reinicio borre la historia del intento.

**Evidencia del repositorio:** [4cf822c](https://github.com/rpedrazav/TallerIntegracion3/commit/4cf822c347c18d987282a31b5babe3e2f3582a31) (integrado en dev): [src/POSCartService/Services/OutboxPublisherService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Services/OutboxPublisherService.cs), [docs/context/eventos/sale-completed-outbox.md](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/docs/context/eventos/sale-completed-outbox.md).

**Posible repregunta:** ¿Qué significa FALLIDO para la garantía de entrega eventual?

---

<a id="n-10"></a>

**Pregunta N.º:** 10 (N-10)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué hace PaymentGatewayClient cuando se agota el tiempo de autorización?

**Respuesta modelo:** Distingue timeout de cancelación del llamador y consulta el estado por tenant y RequestId. Si encuentra un resultado, lo devuelve; si no puede confirmar, expresa autorización incierta. No vuelve a enviar automáticamente el POST. De esta forma no interpreta falta de respuesta como permiso para crear otra autorización.

**Explicación para estudiar:** Una respuesta perdida y una operación no ejecutada son indistinguibles solo desde el timeout. Consultar la misma identidad reduce ese riesgo.

**Evidencia del repositorio:** [1060d53](https://github.com/rpedrazav/TallerIntegracion3/commit/1060d5308c131d7ffe9e73bcd71a497bfda74101) (integrado en dev): [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1060d5308c131d7ffe9e73bcd71a497bfda74101/src/POSCartService/Services/PaymentGatewayClient.cs), [tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1060d5308c131d7ffe9e73bcd71a497bfda74101/tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs).

**Posible repregunta:** ¿Qué debe hacer el endpoint si el adaptador devuelve incertidumbre?

---

<a id="n-11"></a>

**Pregunta N.º:** 11 (N-11)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué tu prueba de recuperación no demuestra exactamente una vez en Kafka real?

**Respuesta modelo:** La prueba usa PostgreSQL real y un productor controlado que falla y luego se recupera. Verifica persistencia, mismo payload y una entrega exitosa observada. No detiene un broker real ni fuerza la ventana donde Kafka recibió y el marcado local falló. Por eso acredita recuperación del publicador bajo ese modelo, no una garantía universal de transporte.

**Explicación para estudiar:** El doble de prueba delimita los fallos representados. Una afirmación sobre sistemas distribuidos requiere cubrir las ventanas de confirmación de los sistemas reales.

**Evidencia del repositorio:** [731a23f](https://github.com/rpedrazav/TallerIntegracion3/commit/731a23fc0f794be061a12882952ad4d003757cac) (integrado en dev): [tests/GlobalMart.IntegrationTests/OutboxBrokerRecoveryIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/731a23fc0f794be061a12882952ad4d003757cac/tests/GlobalMart.IntegrationTests/OutboxBrokerRecoveryIntegrationTests.cs), [docs/context/eventos/sale-completed-outbox.md](https://github.com/rpedrazav/TallerIntegracion3/blob/731a23fc0f794be061a12882952ad4d003757cac/docs/context/eventos/sale-completed-outbox.md).

**Posible repregunta:** ¿Qué variante añadirías para simular confirmación perdida?

---

<a id="n-12"></a>

**Pregunta N.º:** 12 (N-12)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo garantizas que un worker antiguo no marque una reserva que ya perdió?

**Respuesta modelo:** Cada reserva recibe un LeaseToken y fecha de expiración. MarkPublishedAsync y el registro de fallo condicionan la escritura a tenant, EventId, estado y token vigente. Si el lease expiró y otro worker tomó el mensaje, la actualización antigua no debe aplicarse. Esto protege el registro, aunque una publicación externa anterior todavía pueda producir duplicados.

**Explicación para estudiar:** La propiedad temporal se verifica al escribir, no solo al leer. Separar token de reserva e identidad de evento permite reintentar sin crear otro evento comercial.

**Evidencia del repositorio:** [4cf822c](https://github.com/rpedrazav/TallerIntegracion3/commit/4cf822c347c18d987282a31b5babe3e2f3582a31) (integrado en dev): [src/POSCartService/Repositories/OutboxRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Repositories/OutboxRepository.cs), [src/POSCartService/Services/OutboxPublisherService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Services/OutboxPublisherService.cs).

**Posible repregunta:** ¿Qué identificador cambia al reservar nuevamente y cuál debe permanecer?

---

<a id="n-13"></a>

**Pregunta N.º:** 13 (N-13)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué operaciones expusiste para balanza en preload?

**Respuesta modelo:** Conectar, desconectar, leer un peso estable y consultar estado. El contrato devuelve resultados discriminados con ok, valor o error y estado. La lectura normalizada usa kg y stable=true. No expuse puerto serial, baud rate ni comandos arbitrarios al renderer; esos detalles pertenecen a main y al adaptador concreto.

**Explicación para estudiar:** Una API de intención expresa lo que necesita el caso de uso. Exponer mecanismos de bajo nivel ampliaría permisos sin ayudar al cajero.

**Evidencia del repositorio:** [7c6145f](https://github.com/rpedrazav/TallerIntegracion3/commit/7c6145fc9abff02e1da50d989db34d78a2fa23d0) (integrado en dev): [globalmart-frontend/src/hardware/scale-contract.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/7c6145fc9abff02e1da50d989db34d78a2fa23d0/globalmart-frontend/src/hardware/scale-contract.ts); [71847b1](https://github.com/rpedrazav/TallerIntegracion3/commit/71847b18c559111107436474461d18542688b7dc) (integrado en dev): [globalmart-frontend/src/preload.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/71847b18c559111107436474461d18542688b7dc/globalmart-frontend/src/preload.ts), [globalmart-frontend/src/hardware/scale-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/71847b18c559111107436474461d18542688b7dc/globalmart-frontend/src/hardware/scale-ipc.ts).

**Posible repregunta:** ¿Cómo informa el contrato una balanza desconectada sin lanzar datos internos del dispositivo?

---


### Decisiones técnicas, tecnologías y arquitectura

<a id="n-14"></a>

**Pregunta N.º:** 14 (N-14)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué empleaste una interfaz IPaymentGatewayClient en lugar de llamar HTTP desde cualquier servicio?

**Respuesta modelo:** Centraliza el contrato de autorización, la correlación, los errores y la consulta. Permite probar la orquestación con un adaptador controlado y probar el adaptador con HTTP específico. También evita duplicar reglas de transporte por cada consumidor. La interfaz no reemplaza las reglas de venta, que permanecen en el flujo de MS-5.

**Explicación para estudiar:** Un puerto desacopla una capacidad de su proveedor. Resulta útil cuando conserva un significado de negocio claro y no filtra detalles irrelevantes.

**Evidencia del repositorio:** [866e5b1](https://github.com/rpedrazav/TallerIntegracion3/commit/866e5b156356706f02d5ceaadedcd9965c9b7ed5) (integrado en dev): [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/866e5b156356706f02d5ceaadedcd9965c9b7ed5/src/POSCartService/Services/PaymentGatewayClient.cs), [src/POSCartService/DTOs/PaymentGatewayDtos.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/866e5b156356706f02d5ceaadedcd9965c9b7ed5/src/POSCartService/DTOs/PaymentGatewayDtos.cs).

**Posible repregunta:** ¿Qué cambiaría en los consumidores al reemplazar el simulador por otra implementación?

---

<a id="n-15"></a>

**Pregunta N.º:** 15 (N-15)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué EventId se mantiene estable y VentaId también aparece en el evento?

**Respuesta modelo:** EventId identifica el evento lógico para deduplicación; VentaId identifica la venta que lo originó. Pueden existir distintos tipos de evento para una misma venta, por eso no conviene tratarlos siempre como sinónimos. La restricción del outbox por tenant, venta y tipo impide duplicar sale.completed dentro de ese contrato.

**Explicación para estudiar:** Identidad del agregado e identidad del mensaje tienen ciclos de vida distintos. Mantener ambas ayuda a trazabilidad y a futuras compensaciones.

**Evidencia del repositorio:** [0602a93](https://github.com/rpedrazav/TallerIntegracion3/commit/0602a936469683c8a1f3409477c6c644aa9fe5c0) (integrado en dev): [src/POSCartService/Messaging/SaleCompletedEvent.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0602a936469683c8a1f3409477c6c644aa9fe5c0/src/POSCartService/Messaging/SaleCompletedEvent.cs); [2e546dc](https://github.com/rpedrazav/TallerIntegracion3/commit/2e546dc7110791c030047eacf9e5432ceff6be75) (integrado en dev): [src/POSCartService/Data/PosCartDbContext.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2e546dc7110791c030047eacf9e5432ceff6be75/src/POSCartService/Data/PosCartDbContext.cs).

**Posible repregunta:** ¿Qué identidad debería tener un futuro sale.reversed?

---

<a id="n-16"></a>

**Pregunta N.º:** 16 (N-16)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué carrera resuelven FOR UPDATE SKIP LOCKED y el lease, y qué problema no resuelven?

**Respuesta modelo:** El bloqueo permite reservar filas sin que varios workers seleccionen la misma al mismo tiempo; SKIP LOCKED permite avanzar sobre otras filas ocupadas. El lease recupera trabajo si muere quien reservó. Ninguno confirma que Kafka no recibió antes el evento. Por eso el consumidor debe deduplicar y no se promete ausencia de reentregas.

**Explicación para estudiar:** Exclusión local de reservas y efectos remotos son garantías diferentes. El diseño necesita ambas, sin atribuir a un lock lo que ocurre fuera de la base.

**Evidencia del repositorio:** [4cf822c](https://github.com/rpedrazav/TallerIntegracion3/commit/4cf822c347c18d987282a31b5babe3e2f3582a31) (integrado en dev): [src/POSCartService/Repositories/OutboxRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Repositories/OutboxRepository.cs), [src/POSCartService/Services/OutboxPublisherService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Services/OutboxPublisherService.cs).

**Posible repregunta:** ¿Por qué mantener una transacción abierta durante todo el envío sería costoso?

---

<a id="n-17"></a>

**Pregunta N.º:** 17 (N-17)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué límite tiene la idempotencia del simulador de pagos que implementaste?

**Respuesta modelo:** Reutiliza la respuesta para la misma clave y tenant dentro de su proceso y rechaza usos inconsistentes del intento. El estado está en memoria: reiniciar el simulador puede perder ese registro. No debemos trasladar esa garantía a una pasarela bancaria durable. MS-5 conserva sus intentos en PostgreSQL y evita nuevas autorizaciones inciertas, pero el simulador sigue teniendo ese límite.

**Explicación para estudiar:** La durabilidad debe evaluarse por componente. Repetir una solicitud durante el mismo proceso no demuestra recuperación después de un reinicio.

**Evidencia del repositorio:** [60af3d6](https://github.com/rpedrazav/TallerIntegracion3/commit/60af3d652813fd039f301938d3b616c700485318) (integrado en dev): [Docker/simulator/server.js](https://github.com/rpedrazav/TallerIntegracion3/blob/60af3d652813fd039f301938d3b616c700485318/Docker/simulator/server.js); [1060d53](https://github.com/rpedrazav/TallerIntegracion3/commit/1060d5308c131d7ffe9e73bcd71a497bfda74101) (integrado en dev): [Docker/simulator/server.js](https://github.com/rpedrazav/TallerIntegracion3/blob/1060d5308c131d7ffe9e73bcd71a497bfda74101/Docker/simulator/server.js); [abfd1f0](https://github.com/rpedrazav/TallerIntegracion3/commit/abfd1f0e1050027701674889775d2f28c19e780a) (integrado en dev): [docs/qa/S2-053/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/abfd1f0e1050027701674889775d2f28c19e780a/docs/qa/S2-053/reporte.md).

**Posible repregunta:** ¿Qué almacenamiento y prueba añadirías para evaluar esa durabilidad?

---

<a id="n-18"></a>

**Pregunta N.º:** 18 (N-18)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué separaste transporte serial y parser?

**Respuesta modelo:** El transporte abre, cierra y lee tramas; el parser interpreta valor, unidad y estabilidad según un protocolo. El adaptador aplica el contrato común y normaliza. Esa separación permite cambiar biblioteca o modelo sin reescribir la lógica de carrito, y probar errores de comunicación independientemente de tramas inválidas. La configuración serial permanece fuera del renderer.

**Explicación para estudiar:** Separar cambios que tienen causas distintas reduce acoplamiento. Un protocolo nuevo no debería obligar a modificar todo el POS.

**Evidencia del repositorio:** [0afc855](https://github.com/rpedrazav/TallerIntegracion3/commit/0afc855588582f8294644160dff4f418f40b2683) (integrado en dev): [globalmart-frontend/src/hardware/serial-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/src/hardware/serial-scale-adapter.ts), [globalmart-frontend/tests/serial-scale-adapter.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/tests/serial-scale-adapter.test.cjs).

**Posible repregunta:** ¿Dónde rechazarías una trama reconocible pero con unidad no soportada?

---


### Problemas encontrados y soluciones

<a id="n-19"></a>

**Pregunta N.º:** 19 (N-19)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué problema del cobro directo a Kafka resolvió tu transacción outbox?

**Respuesta modelo:** Si la base confirmaba y la publicación fallaba, podía quedar una venta sin evento recuperable. Ahora la intención de publicar queda guardada junto al pago. La prueba de rollback cubre el caso contrario: si no se inserta el evento, tampoco se confirma localmente el cobro. El worker puede continuar después sin repetir la venta.

**Explicación para estudiar:** El problema no era solo manejar una excepción, sino conservar información suficiente para recuperar el trabajo pendiente de forma durable.

**Evidencia del repositorio:** [0602a93](https://github.com/rpedrazav/TallerIntegracion3/commit/0602a936469683c8a1f3409477c6c644aa9fe5c0) (integrado en dev): [tests/GlobalMart.IntegrationTests/VentaOutboxTransactionTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0602a936469683c8a1f3409477c6c644aa9fe5c0/tests/GlobalMart.IntegrationTests/VentaOutboxTransactionTests.cs), [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0602a936469683c8a1f3409477c6c644aa9fe5c0/src/POSCartService/Repositories/VentaRepository.cs).

**Posible repregunta:** ¿Por qué capturar y registrar el error de Kafka no era suficiente?

---

<a id="n-20"></a>

**Pregunta N.º:** 20 (N-20)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué haría tu publicador al apagarse durante el envío y por qué no marca éxito?

**Respuesta modelo:** Propaga la cancelación y deja que venza la reserva si no hay resultado confirmado. El siguiente ciclo podrá recuperarla. Marcar PUBLICADO durante un apagado sería afirmar un efecto desconocido. La prueba del worker comprueba cancelación limpia; el resultado externo ambiguo se maneja conservando la identidad para una posible reentrega.

**Explicación para estudiar:** Cancelar una espera no deshace necesariamente lo ocurrido fuera del proceso. La recuperación debe ser conservadora frente a esa incertidumbre.

**Evidencia del repositorio:** [4cf822c](https://github.com/rpedrazav/TallerIntegracion3/commit/4cf822c347c18d987282a31b5babe3e2f3582a31) (integrado en dev): [src/POSCartService/Services/OutboxPublisherService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Services/OutboxPublisherService.cs), [tests/GlobalMart.IntegrationTests/OutboxPublisherWorkerTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/tests/GlobalMart.IntegrationTests/OutboxPublisherWorkerTests.cs).

**Posible repregunta:** ¿Qué sucedería si el broker sí recibió justo antes de cancelar?

---

<a id="n-21"></a>

**Pregunta N.º:** 21 (N-21)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo evitaste que el renderer enviara opciones seriales por el IPC de balanza?

**Respuesta modelo:** parseStableWeightReadOptions acepta solo timeoutMs y stabilityWindowMs, ambos enteros dentro del rango definido. Rechaza arrays, objetos inválidos y propiedades desconocidas. Los handlers registrados son una lista cerrada. Eso mantiene el contrato de lectura y evita que un payload con portPath o comandos llegue al adaptador como configuración.

**Explicación para estudiar:** Un tipo TypeScript no impide que llegue un objeto malicioso en ejecución. Una lista positiva de campos reduce lo que la frontera acepta.

**Evidencia del repositorio:** [71847b1](https://github.com/rpedrazav/TallerIntegracion3/commit/71847b18c559111107436474461d18542688b7dc) (integrado en dev): [globalmart-frontend/src/hardware/scale-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/71847b18c559111107436474461d18542688b7dc/globalmart-frontend/src/hardware/scale-ipc.ts).

**Posible repregunta:** ¿Por qué no conviene ignorar silenciosamente una propiedad de comando desconocida?

---

<a id="n-22"></a>

**Pregunta N.º:** 22 (N-22)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué harías ante el defecto de unidad desconocida detectado después en la rama de Daniel?

**Respuesta modelo:** Revisaría su caso de prueba y la guardia de normalizeWeight, lo integraría y repetiría pruebas del simulador, serial, IPC y carrito. El tipo g o kg no es suficiente frente a datos runtime. Debo reconocer que 5850d61 está fuera de dev final: su existencia no permite afirmar que mi implementación integrada ya recibió esa corrección.

**Explicación para estudiar:** Las revisiones y pruebas posteriores pueden encontrar límites en un contrato inicialmente tipado. La respuesta correcta es rastrear incorporación y regresión, no atribuirse cobertura inexistente.

**Evidencia del repositorio:** [5850d61](https://github.com/rpedrazav/TallerIntegracion3/commit/5850d6111d0c5e9086bf6d156cc89fd7c9777dab) (rama, no integrado en dev): [globalmart-frontend/src/hardware/simulated-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/5850d6111d0c5e9086bf6d156cc89fd7c9777dab/globalmart-frontend/src/hardware/simulated-scale-adapter.ts), [tests/GlobalMart.IntegrationTests/ScaleParserRoundingIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/5850d6111d0c5e9086bf6d156cc89fd7c9777dab/tests/GlobalMart.IntegrationTests/ScaleParserRoundingIntegrationTests.cs); [33891a1](https://github.com/rpedrazav/TallerIntegracion3/commit/33891a1510182848eb9f99b1d02f695f669598b1) (integrado en dev): [globalmart-frontend/src/hardware/simulated-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/33891a1510182848eb9f99b1d02f695f669598b1/globalmart-frontend/src/hardware/simulated-scale-adapter.ts).

**Posible repregunta:** ¿Cómo construirías una entrada JavaScript que viole el union type?

---


### Integración con el trabajo de otros integrantes

<a id="n-23"></a>

**Pregunta N.º:** 23 (N-23)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo usa el trabajo de Rodrigo tu readStableWeightForCart?

**Respuesta modelo:** El POS solicita una lectura, el helper conecta si hace falta y valida estabilidad, kg, mínimo y precisión. Rodrigo agregó presentación y confirmación antes de mutar la venta, además del respaldo manual. La respuesta de balanza es una entrada; MS-5 sigue decidiendo si el producto y peso cumplen el contrato de negocio.

**Explicación para estudiar:** Capturar y confirmar son acciones distintas. Separarlas evita que una lectura nueva cambie una venta sin una decisión explícita del usuario.

**Evidencia del repositorio:** [352fe7f](https://github.com/rpedrazav/TallerIntegracion3/commit/352fe7fef4b5401ea6a2d7bb449c427c3616d91c) (integrado en dev): [globalmart-frontend/src/hardware/scale-cart.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/352fe7fef4b5401ea6a2d7bb449c427c3616d91c/globalmart-frontend/src/hardware/scale-cart.ts); [39b5b2a](https://github.com/rpedrazav/TallerIntegracion3/commit/39b5b2a08291ea2cf299e1a427a3165180ee3cb4) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/39b5b2a08291ea2cf299e1a427a3165180ee3cb4/globalmart-frontend/src/pages/Pos.tsx).

**Posible repregunta:** ¿Qué debe suceder al cancelar el panel de peso?

---

<a id="n-24"></a>

**Pregunta N.º:** 24 (N-24)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué necesitas acordar con Sebastián para que el outbox de tarjeta sea consistente?

**Respuesta modelo:** Debemos guardar un evento solo después de aprobación confirmada y junto con pago y estado final, manteniendo la referencia opaca. El intento ya debe estar reservado. La prueba de secuencia verifica que rechazo y timeout no generen outbox. El adaptador no debe publicar por su cuenta porque desconoce si la transacción de venta llegó a confirmarse.

**Explicación para estudiar:** Un único punto de finalización evita efectos duplicados o prematuros. La secuencia entre autorización y commit forma parte del contrato de integración.

**Evidencia del repositorio:** [ac583e8](https://github.com/rpedrazav/TallerIntegracion3/commit/ac583e8b6d50ef4b1133e21c87d4a5613a95b488) (integrado en dev): [tests/GlobalMart.IntegrationTests/PaymentEventSequenceContractTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/ac583e8b6d50ef4b1133e21c87d4a5613a95b488/tests/GlobalMart.IntegrationTests/PaymentEventSequenceContractTests.cs); [186f8d5](https://github.com/rpedrazav/TallerIntegracion3/commit/186f8d583d6a2a0e6d049784fea3885203f3c305) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/186f8d583d6a2a0e6d049784fea3885203f3c305/src/POSCartService/Repositories/VentaRepository.cs).

**Posible repregunta:** ¿Qué demostraría una fila outbox para una venta todavía pendiente?

---

<a id="n-25"></a>

**Pregunta N.º:** 25 (N-25)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué dependencia de tu outbox identificaste en el consumidor de MS-4?

**Respuesta modelo:** Que la deduplicación y el descuento deben ser durables y atómicos. El consumidor actual no confirma todos los descuentos y el marcador como una sola unidad y hace commit del offset incluso al fallar. Mi outbox no corrige eso. Para afirmar un efecto único de inventario hay que modificar y probar ese tramo, especialmente frente a caídas parciales.

**Explicación para estudiar:** Una garantía de extremo a extremo es tan fuerte como sus pasos. El productor puede conservar el evento y aun así el consumidor perder o repetir efectos.

**Evidencia del repositorio:** [054d7d3](https://github.com/rpedrazav/TallerIntegracion3/commit/054d7d382ca6a91967e96dc885b9cfb6b1558c68) (integrado en dev): [docs/context/eventos/sale-completed-outbox.md](https://github.com/rpedrazav/TallerIntegracion3/blob/054d7d382ca6a91967e96dc885b9cfb6b1558c68/docs/context/eventos/sale-completed-outbox.md); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/WarehouseInventoryService/Messaging/KafkaConsumerService.cs).

**Posible repregunta:** ¿Qué falla inyectarías entre dos ítems para probarlo?

---

<a id="n-26"></a>

**Pregunta N.º:** 26 (N-26)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo se relaciona tu balanza con DeviceManager incorporado por Diego?

**Respuesta modelo:** Coexisten una capa genérica de dispositivos y el contrato más específico de balanza. Para explicar el comportamiento debo seguir el handler que realmente utiliza el POS, no asumir que todos los adaptadores con nombres parecidos son intercambiables. La fábrica ScaleAdapter y window.api.scale son el recorrido especializado; DeviceManager tiene su propia interfaz y simuladores.

**Explicación para estudiar:** La duplicación de abstracciones puede confundir responsabilidades y pruebas. La integración debe identificar el camino efectivo de llamadas antes de consolidarlas.

**Evidencia del repositorio:** [0afc855](https://github.com/rpedrazav/TallerIntegracion3/commit/0afc855588582f8294644160dff4f418f40b2683) (integrado en dev): [globalmart-frontend/src/hardware/scale-adapter-factory.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/src/hardware/scale-adapter-factory.ts); [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [globalmart-frontend/src/devices/DeviceManager.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/devices/DeviceManager.ts); [71847b1](https://github.com/rpedrazav/TallerIntegracion3/commit/71847b18c559111107436474461d18542688b7dc) (integrado en dev): [globalmart-frontend/src/hardware/scale-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/71847b18c559111107436474461d18542688b7dc/globalmart-frontend/src/hardware/scale-ipc.ts).

**Posible repregunta:** ¿Qué riesgo habría al probar MockScaleAdapter y dar por probado SerialScaleAdapter?

---


### Logros personales, aprendizajes y mejoras

<a id="n-27"></a>

**Pregunta N.º:** 27 (N-27)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Básica.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Qué logro de resiliencia puedes demostrar sin depender de un Kafka disponible?

**Respuesta modelo:** Puedo cobrar, mostrar la fila outbox pendiente mientras el productor controlado falla y recuperarla después con el mismo EventId y payload. La prueba demuestra que no se pierde la intención durable ni se repite la venta. Debo declarar que el límite Kafka está simulado y que la prueba no certifica un broker ni inventario reales.

**Explicación para estudiar:** Una demostración controlada permite repetir fallos. Su valor depende de formular una conclusión que coincida con el componente ejercitado.

**Evidencia del repositorio:** [731a23f](https://github.com/rpedrazav/TallerIntegracion3/commit/731a23fc0f794be061a12882952ad4d003757cac) (integrado en dev): [tests/GlobalMart.IntegrationTests/OutboxBrokerRecoveryIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/731a23fc0f794be061a12882952ad4d003757cac/tests/GlobalMart.IntegrationTests/OutboxBrokerRecoveryIntegrationTests.cs).

**Posible repregunta:** ¿Qué dato compararías antes y después de recuperar el productor?

---

<a id="n-28"></a>

**Pregunta N.º:** 28 (N-28)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué aprendizaje técnico evidencia tu paso de autorización directa a consulta tras timeout?

**Respuesta modelo:** El cambio reconoce que ausencia de respuesta no equivale a rechazo. El adaptador conserva RequestId y consulta el mismo intento en lugar de repetir el POST. Esa evolución está en el código. Mi aprendizaje personal concreto debe confirmarse, pero puedo explicar la falla distribuida que la solución evita y cómo la prueba cuenta autorizaciones.

**Explicación para estudiar:** En red hay que distinguir resultado comercial y conocimiento local del resultado. La idempotencia requiere diseñar la identidad desde el comienzo.

**Evidencia del repositorio:** [1060d53](https://github.com/rpedrazav/TallerIntegracion3/commit/1060d5308c131d7ffe9e73bcd71a497bfda74101) (integrado en dev): [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1060d5308c131d7ffe9e73bcd71a497bfda74101/src/POSCartService/Services/PaymentGatewayClient.cs), [tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1060d5308c131d7ffe9e73bcd71a497bfda74101/tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs).

**Posible repregunta:** ¿Qué otra operación del proyecto tiene una ventana de incertidumbre parecida?

---

<a id="n-29"></a>

**Pregunta N.º:** 29 (N-29)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo explicarías una dificultad personal de diseño del adaptador serial sin inventar experiencia con hardware?

**Respuesta modelo:** Debo confirmar qué dificultades experimenté realmente. Puedo organizar la respuesta en restricción, alternativa evaluada, decisión y prueba. El repositorio acredita un adaptador abstracto con transporte y parser inyectables; no acredita que conecté una balanza física. Si no lo hice, debo explicar la preparación técnica y qué información quedó pendiente.

**Explicación para estudiar:** Una contribución arquitectónica puede defenderse por sus contratos y pruebas. No necesita adornarse con una integración física inexistente.

**Evidencia del repositorio:** [0afc855](https://github.com/rpedrazav/TallerIntegracion3/commit/0afc855588582f8294644160dff4f418f40b2683) (integrado en dev): [globalmart-frontend/src/hardware/serial-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/src/hardware/serial-scale-adapter.ts), [globalmart-frontend/tests/serial-scale-adapter.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/tests/serial-scale-adapter.test.cjs).

**Posible repregunta:** ¿Qué decisión del diseño facilitaría tu primer ensayo con un dispositivo?

---

<a id="n-30"></a>

**Pregunta N.º:** 30 (N-30)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué mejorarías primero en la operación del outbox y cómo lo validarías?

**Respuesta modelo:** Añadiría visibilidad de filas FALLIDO y un reproceso autorizado que conserve EventId, payload y contador total. Probaría aislamiento, auditoría, repetición y concurrencia con workers. También separaría fallos de configuración que requieren intervención de fallos transitorios. Es propuesta futura: el documento describe reproceso, pero el ciclo automático actual no entrega una herramienta completa para ejecutarlo.

**Explicación para estudiar:** Limitar reintentos evita bucles, pero crea una responsabilidad operativa. La recuperación manual debe respetar las mismas identidades y controles que la automática.

**Evidencia del repositorio:** [4cf822c](https://github.com/rpedrazav/TallerIntegracion3/commit/4cf822c347c18d987282a31b5babe3e2f3582a31) (integrado en dev): [docs/context/eventos/sale-completed-outbox.md](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/docs/context/eventos/sale-completed-outbox.md), [src/POSCartService/Services/OutboxPublisherService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Services/OutboxPublisherService.cs), [src/POSCartService/Repositories/OutboxRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4cf822c347c18d987282a31b5babe3e2f3582a31/src/POSCartService/Repositories/OutboxRepository.cs).

**Posible repregunta:** ¿Qué peligro tiene generar un nuevo EventId al reprocesar?

---


## 7. Banco de 30 preguntas — Martín

**Registro:** 30 preguntas; 10 básicas, 12 intermedias, 8 avanzadas. Numeración única en este banco.

Las respuestas describen el código o la revisión enlazada. Los resultados de ejecución son históricos. “Rama, no integrado” identifica evidencia que no forma parte de dev al corte. Las experiencias personales deben ser confirmadas por el integrante.


### Tareas, responsabilidades y contribuciones

<a id="m-01"></a>

**Pregunta N.º:** 1 (M-01)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué áreas concretas del Sprint 2 aparecen en tus commits?

**Respuesta modelo:** Trabajé en baja lógica de productos, ajustes de TenantConfig, permisos y edición/baja en administración, y contrato, simulador e IPC del terminal. También hay pruebas de interfaz y terminal en una rama no integrada. Mi banco separa esos aportes del cierre administrativo posterior de Rodrigo, que amplió backend, roles propios e inventario.

**Explicación para estudiar:** La autoría de una pantalla puede estar distribuida por etapas. Reconstruir la evolución evita atribuir a un autor todos los cambios finales.

**Evidencia del repositorio:** [cb7b8df](https://github.com/rpedrazav/TallerIntegracion3/commit/cb7b8df333e309d4e8e93e87498aa10e54d02c10) (integrado en dev): [src/CatalogPricingService/Controllers/ProductoController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/cb7b8df333e309d4e8e93e87498aa10e54d02c10/src/CatalogPricingService/Controllers/ProductoController.cs); [72c1355](https://github.com/rpedrazav/TallerIntegracion3/commit/72c135576af49ffde6f244bc8eba6236527f77c6) (integrado en dev): [globalmart-frontend/src/components/admin/editarUsuario.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/72c135576af49ffde6f244bc8eba6236527f77c6/globalmart-frontend/src/components/admin/editarUsuario.ts); [96dc728](https://github.com/rpedrazav/TallerIntegracion3/commit/96dc728f1adf215086f49f01f6271f38a43d9257) (integrado en dev): [globalmart-frontend/src/payments/terminal-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/96dc728f1adf215086f49f01f6271f38a43d9257/globalmart-frontend/src/payments/terminal-ipc.ts).

**Posible repregunta:** ¿Qué parte de Admin no estaba en tu versión inicial y llegó después?

---

<a id="m-02"></a>

**Pregunta N.º:** 2 (M-02)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué diferencia hay entre tus commits TI3-642 y TI3-643?

**Respuesta modelo:** El primero implementó el contrato de baja lógica, permisos y pruebas de conservación. El segundo ajustó la consulta activa usada por POS para que un producto desactivado no siguiera vendible por ID. Ambos son necesarios: marcar IsActive=false no sirve si el camino de lectura para vender lo ignora.

**Explicación para estudiar:** Una regla de ciclo de vida afecta escritura y lectura. La prueba debe recorrer ambos lados para demostrar su efecto observable.

**Evidencia del repositorio:** [cb7b8df](https://github.com/rpedrazav/TallerIntegracion3/commit/cb7b8df333e309d4e8e93e87498aa10e54d02c10) (integrado en dev): [src/CatalogPricingService/Controllers/ProductoController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/cb7b8df333e309d4e8e93e87498aa10e54d02c10/src/CatalogPricingService/Controllers/ProductoController.cs), [tests/GlobalMart.IntegrationTests/DesactivarProductoIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/cb7b8df333e309d4e8e93e87498aa10e54d02c10/tests/GlobalMart.IntegrationTests/DesactivarProductoIntegrationTests.cs); [4bb39aa](https://github.com/rpedrazav/TallerIntegracion3/commit/4bb39aa947e6e7586d437b8ab490c1a99c03f02e) (integrado en dev): [src/CatalogPricingService/Data/ProductoRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4bb39aa947e6e7586d437b8ab490c1a99c03f02e/src/CatalogPricingService/Data/ProductoRepository.cs), [src/CatalogPricingService/Services/ProductoService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4bb39aa947e6e7586d437b8ab490c1a99c03f02e/src/CatalogPricingService/Services/ProductoService.cs).

**Posible repregunta:** ¿Por qué no bastaba con cambiar el texto del botón Eliminar?

---

<a id="m-03"></a>

**Pregunta N.º:** 3 (M-03)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Qué entregaste al definir los permisos de Admin en TI3-672 y TI3-675?

**Respuesta modelo:** Centralicé la decisión de acceso y luego protegí las rutas administrativas con AdminRoute, incluyendo productos, y feedback de acceso denegado. La intención era impedir que CAJERO navegara a funciones de administración. Esa protección visual complementa los permisos backend; no puede sustituirlos ni ampliar permisos que el servidor no reconoce.

**Explicación para estudiar:** El frontend controla navegación y experiencia. El backend sigue siendo la frontera que autoriza datos y mutaciones.

**Evidencia del repositorio:** [7f26d0f](https://github.com/rpedrazav/TallerIntegracion3/commit/7f26d0fec50937cda0bec87a22af6b34dd41814d) (integrado en dev): [globalmart-frontend/src/utils/adminAccess.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/7f26d0fec50937cda0bec87a22af6b34dd41814d/globalmart-frontend/src/utils/adminAccess.ts), [globalmart-frontend/src/App.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/7f26d0fec50937cda0bec87a22af6b34dd41814d/globalmart-frontend/src/App.tsx); [e56597d](https://github.com/rpedrazav/TallerIntegracion3/commit/e56597d569c908301eb6f97274d2ffc89ef36acf) (integrado en dev): [globalmart-frontend/src/App.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/e56597d569c908301eb6f97274d2ffc89ef36acf/globalmart-frontend/src/App.tsx).

**Posible repregunta:** ¿Por qué ocultar un enlace no impide una llamada HTTP directa?

---

<a id="m-04"></a>

**Pregunta N.º:** 4 (M-04)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo describirías la contribución de 9b7cb4b sin afirmar que ya está integrada?

**Respuesta modelo:** Añade cobertura de estados del terminal, IPC y sincronización, y extrae helpers de single-flight. Es un commit accesible en otra rama y no ancestro de f0e6f31. Puedo defender lo implementado allí y sus pruebas, pero debo distinguirlo del estado de dev y proponer una integración con regresión antes de considerarlo parte del cierre común.

**Explicación para estudiar:** La existencia de un cambio y su incorporación a la rama de entrega son evidencias diferentes. Ambas importan al evaluar contribuciones.

**Evidencia del repositorio:** [9b7cb4b](https://github.com/rpedrazav/TallerIntegracion3/commit/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1) (rama, no integrado en dev): [globalmart-frontend/src/payments/synchronizeCardPayment.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1/globalmart-frontend/src/payments/synchronizeCardPayment.ts), [globalmart-frontend/tests/synchronized-card-payment.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1/globalmart-frontend/tests/synchronized-card-payment.test.cjs).

**Posible repregunta:** ¿Qué comportamiento adicional debe revisarse al incorporar ese commit?

---

<a id="m-05"></a>

**Pregunta N.º:** 5 (M-05)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué tareas del terminal completan tu secuencia de aportes?

**Respuesta modelo:** Definí TerminalPort y estados, implementé SimulatedTerminalAdapter, expuse IPC limitado, sincronicé la aprobación con MS-5 y agregué timeout determinista. Después amplié pruebas en la rama. El recorrido muestra un contrato que pasó de representar estados a coordinar una venta persistida; no supone lectura de una tarjeta física ni integración bancaria.

**Explicación para estudiar:** Un simulador útil debe conectarse al caso de uso y conservar sus límites. Su salida no debe convertirse por sí sola en una confirmación comercial.

**Evidencia del repositorio:** [0ff7e00](https://github.com/rpedrazav/TallerIntegracion3/commit/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a) (integrado en dev): [globalmart-frontend/src/payments/TerminalPort.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a/globalmart-frontend/src/payments/TerminalPort.ts); [ed50883](https://github.com/rpedrazav/TallerIntegracion3/commit/ed508832009bce8ffd40f13dacc64a3baa40ef73) (integrado en dev): [globalmart-frontend/src/payments/SimulatedTerminalAdapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/ed508832009bce8ffd40f13dacc64a3baa40ef73/globalmart-frontend/src/payments/SimulatedTerminalAdapter.ts); [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) (integrado en dev): [globalmart-frontend/src/payments/synchronizeCardPayment.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/src/payments/synchronizeCardPayment.ts).

**Posible repregunta:** ¿Qué commit conecta la aprobación preliminar con MS-5?

---


### Funcionalidades implementadas y explicación del código

<a id="m-06"></a>

**Pregunta N.º:** 6 (M-06)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué devuelve DELETE de producto cuando se repite y por qué?

**Respuesta modelo:** Devuelve 204 para un producto del tenant que ya está inactivo y conserva la fila sin una nueva escritura innecesaria. Si el ID no existe o es ajeno, responde 404. ADMIN es obligatorio. Así la baja es idempotente y no revela mediante respuestas distintas si existe un recurso de otro tenant.

**Explicación para estudiar:** Idempotencia significa que repetir la operación conserva el mismo efecto final; no requiere que cada ejecución modifique la base.

**Evidencia del repositorio:** [cb7b8df](https://github.com/rpedrazav/TallerIntegracion3/commit/cb7b8df333e309d4e8e93e87498aa10e54d02c10) (integrado en dev): [src/CatalogPricingService/Controllers/ProductoController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/cb7b8df333e309d4e8e93e87498aa10e54d02c10/src/CatalogPricingService/Controllers/ProductoController.cs), [src/CatalogPricingService/Data/ProductoRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/cb7b8df333e309d4e8e93e87498aa10e54d02c10/src/CatalogPricingService/Data/ProductoRepository.cs), [tests/GlobalMart.IntegrationTests/DesactivarProductoIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/cb7b8df333e309d4e8e93e87498aa10e54d02c10/tests/GlobalMart.IntegrationTests/DesactivarProductoIntegrationTests.cs).

**Posible repregunta:** ¿Cómo comprobarías que no se alteraron precio ni nombre al desactivar?

---

<a id="m-07"></a>

**Pregunta N.º:** 7 (M-07)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué mantuviste dos formas de buscar producto por ID?

**Respuesta modelo:** El camino de venta usa GetActiveByIdAsync para no ofrecer inactivos. El de actualización puede usar GetByIdAsync sin ese filtro de estado para permitir una reactivación autorizada. Ambos deben respetar tenant. Un filtro de activos aplicado indiscriminadamente impediría administrar un producto que precisamente necesita salir de la baja.

**Explicación para estudiar:** Visibilidad comercial y visibilidad administrativa son políticas distintas. Comparten aislamiento, pero no necesariamente el mismo filtro de ciclo de vida.

**Evidencia del repositorio:** [4bb39aa](https://github.com/rpedrazav/TallerIntegracion3/commit/4bb39aa947e6e7586d437b8ab490c1a99c03f02e) (integrado en dev): [src/CatalogPricingService/Data/ProductoRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4bb39aa947e6e7586d437b8ab490c1a99c03f02e/src/CatalogPricingService/Data/ProductoRepository.cs), [src/CatalogPricingService/Services/ProductoService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4bb39aa947e6e7586d437b8ab490c1a99c03f02e/src/CatalogPricingService/Services/ProductoService.cs); [cb7b8df](https://github.com/rpedrazav/TallerIntegracion3/commit/cb7b8df333e309d4e8e93e87498aa10e54d02c10) (integrado en dev): [src/CatalogPricingService/Controllers/ProductoController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/cb7b8df333e309d4e8e93e87498aa10e54d02c10/src/CatalogPricingService/Controllers/ProductoController.cs).

**Posible repregunta:** ¿Qué permisos exige cambiar IsActive mediante PUT?

---

<a id="m-08"></a>

**Pregunta N.º:** 8 (M-08)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo evita la edición de usuario borrar campos que el endpoint no edita?

**Respuesta modelo:** El formulario y helper construyen el payload del contrato real y procesan la respuesta confirmada. No deberían enviar contraseña, roles o estado como parte de una edición básica. Mis pruebas cubren el flujo y errores. El backend fue unificado después sobre Usuarios; por eso para describir dev final debo revisar también el cambio de Rodrigo.

**Explicación para estudiar:** Un formulario puede mostrar más información de la que una operación permite modificar. Mantener un DTO acotado reduce cambios accidentales y confusión de permisos.

**Evidencia del repositorio:** [72c1355](https://github.com/rpedrazav/TallerIntegracion3/commit/72c135576af49ffde6f244bc8eba6236527f77c6) (integrado en dev): [globalmart-frontend/src/components/admin/editarUsuario.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/72c135576af49ffde6f244bc8eba6236527f77c6/globalmart-frontend/src/components/admin/editarUsuario.ts), [globalmart-frontend/src/components/admin/UsuariosList.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/72c135576af49ffde6f244bc8eba6236527f77c6/globalmart-frontend/src/components/admin/UsuariosList.tsx); [bfd46b0](https://github.com/rpedrazav/TallerIntegracion3/commit/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1) (integrado en dev): [src/TenantIdentityService/Repositories/UsuarioRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1/src/TenantIdentityService/Repositories/UsuarioRepository.cs).

**Posible repregunta:** ¿Por qué hay que distinguir una edición básica de asignar roles?

---

<a id="m-09"></a>

**Pregunta N.º:** 9 (M-09)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo maneja tu baja de usuario la lista y los errores?

**Respuesta modelo:** Solicita confirmación, llama DELETE y refleja al usuario como inactivo tras éxito en lugar de asumir eliminación física. Los errores deben conservar los datos y dar feedback. En su momento documenté la dependencia de que el backend listara inactivos tras recarga. Esa limitación se debe explicar históricamente y contrastar con la unificación administrativa posterior.

**Explicación para estudiar:** Actualización local y reconsulta al servidor tienen que representar el mismo estado. Una UI correcta durante unos segundos no garantiza persistencia.

**Evidencia del repositorio:** [97dc4e7](https://github.com/rpedrazav/TallerIntegracion3/commit/97dc4e768a7fee4ab18133bf962d9d004880da17) (integrado en dev): [globalmart-frontend/src/components/admin/desactivarUsuario.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/97dc4e768a7fee4ab18133bf962d9d004880da17/globalmart-frontend/src/components/admin/desactivarUsuario.ts), [globalmart-frontend/src/components/admin/UsuariosList.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/97dc4e768a7fee4ab18133bf962d9d004880da17/globalmart-frontend/src/components/admin/UsuariosList.tsx); [dbd2b09](https://github.com/rpedrazav/TallerIntegracion3/commit/dbd2b096d545cd5476821dd3a2baa4f420db2eca) (integrado en dev): [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/dbd2b096d545cd5476821dd3a2baa4f420db2eca/docs/context/estado-actual.md).

**Posible repregunta:** ¿Qué pasaría si el listado ocultara automáticamente los inactivos?

---

<a id="m-10"></a>

**Pregunta N.º:** 10 (M-10)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué información transporta TerminalPort sin manejar datos de tarjeta?

**Respuesta modelo:** Transporta venta, monto, clave o referencia del intento, estado y referencia opaca. Define inicio, consulta y cancelación como capacidades del contrato. No recibe PAN, CVV ni comandos de terminal. La implementación MS-5 informa explícitamente cuando la cancelación no está soportada, en vez de fingir que anuló un cobro.

**Explicación para estudiar:** Un contrato puede describir capacidades opcionales con errores tipados. La interfaz no debe prometer efectos que el proveedor no implementa.

**Evidencia del repositorio:** [0ff7e00](https://github.com/rpedrazav/TallerIntegracion3/commit/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a) (integrado en dev): [globalmart-frontend/src/payments/TerminalPort.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a/globalmart-frontend/src/payments/TerminalPort.ts), [globalmart-frontend/src/payments/Ms5TerminalAdapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a/globalmart-frontend/src/payments/Ms5TerminalAdapter.ts).

**Posible repregunta:** ¿Qué endpoint faltaría para una cancelación real coordinada con MS-5?

---

<a id="m-11"></a>

**Pregunta N.º:** 11 (M-11)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo impide synchronizeCardPayment aceptar la respuesta de otro intento?

**Respuesta modelo:** Compara referencias sin distinguir mayúsculas y exige que la respuesta del terminal conserve la clave esperada. Para confirmar aprobación de MS-5 exige venta correcta y COMPLETADA. La reconciliación también comprueba RequestId. Si no puede correlacionar, devuelve uncertain y no permite que una respuesta ajena cierre el carrito como cobrado.

**Explicación para estudiar:** Correlación es parte de integridad, no solo de logging. Una respuesta válida de otra operación sigue siendo inválida para la venta actual.

**Evidencia del repositorio:** [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) (integrado en dev): [globalmart-frontend/src/payments/synchronizeCardPayment.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/src/payments/synchronizeCardPayment.ts), [globalmart-frontend/tests/synchronized-card-payment.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/tests/synchronized-card-payment.test.cjs).

**Posible repregunta:** ¿Por qué no basta con revisar status APPROVED?

---

<a id="m-12"></a>

**Pregunta N.º:** 12 (M-12)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué la sincronización no debe considerar approved del simulador como autoridad final?

**Respuesta modelo:** El simulador representa la interacción del terminal, pero la venta solo queda confirmada cuando MS-5 persiste su resultado. Mi función envía o consulta con la misma clave y exige el estado comercial final. Si el backend falla tras enviar, reconcilia sin generar otra clave. Esto evita vaciar el carrito sobre una aprobación únicamente visual.

**Explicación para estudiar:** Los estados de dispositivos y de negocio tienen propietarios distintos. Integrarlos requiere condiciones explícitas para pasar de uno al otro.

**Evidencia del repositorio:** [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) (integrado en dev): [globalmart-frontend/src/payments/synchronizeCardPayment.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/src/payments/synchronizeCardPayment.ts).

**Posible repregunta:** ¿Qué resultado devuelve si la consulta falla después del error de cobro?

---

<a id="m-13"></a>

**Pregunta N.º:** 13 (M-13)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo funciona el timeout determinista de SimulatedTerminalAdapter?

**Respuesta modelo:** El escenario mantiene processing hasta transcurrir el delay configurado y luego entrega uncertain. Conserva venta, monto y referencia del intento; no crea una nueva operación al consultar. Eso permite probar una espera reproducible sin depender de una red o terminal físico, diferenciándola de rechazo definitivo.

**Explicación para estudiar:** El tiempo puede formar parte de un escenario controlado. La prueba debe observar identidad y transición, no solo que se produjo una demora.

**Evidencia del repositorio:** [de5bf27](https://github.com/rpedrazav/TallerIntegracion3/commit/de5bf2760b1ebfd5613f6483cb594ee3bb02f461) (integrado en dev): [globalmart-frontend/src/payments/SimulatedTerminalAdapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/de5bf2760b1ebfd5613f6483cb594ee3bb02f461/globalmart-frontend/src/payments/SimulatedTerminalAdapter.ts), [globalmart-frontend/tests/terminal-port.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/de5bf2760b1ebfd5613f6483cb594ee3bb02f461/globalmart-frontend/tests/terminal-port.test.cjs).

**Posible repregunta:** ¿Qué diferencia observable hay entre timeout y declined?

---


### Decisiones técnicas, tecnologías y arquitectura

<a id="m-14"></a>

**Pregunta N.º:** 14 (M-14)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué elegiste una máquina de estados separada del componente React?

**Respuesta modelo:** Centraliza transiciones y hace explícitos available, processing, approved, declined, uncertain y cancelled. Puede probarse sin montar toda la interfaz. React consume el resultado para mostrar feedback. Esto reduce la posibilidad de que distintos botones interpreten de manera diferente el mismo estado del terminal.

**Explicación para estudiar:** Una máquina de estados expresa qué transiciones tienen sentido. No reemplaza la confirmación backend, pero organiza cómo la interfaz reacciona a ella.

**Evidencia del repositorio:** [0ff7e00](https://github.com/rpedrazav/TallerIntegracion3/commit/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a) (integrado en dev): [globalmart-frontend/src/payments/terminalStateMachine.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a/globalmart-frontend/src/payments/terminalStateMachine.ts), [globalmart-frontend/tests/terminal-port.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a/globalmart-frontend/tests/terminal-port.test.cjs).

**Posible repregunta:** ¿Por qué uncertain debe tratarse distinto de declined al permitir otro intento?

---

<a id="m-15"></a>

**Pregunta N.º:** 15 (M-15)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué significa real en la fábrica del terminal y qué no deberías afirmar?

**Respuesta modelo:** La fábrica distingue modo simulado y el adaptador que conversa con MS-5. El nombre real no demuestra conexión a un pinpad bancario físico. Debo explicar qué implementación devuelve createTerminalPort y qué capacidad de transporte tiene en el contexto donde se ejecuta. Las limitaciones de IPC y cancelación siguen siendo relevantes aunque el nombre sugiera otra cosa.

**Explicación para estudiar:** Los nombres de configuración no son evidencia de capacidad. Hay que seguir la instancia creada y sus llamadas efectivas.

**Evidencia del repositorio:** [ed50883](https://github.com/rpedrazav/TallerIntegracion3/commit/ed508832009bce8ffd40f13dacc64a3baa40ef73) (integrado en dev): [globalmart-frontend/src/payments/createTerminalPort.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/ed508832009bce8ffd40f13dacc64a3baa40ef73/globalmart-frontend/src/payments/createTerminalPort.ts); [0ff7e00](https://github.com/rpedrazav/TallerIntegracion3/commit/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a) (integrado en dev): [globalmart-frontend/src/payments/Ms5TerminalAdapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a/globalmart-frontend/src/payments/Ms5TerminalAdapter.ts); [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [globalmart-frontend/src/devices/adapters/RealTerminalAdapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/devices/adapters/RealTerminalAdapter.ts).

**Posible repregunta:** ¿Qué diferencia hay con RealTerminalAdapter de la capa DeviceManager?

---

<a id="m-16"></a>

**Pregunta N.º:** 16 (M-16)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué el IPC del terminal rechaza propiedades extra en vez de confiar en los tipos?

**Respuesta modelo:** La entrada llega en ejecución desde otro proceso. parsePlainObject, la lista de propiedades, UUID, referencia segura y monto finito acotan lo aceptado. Una propiedad PAN o command no forma parte del contrato y se rechaza. La salida se proyecta a campos permitidos, evitando exponer accidentalmente datos internos de otro adaptador.

**Explicación para estudiar:** Serialización y tipado estático no constituyen una frontera de autorización. Una lista positiva de capacidades y campos reduce superficie de error y abuso.

**Evidencia del repositorio:** [96dc728](https://github.com/rpedrazav/TallerIntegracion3/commit/96dc728f1adf215086f49f01f6271f38a43d9257) (integrado en dev): [globalmart-frontend/src/payments/terminal-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/96dc728f1adf215086f49f01f6271f38a43d9257/globalmart-frontend/src/payments/terminal-ipc.ts).

**Posible repregunta:** ¿Qué pasaría si un objeto heredara propiedades de un prototipo inesperado?

---

<a id="m-17"></a>

**Pregunta N.º:** 17 (M-17)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué garantiza single-flight en tu rama y por qué aún necesitas idempotencia backend?

**Respuesta modelo:** El helper toma un lock en memoria para evitar dos ejecuciones simultáneas de la UI y lo libera al terminar. Eso reduce dobles clics dentro de ese proceso, pero no coordina otra ventana, recarga o instancia. La reserva durable y clave en MS-5 siguen siendo necesarias. Además el commit con esos helpers está pendiente de integración en dev.

**Explicación para estudiar:** Exclusión local y deduplicación distribuida operan en ámbitos distintos. La segunda sigue protegiendo el negocio cuando la primera desaparece.

**Evidencia del repositorio:** [9b7cb4b](https://github.com/rpedrazav/TallerIntegracion3/commit/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1) (rama, no integrado en dev): [globalmart-frontend/src/payments/synchronizeCardPayment.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1/globalmart-frontend/src/payments/synchronizeCardPayment.ts), [globalmart-frontend/tests/synchronized-card-payment.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1/globalmart-frontend/tests/synchronized-card-payment.test.cjs); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/POSCartService/Repositories/VentaRepository.cs).

**Posible repregunta:** ¿Qué ocurre si el proceso se cierra mientras la pasarela sigue trabajando?

---

<a id="m-18"></a>

**Pregunta N.º:** 18 (M-18)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo separaste errores HTTP de rechazo comercial al trabajar con el terminal?

**Respuesta modelo:** La interfaz representa rechazo definitivo, procesamiento e incertidumbre en vez de traducir cualquier excepción a rechazo. La sincronización consulta MS-5 tras un resultado ambiguo del cobro y conserva referencia. Un error HTTP puede ocurrir después de la autorización, por lo que no corresponde habilitar automáticamente otra operación como si nada hubiese pasado.

**Explicación para estudiar:** El transporte describe si recibimos respuesta; el negocio describe si se autorizó. Mezclarlos genera reintentos peligrosos.

**Evidencia del repositorio:** [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) (integrado en dev): [globalmart-frontend/src/payments/synchronizeCardPayment.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/src/payments/synchronizeCardPayment.ts); [0ff7e00](https://github.com/rpedrazav/TallerIntegracion3/commit/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a) (integrado en dev): [globalmart-frontend/src/payments/terminalStateMachine.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a/globalmart-frontend/src/payments/terminalStateMachine.ts).

**Posible repregunta:** ¿Qué estado usarías ante una respuesta con referencia inconsistente?

---


### Problemas encontrados y soluciones

<a id="m-19"></a>

**Pregunta N.º:** 19 (M-19)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué corrigieron tus cambios de TenantConfig y cómo lo probaste?

**Respuesta modelo:** Ajusté los tipos del formulario y su validación para que entradas y datos enviados fueran coherentes. Añadí pruebas de esquema y del flujo por tenant con respuestas controladas. No basta un cambio para silenciar TypeScript: el valor que llega a la API debe representar lo que el usuario ingresó, especialmente el porcentaje de IVA.

**Explicación para estudiar:** Los formularios convierten texto de UI en valores de dominio. Tipos, conversión y validación deben concordar para no guardar un número distinto del esperado.

**Evidencia del repositorio:** [b4f6d20](https://github.com/rpedrazav/TallerIntegracion3/commit/b4f6d20d5d9f6cdc9e85b73124b56e29bc5a7073) (integrado en dev): [globalmart-frontend/src/components/admin/TenantConfig.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/b4f6d20d5d9f6cdc9e85b73124b56e29bc5a7073/globalmart-frontend/src/components/admin/TenantConfig.tsx); [577fbd1](https://github.com/rpedrazav/TallerIntegracion3/commit/577fbd1f3d3cfb3bc792661ae928374eebe04dcc) (integrado en dev): [globalmart-frontend/src/components/admin/TenantConfig.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/577fbd1f3d3cfb3bc792661ae928374eebe04dcc/globalmart-frontend/src/components/admin/TenantConfig.tsx).

**Posible repregunta:** ¿Qué diferencia hay entre un error de tipos y un 400 del servidor?

---

<a id="m-20"></a>

**Pregunta N.º:** 20 (M-20)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo comprobaste que desactivar producto no destruyera ventas históricas?

**Respuesta modelo:** Preparé una venta completada con un ítem referido al producto y la consulté antes y después de la baja. La prueba compara IDs, nombre, precio, cantidad y totales, además de confirmar IsActive=false. Así el comportamiento se verifica entre catálogo y la consulta de venta, no solo mirando una fila de producto.

**Explicación para estudiar:** Los snapshots protegen hechos históricos frente a cambios del maestro. El vínculo con el producto no debe obligar a reconstruir el pasado desde datos actuales.

**Evidencia del repositorio:** [cb7b8df](https://github.com/rpedrazav/TallerIntegracion3/commit/cb7b8df333e309d4e8e93e87498aa10e54d02c10) (integrado en dev): [tests/GlobalMart.IntegrationTests/DesactivarProductoIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/cb7b8df333e309d4e8e93e87498aa10e54d02c10/tests/GlobalMart.IntegrationTests/DesactivarProductoIntegrationTests.cs).

**Posible repregunta:** ¿Qué fallaría si el listado histórico filtrara sus ítems por producto activo?

---

<a id="m-21"></a>

**Pregunta N.º:** 21 (M-21)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué dificultad de permisos detectaste al proteger rutas de administración?

**Respuesta modelo:** Proteger el enlace principal no cubría necesariamente rutas hijas como productos. Centralicé la comprobación mediante AdminRoute y añadí feedback de acceso denegado. Aun así, el servidor debe rechazar una llamada directa sin ADMIN. Las pruebas de UI y de API se complementan, y la existencia de SUPER_ADMIN visual no permite ignorar las políticas reales del endpoint.

**Explicación para estudiar:** La navegación tiene múltiples entradas. Una política consistente debe aplicarse en el contenedor adecuado y repetirse donde se protegen datos.

**Evidencia del repositorio:** [e56597d](https://github.com/rpedrazav/TallerIntegracion3/commit/e56597d569c908301eb6f97274d2ffc89ef36acf) (integrado en dev): [globalmart-frontend/src/App.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/e56597d569c908301eb6f97274d2ffc89ef36acf/globalmart-frontend/src/App.tsx); [7f26d0f](https://github.com/rpedrazav/TallerIntegracion3/commit/7f26d0fec50937cda0bec87a22af6b34dd41814d) (integrado en dev): [globalmart-frontend/src/utils/adminAccess.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/7f26d0fec50937cda0bec87a22af6b34dd41814d/globalmart-frontend/src/utils/adminAccess.ts).

**Posible repregunta:** ¿Cómo probarías entrar escribiendo directamente la ruta administrativa?

---

<a id="m-22"></a>

**Pregunta N.º:** 22 (M-22)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo evitarías perder estado al recibir una respuesta tardía del terminal?

**Respuesta modelo:** Conservaría venta, monto y referencia del intento y aceptaría resultados solo si corresponden al ciclo esperado. Mi timeout no cambia identidad y la sincronización verifica correlación. Una respuesta tardía no debe aplicarse a otra venta ni habilitar un nuevo cobro automáticamente. La validación debe incluir consultas repetidas y transiciones fuera de orden.

**Explicación para estudiar:** La interfaz necesita identificar el ciclo al que pertenece cada respuesta asíncrona. El orden de llegada no garantiza el orden de negocio.

**Evidencia del repositorio:** [de5bf27](https://github.com/rpedrazav/TallerIntegracion3/commit/de5bf2760b1ebfd5613f6483cb594ee3bb02f461) (integrado en dev): [globalmart-frontend/src/payments/SimulatedTerminalAdapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/de5bf2760b1ebfd5613f6483cb594ee3bb02f461/globalmart-frontend/src/payments/SimulatedTerminalAdapter.ts); [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) (integrado en dev): [globalmart-frontend/tests/synchronized-card-payment.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/tests/synchronized-card-payment.test.cjs).

**Posible repregunta:** ¿Qué prueba añadirías para cambiar de venta mientras queda una consulta pendiente?

---


### Integración con el trabajo de otros integrantes

<a id="m-23"></a>

**Pregunta N.º:** 23 (M-23)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo se integra tu terminal con el modal de Rodrigo?

**Respuesta modelo:** El modal presenta estados y acciones; mi contrato y sincronización determinan qué estado está confirmado y qué referencia conservar. El resultado aprobado debe incluir la venta confirmada por MS-5 antes de limpiar el carrito. No diseñé un pago separado de ese flujo: la intención es que la experiencia visual siga los mismos estados durables del backend.

**Explicación para estudiar:** La interfaz representa decisiones de negocio, no las sustituye. Un contrato compartido reduce discrepancias entre mensajes y efectos reales.

**Evidencia del repositorio:** [0ff7e00](https://github.com/rpedrazav/TallerIntegracion3/commit/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a) (integrado en dev): [globalmart-frontend/src/components/pos/PaymentModal.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a/globalmart-frontend/src/components/pos/PaymentModal.tsx); [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) (integrado en dev): [globalmart-frontend/src/payments/synchronizeCardPayment.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/src/payments/synchronizeCardPayment.ts); [32d1fb5](https://github.com/rpedrazav/TallerIntegracion3/commit/32d1fb5a69a4296941fde148642d0dad85326013) (integrado en dev): [globalmart-frontend/src/components/pos/PaymentModal.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/32d1fb5a69a4296941fde148642d0dad85326013/globalmart-frontend/src/components/pos/PaymentModal.tsx).

**Posible repregunta:** ¿Qué mensaje debería ver el cajero después de un resultado incierto?

---

<a id="m-24"></a>

**Pregunta N.º:** 24 (M-24)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué dependías del backend para que tu desactivación de usuario funcionara después de recargar?

**Respuesta modelo:** Dependía de que DELETE actuara sobre la cuenta usada por login y de que el listado incluyera el estado inactivo. Mi cambio visual documentó esa dependencia. Rodrigo luego unificó las operaciones sobre Usuarios. En la defensa debo distinguir mi interacción y manejo de errores del cambio posterior que cerró la coherencia de persistencia.

**Explicación para estudiar:** Un formulario no puede corregir una divergencia entre tablas backend. La integración se valida observando los siguientes usos de la entidad.

**Evidencia del repositorio:** [97dc4e7](https://github.com/rpedrazav/TallerIntegracion3/commit/97dc4e768a7fee4ab18133bf962d9d004880da17) (integrado en dev): [globalmart-frontend/src/components/admin/desactivarUsuario.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/97dc4e768a7fee4ab18133bf962d9d004880da17/globalmart-frontend/src/components/admin/desactivarUsuario.ts); [bfd46b0](https://github.com/rpedrazav/TallerIntegracion3/commit/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1) (integrado en dev): [src/TenantIdentityService/Repositories/UsuarioRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1/src/TenantIdentityService/Repositories/UsuarioRepository.cs).

**Posible repregunta:** ¿Qué secuencia probarías desde crear usuario hasta intentar iniciar sesión tras la baja?

---

<a id="m-25"></a>

**Pregunta N.º:** 25 (M-25)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué deben acordar tus estados de terminal y la persistencia de Sebastián para permitir un reintento seguro?

**Respuesta modelo:** El rechazo definitivo puede permitir un intento manual nuevo, pero la incertidumbre debe conservar la referencia anterior y bloquear autorización adicional. MS-5 decide si existe un intento no rechazado y la interfaz debe respetarlo. No basta con reiniciar mi máquina a available si el servidor conserva una reserva pendiente.

**Explicación para estudiar:** El estado durable prevalece sobre el estado visual reconstruido. Reiniciar una pantalla no elimina efectos externos ni reservas de negocio.

**Evidencia del repositorio:** [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) (integrado en dev): [globalmart-frontend/src/payments/synchronizeCardPayment.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/src/payments/synchronizeCardPayment.ts); [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/src/POSCartService/Repositories/VentaRepository.cs), [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md).

**Posible repregunta:** ¿Qué harías al recargar si el último intento sigue PENDIENTE?

---

<a id="m-26"></a>

**Pregunta N.º:** 26 (M-26)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué tus pruebas Admin de 3eb4cb4 no sustituyen las pruebas finales de Rodrigo?

**Respuesta modelo:** Corresponden a otra revisión y una rama no integrada, con fixtures HTTP para alta, edición, baja y configuración. Rodrigo añadió después otro conjunto de escenarios en dev para administración ampliada, inventario y roles. Puedo defender mi cobertura y mejoras de feedback, pero no asumir que ambas suites son idénticas o que ejecutan las mismas dependencias reales.

**Explicación para estudiar:** Dos archivos con el mismo nombre en ramas distintas pueden representar pruebas diferentes. La referencia al SHA evita mezclar resultados.

**Evidencia del repositorio:** [3eb4cb4](https://github.com/rpedrazav/TallerIntegracion3/commit/3eb4cb47955ee8976aaf1c108a95bfcab00bc875) (rama, no integrado en dev): [globalmart-frontend/e2e/admin.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/3eb4cb47955ee8976aaf1c108a95bfcab00bc875/globalmart-frontend/e2e/admin.spec.ts); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (integrado en dev): [globalmart-frontend/e2e/admin.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/globalmart-frontend/e2e/admin.spec.ts).

**Posible repregunta:** ¿Cómo decidirías qué casos conservar al integrar ambas versiones?

---


### Logros personales, aprendizajes y mejoras

<a id="m-27"></a>

**Pregunta N.º:** 27 (M-27)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Básica.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Qué logro personal de catálogo puedes demostrar en una secuencia breve?

**Respuesta modelo:** Con ADMIN desactivo un producto, repito la baja y compruebo 204; con el camino de venta ya no lo obtengo como activo. La venta histórica conserva su snapshot. Esa secuencia muestra permiso, idempotencia y trazabilidad. Para presentarla como ejecución actual necesitaría correrla en el entorno de defensa; aquí la respaldo con la prueba versionada.

**Explicación para estudiar:** Una demostración acotada debe evidenciar la consecuencia de la regla y el dato que se conserva, no solo una notificación de éxito.

**Evidencia del repositorio:** [cb7b8df](https://github.com/rpedrazav/TallerIntegracion3/commit/cb7b8df333e309d4e8e93e87498aa10e54d02c10) (integrado en dev): [tests/GlobalMart.IntegrationTests/DesactivarProductoIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/cb7b8df333e309d4e8e93e87498aa10e54d02c10/tests/GlobalMart.IntegrationTests/DesactivarProductoIntegrationTests.cs); [4bb39aa](https://github.com/rpedrazav/TallerIntegracion3/commit/4bb39aa947e6e7586d437b8ab490c1a99c03f02e) (integrado en dev): [src/CatalogPricingService/Data/ProductoRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/4bb39aa947e6e7586d437b8ab490c1a99c03f02e/src/CatalogPricingService/Data/ProductoRepository.cs).

**Posible repregunta:** ¿Qué diferencia esperas al ejecutar DELETE con un CAJERO?

---

<a id="m-28"></a>

**Pregunta N.º:** 28 (M-28)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué aprendizaje técnico muestra la evolución de tu terminal durante el sprint?

**Respuesta modelo:** Pasó de un contrato y una máquina de estados a simulación, IPC y sincronización con la venta autoritativa. El código evidencia que separar esos niveles ayuda a probarlos y a no confundir aprobación visual con cobro. La experiencia personal concreta que aprendí debo confirmarla, pero esa evolución sí puede mostrarse en los commits.

**Explicación para estudiar:** Una abstracción es útil cuando permite agregar comportamiento sin romper el contrato de sus consumidores. La prueba de sincronización verifica el límite comercial.

**Evidencia del repositorio:** [0ff7e00](https://github.com/rpedrazav/TallerIntegracion3/commit/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a) (integrado en dev): [globalmart-frontend/src/payments/TerminalPort.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a/globalmart-frontend/src/payments/TerminalPort.ts); [96dc728](https://github.com/rpedrazav/TallerIntegracion3/commit/96dc728f1adf215086f49f01f6271f38a43d9257) (integrado en dev): [globalmart-frontend/src/payments/terminal-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/96dc728f1adf215086f49f01f6271f38a43d9257/globalmart-frontend/src/payments/terminal-ipc.ts); [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) (integrado en dev): [globalmart-frontend/src/payments/synchronizeCardPayment.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/src/payments/synchronizeCardPayment.ts).

**Posible repregunta:** ¿Cuál fue el cambio que más redujo un falso positivo de aprobación?

---

<a id="m-29"></a>

**Pregunta N.º:** 29 (M-29)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué experiencia personal sobre integración de tu rama necesitas confirmar antes de la defensa?

**Respuesta modelo:** Debo confirmar qué conflicto resolví, cómo lo coordiné y qué prueba ejecuté personalmente. Git muestra merges y pruebas nuevas, pero no mi razonamiento ni conversaciones. Puedo responder con problema concreto, alternativas, decisión y evidencia, señalando que 9b7cb4b sigue fuera de dev y sin presentarlo como una entrega ya incorporada.

**Explicación para estudiar:** La reflexión debe ser auténtica y específica. La evidencia técnica respalda el resultado, mientras la narración personal requiere confirmación del autor.

**Evidencia del repositorio:** [9b7cb4b](https://github.com/rpedrazav/TallerIntegracion3/commit/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1) (rama, no integrado en dev): [globalmart-frontend/tests/synchronized-card-payment.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/9b7cb4b9a84b7c685269906c515fa7ec3941d9c1/globalmart-frontend/tests/synchronized-card-payment.test.cjs).

**Posible repregunta:** ¿Qué comprobarías primero antes de integrar tus pruebas del terminal?

---

<a id="m-30"></a>

**Pregunta N.º:** 30 (M-30)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué mejora priorizarías en el contrato de cancelación del terminal?

**Respuesta modelo:** Definiría una cancelación coordinada con MS-5 y la pasarela, distinguiendo cancelar una interacción local de revertir una autorización. Haría explícitos los estados permitidos, idempotencia y recuperación ante incertidumbre. Actualmente Ms5TerminalAdapter devuelve cancelación no soportada; no cambiaría solo el mensaje visual para aparentar que se anuló el pago.

**Explicación para estudiar:** Cancelación, rechazo y reembolso son operaciones diferentes. Una mejora correcta necesita contrato y efectos verificables en todas las capas involucradas.

**Evidencia del repositorio:** [0ff7e00](https://github.com/rpedrazav/TallerIntegracion3/commit/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a) (integrado en dev): [globalmart-frontend/src/payments/Ms5TerminalAdapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a/globalmart-frontend/src/payments/Ms5TerminalAdapter.ts), [globalmart-frontend/src/payments/TerminalPort.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0ff7e00a9de2596adf4f5f73f55ac4156c01dc9a/globalmart-frontend/src/payments/TerminalPort.ts).

**Posible repregunta:** ¿Qué debería pasar si llega aprobación mientras se solicita cancelar?

---


## 8. Banco de 30 preguntas — Rodrigo

**Registro:** 30 preguntas; 10 básicas, 12 intermedias, 8 avanzadas. Numeración única en este banco.

Las respuestas describen el código o la revisión enlazada. Los resultados de ejecución son históricos. “Rama, no integrado” identifica evidencia que no forma parte de dev al corte. Las experiencias personales deben ser confirmadas por el integrante.


### Tareas, responsabilidades y contribuciones

<a id="r-01"></a>

**Pregunta N.º:** 1 (R-01)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué contribuciones tuyas conectan planificación y producto durante este sprint?

**Respuesta modelo:** Incorporé el plan revisado y su correspondencia con YouTrack, centralicé clientes API, integré carrito y cobros, añadí la UX de peso y cerré funciones administrativas. No presentaría cada fila del plan como completada por mí. La atribución técnica se apoya en mis diffs y distingue componentes previos o aportados por otros integrantes.

**Explicación para estudiar:** Planificar y ejecutar son actividades relacionadas con evidencias diferentes. El plan identifica intención; el código y pruebas muestran entrega.

**Evidencia del repositorio:** [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) (integrado en dev): [tareas/plan_sprint2_revisado.xlsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/plan_sprint2_revisado.xlsx), [tareas/sprint2_youtrack_resultado.json](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/sprint2_youtrack_resultado.json); [160846e](https://github.com/rpedrazav/TallerIntegracion3/commit/160846eb68bac27937560baabb6a10d4ed545c73) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/160846eb68bac27937560baabb6a10d4ed545c73/globalmart-frontend/src/pages/Pos.tsx); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (integrado en dev): [globalmart-frontend/src/pages/AdminRoles.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/globalmart-frontend/src/pages/AdminRoles.tsx).

**Posible repregunta:** ¿Qué condición del plan todavía no está cerrada aunque avanzó el POS?

---

<a id="r-02"></a>

**Pregunta N.º:** 2 (R-02)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué cambió al centralizar los clientes de login, catálogo y turnos?

**Respuesta modelo:** Se reunieron URL base, token, timeout y tratamiento de 401 en clientes tipados, y las pantallas pasaron a consumirlos. Eso reduce duplicación de llamadas y manejo desigual de sesión. Ese primer cambio todavía mantenía carrito local; la integración de ventas llegó después. Debo distinguir centralizar transporte de completar un flujo funcional.

**Explicación para estudiar:** Una refactorización de acceso a API facilita consistencia, pero no garantiza que cada pantalla utilice ya las operaciones de negocio necesarias.

**Evidencia del repositorio:** [e1a2691](https://github.com/rpedrazav/TallerIntegracion3/commit/e1a269113ceeef94ed89dd79502b830fc2557ab2) (integrado en dev): [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/e1a269113ceeef94ed89dd79502b830fc2557ab2/globalmart-frontend/src/api/client.ts); [160846e](https://github.com/rpedrazav/TallerIntegracion3/commit/160846eb68bac27937560baabb6a10d4ed545c73) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/160846eb68bac27937560baabb6a10d4ed545c73/globalmart-frontend/src/pages/Pos.tsx).

**Posible repregunta:** ¿Qué commit posterior crea y recupera una venta persistida?

---

<a id="r-03"></a>

**Pregunta N.º:** 3 (R-03)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Cómo separas tu recuperación de pagos del cobro seguro de Sebastián?

**Respuesta modelo:** Mi cambio añadió reserva y recuperación del intento tras timeout y consulta al recargar; Sebastián reforzó validación y reserva segura concurrente. El merge combinó ambas implementaciones sin duplicar reservas. Puedo describir mi aporte y el resultado integrado, pero no atribuirme exclusivamente toda la protección final contra doble cobro.

**Explicación para estudiar:** La autoría del comportamiento final puede ser conjunta, especialmente cuando dos ramas resuelven partes del mismo problema.

**Evidencia del repositorio:** [f6727db](https://github.com/rpedrazav/TallerIntegracion3/commit/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d/src/POSCartService/Controllers/VentasController.cs), [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d/globalmart-frontend/src/pages/Pos.tsx); [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md).

**Posible repregunta:** ¿Qué semántica de estado incierto se conservó en el merge?

---

<a id="r-04"></a>

**Pregunta N.º:** 4 (R-04)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué tus tres commits administrativos finales deben estudiarse separados de la UI Admin previa?

**Respuesta modelo:** Amplían el alcance: aprovisionamiento ADMIN, coherencia de Usuarios, inventario por sucursal, despliegue de MS-4 y roles propios con permisos heredados. Martín ya había construido edición, baja y protección de rutas. Mis commits cierran dependencias y agregan capacidades; no son evidencia de que yo haya originado todo el módulo administrativo.

**Explicación para estudiar:** Reconstruir etapas permite reconocer colaboración y explicar qué problema concreto resuelve cada ampliación.

**Evidencia del repositorio:** [bfd46b0](https://github.com/rpedrazav/TallerIntegracion3/commit/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1) (integrado en dev): [src/TenantIdentityService/Repositories/UsuarioRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1/src/TenantIdentityService/Repositories/UsuarioRepository.cs), [src/TenantIdentityService/Services/AdminBootstrap.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1/src/TenantIdentityService/Services/AdminBootstrap.cs); [dbd2b09](https://github.com/rpedrazav/TallerIntegracion3/commit/dbd2b096d545cd5476821dd3a2baa4f420db2eca) (integrado en dev): [k8s/manifests/warehouse-inventory.yaml](https://github.com/rpedrazav/TallerIntegracion3/blob/dbd2b096d545cd5476821dd3a2baa4f420db2eca/k8s/manifests/warehouse-inventory.yaml); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (integrado en dev): [src/TenantIdentityService/Services/JwtService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/TenantIdentityService/Services/JwtService.cs).

**Posible repregunta:** ¿Qué discrepancia entre tablas solucionaste para que la baja afectara el login?

---

<a id="r-05"></a>

**Pregunta N.º:** 5 (R-05)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué representan los dos objetos de stash encontrados con tu autor?

**Respuesta modelo:** Son instantáneas auxiliares del estado de trabajo de GitHub Desktop, con cambios de API y despliegue. No los contaría como entregas ni commits integrados del sprint. Se inventarían para no ocultar historial, pero la contribución terminada debe buscarse en commits de trabajo y su estado de integración. Tampoco probarían por sí solos una funcionalidad desplegada.

**Explicación para estudiar:** Un stash puede tener padres y archivos como un commit, pero su propósito es guardar trabajo temporal. Su existencia no equivale a aceptación.

**Evidencia del repositorio:** [e1a2691](https://github.com/rpedrazav/TallerIntegracion3/commit/e1a269113ceeef94ed89dd79502b830fc2557ab2) (integrado en dev): [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/e1a269113ceeef94ed89dd79502b830fc2557ab2/globalmart-frontend/src/api/client.ts); [1331133](https://github.com/rpedrazav/TallerIntegracion3/commit/1331133587ec9818ab5bc85cb17934cb31ceee56) (integrado en dev): [new-ingresses.yaml](https://github.com/rpedrazav/TallerIntegracion3/blob/1331133587ec9818ab5bc85cb17934cb31ceee56/new-ingresses.yaml).

**Posible repregunta:** ¿Cómo comprobarías si una idea del stash terminó incorporada a dev?

---


### Funcionalidades implementadas y explicación del código

<a id="r-06"></a>

**Pregunta N.º:** 6 (R-06)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué hace el POS mientras verifica si hay un turno abierto?

**Respuesta modelo:** Presenta un estado de carga y consulta el turno activo. Si no existe, dirige al formulario de apertura; si la consulta falla, bloquea el carrito y permite reintentar. No interpreta una falla de red como autorización para vender. El backend mantiene su propia guardia, de modo que la navegación no es la única protección de RN-06.

**Explicación para estudiar:** Ausencia confirmada y error al consultar son situaciones diferentes. La interfaz debe comunicar ambas sin inventar un estado de caja.

**Evidencia del repositorio:** [ccf7bf3](https://github.com/rpedrazav/TallerIntegracion3/commit/ccf7bf330b2e9ae25d93d689d7926db9f81d0071) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/ccf7bf330b2e9ae25d93d689d7926db9f81d0071/globalmart-frontend/src/pages/Pos.tsx).

**Posible repregunta:** ¿Por qué no mostrar inmediatamente un carrito operativo antes de la respuesta?

---

<a id="r-07"></a>

**Pregunta N.º:** 7 (R-07)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo cambió el carrito al incorporar POST /ventas?

**Respuesta modelo:** Envía producto y cantidad o peso sin tomar precios locales como autoridad. Conserva el identificador y los ítems y totales de la respuesta. Después puede recuperar la venta pendiente mediante GET. Así el carrito representa una venta de MS-5 y no solo un arreglo de React. Si no se confirma su estado, muestra error y evita tratarlo como sincronizado.

**Explicación para estudiar:** Una interfaz puede tener borradores, pero debe distinguirlos del estado durable. El ID del servidor permite correlacionar edición, cobro y recuperación.

**Evidencia del repositorio:** [160846e](https://github.com/rpedrazav/TallerIntegracion3/commit/160846eb68bac27937560baabb6a10d4ed545c73) (integrado en dev): [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/160846eb68bac27937560baabb6a10d4ed545c73/globalmart-frontend/src/api/client.ts), [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/160846eb68bac27937560baabb6a10d4ed545c73/globalmart-frontend/src/pages/Pos.tsx); [9dedcc5](https://github.com/rpedrazav/TallerIntegracion3/commit/9dedcc50684998bb0791c214e7947939c0503021) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/9dedcc50684998bb0791c214e7947939c0503021/globalmart-frontend/src/pages/Pos.tsx).

**Posible repregunta:** ¿Qué información necesitas conservar para recuperar la venta al recargar?

---

<a id="r-08"></a>

**Pregunta N.º:** 8 (R-08)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo manejas agregar, cambiar cantidad y eliminar sin dejar totales locales inconsistentes?

**Respuesta modelo:** Las mutaciones se envían a MS-5 y después se obtiene la venta autoritativa. El código coordina operaciones para no mezclar respuestas y marca cuando necesita refrescar. No recalcula IVA fijo como confirmación del negocio. Si una petición queda ambigua, recuperar el estado del servidor es más seguro que suponer que el arreglo local refleja lo persistido.

**Explicación para estudiar:** Las respuestas asíncronas pueden llegar fuera de orden. Serializar cambios y refrescar reduce discrepancias, aunque no reemplaza la concurrencia del backend.

**Evidencia del repositorio:** [f612f9a](https://github.com/rpedrazav/TallerIntegracion3/commit/f612f9aa4a35460ba9ec56d52530fc9fc1bbb962) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f612f9aa4a35460ba9ec56d52530fc9fc1bbb962/globalmart-frontend/src/pages/Pos.tsx), [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/f612f9aa4a35460ba9ec56d52530fc9fc1bbb962/globalmart-frontend/src/api/client.ts); [9dedcc5](https://github.com/rpedrazav/TallerIntegracion3/commit/9dedcc50684998bb0791c214e7947939c0503021) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/9dedcc50684998bb0791c214e7947939c0503021/globalmart-frontend/src/pages/Pos.tsx).

**Posible repregunta:** ¿Qué harías si DELETE fue aceptado pero se perdió su respuesta?

---

<a id="r-09"></a>

**Pregunta N.º:** 9 (R-09)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué hace tu modal si el efectivo recibido es insuficiente?

**Respuesta modelo:** Muestra la insuficiencia y conserva la venta pendiente. El cobro llama a MS-5, que valida monto y devuelve el vuelto cuando corresponde. La UI no limpia el carrito por una intención de cobro ni debe reintentar automáticamente si desconoce el resultado. Las pruebas dirigidas cubren aprobación, vuelto y rechazo por monto.

**Explicación para estudiar:** Un mensaje visual debe seguir el resultado de la transacción. Confirmar por optimismo en una operación monetaria puede esconder un estado pendiente.

**Evidencia del repositorio:** [12a8c4b](https://github.com/rpedrazav/TallerIntegracion3/commit/12a8c4bb50ed05e56b599f0569fe14210225ba29) (integrado en dev): [globalmart-frontend/src/components/pos/PaymentModal.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/12a8c4bb50ed05e56b599f0569fe14210225ba29/globalmart-frontend/src/components/pos/PaymentModal.tsx), [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/12a8c4bb50ed05e56b599f0569fe14210225ba29/globalmart-frontend/src/pages/Pos.tsx), [globalmart-frontend/e2e/cash-payment.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/12a8c4bb50ed05e56b599f0569fe14210225ba29/globalmart-frontend/e2e/cash-payment.spec.ts).

**Posible repregunta:** ¿De dónde toma la pantalla el vuelto definitivo?

---

<a id="r-10"></a>

**Pregunta N.º:** 10 (R-10)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo se recupera en el POS un intento de tarjeta después de recargar?

**Respuesta modelo:** Conserva la venta y consulta su intento en MS-5. Si continúa incierto, muestra esa condición y mantiene bloqueada una nueva autorización; si hay aprobación o rechazo confirmado, actualiza la vista. La consulta se refiere a la misma clave y no debe convertirse en otro POST de cobro. El backend conserva la reserva que da continuidad al flujo.

**Explicación para estudiar:** La recarga elimina memoria de UI, pero no el estado durable ni un posible efecto externo. La recuperación debe partir del servidor.

**Evidencia del repositorio:** [f6727db](https://github.com/rpedrazav/TallerIntegracion3/commit/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d/globalmart-frontend/src/pages/Pos.tsx), [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d/globalmart-frontend/src/api/client.ts), [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d/src/POSCartService/Controllers/VentasController.cs).

**Posible repregunta:** ¿Cuándo sí podría habilitarse un reintento manual?

---

<a id="r-11"></a>

**Pregunta N.º:** 11 (R-11)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo evita tu flujo de peso agregar un ítem al leer o al cancelar?

**Respuesta modelo:** Separa producto seleccionado, lectura y confirmación. La lectura estable se presenta antes de mutar el carrito; solo confirmar crea o modifica la línea. Cancelar o recibir un error no envía el ítem. Esto permite al cajero revisar origen y peso y evita que un cambio del dispositivo produzca una venta involuntaria.

**Explicación para estudiar:** Captura y aceptación son transiciones distintas. La prueba debe comprobar ausencia de solicitudes antes de confirmar, no solo el texto del panel.

**Evidencia del repositorio:** [4f3c604](https://github.com/rpedrazav/TallerIntegracion3/commit/4f3c60432d01e7146f3957ac79f48d9b07654dc9) (integrado en dev): [globalmart-frontend/src/components/pos/PesoProducto.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/4f3c60432d01e7146f3957ac79f48d9b07654dc9/globalmart-frontend/src/components/pos/PesoProducto.tsx); [39b5b2a](https://github.com/rpedrazav/TallerIntegracion3/commit/39b5b2a08291ea2cf299e1a427a3165180ee3cb4) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/39b5b2a08291ea2cf299e1a427a3165180ee3cb4/globalmart-frontend/src/pages/Pos.tsx); [4bde899](https://github.com/rpedrazav/TallerIntegracion3/commit/4bde899fce12fc6d005100d7585ec27cfbda8024) (integrado en dev): [globalmart-frontend/e2e/weight-cart.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/4bde899fce12fc6d005100d7585ec27cfbda8024/globalmart-frontend/e2e/weight-cart.spec.ts).

**Posible repregunta:** ¿Qué debe ocurrir con un producto unitario en esa misma pantalla?

---

<a id="r-12"></a>

**Pregunta N.º:** 12 (R-12)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo se relacionan roles propios, permisos base y el JWT final?

**Respuesta modelo:** Un rol propio pertenece al tenant y hereda ADMIN, CAJERO o REPONEDOR. Login y JwtService incorporan nombre propio y rol base, filtrando roles ajenos. Así las políticas existentes reconocen la capacidad base. Crear o eliminar roles requiere ADMIN y no se elimina uno asignado. No se implementó un lenguaje arbitrario de permisos ni revocación inmediata de tokens.

**Explicación para estudiar:** Nombre del rol y capacidad autorizada no son equivalentes. Heredar una base permite compatibilidad, pero también obliga a impedir escalamiento entre tenants.

**Evidencia del repositorio:** [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (integrado en dev): [src/TenantIdentityService/Controllers/UsuarioController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/TenantIdentityService/Controllers/UsuarioController.cs), [src/TenantIdentityService/Services/JwtService.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/TenantIdentityService/Services/JwtService.cs), [src/TenantIdentityService/Data/TenantDbContext.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/TenantIdentityService/Data/TenantDbContext.cs), [tests/GlobalMart.IntegrationTests/AdminCustomRolesIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/tests/GlobalMart.IntegrationTests/AdminCustomRolesIntegrationTests.cs).

**Posible repregunta:** ¿Por qué un rol propio con base ADMIN cuenta al evitar quitarse todos los permisos administrativos?

---

<a id="r-13"></a>

**Pregunta N.º:** 13 (R-13)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué valida manualWeightForCart antes de habilitar el respaldo?

**Respuesta modelo:** Normaliza coma a punto, exige una forma numérica con hasta tres decimales y comprueba valor finito, mínimo 0,010 kg y precisión de gramo. No acepta cero, negativos ni texto ambiguo. El backend repite su contrato. El origen manual se muestra en el panel y no se presenta como una lectura física certificada.

**Explicación para estudiar:** Validar sintaxis y magnitud evita entradas que Number podría interpretar de formas no deseadas. La regla de negocio sigue siendo independiente del formato visual.

**Evidencia del repositorio:** [755ba27](https://github.com/rpedrazav/TallerIntegracion3/commit/755ba275391164c675cbc95aaed0eb1fb9c83dbd) (integrado en dev): [globalmart-frontend/src/hardware/scale-cart.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/755ba275391164c675cbc95aaed0eb1fb9c83dbd/globalmart-frontend/src/hardware/scale-cart.ts), [globalmart-frontend/src/components/pos/PesoProducto.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/755ba275391164c675cbc95aaed0eb1fb9c83dbd/globalmart-frontend/src/components/pos/PesoProducto.tsx).

**Posible repregunta:** ¿Por qué no aceptar 0,7505 y redondearlo automáticamente?

---


### Decisiones técnicas, tecnologías y arquitectura

<a id="r-14"></a>

**Pregunta N.º:** 14 (R-14)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué usas clientes tipados y manejo centralizado de 401?

**Respuesta modelo:** Para que login, catálogo y turnos compartan el envío de credenciales y la reacción a sesión inválida. Los tipos documentan request y respuesta y ayudan al compilador a detectar incompatibilidades. El manejo central evita pantallas que continúan como autenticadas mientras otra ya perdió la sesión. No valida por sí solo el contenido real de cualquier respuesta externa.

**Explicación para estudiar:** Compartir comportamiento transversal reduce divergencia. La validación y autorización siguen siendo responsabilidad del servidor y de las fronteras runtime.

**Evidencia del repositorio:** [e1a2691](https://github.com/rpedrazav/TallerIntegracion3/commit/e1a269113ceeef94ed89dd79502b830fc2557ab2) (integrado en dev): [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/e1a269113ceeef94ed89dd79502b830fc2557ab2/globalmart-frontend/src/api/client.ts).

**Posible repregunta:** ¿Qué diferencia de tratamiento existe entre 401 y 403?

---

<a id="r-15"></a>

**Pregunta N.º:** 15 (R-15)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué mostraste subtotal estimado antes de recibir impuestos del servidor?

**Respuesta modelo:** El borrador puede orientar al cajero con precio por kg y peso, pero el IVA depende del cálculo autoritativo del tenant. Marqué esa estimación y reservé impuestos y total confirmados para la respuesta de MS-5. Así la pantalla no reproduce una tasa fija local ni presenta una cifra provisional como obligación de pago definitiva.

**Explicación para estudiar:** Una estimación es útil si se identifica como tal. La interfaz debe separar datos disponibles localmente de resultados que exigen reglas remotas.

**Evidencia del repositorio:** [00fbb39](https://github.com/rpedrazav/TallerIntegracion3/commit/00fbb3981562a122d76fd6c61e2e3104c2f1bb42) (integrado en dev): [globalmart-frontend/src/components/pos/CartItem.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/00fbb3981562a122d76fd6c61e2e3104c2f1bb42/globalmart-frontend/src/components/pos/CartItem.tsx), [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/00fbb3981562a122d76fd6c61e2e3104c2f1bb42/globalmart-frontend/src/pages/Pos.tsx); [4bde899](https://github.com/rpedrazav/TallerIntegracion3/commit/4bde899fce12fc6d005100d7585ec27cfbda8024) (integrado en dev): [globalmart-frontend/e2e/weight-cart.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/4bde899fce12fc6d005100d7585ec27cfbda8024/globalmart-frontend/e2e/weight-cart.spec.ts).

**Posible repregunta:** ¿Qué prueba usa totales controlados para detectar que la UI recalculó por su cuenta?

---

<a id="r-16"></a>

**Pregunta N.º:** 16 (R-16)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué protege el bootstrap administrativo y por qué no es un registro público?

**Respuesta modelo:** Se ejecuta explícitamente mediante argumento y variables de entorno para crear tenant y administrador. Usa hashing de contraseña y no expone una ruta anónima para crear administradores. Permite preparar el entorno académico de forma controlada. La funcionalidad no significa que cualquier usuario pueda aprovisionar tenants ni que se haya implementado un onboarding público completo.

**Explicación para estudiar:** Una operación privilegiada de instalación tiene un modelo de acceso distinto de una API pública. Mantener esa distinción limita escalamiento de privilegios.

**Evidencia del repositorio:** [bfd46b0](https://github.com/rpedrazav/TallerIntegracion3/commit/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1) (integrado en dev): [src/TenantIdentityService/Services/AdminBootstrap.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1/src/TenantIdentityService/Services/AdminBootstrap.cs).

**Posible repregunta:** ¿Qué riesgo tendría activar el bootstrap automáticamente con credenciales conocidas?

---

<a id="r-17"></a>

**Pregunta N.º:** 17 (R-17)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué implica alojar warehouse_db en la instancia PostgreSQL del POS?

**Respuesta modelo:** Mantiene bases lógicas separadas, pero comparten instancia y recursos. Fue una adaptación a la cuota de PVC documentada, no una eliminación de la responsabilidad de MS-4 sobre sus datos. Debo reconocer acoplamiento operativo de capacidad y disponibilidad. Además Kafka quedó desactivado por falta de broker, por lo que no puedo afirmar sincronización remota de stock.

**Explicación para estudiar:** La separación por servicio puede tener distintos niveles físicos. Compartir instancia conserva ciertas fronteras lógicas, pero introduce fallos comunes.

**Evidencia del repositorio:** [dbd2b09](https://github.com/rpedrazav/TallerIntegracion3/commit/dbd2b096d545cd5476821dd3a2baa4f420db2eca) (integrado en dev): [k8s/manifests/warehouse-inventory.yaml](https://github.com/rpedrazav/TallerIntegracion3/blob/dbd2b096d545cd5476821dd3a2baa4f420db2eca/k8s/manifests/warehouse-inventory.yaml), [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/dbd2b096d545cd5476821dd3a2baa4f420db2eca/docs/context/estado-actual.md).

**Posible repregunta:** ¿Qué revisarías al migrar MS-4 a una instancia independiente?

---

<a id="r-18"></a>

**Pregunta N.º:** 18 (R-18)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué borrar un rol propio exige comprobar asignaciones y tenant?

**Respuesta modelo:** Solo puede eliminarse un rol del tenant actual, y si está asignado se devuelve conflicto. Los roles globales no se eliminan por ese endpoint. Eso evita dejar usuarios con referencias inválidas o permitir a un administrador afectar otro tenant. La interfaz facilita desasignar, pero la decisión final y sus restricciones están en MS-1.

**Explicación para estudiar:** Una baja de configuración afecta entidades que la referencian. La integridad debe sostenerse frente a solicitudes directas y concurrencia, no solo mediante botones deshabilitados.

**Evidencia del repositorio:** [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (integrado en dev): [src/TenantIdentityService/Controllers/UsuarioController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/src/TenantIdentityService/Controllers/UsuarioController.cs), [tests/GlobalMart.IntegrationTests/AdminCustomRolesIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/tests/GlobalMart.IntegrationTests/AdminCustomRolesIntegrationTests.cs).

**Posible repregunta:** ¿Qué prueban los casos de rol ajeno y rol base?

---


### Problemas encontrados y soluciones

<a id="r-19"></a>

**Pregunta N.º:** 19 (R-19)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué problema resolviste al unificar users y Usuarios?

**Respuesta modelo:** La edición y baja debían afectar la tabla que consulta el login. Al unificar sobre Usuarios, el cambio administrativo tiene efecto en la autenticación siguiente y en el listado. Se actualizaron pruebas PostgreSQL para el esquema real. Esto evita una falsa sensación de seguridad causada por un DELETE exitoso sobre una representación que el login no utiliza.

**Explicación para estudiar:** Una API puede devolver éxito y aun así no cumplir el caso de uso si modifica la entidad equivocada. El test debe recorrer el consumidor posterior.

**Evidencia del repositorio:** [bfd46b0](https://github.com/rpedrazav/TallerIntegracion3/commit/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1) (integrado en dev): [src/TenantIdentityService/Repositories/UsuarioRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1/src/TenantIdentityService/Repositories/UsuarioRepository.cs); [dbd2b09](https://github.com/rpedrazav/TallerIntegracion3/commit/dbd2b096d545cd5476821dd3a2baa4f420db2eca) (integrado en dev): [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/dbd2b096d545cd5476821dd3a2baa4f420db2eca/docs/context/estado-actual.md).

**Posible repregunta:** ¿Por qué un token ya emitido sigue siendo un problema distinto?

---

<a id="r-20"></a>

**Pregunta N.º:** 20 (R-20)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo evitaste que el timeout de tarjeta se convirtiera en un doble cobro por la UI?

**Respuesta modelo:** Mantengo el intento incierto y su referencia y consulto estado antes de permitir otra autorización. La reserva backend sobrevive a recargas y bloquea incluso otra clave mientras no haya rechazo definitivo. No me apoyo solo en deshabilitar el botón. El merge conservó esa recuperación junto a la reserva concurrente reforzada de Sebastián.

**Explicación para estudiar:** Un control de UI puede desaparecer por recarga o por otro cliente. La seguridad monetaria exige estado compartido y durable.

**Evidencia del repositorio:** [f6727db](https://github.com/rpedrazav/TallerIntegracion3/commit/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d/globalmart-frontend/src/pages/Pos.tsx), [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d/src/POSCartService/Controllers/VentasController.cs); [1ba6b37](https://github.com/rpedrazav/TallerIntegracion3/commit/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770) (integrado en dev): [docs/qa/S2-035/merge.md](https://github.com/rpedrazav/TallerIntegracion3/blob/1ba6b371cd9f026ae0b8fd16dbf3b746a3991770/docs/qa/S2-035/merge.md).

**Posible repregunta:** ¿Qué diferencia hay entre una consulta GET de intento y un nuevo POST?

---

<a id="r-21"></a>

**Pregunta N.º:** 21 (R-21)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué dificultad había al listar inventario con un catálogo paginado?

**Respuesta modelo:** Cargar únicamente la primera página dejaba productos fuera de la vista administrativa. El cambio final recorre páginas y las pruebas incluyen productos posteriores, de modo que el inventario puede relacionar stock con todo el catálogo disponible. No se debe confundir un producto ausente de la primera respuesta con inexistencia de producto o stock.

**Explicación para estudiar:** La paginación forma parte del contrato de datos. Una integración que ignora páginas puede parecer correcta con pocos registros y fallar al crecer.

**Evidencia del repositorio:** [dbd2b09](https://github.com/rpedrazav/TallerIntegracion3/commit/dbd2b096d545cd5476821dd3a2baa4f420db2eca) (integrado en dev): [globalmart-frontend/src/pages/AdminInventario.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/dbd2b096d545cd5476821dd3a2baa4f420db2eca/globalmart-frontend/src/pages/AdminInventario.tsx), [globalmart-frontend/e2e/admin.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/dbd2b096d545cd5476821dd3a2baa4f420db2eca/globalmart-frontend/e2e/admin.spec.ts).

**Posible repregunta:** ¿Qué caso de prueba detectaría el error sin depender de miles de productos?

---

<a id="r-22"></a>

**Pregunta N.º:** 22 (R-22)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué límite tiene afirmar que cinco E2E de Admin prueban toda la administración remota?

**Respuesta modelo:** Esas pruebas de interfaz usan respuestas controladas y verifican interacción, payloads y presentación. El estado final también documenta pruebas remotas y una suite backend, pero son evidencias diferentes. No convertiría las cinco pruebas de navegador en una prueba contra el clúster ni afirmaría haber reejecutado sus resultados durante esta revisión del banco.

**Explicación para estudiar:** El mismo nombre E2E puede cubrir fronteras diferentes. La descripción de dependencias es parte necesaria del resultado.

**Evidencia del repositorio:** [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (integrado en dev): [globalmart-frontend/e2e/admin.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/globalmart-frontend/e2e/admin.spec.ts), [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/docs/context/estado-actual.md).

**Posible repregunta:** ¿Qué observación backend agregarías para probar realmente una baja desde UI hasta login?

---


### Integración con el trabajo de otros integrantes

<a id="r-23"></a>

**Pregunta N.º:** 23 (R-23)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué contrato de Sebastián consumen tus clientes de creación de venta?

**Respuesta modelo:** Envían identificador de producto y cantidad o peso y reciben una venta con IDs, estado y totales autoritativos. No envían precio como fuente de verdad. La sucursal se obtiene del turno en backend. Al consumir ese contrato, mi interfaz puede recuperar y editar la misma venta sin recrear localmente las reglas de impuestos o pertenencia.

**Explicación para estudiar:** Un cliente debe respetar qué campos son entrada y cuáles resultado. Confundirlos abre discrepancias y manipulación del negocio.

**Evidencia del repositorio:** [160846e](https://github.com/rpedrazav/TallerIntegracion3/commit/160846eb68bac27937560baabb6a10d4ed545c73) (integrado en dev): [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/160846eb68bac27937560baabb6a10d4ed545c73/globalmart-frontend/src/api/client.ts); [152f39b](https://github.com/rpedrazav/TallerIntegracion3/commit/152f39b00076de93573a261f9721ad1c29eb3c5c) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/152f39b00076de93573a261f9721ad1c29eb3c5c/src/POSCartService/Controllers/VentasController.cs).

**Posible repregunta:** ¿Por qué no elegir sucursal de venta desde un campo libre del carrito?

---

<a id="r-24"></a>

**Pregunta N.º:** 24 (R-24)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo se conecta tu panel de peso con los adaptadores de Nicolás?

**Respuesta modelo:** El panel usa la API de balanza expuesta por preload y el filtro readStableWeightForCart. Nicolás entrega lectura canónica y errores; mi UI decide cómo presentar, confirmar o pedir respaldo manual. La validación de MS-5 sigue siendo independiente. Esa separación permite cambiar adaptador sin modificar la lógica comercial de confirmación.

**Explicación para estudiar:** La capacidad técnica de leer un dispositivo y la decisión del usuario de aceptar un dato son responsabilidades complementarias.

**Evidencia del repositorio:** [352fe7f](https://github.com/rpedrazav/TallerIntegracion3/commit/352fe7fef4b5401ea6a2d7bb449c427c3616d91c) (integrado en dev): [globalmart-frontend/src/hardware/scale-cart.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/352fe7fef4b5401ea6a2d7bb449c427c3616d91c/globalmart-frontend/src/hardware/scale-cart.ts); [4f3c604](https://github.com/rpedrazav/TallerIntegracion3/commit/4f3c60432d01e7146f3957ac79f48d9b07654dc9) (integrado en dev): [globalmart-frontend/src/components/pos/PesoProducto.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/4f3c60432d01e7146f3957ac79f48d9b07654dc9/globalmart-frontend/src/components/pos/PesoProducto.tsx).

**Posible repregunta:** ¿Qué estado visual debe corresponder a una lectura inestable?

---

<a id="r-25"></a>

**Pregunta N.º:** 25 (R-25)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo integraste la sincronización de Martín con la recuperación de tarjeta existente?

**Respuesta modelo:** El POS utiliza su contrato y synchronizeCardPayment para unir la respuesta preliminar del terminal con MS-5. La referencia del intento y venta deben coincidir, y la recuperación conserva la consulta de estado. El resultado final solo es aprobado cuando el backend confirma la venta. Debo explicar ese trabajo como integración de aportes, no como sustitución de idempotencia backend.

**Explicación para estudiar:** Añadir un dispositivo no cambia quién es dueño del estado comercial. La integración debe mantener las invariantes del flujo anterior.

**Evidencia del repositorio:** [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) (integrado en dev): [globalmart-frontend/src/payments/synchronizeCardPayment.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/src/payments/synchronizeCardPayment.ts), [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/src/pages/Pos.tsx); [f6727db](https://github.com/rpedrazav/TallerIntegracion3/commit/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d) (integrado en dev): [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d/globalmart-frontend/src/api/client.ts).

**Posible repregunta:** ¿Qué pasa si el terminal aprueba pero MS-5 responde incierto?

---

<a id="r-26"></a>

**Pregunta N.º:** 26 (R-26)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué aportes de Diego necesitabas para alternar entre local, Kong y remoto?

**Respuesta modelo:** Su configuración de API y los alias de rutas permiten seleccionar destinos coherentes; mi cliente centralizado consume esas URLs. También son necesarios puertos, DNS y prefijos correctos en cada entorno. La existencia de un cliente tipado no corrige una dirección equivocada dentro de un contenedor, por lo que el diagnóstico debe revisar la configuración efectiva.

**Explicación para estudiar:** Transporte y contrato funcional se apoyan mutuamente. El mismo método cliente puede fallar por entorno antes de alcanzar la lógica de negocio.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [globalmart-frontend/src/config/api.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/globalmart-frontend/src/config/api.ts), [docs/context/infraestructura/matriz-rutas.md](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/docs/context/infraestructura/matriz-rutas.md); [e1a2691](https://github.com/rpedrazav/TallerIntegracion3/commit/e1a269113ceeef94ed89dd79502b830fc2557ab2) (integrado en dev): [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/e1a269113ceeef94ed89dd79502b830fc2557ab2/globalmart-frontend/src/api/client.ts).

**Posible repregunta:** ¿Cómo evitarías construir /api/api/ventas?

---


### Logros personales, aprendizajes y mejoras

<a id="r-27"></a>

**Pregunta N.º:** 27 (R-27)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Básica.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Qué logro de UX pesable puedes demostrar con tus tres pruebas dirigidas?

**Respuesta modelo:** Puedo demostrar lectura estable con confirmación, desconexión y respaldo manual, y la diferencia con producto unitario. Los casos verifican que no aparezcan líneas duplicadas y que los totales mostrados provengan de MS-5 controlado. Es evidencia de interfaz, no de balanza física ni de integración completa de todos los microservicios.

**Explicación para estudiar:** Una prueba de UX debe observar las decisiones y sus efectos. Comprobar ausencia de envío antes de confirmar es tan relevante como ver el resultado exitoso.

**Evidencia del repositorio:** [4bde899](https://github.com/rpedrazav/TallerIntegracion3/commit/4bde899fce12fc6d005100d7585ec27cfbda8024) (integrado en dev): [globalmart-frontend/e2e/weight-cart.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/4bde899fce12fc6d005100d7585ec27cfbda8024/globalmart-frontend/e2e/weight-cart.spec.ts).

**Posible repregunta:** ¿Qué caso impediría vender una cantidad inválida desde el respaldo manual?

---

<a id="r-28"></a>

**Pregunta N.º:** 28 (R-28)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué aprendizaje técnico muestra pasar del carrito local a venta recuperable?

**Respuesta modelo:** La evolución evidencia que el estado visual debe sincronizarse con una fuente durable y manejar respuestas ambiguas. Recuperar por ID y refrescar tras mutaciones permite explicar qué quedó realmente guardado. Mi aprendizaje personal sobre esa transición debe confirmarse, pero el código muestra por qué una colección local y un cálculo de IVA no bastaban para operar la caja.

**Explicación para estudiar:** La resiliencia de UI incluye poder reconstruir estado después de perder memoria o conexión. No se limita a mostrar un spinner.

**Evidencia del repositorio:** [160846e](https://github.com/rpedrazav/TallerIntegracion3/commit/160846eb68bac27937560baabb6a10d4ed545c73) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/160846eb68bac27937560baabb6a10d4ed545c73/globalmart-frontend/src/pages/Pos.tsx); [9dedcc5](https://github.com/rpedrazav/TallerIntegracion3/commit/9dedcc50684998bb0791c214e7947939c0503021) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/9dedcc50684998bb0791c214e7947939c0503021/globalmart-frontend/src/pages/Pos.tsx).

**Posible repregunta:** ¿Qué dato perdería el usuario si solo guardaras el arreglo visual?

---

<a id="r-29"></a>

**Pregunta N.º:** 29 (R-29)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué información personal debes confirmar para explicar cómo priorizaste el cierre administrativo?

**Respuesta modelo:** Debo confirmar qué necesidad del equipo motivó el orden, quién la validó y qué dificultades experimenté. Git muestra bootstrap, unificación, inventario y roles, pero no demuestra esas conversaciones ni horas. Puedo responder con necesidad concreta, decisión propia, cambio observable y validación, señalando cualquier recuerdo que deba contrastarse.

**Explicación para estudiar:** Una historia de desarrollo debe distinguir motivación confirmada de reconstrucción técnica. Los commits respaldan el resultado y su secuencia, no todas las razones.

**Evidencia del repositorio:** [bfd46b0](https://github.com/rpedrazav/TallerIntegracion3/commit/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1) (integrado en dev): [src/TenantIdentityService/Services/AdminBootstrap.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/bfd46b015d69b2e52c84c22ebccc2b1197b8f0d1/src/TenantIdentityService/Services/AdminBootstrap.cs); [dbd2b09](https://github.com/rpedrazav/TallerIntegracion3/commit/dbd2b096d545cd5476821dd3a2baa4f420db2eca) (integrado en dev): [docs/context/estado-actual.md](https://github.com/rpedrazav/TallerIntegracion3/blob/dbd2b096d545cd5476821dd3a2baa4f420db2eca/docs/context/estado-actual.md); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (integrado en dev): [globalmart-frontend/src/pages/AdminRoles.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/globalmart-frontend/src/pages/AdminRoles.tsx).

**Posible repregunta:** ¿Qué cambio considerarías imprescindible para una demo ADMIN y por qué?

---

<a id="r-30"></a>

**Pregunta N.º:** 30 (R-30)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué mejorarías para que la recuperación de UI sea más mantenible después de este sprint?

**Respuesta modelo:** Extraería de Pos.tsx la coordinación de venta, captura de peso y pago en unidades con contratos explícitos, preservando una única fuente de verdad. Probaría recarga, respuesta tardía y cambio de sesión sobre el flujo integrado. Es una propuesta de refactorización: no debe eliminar las guardias actuales ni presentar una pantalla recuperada como un cobro confirmado sin MS-5.

**Explicación para estudiar:** Separar responsabilidades es útil si conserva transiciones e invariantes observables. Reducir tamaño de archivo por sí solo no demuestra mejora.

**Evidencia del repositorio:** [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/globalmart-frontend/src/pages/Pos.tsx); [f6727db](https://github.com/rpedrazav/TallerIntegracion3/commit/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d) (integrado en dev): [globalmart-frontend/e2e/card-search.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/f6727dbe7dc49a631887349bd2a7ea8fb28fc87d/globalmart-frontend/e2e/card-search.spec.ts); [4bde899](https://github.com/rpedrazav/TallerIntegracion3/commit/4bde899fce12fc6d005100d7585ec27cfbda8024) (integrado en dev): [globalmart-frontend/e2e/weight-cart.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/4bde899fce12fc6d005100d7585ec27cfbda8024/globalmart-frontend/e2e/weight-cart.spec.ts).

**Posible repregunta:** ¿Qué test de regresión usarías para impedir que una respuesta vieja modifique otra venta?

---


## 9. Banco de 30 preguntas — Daniel

**Registro:** 30 preguntas; 10 básicas, 12 intermedias, 8 avanzadas. Numeración única en este banco.

Las respuestas describen el código o la revisión enlazada. Los resultados de ejecución son históricos. “Rama, no integrado” identifica evidencia que no forma parte de dev al corte. Las experiencias personales deben ser confirmadas por el integrante.


### Tareas, responsabilidades y contribuciones

<a id="d-01"></a>

**Pregunta N.º:** 1 (D-01)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué aportes verificables realizaste además de escribir pruebas?

**Respuesta modelo:** Preparé matrices de aceptación, fixtures de dos tenants, reportes de fallos y protocolo físico. También reforcé aislamiento en repositorios y catálogo y corregí validaciones de balanza en mi rama. Mi aporte principal es validar contratos y encontrar brechas; no consiste solo en ejecutar una suite ajena ni implica que todas mis pruebas estén integradas en dev.

**Explicación para estudiar:** Una tarea de QA puede producir infraestructura, documentación y correcciones. Debe distinguirse el hallazgo, la solución y el alcance de la verificación.

**Evidencia del repositorio:** [00572cf](https://github.com/rpedrazav/TallerIntegracion3/commit/00572cfb5054e43bea900b06b4baef43328b34c7) (integrado en dev): [tests/GlobalMart.IntegrationTests/Fixtures/TwoTenantFixture.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/00572cfb5054e43bea900b06b4baef43328b34c7/tests/GlobalMart.IntegrationTests/Fixtures/TwoTenantFixture.cs); [d44e22b](https://github.com/rpedrazav/TallerIntegracion3/commit/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e) (integrado en dev): [src/POSCartService/Repositories/ItemVentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/src/POSCartService/Repositories/ItemVentaRepository.cs); [5850d61](https://github.com/rpedrazav/TallerIntegracion3/commit/5850d6111d0c5e9086bf6d156cc89fd7c9777dab) (rama, no integrado en dev): [globalmart-frontend/src/hardware/simulated-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/5850d6111d0c5e9086bf6d156cc89fd7c9777dab/globalmart-frontend/src/hardware/simulated-scale-adapter.ts).

**Posible repregunta:** ¿Qué commit tuyo modifica código productivo para reforzar RN-01?

---

<a id="d-02"></a>

**Pregunta N.º:** 2 (D-02)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué entregaste en la matriz S2-021?

**Respuesta modelo:** Convertí las cinco metas en escenarios con resultados esperados, dependencias y evidencia requerida. Separé lo simulado de lo físico y marqué condiciones que faltaban. Es un instrumento de aceptación, no un certificado de ejecución. Para defender un caso debo acompañar la fila con su test o reporte, y reconocer cuando todavía no tiene resultado.

**Explicación para estudiar:** Un plan de prueba define cómo decidir; una evidencia registra qué pasó. Confundirlos puede convertir trabajo pendiente en logro aparente.

**Evidencia del repositorio:** [806450b](https://github.com/rpedrazav/TallerIntegracion3/commit/806450bec2bda6cee096bb62cbc02b204d5daae9) (integrado en dev): [diagramas/matriz/S2-021-matriz-aceptacion.md](https://github.com/rpedrazav/TallerIntegracion3/blob/806450bec2bda6cee096bb62cbc02b204d5daae9/diagramas/matriz/S2-021-matriz-aceptacion.md).

**Posible repregunta:** ¿Qué caso de la matriz depende de disponibilidad de hardware?

---

<a id="d-03"></a>

**Pregunta N.º:** 3 (D-03)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Por qué no se deben contar tus dieciocho commits como dieciocho funcionalidades terminadas en dev?

**Respuesta modelo:** Incluyen pruebas, documentación, un merge y trabajo en otra rama. Ocho son ancestros de dev final y diez no. Algunos verifican funciones que implementaron otros, otros reportan límites. Esas contribuciones tienen valor propio, pero ni el número ni la fecha prueban integración o aceptación funcional. Debo citar cada SHA al hablar del resultado.

**Explicación para estudiar:** La medición debe separar actividad, contribución y estado de entrega. Un test de una rama puede ser correcto sin estar disponible en el corte integrado.

**Evidencia del repositorio:** [3148c9a](https://github.com/rpedrazav/TallerIntegracion3/commit/3148c9aa4a19b7a5448bb85107a41c1c060eb37b) (rama, no integrado en dev): [docs/qa/S2-055/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/3148c9aa4a19b7a5448bb85107a41c1c060eb37b/docs/qa/S2-055/reporte.md); [00572cf](https://github.com/rpedrazav/TallerIntegracion3/commit/00572cfb5054e43bea900b06b4baef43328b34c7) (integrado en dev): [tests/GlobalMart.IntegrationTests/Fixtures/TwoTenantFixture.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/00572cfb5054e43bea900b06b4baef43328b34c7/tests/GlobalMart.IntegrationTests/Fixtures/TwoTenantFixture.cs).

**Posible repregunta:** ¿Qué comando usarías para comprobar la pertenencia de 3148c9a?

---

<a id="d-04"></a>

**Pregunta N.º:** 4 (D-04)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo diferencias tus tareas S2-052, S2-053 y S2-054?

**Respuesta modelo:** S2-052 prueba rechazo y timeout; S2-053 autorización e idempotencia; S2-054 aislamiento entre tenants en el pago. Comparten infraestructura, pero comprueban invariantes distintas: ausencia de efectos ante fallo, unicidad de autorización y separación de datos. No sumaría sus corridas como cobertura independiente de todo el sistema ni afirmaría que incluyen Electron o Kafka.

**Explicación para estudiar:** Una matriz útil divide riesgos y observaciones. Reutilizar fixtures no elimina la necesidad de explicar qué cambia en cada escenario.

**Evidencia del repositorio:** [25a40f5](https://github.com/rpedrazav/TallerIntegracion3/commit/25a40f52648694c51c2ffa429c065349c3046202) (integrado en dev): [docs/qa/S2-052/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/25a40f52648694c51c2ffa429c065349c3046202/docs/qa/S2-052/reporte.md); [abfd1f0](https://github.com/rpedrazav/TallerIntegracion3/commit/abfd1f0e1050027701674889775d2f28c19e780a) (integrado en dev): [docs/qa/S2-053/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/abfd1f0e1050027701674889775d2f28c19e780a/docs/qa/S2-053/reporte.md); [bbc5b5f](https://github.com/rpedrazav/TallerIntegracion3/commit/bbc5b5f5df03fec2af2a65e75d11bc6dfc01b291) (integrado en dev): [tests/GlobalMart.IntegrationTests/qa/S2-054/README.md](https://github.com/rpedrazav/TallerIntegracion3/blob/bbc5b5f5df03fec2af2a65e75d11bc6dfc01b291/tests/GlobalMart.IntegrationTests/qa/S2-054/README.md).

**Posible repregunta:** ¿Qué observación adicional exige S2-054 que no basta en un caso de duplicado?

---

<a id="d-05"></a>

**Pregunta N.º:** 5 (D-05)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué entregaste para balanza física y qué quedó condicionado?

**Respuesta modelo:** Preparé un protocolo, captura serial y requisitos de equipo, manual y parámetros de medición. El documento deja la ejecución física condicionada y [NO VERIFICADO]. Es un entregable de preparación en otra rama; no acredita conexión real, calibración ni aceptación E2E. Las pruebas simuladas posteriores tampoco reemplazan esa ejecución con el modelo disponible.

**Explicación para estudiar:** Planificar una verificación reduce incertidumbre futura, pero no transforma una hipótesis de compatibilidad en un resultado medido.

**Evidencia del repositorio:** [8c20fee](https://github.com/rpedrazav/TallerIntegracion3/commit/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98) (rama, no integrado en dev): [docs/qa/S2-085/protocolo-balanza-fisica.md](https://github.com/rpedrazav/TallerIntegracion3/blob/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98/docs/qa/S2-085/protocolo-balanza-fisica.md), [docs/qa/S2-085/capturar-balanza.ps1](https://github.com/rpedrazav/TallerIntegracion3/blob/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98/docs/qa/S2-085/capturar-balanza.ps1).

**Posible repregunta:** ¿Qué dato del manual debe obtenerse antes de enviar comandos al dispositivo?

---


### Funcionalidades implementadas y explicación del código

<a id="d-06"></a>

**Pregunta N.º:** 6 (D-06)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué construye TwoTenantFixture para las pruebas de aislamiento?

**Respuesta modelo:** Crea esquemas propios de prueba y datos de dos tenants, con usuarios, sucursales, productos, turnos y ventas en estados útiles. Su limpieza se limita a esos datos y esquemas y puede repetirse. Eso permite comprobar que una operación sobre A no altera B sin depender de datos compartidos o de una base de desarrollo poblada manualmente.

**Explicación para estudiar:** La independencia de la fixture hace reproducible el caso y evita que una prueba apruebe o falle por residuos de otra ejecución.

**Evidencia del repositorio:** [00572cf](https://github.com/rpedrazav/TallerIntegracion3/commit/00572cfb5054e43bea900b06b4baef43328b34c7) (integrado en dev): [tests/GlobalMart.IntegrationTests/Fixtures/TwoTenantFixture.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/00572cfb5054e43bea900b06b4baef43328b34c7/tests/GlobalMart.IntegrationTests/Fixtures/TwoTenantFixture.cs), [tests/GlobalMart.IntegrationTests/Fixtures/README.md](https://github.com/rpedrazav/TallerIntegracion3/blob/00572cfb5054e43bea900b06b4baef43328b34c7/tests/GlobalMart.IntegrationTests/Fixtures/README.md).

**Posible repregunta:** ¿Por qué el control de limpieza debe verificar que el otro tenant permanece intacto?

---

<a id="d-07"></a>

**Pregunta N.º:** 7 (D-07)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué reveló tu regresión inicial S2-025?

**Respuesta modelo:** Registró una interacción de carrito aprobada y fallos en comunicación con ventas y persistencia al cambiar tenant. Conservé reporte y evidencia para distinguir interfaz local de venta funcional. Es un resultado histórico que ayudó a identificar la brecha; la integración posterior de Rodrigo debe evaluarse con sus nuevas pruebas, sin asumir que el reporte inicial sigue describiendo dev.

**Explicación para estudiar:** La línea base permite medir evolución. Un fallo documentado es útil si se conserva su revisión y se enlaza con la corrección posterior.

**Evidencia del repositorio:** [a451b5d](https://github.com/rpedrazav/TallerIntegracion3/commit/a451b5d22e4632ae811e8adcf1814b9de98b20c2) (integrado en dev): [docs/qa/S2-025/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/a451b5d22e4632ae811e8adcf1814b9de98b20c2/docs/qa/S2-025/reporte.md); [160846e](https://github.com/rpedrazav/TallerIntegracion3/commit/160846eb68bac27937560baabb6a10d4ed545c73) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/160846eb68bac27937560baabb6a10d4ed545c73/globalmart-frontend/src/pages/Pos.tsx).

**Posible repregunta:** ¿Por qué una captura del carrito no acredita que la venta exista en PostgreSQL?

---

<a id="d-08"></a>

**Pregunta N.º:** 8 (D-08)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo ejercitan tus tests RN-01 la autenticación real?

**Respuesta modelo:** Generan JWT firmados y envían solicitudes al pipeline HTTP de los servicios, conservando middleware, validadores y repositorios. Usan PostgreSQL en esquemas propios y comparan datos antes y después. Algunos hosts evitan migraciones de inicio porque la fixture prepara el modelo; por eso esas pruebas no demuestran necesariamente la ejecución de todas las migraciones.

**Explicación para estudiar:** Es importante saber qué componente sigue siendo real y cuál prepara el test. Real HTTP interno no implica una red o despliegue completo.

**Evidencia del repositorio:** [d44e22b](https://github.com/rpedrazav/TallerIntegracion3/commit/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e) (integrado en dev): [tests/GlobalMart.IntegrationTests/VentaTenantIsolationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/VentaTenantIsolationTests.cs), [tests/GlobalMart.IntegrationTests/ProductoTenantIsolationIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/ProductoTenantIsolationIntegrationTests.cs), [tests/GlobalMart.IntegrationTests/Fixtures/README.md](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/Fixtures/README.md).

**Posible repregunta:** ¿Qué afirmación sobre migraciones no podrías hacer con esa fixture?

---

<a id="d-09"></a>

**Pregunta N.º:** 9 (D-09)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué añadiste pruebas directas de repositorios además de casos HTTP?

**Respuesta modelo:** Para comprobar que una escritura no depende exclusivamente del controlador para respetar tenant. Los casos fuerzan objetos rastreados y contextos sin tenant o con tenant ajeno y verifican que no modifiquen datos. La corrección consulta pertenencia persistida con ID y tenant. Así una futura llamada interna no debería saltarse la protección por evitar HTTP.

**Explicación para estudiar:** Los filtros de consulta no protegen automáticamente todas las mutaciones. La seguridad debe sostenerse en la capa que efectivamente escribe.

**Evidencia del repositorio:** [d44e22b](https://github.com/rpedrazav/TallerIntegracion3/commit/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e) (integrado en dev): [tests/GlobalMart.IntegrationTests/PosRepositoryTenantIsolationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/PosRepositoryTenantIsolationTests.cs), [src/POSCartService/Repositories/VentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/src/POSCartService/Repositories/VentaRepository.cs), [src/POSCartService/Repositories/ItemVentaRepository.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/src/POSCartService/Repositories/ItemVentaRepository.cs).

**Posible repregunta:** ¿Qué puede salir mal si se confía en TenantId del objeto recibido?

---

<a id="d-10"></a>

**Pregunta N.º:** 10 (D-10)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué observas en S2-052 después de un timeout y de la respuesta tardía?

**Respuesta modelo:** Compruebo que la venta siga pendiente, sin Pago ni outbox, y que el intento conserve el estado incierto esperado. También pruebo que un nuevo cobro quede bloqueado sin otro POST. El reporte incluye observación posterior a la respuesta tardía. La corrida local no acredita el entorno remoto, que se bloqueó antes en la consulta de catálogo.

**Explicación para estudiar:** Probar solo el instante del timeout no cubre efectos que ocurren después. La validación debe continuar durante la ventana de respuesta tardía.

**Evidencia del repositorio:** [25a40f5](https://github.com/rpedrazav/TallerIntegracion3/commit/25a40f52648694c51c2ffa429c065349c3046202) (integrado en dev): [docs/qa/S2-052/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/25a40f52648694c51c2ffa429c065349c3046202/docs/qa/S2-052/reporte.md), [tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/25a40f52648694c51c2ffa429c065349c3046202/tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs).

**Posible repregunta:** ¿Qué diferencia existe entre bloquear reintento y reconciliar aprobación?

---

<a id="d-11"></a>

**Pregunta N.º:** 11 (D-11)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo demuestra S2-053 idempotencia sin inventar una respuesta de pasarela?

**Respuesta modelo:** El observador cuenta solicitudes y puede retener la primera respuesta real del simulador. Mientras está pendiente, el duplicado debe devolver 409 sin otra autorización; al liberarla, se comprueba un único pago y referencia. Otro caso repite directamente contra el simulador y compara sus identificadores. Se usan PostgreSQL y proceso Node reales dentro del entorno de prueba.

**Explicación para estudiar:** Controlar cuándo llega una respuesta permite reproducir concurrencia sin falsificar el resultado comercial. El contador externo verifica efectos además del estado local.

**Evidencia del repositorio:** [abfd1f0](https://github.com/rpedrazav/TallerIntegracion3/commit/abfd1f0e1050027701674889775d2f28c19e780a) (integrado en dev): [docs/qa/S2-053/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/abfd1f0e1050027701674889775d2f28c19e780a/docs/qa/S2-053/reporte.md), [tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/abfd1f0e1050027701674889775d2f28c19e780a/tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs), [tests/GlobalMart.IntegrationTests/PaymentSimulatorIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/abfd1f0e1050027701674889775d2f28c19e780a/tests/GlobalMart.IntegrationTests/PaymentSimulatorIntegrationTests.cs).

**Posible repregunta:** ¿Qué diferencia hay entre 409 del POS y repetir la respuesta original en el simulador?

---

<a id="d-12"></a>

**Pregunta N.º:** 12 (D-12)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué prueba realmente tu E2E Electron de tarjeta y qué deja fuera?

**Respuesta modelo:** Arranca main, preload y renderer productivos, MS-5 con JWT y PostgreSQL y un simulador Node. Comprueba aprobación y rechazo correlacionando UI, intento, pago y outbox. Identidad, catálogo e IVA son controlados y el worker está deshabilitado. No prueba Kafka, hardware ni despliegue total. Además 3148c9a no está integrado en dev final.

**Explicación para estudiar:** E2E siempre tiene una frontera concreta. Explicitarla evita que el nombre de la suite prometa más que lo efectivamente ejecutado.

**Evidencia del repositorio:** [3148c9a](https://github.com/rpedrazav/TallerIntegracion3/commit/3148c9aa4a19b7a5448bb85107a41c1c060eb37b) (rama, no integrado en dev): [docs/qa/S2-055/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/3148c9aa4a19b7a5448bb85107a41c1c060eb37b/docs/qa/S2-055/reporte.md), [tests/GlobalMart.IntegrationTests/ElectronCardE2ETests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/3148c9aa4a19b7a5448bb85107a41c1c060eb37b/tests/GlobalMart.IntegrationTests/ElectronCardE2ETests.cs).

**Posible repregunta:** ¿Qué evidencia comprueba que no se interceptó la respuesta de cobro en Playwright?

---

<a id="d-13"></a>

**Pregunta N.º:** 13 (D-13)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué verifica el plan S2-081 al separar filas SIMULADA y FÍSICA?

**Respuesta modelo:** Verifica documentalmente que cada fila identifique tipo de prueba, evidencia, límites y requisitos del equipo. Las pruebas de ese plan revisan estructura y existencia de enlaces. No ejecutan balanza ni POS. El resultado 3/3 citado en su verificación corresponde a calidad del plan, no a tres integraciones físicas aprobadas.

**Explicación para estudiar:** Hay pruebas de documentación y pruebas del producto. Un número de casos aprobados carece de significado si se omite qué objeto se probó.

**Evidencia del repositorio:** [ce542e9](https://github.com/rpedrazav/TallerIntegracion3/commit/ce542e946fc6da1d4f5c11b91acc56d22e141367) (rama, no integrado en dev): [docs/qa/S2-081/plan-validacion.md](https://github.com/rpedrazav/TallerIntegracion3/blob/ce542e946fc6da1d4f5c11b91acc56d22e141367/docs/qa/S2-081/plan-validacion.md).

**Posible repregunta:** ¿Por qué verificar que un enlace existe no valida el contenido enlazado?

---


### Decisiones técnicas, tecnologías y arquitectura

<a id="d-14"></a>

**Pregunta N.º:** 14 (D-14)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué usaste esquemas aislados en PostgreSQL para tus fixtures?

**Respuesta modelo:** Permiten probar restricciones y consultas del motor real sin compartir datos de casos o tocar tablas habituales de desarrollo. Cada ejecución crea su espacio y lo elimina. Eso hace más fiable la comparación de snapshots y la limpieza. La conexión debe apuntar a una base de pruebas y no debe tener un destino compartido implícito.

**Explicación para estudiar:** Aislamiento del entorno de prueba y aislamiento multi-tenant del producto son distintos: el primero hace confiable la prueba del segundo.

**Evidencia del repositorio:** [00572cf](https://github.com/rpedrazav/TallerIntegracion3/commit/00572cfb5054e43bea900b06b4baef43328b34c7) (integrado en dev): [tests/GlobalMart.IntegrationTests/Fixtures/README.md](https://github.com/rpedrazav/TallerIntegracion3/blob/00572cfb5054e43bea900b06b4baef43328b34c7/tests/GlobalMart.IntegrationTests/Fixtures/README.md), [tests/GlobalMart.IntegrationTests/Fixtures/TwoTenantFixture.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/00572cfb5054e43bea900b06b4baef43328b34c7/tests/GlobalMart.IntegrationTests/Fixtures/TwoTenantFixture.cs).

**Posible repregunta:** ¿Por qué usar una base en memoria no prueba todas las particularidades de PostgreSQL?

---

<a id="d-15"></a>

**Pregunta N.º:** 15 (D-15)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué comparas snapshots completos en rechazos de tenant?

**Respuesta modelo:** Porque una respuesta 404 o 403 no garantiza que nada se haya escrito antes. Comparar filas y columnas antes y después verifica ausencia de cambios, y los controles propios muestran que el endpoint sí funciona. Así detecto tanto modificaciones parciales como un test que aprobaría porque el sistema rechaza todas las solicitudes.

**Explicación para estudiar:** Las propiedades de seguridad incluyen efectos negativos: algo que nunca debe ocurrir. Se necesitan observaciones posteriores para acreditarlas.

**Evidencia del repositorio:** [d44e22b](https://github.com/rpedrazav/TallerIntegracion3/commit/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e) (integrado en dev): [tests/GlobalMart.IntegrationTests/ProductoTenantIsolationIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/ProductoTenantIsolationIntegrationTests.cs), [tests/GlobalMart.IntegrationTests/PosRepositoryTenantIsolationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/PosRepositoryTenantIsolationTests.cs).

**Posible repregunta:** ¿Qué datos del tenant B compararías después de intentar editar desde A?

---

<a id="d-16"></a>

**Pregunta N.º:** 16 (D-16)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué valor y qué limitación tiene tu prueba entre servicios S2-084?

**Respuesta modelo:** Usa MS-3 y MS-5 por TestServer, JWT y PostgreSQL, y llama al calculador productivo de MS-2 mediante un adaptador. Compara valores de precio, peso, redondeo y representación del renderer con casos independientes. No ejecuta HTTP de MS-2 ni Electron completo. Por eso demuestra coherencia de cálculo en esa composición, no despliegue integral.

**Explicación para estudiar:** Utilizar código productivo dentro de un doble conserva parte de la lógica, pero omite transporte, autenticación y disponibilidad de ese servicio.

**Evidencia del repositorio:** [7f85701](https://github.com/rpedrazav/TallerIntegracion3/commit/7f85701bac4c9f94c95bd229410574ec37d2734f) (rama, no integrado en dev): [tests/GlobalMart.IntegrationTests/PesableEntreServiciosIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/7f85701bac4c9f94c95bd229410574ec37d2734f/tests/GlobalMart.IntegrationTests/PesableEntreServiciosIntegrationTests.cs).

**Posible repregunta:** ¿Qué falla no detectaría si el endpoint HTTP de MS-2 estuviera mal configurado?

---

<a id="d-17"></a>

**Pregunta N.º:** 17 (D-17)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué tu prueba de fallback basada en AST debe describirse con especial cuidado?

**Respuesta modelo:** Extrae y ejecuta un callback productivo con spies; no monta React, IPC ni HTTP. Incluye una guardia que denuncia aceptación incompleta cuando no se ejercita continuación manual. Además se escribió antes de la captura confirmada posterior de dev. No debo convertirla en un E2E aprobado del modo manual ni trasladarla sin adaptar la estructura actual.

**Explicación para estudiar:** Inspeccionar implementación puede detectar una brecha específica, pero acopla el test a la forma del código. El requisito observable puede sobrevivir a una refactorización.

**Evidencia del repositorio:** [f787762](https://github.com/rpedrazav/TallerIntegracion3/commit/f787762c367100e6d7f5d85815d59e697f5330c6) (rama, no integrado en dev): [tests/GlobalMart.IntegrationTests/s2083-scale-tests.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/f787762c367100e6d7f5d85815d59e697f5330c6/tests/GlobalMart.IntegrationTests/s2083-scale-tests.cjs), [tests/GlobalMart.IntegrationTests/ScaleFallbackIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f787762c367100e6d7f5d85815d59e697f5330c6/tests/GlobalMart.IntegrationTests/ScaleFallbackIntegrationTests.cs); [4bde899](https://github.com/rpedrazav/TallerIntegracion3/commit/4bde899fce12fc6d005100d7585ec27cfbda8024) (integrado en dev): [globalmart-frontend/e2e/weight-cart.spec.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/4bde899fce12fc6d005100d7585ec27cfbda8024/globalmart-frontend/e2e/weight-cart.spec.ts).

**Posible repregunta:** ¿Qué prueba de interfaz reemplazaría mejor esa inspección estructural?

---

<a id="d-18"></a>

**Pregunta N.º:** 18 (D-18)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué rechazaste Infinity y unidades desconocidas en ejecución aunque existan tipos?

**Respuesta modelo:** JavaScript permite Infinity y los datos de ejecución pueden violar un tipo declarado. Cambié isNaN por Number.isFinite en el mock y agregué la guardia g/kg antes de normalizar. Esas correcciones están en mi rama, por lo que hay que integrarlas antes de atribuirlas a dev. Un número válido con unidad incorrecta sigue representando un peso incorrecto.

**Explicación para estudiar:** Tipos estáticos y validación de datos externos cubren momentos distintos. El parser debe tratar entradas desconocidas como tales.

**Evidencia del repositorio:** [5850d61](https://github.com/rpedrazav/TallerIntegracion3/commit/5850d6111d0c5e9086bf6d156cc89fd7c9777dab) (rama, no integrado en dev): [globalmart-frontend/src/devices/adapters/MockScaleAdapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/5850d6111d0c5e9086bf6d156cc89fd7c9777dab/globalmart-frontend/src/devices/adapters/MockScaleAdapter.ts), [globalmart-frontend/src/hardware/simulated-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/5850d6111d0c5e9086bf6d156cc89fd7c9777dab/globalmart-frontend/src/hardware/simulated-scale-adapter.ts), [tests/GlobalMart.IntegrationTests/ScaleParserRoundingIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/5850d6111d0c5e9086bf6d156cc89fd7c9777dab/tests/GlobalMart.IntegrationTests/ScaleParserRoundingIntegrationTests.cs).

**Posible repregunta:** ¿Qué caso prueba que 750 g se convierte exactamente una vez?

---


### Problemas encontrados y soluciones

<a id="d-19"></a>

**Pregunta N.º:** 19 (D-19)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué dificultad encontraste al seleccionar la ventana de Electron en S2-024?

**Respuesta modelo:** firstWindow podía devolver DevTools abierto por el main real. La fixture pasó a esperar la ventana file de la aplicación. El reporte registra esa carrera y el resultado local de dos casos. Debo acotar que las APIs y token eran simulados y que no se verificó la ejecución del workflow en GitHub Actions.

**Explicación para estudiar:** Una automatización puede controlar la ventana equivocada y producir falsos fallos. La selección debe basarse en la superficie que realmente se quiere probar.

**Evidencia del repositorio:** [fac1511](https://github.com/rpedrazav/TallerIntegracion3/commit/fac151121f8fbc3c9a5a43417a9183242ffbbdb5) (rama, no integrado en dev): [.github/workflows/electron-e2e.yml](https://github.com/rpedrazav/TallerIntegracion3/blob/fac151121f8fbc3c9a5a43417a9183242ffbbdb5/.github/workflows/electron-e2e.yml).

**Posible repregunta:** ¿Cómo confirmarías que el token se conservó por IPC al recargar?

---

<a id="d-20"></a>

**Pregunta N.º:** 20 (D-20)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué aprendiste del bloqueo remoto de S2-052 al diagnosticar pagos?

**Respuesta modelo:** La creación devolvía 502 antes de llegar a autorización porque MS-5 buscaba catálogo en localhost:5203. Separé ese bloqueo de los resultados locales de rechazo y timeout. El diagnóstico evita atribuir a pagos un fallo de configuración de dependencia y evita anunciar una aceptación remota que nunca alcanzó la etapa bajo prueba.

**Explicación para estudiar:** Localizar el primer paso fallido delimita lo que puede concluirse. Los errores posteriores no se observan si el flujo no llega a ellos.

**Evidencia del repositorio:** [25a40f5](https://github.com/rpedrazav/TallerIntegracion3/commit/25a40f52648694c51c2ffa429c065349c3046202) (integrado en dev): [docs/qa/S2-052/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/25a40f52648694c51c2ffa429c065349c3046202/docs/qa/S2-052/reporte.md).

**Posible repregunta:** ¿Qué dato guardarías para que otro integrante reproduzca ese diagnóstico?

---

<a id="d-21"></a>

**Pregunta N.º:** 21 (D-21)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué problema de expectativas corregiste al verificar idempotencia S2-053?

**Respuesta modelo:** El reporte señala una expectativa incorrecta de la prueba nueva respecto de campos de la respuesta aprobada. La ajusté al DTO real y volví a ejecutar los tres casos. Eso no fue una corrección del negocio productivo. Debo distinguir un fallo del sistema de un test que describe mal el contrato que intenta validar.

**Explicación para estudiar:** Una prueba también puede tener defectos. El contrato y la evidencia de entrada/salida deben guiar si se corrige implementación o expectativa.

**Evidencia del repositorio:** [abfd1f0](https://github.com/rpedrazav/TallerIntegracion3/commit/abfd1f0e1050027701674889775d2f28c19e780a) (integrado en dev): [docs/qa/S2-053/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/abfd1f0e1050027701674889775d2f28c19e780a/docs/qa/S2-053/reporte.md).

**Posible repregunta:** ¿Cómo evitarías acomodar una prueba a un comportamiento incorrecto?

---

<a id="d-22"></a>

**Pregunta N.º:** 22 (D-22)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo evitarías que una suite aparentemente verde oculte casos físicos pendientes?

**Respuesta modelo:** Exigiría que cada resultado declare tipo, condiciones y omisiones, y que una prueba documental o simulada no cambie el estado físico. S2-081 formaliza esa separación. En los scripts dedicados también comprobaría que realmente se ejecutaron los casos requeridos; salida exitosa con cero pruebas no es aceptación. La evidencia debe conservar revisión y entorno.

**Explicación para estudiar:** El significado de aprobado depende de un criterio previo. Cambiar el criterio para incluir sustitutos invalida la conclusión aunque la herramienta termine en cero.

**Evidencia del repositorio:** [ce542e9](https://github.com/rpedrazav/TallerIntegracion3/commit/ce542e946fc6da1d4f5c11b91acc56d22e141367) (rama, no integrado en dev): [docs/qa/S2-081/plan-validacion.md](https://github.com/rpedrazav/TallerIntegracion3/blob/ce542e946fc6da1d4f5c11b91acc56d22e141367/docs/qa/S2-081/plan-validacion.md); [8c20fee](https://github.com/rpedrazav/TallerIntegracion3/commit/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98) (rama, no integrado en dev): [docs/qa/S2-085/protocolo-balanza-fisica.md](https://github.com/rpedrazav/TallerIntegracion3/blob/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98/docs/qa/S2-085/protocolo-balanza-fisica.md).

**Posible repregunta:** ¿Qué harías si falta la balanza pero todas las pruebas mock pasan?

---


### Integración con el trabajo de otros integrantes

<a id="d-23"></a>

**Pregunta N.º:** 23 (D-23)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué proporcionan tus fixtures a las pruebas de Sebastián?

**Respuesta modelo:** Datos consistentes de dos tenants y turnos permiten verificar que creación, cobro y peso respeten pertenencia y estado. Su reutilización reduce preparación duplicada y hace visibles regresiones de seguridad. Los casos de Sebastián añaden reglas de negocio específicas; mi fixture no demuestra por sí sola que esas reglas se cumplan.

**Explicación para estudiar:** La infraestructura de pruebas habilita escenarios, pero las assertions son las que verifican las propiedades del producto.

**Evidencia del repositorio:** [00572cf](https://github.com/rpedrazav/TallerIntegracion3/commit/00572cfb5054e43bea900b06b4baef43328b34c7) (integrado en dev): [tests/GlobalMart.IntegrationTests/Fixtures/TwoTenantFixture.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/00572cfb5054e43bea900b06b4baef43328b34c7/tests/GlobalMart.IntegrationTests/Fixtures/TwoTenantFixture.cs); [1b5c497](https://github.com/rpedrazav/TallerIntegracion3/commit/1b5c4979be1d204d6e476dff0b33d67f63aca40b) (integrado en dev): [tests/GlobalMart.IntegrationTests/PesoFronterasTenantTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1b5c4979be1d204d6e476dff0b33d67f63aca40b/tests/GlobalMart.IntegrationTests/PesoFronterasTenantTests.cs).

**Posible repregunta:** ¿Qué condición de la fixture necesitas para probar ausencia de turno abierto?

---

<a id="d-24"></a>

**Pregunta N.º:** 24 (D-24)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo se relaciona tu reporte de carrito con la integración posterior de Rodrigo?

**Respuesta modelo:** El reporte registra problemas del estado local y falta de venta confirmada. Rodrigo luego incorporó creación, recuperación y actualización por MS-5. Para vincular ambos no afirmaría una causalidad personal no registrada; mostraría la misma propiedad antes y después y ejecutaría una regresión sobre dev. La evidencia histórica explica la brecha que cambió.

**Explicación para estudiar:** Un antes y después técnico no prueba quién pidió un cambio, pero sí permite evaluar si se atendió un requisito.

**Evidencia del repositorio:** [a451b5d](https://github.com/rpedrazav/TallerIntegracion3/commit/a451b5d22e4632ae811e8adcf1814b9de98b20c2) (integrado en dev): [docs/qa/S2-025/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/a451b5d22e4632ae811e8adcf1814b9de98b20c2/docs/qa/S2-025/reporte.md); [9dedcc5](https://github.com/rpedrazav/TallerIntegracion3/commit/9dedcc50684998bb0791c214e7947939c0503021) (integrado en dev): [globalmart-frontend/src/pages/Pos.tsx](https://github.com/rpedrazav/TallerIntegracion3/blob/9dedcc50684998bb0791c214e7947939c0503021/globalmart-frontend/src/pages/Pos.tsx).

**Posible repregunta:** ¿Qué caso repetirías para verificar que no se hereda el carrito de otro tenant?

---

<a id="d-25"></a>

**Pregunta N.º:** 25 (D-25)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué hallazgos debes coordinar con Nicolás antes de cerrar balanza?

**Respuesta modelo:** La validación runtime de unidad y números, el comportamiento ante desconexión y el alcance real del adaptador serial. Debemos integrar mis correcciones con sus contratos y repetir pruebas, manteniendo la distinción entre transporte simulado y equipo físico. Mi protocolo no demuestra su implementación real y sus tests de adaptador no sustituyen el protocolo físico.

**Explicación para estudiar:** La coordinación une requisitos de medición con fronteras de software. Cada evidencia cubre una parte y debe conservar su ámbito.

**Evidencia del repositorio:** [5850d61](https://github.com/rpedrazav/TallerIntegracion3/commit/5850d6111d0c5e9086bf6d156cc89fd7c9777dab) (rama, no integrado en dev): [tests/GlobalMart.IntegrationTests/ScaleParserRoundingIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/5850d6111d0c5e9086bf6d156cc89fd7c9777dab/tests/GlobalMart.IntegrationTests/ScaleParserRoundingIntegrationTests.cs); [0afc855](https://github.com/rpedrazav/TallerIntegracion3/commit/0afc855588582f8294644160dff4f418f40b2683) (integrado en dev): [globalmart-frontend/src/hardware/serial-scale-adapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0afc855588582f8294644160dff4f418f40b2683/globalmart-frontend/src/hardware/serial-scale-adapter.ts); [8c20fee](https://github.com/rpedrazav/TallerIntegracion3/commit/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98) (rama, no integrado en dev): [docs/qa/S2-085/protocolo-balanza-fisica.md](https://github.com/rpedrazav/TallerIntegracion3/blob/8c20feeab5e9e08cd8df49a5e73e6a18a6cd7a98/docs/qa/S2-085/protocolo-balanza-fisica.md).

**Posible repregunta:** ¿Qué dato de una captura real convertirías en fixture de regresión?

---

<a id="d-26"></a>

**Pregunta N.º:** 26 (D-26)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo usarías tu matriz RN-02 para revisar el terminal de Martín?

**Respuesta modelo:** Contrastaría aprobación, rechazo, incertidumbre y reintento con la venta persistida y el número de autorizaciones. Un approved local no debe cerrar el caso si MS-5 no confirma COMPLETADA. También verificaría la correlación de referencias al recargar o consultar. La matriz fija la propiedad y sus tests de sincronización muestran un mecanismo que debe cumplirla.

**Explicación para estudiar:** Una matriz de negocio permite evaluar implementaciones distintas sin depender exclusivamente de detalles internos de una pantalla.

**Evidencia del repositorio:** [4a20547](https://github.com/rpedrazav/TallerIntegracion3/commit/4a20547e4261ba810385936c4ca3f5b2b115fe2b) (integrado en dev): [diagramas/matriz/S2-051-matriz-rn02-pagos.md](https://github.com/rpedrazav/TallerIntegracion3/blob/4a20547e4261ba810385936c4ca3f5b2b115fe2b/diagramas/matriz/S2-051-matriz-rn02-pagos.md); [c552629](https://github.com/rpedrazav/TallerIntegracion3/commit/c5526296852d2cfaad1b3af16ded47ad77fbeceb) (integrado en dev): [globalmart-frontend/tests/synchronized-card-payment.test.cjs](https://github.com/rpedrazav/TallerIntegracion3/blob/c5526296852d2cfaad1b3af16ded47ad77fbeceb/globalmart-frontend/tests/synchronized-card-payment.test.cjs).

**Posible repregunta:** ¿Qué caso detectaría un carrito vaciado por una aprobación solo visual?

---


### Logros personales, aprendizajes y mejoras

<a id="d-27"></a>

**Pregunta N.º:** 27 (D-27)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Básica.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Qué logro personal de QA presentarías con evidencia breve?

**Respuesta modelo:** Presentaría el refuerzo RN-01: solicitud propia válida, solicitud ajena rechazada y snapshots intactos, incluyendo la prueba directa al repositorio. Es una demostración concreta de un riesgo reducido, no solo del número de tests. La explicación personal sobre cómo encontré el defecto debe confirmarse, pero el cambio y los casos están versionados.

**Explicación para estudiar:** Una prueba de seguridad es más convincente cuando combina control positivo, negativo y ausencia de efectos indebidos.

**Evidencia del repositorio:** [d44e22b](https://github.com/rpedrazav/TallerIntegracion3/commit/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e) (integrado en dev): [tests/GlobalMart.IntegrationTests/PosRepositoryTenantIsolationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/PosRepositoryTenantIsolationTests.cs), [tests/GlobalMart.IntegrationTests/ProductoTenantIsolationIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/d44e22b13c9c4621e2d3dc8954b04f2186d4f01e/tests/GlobalMart.IntegrationTests/ProductoTenantIsolationIntegrationTests.cs).

**Posible repregunta:** ¿Qué operación de escritura te parece más crítica para demostrar?

---

<a id="d-28"></a>

**Pregunta N.º:** 28 (D-28)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué aprendizaje verificable aporta separar prueba documental, componente y E2E?

**Respuesta modelo:** Mis entregas muestran que cada nivel responde una pregunta diferente: S2-081 valida el plan, los tests de parser validan normalización y S2-055 recorre Electron y cobro con dependencias acotadas. Aprender a declarar esos límites evita exagerar resultados. Mi reflexión personal puede usar esa evolución, sin afirmar que una sola suite acredita todo el producto.

**Explicación para estudiar:** La calidad de una conclusión depende de su correspondencia con la observación. Más herramientas no amplían automáticamente el alcance comprobado.

**Evidencia del repositorio:** [ce542e9](https://github.com/rpedrazav/TallerIntegracion3/commit/ce542e946fc6da1d4f5c11b91acc56d22e141367) (rama, no integrado en dev): [docs/qa/S2-081/plan-validacion.md](https://github.com/rpedrazav/TallerIntegracion3/blob/ce542e946fc6da1d4f5c11b91acc56d22e141367/docs/qa/S2-081/plan-validacion.md); [5850d61](https://github.com/rpedrazav/TallerIntegracion3/commit/5850d6111d0c5e9086bf6d156cc89fd7c9777dab) (rama, no integrado en dev): [tests/GlobalMart.IntegrationTests/ScaleParserRoundingIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/5850d6111d0c5e9086bf6d156cc89fd7c9777dab/tests/GlobalMart.IntegrationTests/ScaleParserRoundingIntegrationTests.cs); [3148c9a](https://github.com/rpedrazav/TallerIntegracion3/commit/3148c9aa4a19b7a5448bb85107a41c1c060eb37b) (rama, no integrado en dev): [docs/qa/S2-055/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/3148c9aa4a19b7a5448bb85107a41c1c060eb37b/docs/qa/S2-055/reporte.md).

**Posible repregunta:** ¿Qué resultado de tus pruebas sería incorrecto presentar como validación física?

---

<a id="d-29"></a>

**Pregunta N.º:** 29 (D-29)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué debes confirmar al responder cómo organizaste las pruebas con el equipo?

**Respuesta modelo:** Debo confirmar reuniones, criterios acordados, reparto real y revisiones personales. Git acredita matrices, fixtures, reportes y merges, pero no la dinámica completa. La estructura de respuesta puede ser riesgo elegido, coordinación concreta, caso ejecutado, defecto comunicado y resultado posterior, usando un ejemplo verificable de carrito, RN-01 o pagos.

**Explicación para estudiar:** La colaboración no se infiere de quién hizo el último push. Debe narrarse con hechos personales confirmados y apoyo técnico concreto.

**Evidencia del repositorio:** [806450b](https://github.com/rpedrazav/TallerIntegracion3/commit/806450bec2bda6cee096bb62cbc02b204d5daae9) (integrado en dev): [diagramas/matriz/S2-021-matriz-aceptacion.md](https://github.com/rpedrazav/TallerIntegracion3/blob/806450bec2bda6cee096bb62cbc02b204d5daae9/diagramas/matriz/S2-021-matriz-aceptacion.md); [a451b5d](https://github.com/rpedrazav/TallerIntegracion3/commit/a451b5d22e4632ae811e8adcf1814b9de98b20c2) (integrado en dev): [docs/qa/S2-025/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/a451b5d22e4632ae811e8adcf1814b9de98b20c2/docs/qa/S2-025/reporte.md).

**Posible repregunta:** ¿Qué evidencia usarías para explicar un hallazgo que otro integrante corrigió?

---

<a id="d-30"></a>

**Pregunta N.º:** 30 (D-30)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué priorizarías para que tu evidencia pendiente sea útil al cierre integrado?

**Respuesta modelo:** Integraría las suites exclusivas de rama y adaptaría las que dependen de la estructura anterior del POS. Ejecutaría regresión sobre el SHA resultante y publicaría una matriz que separe local, remoto, mocks y hardware. Priorizaría el recorrido hasta inventario y las brechas del plan, evitando declarar vigente un resultado obtenido antes de los últimos cambios.

**Explicación para estudiar:** La evidencia está ligada a una revisión. Integrar código y pruebas crea una nueva composición que necesita validación pertinente.

**Evidencia del repositorio:** [3148c9a](https://github.com/rpedrazav/TallerIntegracion3/commit/3148c9aa4a19b7a5448bb85107a41c1c060eb37b) (rama, no integrado en dev): [docs/qa/S2-055/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/3148c9aa4a19b7a5448bb85107a41c1c060eb37b/docs/qa/S2-055/reporte.md); [f787762](https://github.com/rpedrazav/TallerIntegracion3/commit/f787762c367100e6d7f5d85815d59e697f5330c6) (rama, no integrado en dev): [tests/GlobalMart.IntegrationTests/ScaleFallbackIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/f787762c367100e6d7f5d85815d59e697f5330c6/tests/GlobalMart.IntegrationTests/ScaleFallbackIntegrationTests.cs); [7f85701](https://github.com/rpedrazav/TallerIntegracion3/commit/7f85701bac4c9f94c95bd229410574ec37d2734f) (rama, no integrado en dev): [tests/GlobalMart.IntegrationTests/PesableEntreServiciosIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/7f85701bac4c9f94c95bd229410574ec37d2734f/tests/GlobalMart.IntegrationTests/PesableEntreServiciosIntegrationTests.cs).

**Posible repregunta:** ¿Qué suites elegirías primero para detectar una regresión de pagos y peso?

---


## 10. Banco de 30 preguntas — Diego

**Registro:** 30 preguntas; 10 básicas, 12 intermedias, 8 avanzadas. Numeración única en este banco.

Las respuestas describen el código o la revisión enlazada. Los resultados de ejecución son históricos. “Rama, no integrado” identifica evidencia que no forma parte de dev al corte. Las experiencias personales deben ser confirmadas por el integrante.


### Tareas, responsabilidades y contribuciones

<a id="l-01"></a>

**Pregunta N.º:** 1 (L-01)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué entregaste en el Sprint 2 aunque tu historial tenga pocos commits?

**Respuesta modelo:** Mis cuatro commits de trabajo agrupan varias tareas: rutas y puertos, configuración de API, TypeScript en CI, arranque reproducible, simulador en Compose/Kong, correlación y dispositivos simulados con IPC. Hay además un merge. El número de commits no refleja la amplitud del diff; cada responsabilidad debe demostrarse con sus archivos y no con una cifra de actividad.

**Explicación para estudiar:** Un commit puede reunir varias tareas y un merge puede integrar trabajo ajeno. La evaluación necesita distinguir esos tipos y leer el contenido.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [docs/context/infraestructura/matriz-rutas.md](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/docs/context/infraestructura/matriz-rutas.md), [globalmart-frontend/src/config/api.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/globalmart-frontend/src/config/api.ts); [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [globalmart-frontend/src/devices/DeviceManager.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/devices/DeviceManager.ts).

**Posible repregunta:** ¿Qué tareas están agrupadas en 0fe41f9?

---

<a id="l-02"></a>

**Pregunta N.º:** 2 (L-02)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué inventariaste antes de corregir rutas?

**Respuesta modelo:** Comparé endpoints de controladores, prefijos de Kong y puertos de servicios y host. Ese inventario permite detectar rutas declaradas sin implementación o prefijos incompatibles. Lo respaldé con cambios de alias y pruebas de RoutePrefixIntegrationTests. La matriz es una fotografía del entorno de esa revisión, y debe actualizarse cuando cambien despliegue o contratos.

**Explicación para estudiar:** Un inventario reduce diagnóstico por ensayo y error. Hay que separar dirección del host, dirección dentro de la red y ruta HTTP.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [docs/context/infraestructura/matriz-rutas.md](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/docs/context/infraestructura/matriz-rutas.md), [tests/GlobalMart.IntegrationTests/RoutePrefixIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/tests/GlobalMart.IntegrationTests/RoutePrefixIntegrationTests.cs).

**Posible repregunta:** ¿Por qué ver un servicio healthy no demuestra que /api/ventas esté bien encaminado?

---

<a id="l-03"></a>

**Pregunta N.º:** 3 (L-03)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Cómo se divide tu aporte al simulador respecto del de Nicolás?

**Respuesta modelo:** Incorporé el proceso al entorno local, Compose, gateway y scripts; Nicolás completó reglas de autorización, idempotencia y consulta. La historia de server.js incluye ambos aportes. Debo explicar mi responsabilidad operativa sin atribuirme toda la semántica final de pagos y contrastar los scripts con las pruebas que realmente ejecutan.

**Explicación para estudiar:** La disponibilidad de un componente y su comportamiento de negocio son dimensiones diferentes de la integración. Ambas necesitan evidencia.

**Evidencia del repositorio:** [1ca8853](https://github.com/rpedrazav/TallerIntegracion3/commit/1ca8853ea62c0c1fccf1b95b886d6996e74d0166) (integrado en dev): [Docker/docker-compose.yml](https://github.com/rpedrazav/TallerIntegracion3/blob/1ca8853ea62c0c1fccf1b95b886d6996e74d0166/Docker/docker-compose.yml), [Docker/simulator/server.js](https://github.com/rpedrazav/TallerIntegracion3/blob/1ca8853ea62c0c1fccf1b95b886d6996e74d0166/Docker/simulator/server.js); [1060d53](https://github.com/rpedrazav/TallerIntegracion3/commit/1060d5308c131d7ffe9e73bcd71a497bfda74101) (integrado en dev): [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1060d5308c131d7ffe9e73bcd71a497bfda74101/src/POSCartService/Services/PaymentGatewayClient.cs).

**Posible repregunta:** ¿Qué commit agrega el manejo de consulta tras timeout?

---

<a id="l-04"></a>

**Pregunta N.º:** 4 (L-04)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué cambios agrupa tu commit S2-059 a S2-089?

**Respuesta modelo:** Añade logs de correlación en cobro, una suite y scripts smoke, documentación de límites del terminal y una capa de dispositivos mock. También configura seguridad de Electron y valida entradas IPC. Es un commit amplio, por lo que lo defendería por recorridos específicos, sin presentar todos sus mensajes o comentarios como resultados ejecutados independientes.

**Explicación para estudiar:** Un commit grande exige descomponer evidencia por capacidad. La etiqueta del mensaje no sustituye revisar qué hace cada script y assertion.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/src/POSCartService/Services/PaymentGatewayClient.cs), [globalmart-frontend/src/devices/DeviceManager.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/devices/DeviceManager.ts), [globalmart-frontend/src/index.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/index.ts), [globalmart-frontend/src/devices/adapters/RealTerminalAdapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/devices/adapters/RealTerminalAdapter.ts).

**Posible repregunta:** ¿Qué archivo permite comprobar que el modo físico sigue reservado?

---

<a id="l-05"></a>

**Pregunta N.º:** 5 (L-05)

**Categoría:** Tareas, responsabilidades y contribuciones

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué evidencia adicional revisaste para atribuir tu trabajo y qué no puedes inventar?

**Respuesta modelo:** Se contrastaron los cambios con las tareas de la planilla y se consultaron PR accesibles actualizados en el período, sin resultados. Eso no prueba que nunca existieron revisiones informales o trabajo en otros canales. Para atribuir actividades de revisión, reuniones o ayuda debo confirmarlas personalmente; el banco se basa en mis cambios y el merge verificables.

**Explicación para estudiar:** La ausencia de resultados en una búsqueda tiene alcance limitado por consulta y acceso. No debe transformarse en una afirmación universal sobre colaboración.

**Evidencia del repositorio:** [f913592](https://github.com/rpedrazav/TallerIntegracion3/commit/f913592d8cc01fcd9f4425e39ab2f0bea08f9947) (integrado en dev): [tareas/plan_sprint2_revisado.xlsx](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/plan_sprint2_revisado.xlsx), [tareas/sprint2_youtrack_resultado.json](https://github.com/rpedrazav/TallerIntegracion3/blob/f913592d8cc01fcd9f4425e39ab2f0bea08f9947/tareas/sprint2_youtrack_resultado.json); [512f2b7](https://github.com/rpedrazav/TallerIntegracion3/commit/512f2b71419c11dc4925120245a78a8e16d95637) (integrado en dev): [.github/workflows/ci.yml](https://github.com/rpedrazav/TallerIntegracion3/blob/512f2b71419c11dc4925120245a78a8e16d95637/.github/workflows/ci.yml).

**Posible repregunta:** ¿Qué tarea del plan puedes enlazar directamente con un archivo que modificaste?

---


### Funcionalidades implementadas y explicación del código

<a id="l-06"></a>

**Pregunta N.º:** 6 (L-06)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué hace buildServiceUrl cuando la base ya termina en /api?

**Respuesta modelo:** Limpia barras finales y evita duplicar /api cuando el path ya lo incluye. Usa la base del servicio seleccionado por configuración. Eso permite consumir rutas consistentes entre gateway y llamadas directas sin repetir composición en cada pantalla. La función no verifica que la ruta exista ni que el valor de modo recibido sea válido en ejecución.

**Explicación para estudiar:** Construir una URL correcta es parte del cliente, pero no es prueba de conectividad ni de autorización. Debe contrastarse con el controlador y gateway.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [globalmart-frontend/src/config/api.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/globalmart-frontend/src/config/api.ts).

**Posible repregunta:** ¿Qué caso de prueba usarías con una base terminada en /api y path /api/users?

---

<a id="l-07"></a>

**Pregunta N.º:** 7 (L-07)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo se elige el destino de identidad, catálogo y POS?

**Respuesta modelo:** API_MODE selecciona gateway, local o remote y hay overrides por servicio. El modo gateway usa una base común con /api; local utiliza puertos de desarrollo y remoto los hosts configurados. Esto evita editar cada componente al cambiar entorno. Hay que comprobar cómo llegan esas variables al renderer y no suponer que toda variable del host se inyecta automáticamente.

**Explicación para estudiar:** Configurar una variable y hacerla visible al código que corre son pasos distintos, especialmente en bundles de frontend.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [globalmart-frontend/src/config/api.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/globalmart-frontend/src/config/api.ts).

**Posible repregunta:** ¿Qué revisarías si el renderer sigue usando remoto después de cambiar el entorno?

---

<a id="l-08"></a>

**Pregunta N.º:** 8 (L-08)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué añadiste alias /api a controladores cuando Kong usa strip_path=false?

**Respuesta modelo:** Kong conserva el prefijo que recibe, por lo que /api/ventas debe coincidir con una ruta aceptada por MS-5. Los alias permiten mantener llamadas directas previas y soportar el gateway sin una transformación incompatible. Las pruebas de prefijo comprueban el contrato de rutas; no equivalen a desplegar y atravesar Kong real en cada caso.

**Explicación para estudiar:** El proxy puede preservar o reescribir paths. La decisión debe concordar con el enrutamiento de la aplicación.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [Docker/config/kong.yaml](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/Docker/config/kong.yaml), [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/src/POSCartService/Controllers/VentasController.cs), [tests/GlobalMart.IntegrationTests/RoutePrefixIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/tests/GlobalMart.IntegrationTests/RoutePrefixIntegrationTests.cs).

**Posible repregunta:** ¿Qué síntoma produciría conservar /api cuando el controller solo acepta /ventas?

---

<a id="l-09"></a>

**Pregunta N.º:** 9 (L-09)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué añade el job frontend-typecheck al CI?

**Respuesta modelo:** Instala dependencias desde el lockfile y ejecuta npm run typecheck con tsc --noEmit. Detecta errores estáticos del frontend en push o PR a main/dev. El job existe junto al build backend, pero no demuestra por sí mismo protección obligatoria de ramas, empaquetado exitoso ni pruebas de comportamiento; esas garantías requieren configuración y evidencia adicionales.

**Explicación para estudiar:** Un job que falla aporta señal. Que ese estado bloquee una fusión depende de reglas de repositorio que no se infieren del YAML.

**Evidencia del repositorio:** [512f2b7](https://github.com/rpedrazav/TallerIntegracion3/commit/512f2b71419c11dc4925120245a78a8e16d95637) (integrado en dev): [.github/workflows/ci.yml](https://github.com/rpedrazav/TallerIntegracion3/blob/512f2b71419c11dc4925120245a78a8e16d95637/.github/workflows/ci.yml).

**Posible repregunta:** ¿Qué diferencia hay entre un gate definido y un check requerido en GitHub?

---

<a id="l-10"></a>

**Pregunta N.º:** 10 (L-10)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué integraste para que payment-simulator arranque con el entorno de pruebas?

**Respuesta modelo:** Lo agregué a Compose y al conjunto que levanta CI, con su healthcheck y las rutas del gateway. Los scripts de arranque y smoke documentan cómo alcanzar sus endpoints. Esto elimina la necesidad de iniciar manualmente un proceso separado para cada prueba local, pero el healthcheck solo demuestra disponibilidad básica, no corrección de cobros.

**Explicación para estudiar:** La preparación reproducible reduce diferencias entre máquinas. La aceptación funcional requiere después escenarios y assertions sobre resultados y persistencia.

**Evidencia del repositorio:** [1ca8853](https://github.com/rpedrazav/TallerIntegracion3/commit/1ca8853ea62c0c1fccf1b95b886d6996e74d0166) (integrado en dev): [Docker/docker-compose.yml](https://github.com/rpedrazav/TallerIntegracion3/blob/1ca8853ea62c0c1fccf1b95b886d6996e74d0166/Docker/docker-compose.yml), [Docker/config/kong.yaml](https://github.com/rpedrazav/TallerIntegracion3/blob/1ca8853ea62c0c1fccf1b95b886d6996e74d0166/Docker/config/kong.yaml), [.github/workflows/ci.yml](https://github.com/rpedrazav/TallerIntegracion3/blob/1ca8853ea62c0c1fccf1b95b886d6996e74d0166/.github/workflows/ci.yml).

**Posible repregunta:** ¿Por qué un /health correcto no demuestra idempotencia?

---

<a id="l-11"></a>

**Pregunta N.º:** 11 (L-11)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué identificadores agregaste a los logs de cobro y cómo ayudan a seguir una operación?

**Respuesta modelo:** Los mensajes estructurados incluyen venta, intento o RequestId, correlación y tenant según el punto del flujo, junto al estado y tiempos en el adaptador. Eso permite relacionar entrada, llamada y respuesta sin imprimir datos de tarjeta. Debo usar los identificadores reales del intento; agregar un prefijo de log no significa tener trazas distribuidas completas ni certificación de seguridad.

**Explicación para estudiar:** La observabilidad útil conserva identidad a través de capas. Un log sin correlación puede describir eventos pero no reconstruir un cobro específico.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/src/POSCartService/Controllers/VentasController.cs), [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/src/POSCartService/Services/PaymentGatewayClient.cs).

**Posible repregunta:** ¿Qué campos buscarías para unir un timeout del controller con la consulta posterior?

---

<a id="l-12"></a>

**Pregunta N.º:** 12 (L-12)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué seguridad aporta Electron y qué queda fuera de esas opciones?

**Respuesta modelo:** contextIsolation separa contextos, nodeIntegration=false evita acceso directo del renderer a Node y sandbox limita su entorno. Los handlers validan argumentos y preload expone capacidades concretas. Estas medidas reducen superficie, pero no reemplazan JWT, tenant ni autorización del backend, y tampoco hacen confiable cualquier dato devuelto por un dispositivo o API.

**Explicación para estudiar:** La seguridad es composición de fronteras. Una configuración de aislamiento no convierte automáticamente las operaciones privilegiadas expuestas por el puente en seguras.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [globalmart-frontend/src/index.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/index.ts), [globalmart-frontend/src/preload.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/preload.ts).

**Posible repregunta:** ¿Qué riesgo seguiría existiendo si el puente aceptara comandos arbitrarios?

---

<a id="l-13"></a>

**Pregunta N.º:** 13 (L-13)

**Categoría:** Funcionalidades implementadas y explicación del código

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo se comporta RealTerminalAdapter cuando se selecciona?

**Respuesta modelo:** Es una reserva de implementación: devuelve error y explica que no hay compatibilidad física probada. No abre drivers ni ejecuta un protocolo desconocido. Eso hace explícita la falta de soporte en vez de simular éxito en modo real. El modo mock es el camino demostrable de esa capa durante el sprint.

**Explicación para estudiar:** Fallar explícitamente ante una capacidad ausente evita conclusiones falsas y operaciones sobre supuestos no verificados. El nombre de una clase no acredita funcionalidad.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [globalmart-frontend/src/devices/adapters/RealTerminalAdapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/devices/adapters/RealTerminalAdapter.ts).

**Posible repregunta:** ¿Qué necesitarías implementar para cambiar ese estado de forma legítima?

---


### Decisiones técnicas, tecnologías y arquitectura

<a id="l-14"></a>

**Pregunta N.º:** 14 (L-14)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué centralizaste adaptadores mediante DeviceManager?

**Respuesta modelo:** Encapsula selección y acceso a terminal y balanza y usa configuración común. Permite que los handlers no creen dispositivos dispersos en cada llamada. En esta implementación conserva una instancia singleton, elige terminal mock o reservado y crea balanza mock. Eso organiza el acceso, pero no elimina las interfaces específicas de hardware y payments agregadas por otros compañeros.

**Explicación para estudiar:** Centralizar ciclo de vida puede evitar estados contradictorios entre llamadas. También exige conocer qué implementación queda seleccionada y cuándo se construye.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [globalmart-frontend/src/devices/DeviceManager.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/devices/DeviceManager.ts), [globalmart-frontend/src/config/devices.config.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/config/devices.config.ts).

**Posible repregunta:** ¿Qué ocurre si se intenta cambiar configuración después de crear el singleton?

---

<a id="l-15"></a>

**Pregunta N.º:** 15 (L-15)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué separaste URL de API de las pantallas que la consumen?

**Respuesta modelo:** El mismo frontend necesita funcionar con servicios locales, Kong y hosts remotos. Una configuración central reduce cambios repetidos y permite overrides. Las pantallas se concentran en operaciones y presentación. Debe validarse el modo efectivo y mantenerse coherente con los puertos y prefijos documentados; centralizar un valor incorrecto solo propaga el mismo error a todas.

**Explicación para estudiar:** Configuración central mejora mantenibilidad, pero su corrección sigue necesitando pruebas en los entornos de uso.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [globalmart-frontend/src/config/api.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/globalmart-frontend/src/config/api.ts), [docs/context/infraestructura/matriz-rutas.md](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/docs/context/infraestructura/matriz-rutas.md).

**Posible repregunta:** ¿Qué error ocurriría si API_MODE tiene un valor inesperado?

---

<a id="l-16"></a>

**Pregunta N.º:** 16 (L-16)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué limitación tiene exponer el simulador por Kong sin JWT en esas rutas?

**Respuesta modelo:** La configuración define rutas de simulador accesibles sin el plugin JWT de las rutas comerciales. Sirven al entorno académico controlado, pero no deben interpretarse como un diseño bancario listo para producción. El payload contiene tenant y claves que el simulador usa; una exposición más amplia necesitaría autenticación y autorización del canal y límites operativos definidos.

**Explicación para estudiar:** Estar detrás de un gateway no significa estar protegido por todas sus políticas. Hay que inspeccionar cada ruta y su modelo de confianza.

**Evidencia del repositorio:** [1ca8853](https://github.com/rpedrazav/TallerIntegracion3/commit/1ca8853ea62c0c1fccf1b95b886d6996e74d0166) (integrado en dev): [Docker/config/kong.yaml](https://github.com/rpedrazav/TallerIntegracion3/blob/1ca8853ea62c0c1fccf1b95b886d6996e74d0166/Docker/config/kong.yaml); [f0e6f31](https://github.com/rpedrazav/TallerIntegracion3/commit/f0e6f31e51068b971fcfb2018a76f19b53d41db7) (código al corte dev): [Docker/simulator/server.js](https://github.com/rpedrazav/TallerIntegracion3/blob/f0e6f31e51068b971fcfb2018a76f19b53d41db7/Docker/simulator/server.js).

**Posible repregunta:** ¿Cómo impedirías que un cliente arbitrario invente tenant_id al llamar directamente al simulador?

---

<a id="l-17"></a>

**Pregunta N.º:** 17 (L-17)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Por qué logs sin campos PAN/CVV no equivalen a certificación PCI?

**Respuesta modelo:** Porque excluir esos campos de DTOs y mensajes es una medida concreta del proyecto, no una auditoría integral de procesos, infraestructura y controles. Puedo demostrar referencias opacas y assertions sobre datos manejados, pero no anunciar certificación. La documentación usa esa motivación de seguridad; la defensa debe limitarse a lo verificable en código y pruebas.

**Explicación para estudiar:** Una práctica alineada con un objetivo no demuestra cumplimiento de un estándar completo. La precisión de la afirmación importa tanto como el control técnico.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/src/POSCartService/Services/PaymentGatewayClient.cs), [tests/GlobalMart.IntegrationTests/CobroTarjetaCorrelationIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/tests/GlobalMart.IntegrationTests/CobroTarjetaCorrelationIntegrationTests.cs); [866e5b1](https://github.com/rpedrazav/TallerIntegracion3/commit/866e5b156356706f02d5ceaadedcd9965c9b7ed5) (integrado en dev): [src/POSCartService/DTOs/PaymentGatewayDtos.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/866e5b156356706f02d5ceaadedcd9965c9b7ed5/src/POSCartService/DTOs/PaymentGatewayDtos.cs).

**Posible repregunta:** ¿Qué superficies revisarías además del log informativo del cobro?

---

<a id="l-18"></a>

**Pregunta N.º:** 18 (L-18)

**Categoría:** Decisiones técnicas, tecnologías y arquitectura

**Dificultad:** Intermedia.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué riesgo de arquitectura ves en tener DeviceManager y puertos específicos de balanza/terminal?

**Respuesta modelo:** Hay abstracciones paralelas con capacidades y estados distintos. Para saber qué se prueba hay que seguir la ruta real desde preload y POS. Consolidarlas podría reducir duplicación, pero necesitaría preservar los contratos de ScaleAdapter y TerminalPort y sus validaciones. No asumiría que probar mi MockScaleAdapter demuestra el adaptador serial de Nicolás.

**Explicación para estudiar:** Duplicación de conceptos puede crear falsas equivalencias. Antes de refactorizar se debe mapear qué consumidores dependen de cada interfaz.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [globalmart-frontend/src/devices/DeviceManager.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/devices/DeviceManager.ts); [71847b1](https://github.com/rpedrazav/TallerIntegracion3/commit/71847b18c559111107436474461d18542688b7dc) (integrado en dev): [globalmart-frontend/src/hardware/scale-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/71847b18c559111107436474461d18542688b7dc/globalmart-frontend/src/hardware/scale-ipc.ts); [96dc728](https://github.com/rpedrazav/TallerIntegracion3/commit/96dc728f1adf215086f49f01f6271f38a43d9257) (integrado en dev): [globalmart-frontend/src/payments/terminal-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/96dc728f1adf215086f49f01f6271f38a43d9257/globalmart-frontend/src/payments/terminal-ipc.ts).

**Posible repregunta:** ¿Qué contrato conservarías como frontera del renderer y por qué?

---


### Problemas encontrados y soluciones

<a id="l-19"></a>

**Pregunta N.º:** 19 (L-19)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué tipo de error resolvían las rutas duales del flujo crítico?

**Respuesta modelo:** Un path válido en Kong podía no coincidir con el controller si se conservaba /api. Agregar alias y alinear prefijos resolvió esa clase de incompatibilidad. La matriz deja visibles puertos y rutas para diagnosticarla. No todos los errores de conexión se resuelven con alias: un destino localhost incorrecto dentro del clúster sigue siendo un problema diferente.

**Explicación para estudiar:** Routing HTTP y resolución del destino son etapas consecutivas. Hay que identificar cuál falló antes de aplicar una corrección.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [docs/context/infraestructura/matriz-rutas.md](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/docs/context/infraestructura/matriz-rutas.md), [tests/GlobalMart.IntegrationTests/RoutePrefixIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/tests/GlobalMart.IntegrationTests/RoutePrefixIntegrationTests.cs); [25a40f5](https://github.com/rpedrazav/TallerIntegracion3/commit/25a40f52648694c51c2ffa429c065349c3046202) (integrado en dev): [docs/qa/S2-052/reporte.md](https://github.com/rpedrazav/TallerIntegracion3/blob/25a40f52648694c51c2ffa429c065349c3046202/docs/qa/S2-052/reporte.md).

**Posible repregunta:** ¿Cómo distinguirías un 404 de ruta de un 502 por upstream inaccesible?

---

<a id="l-20"></a>

**Pregunta N.º:** 20 (L-20)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Por qué el mensaje final del smoke de tarjeta no debe tomarse como evidencia suficiente?

**Respuesta modelo:** El script ejecuta una suite filtrada y después imprime un resumen fijo. set -e evita continuar ante un exit code fallido, pero el texto no demuestra por sí mismo que todos los casos esperados se ejecutaron ni que se publicó en Kafka. Debo revisar resultados y assertions reales, incluidas omisiones y dobles de prueba, antes de citar esa conclusión.

**Explicación para estudiar:** Una herramienta puede producir un texto optimista que exceda sus observaciones. La evidencia debe venir del ejecutor y de lo que la prueba mide.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [scripts/smoke-tarjeta.sh](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/scripts/smoke-tarjeta.sh), [tests/GlobalMart.IntegrationTests/CobroTarjetaCorrelationIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/tests/GlobalMart.IntegrationTests/CobroTarjetaCorrelationIntegrationTests.cs).

**Posible repregunta:** ¿Cómo detectarías una ejecución con cero pruebas seleccionadas?

---

<a id="l-21"></a>

**Pregunta N.º:** 21 (L-21)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué problema evita tener un smoke de dispositivos sin periféricos?

**Respuesta modelo:** Permite ejercitar el comportamiento simulado y detectar roturas del contrato o de la configuración sin depender de equipo prestado. El script llama a la suite del frontend y propaga el fallo. Su utilidad es reproducibilidad de simulación; no permite concluir que una balanza o terminal físico será compatible ni que pasó una transacción bancaria.

**Explicación para estudiar:** Reducir dependencias vuelve estable una prueba, pero también reduce el universo de fallos observados. Esa compensación debe quedar explícita.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [scripts/smoke-simuladores-frontend.sh](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/scripts/smoke-simuladores-frontend.sh), [docs/context/integraciones/limites-terminal-simulado.md](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/docs/context/integraciones/limites-terminal-simulado.md).

**Posible repregunta:** ¿Qué ensayo adicional necesitarías para validar drivers reales?

---

<a id="l-22"></a>

**Pregunta N.º:** 22 (L-22)

**Categoría:** Problemas encontrados y soluciones

**Dificultad:** Avanzada.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo investigarías una pérdida temporal de pasarela usando tus logs y scripts?

**Respuesta modelo:** Identificaría venta e intento y buscaría entrada, llamada, timeout y consulta por la misma referencia. Compararía persistencia de intento, pago y outbox y el número de autorizaciones, sin dar por rechazado un cobro porque hubo timeout. El smoke puede ayudar a reproducir escenarios controlados; el diagnóstico remoto necesita además configuración y evidencia del entorno afectado.

**Explicación para estudiar:** Observabilidad y pruebas responden preguntas complementarias: una explica lo ocurrido y la otra permite repetir una hipótesis en condiciones controladas.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/src/POSCartService/Services/PaymentGatewayClient.cs), [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/src/POSCartService/Controllers/VentasController.cs), [scripts/smoke-tarjeta.sh](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/scripts/smoke-tarjeta.sh); [1060d53](https://github.com/rpedrazav/TallerIntegracion3/commit/1060d5308c131d7ffe9e73bcd71a497bfda74101) (integrado en dev): [tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1060d5308c131d7ffe9e73bcd71a497bfda74101/tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs).

**Posible repregunta:** ¿Qué hallazgo indicaría que se intentó autorizar nuevamente de forma insegura?

---


### Integración con el trabajo de otros integrantes

<a id="l-23"></a>

**Pregunta N.º:** 23 (L-23)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo se integra tu configuración de URLs con el cliente centralizado de Rodrigo?

**Respuesta modelo:** Mi módulo elige destinos y compone paths; su cliente organiza operaciones, token, timeout y errores. Deben utilizar la misma base efectiva y evitar URLs dispersas en componentes. El resultado permite alternar entornos con menos cambios, pero todavía hay que verificar que las variables lleguen al bundle y que los endpoints respondan donde se configuraron.

**Explicación para estudiar:** Separar configuración de operación permite cambiar entorno sin cambiar el caso de uso. El enlace entre módulos debe probarse explícitamente.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [globalmart-frontend/src/config/api.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/globalmart-frontend/src/config/api.ts); [e1a2691](https://github.com/rpedrazav/TallerIntegracion3/commit/e1a269113ceeef94ed89dd79502b830fc2557ab2) (integrado en dev): [globalmart-frontend/src/api/client.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/e1a269113ceeef94ed89dd79502b830fc2557ab2/globalmart-frontend/src/api/client.ts).

**Posible repregunta:** ¿Dónde buscarías una URL hardcodeada que eluda esa configuración?

---

<a id="l-24"></a>

**Pregunta N.º:** 24 (L-24)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué necesitabas del contrato de Nicolás para exponer correctamente el simulador?

**Respuesta modelo:** Debía respetar sus rutas de autorización y consulta, métodos HTTP, clave de idempotencia y healthcheck. Un gateway que solo permita POST impediría reconciliar por GET. El entorno también debe usar el timeout y dirección esperados por PaymentGatewayClient. La integración se evalúa con los escenarios, no solo arrancando el contenedor.

**Explicación para estudiar:** Las necesidades de transporte evolucionan con el contrato: agregar recuperación puede requerir nuevas rutas o métodos además de lógica interna.

**Evidencia del repositorio:** [1ca8853](https://github.com/rpedrazav/TallerIntegracion3/commit/1ca8853ea62c0c1fccf1b95b886d6996e74d0166) (integrado en dev): [Docker/config/kong.yaml](https://github.com/rpedrazav/TallerIntegracion3/blob/1ca8853ea62c0c1fccf1b95b886d6996e74d0166/Docker/config/kong.yaml), [Docker/docker-compose.yml](https://github.com/rpedrazav/TallerIntegracion3/blob/1ca8853ea62c0c1fccf1b95b886d6996e74d0166/Docker/docker-compose.yml); [1060d53](https://github.com/rpedrazav/TallerIntegracion3/commit/1060d5308c131d7ffe9e73bcd71a497bfda74101) (integrado en dev): [src/POSCartService/Services/PaymentGatewayClient.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/1060d5308c131d7ffe9e73bcd71a497bfda74101/src/POSCartService/Services/PaymentGatewayClient.cs).

**Posible repregunta:** ¿Qué ruta utiliza la consulta por RequestId y tenant?

---

<a id="l-25"></a>

**Pregunta N.º:** 25 (L-25)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Cómo complementarías las pruebas de correlación con el cobro seguro de Sebastián?

**Respuesta modelo:** Verificaría que cada guardia fallida evite la autorización, que el intento persista antes del POST y que los logs usen la misma referencia sin secretos. La correlación facilita observar esos pasos, pero no reemplaza assertions de reserva, estado y unicidad. Una línea de log de éxito no debe confirmar algo que la transacción revirtió.

**Explicación para estudiar:** Instrumentación y reglas de negocio tienen responsabilidades distintas. La observación debe reflejar el commit efectivo, no solamente la respuesta del proveedor externo.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [tests/GlobalMart.IntegrationTests/CobroTarjetaCorrelationIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/tests/GlobalMart.IntegrationTests/CobroTarjetaCorrelationIntegrationTests.cs), [src/POSCartService/Controllers/VentasController.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/src/POSCartService/Controllers/VentasController.cs); [212cc56](https://github.com/rpedrazav/TallerIntegracion3/commit/212cc56b8ffe0c44dc07650a40b01ebc2996f215) (integrado en dev): [tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/212cc56b8ffe0c44dc07650a40b01ebc2996f215/tests/GlobalMart.IntegrationTests/CobroSeguroIntegrationTests.cs).

**Posible repregunta:** ¿En qué punto tendría sentido registrar venta completada?

---

<a id="l-26"></a>

**Pregunta N.º:** 26 (L-26)

**Categoría:** Integración con el trabajo de otros integrantes

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué debe coordinarse entre tus canales IPC y los de Nicolás y Martín?

**Respuesta modelo:** Los nombres, payloads y capacidades de cada API, para que preload no exponga rutas genéricas ni el POS llame al adaptador equivocado. Mi capa devices convive con scale y con el terminal de pagos. La revisión debe seguir cada llamada real y comprobar validación, estados y pruebas correspondientes antes de unificar o retirar una implementación.

**Explicación para estudiar:** Compatibilidad de nombres no garantiza equivalencia semántica. Dos métodos llamados autorizar pueden tener autoridades y efectos distintos.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [globalmart-frontend/src/index.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/index.ts), [globalmart-frontend/src/preload.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/preload.ts); [71847b1](https://github.com/rpedrazav/TallerIntegracion3/commit/71847b18c559111107436474461d18542688b7dc) (integrado en dev): [globalmart-frontend/src/hardware/scale-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/71847b18c559111107436474461d18542688b7dc/globalmart-frontend/src/hardware/scale-ipc.ts); [96dc728](https://github.com/rpedrazav/TallerIntegracion3/commit/96dc728f1adf215086f49f01f6271f38a43d9257) (integrado en dev): [globalmart-frontend/src/payments/terminal-ipc.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/96dc728f1adf215086f49f01f6271f38a43d9257/globalmart-frontend/src/payments/terminal-ipc.ts).

**Posible repregunta:** ¿Cómo comprobarías qué adaptador atiende una lectura del POS actual?

---


### Logros personales, aprendizajes y mejoras

<a id="l-27"></a>

**Pregunta N.º:** 27 (L-27)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Básica.

**Prioridad de estudio:** Baja.

**Pregunta del profesor:** ¿Qué logro de infraestructura puedes demostrar sin afirmar que todo el clúster funciona?

**Respuesta modelo:** Puedo mostrar la matriz y correspondencia de rutas, el simulador en Compose y el job de TypeScript, junto a pruebas enfocadas. Eso acredita configuración y contratos locales. Una demostración de disponibilidad remota necesita una ejecución propia del entorno; no trasladaría un smoke local a una afirmación de despliegue completo.

**Explicación para estudiar:** La reproducibilidad local reduce riesgos de integración, pero no incorpora cuotas, DNS o configuración efectiva de Kubernetes.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [docs/context/infraestructura/matriz-rutas.md](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/docs/context/infraestructura/matriz-rutas.md); [1ca8853](https://github.com/rpedrazav/TallerIntegracion3/commit/1ca8853ea62c0c1fccf1b95b886d6996e74d0166) (integrado en dev): [Docker/docker-compose.yml](https://github.com/rpedrazav/TallerIntegracion3/blob/1ca8853ea62c0c1fccf1b95b886d6996e74d0166/Docker/docker-compose.yml); [512f2b7](https://github.com/rpedrazav/TallerIntegracion3/commit/512f2b71419c11dc4925120245a78a8e16d95637) (integrado en dev): [.github/workflows/ci.yml](https://github.com/rpedrazav/TallerIntegracion3/blob/512f2b71419c11dc4925120245a78a8e16d95637/.github/workflows/ci.yml).

**Posible repregunta:** ¿Qué comprobación remota priorizarías antes de comenzar la demo del POS?

---

<a id="l-28"></a>

**Pregunta N.º:** 28 (L-28)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Básica.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Qué aprendizaje técnico muestra mantener RealTerminalAdapter explícitamente no soportado?

**Respuesta modelo:** Muestra que una extensión arquitectónica puede reservarse sin prometer compatibilidad desconocida. El código informa error y orienta al modo mock. Mi aprendizaje personal debe confirmarse, pero puedo explicar el valor técnico: evitar drivers y comandos supuestos y exigir evidencia antes de cambiar el estado de soporte físico.

**Explicación para estudiar:** Un límite explícito hace el sistema más predecible que una implementación vacía que aparenta éxito. El contrato debe representar capacidad real.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [globalmart-frontend/src/devices/adapters/RealTerminalAdapter.ts](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/globalmart-frontend/src/devices/adapters/RealTerminalAdapter.ts), [docs/context/integraciones/limites-terminal-simulado.md](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/docs/context/integraciones/limites-terminal-simulado.md).

**Posible repregunta:** ¿Qué prueba impediría que el modo real empiece a devolver éxito accidentalmente?

---

<a id="l-29"></a>

**Pregunta N.º:** 29 (L-29)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Intermedia.

**Prioridad de estudio:** Media.

**Pregunta del profesor:** ¿Cómo explicarías una dificultad personal de integración de entornos sin inferirla solo del diff?

**Respuesta modelo:** Debo confirmar el problema que experimenté, las alternativas y quién participó. Puedo estructurar la respuesta con síntoma, hipótesis, configuración revisada, cambio y evidencia. Las rutas y scripts respaldan el resultado técnico, pero no prueban tiempo invertido ni reuniones. Conviene elegir un caso concreto de prefijo o simulador que pueda explicar de principio a fin.

**Explicación para estudiar:** La experiencia personal requiere memoria y confirmación del integrante. El repositorio aporta un ancla verificable para evitar una respuesta genérica.

**Evidencia del repositorio:** [0fe41f9](https://github.com/rpedrazav/TallerIntegracion3/commit/0fe41f9793f9422c2c57ec5566160c74bc20f3e8) (integrado en dev): [docs/context/infraestructura/matriz-rutas.md](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/docs/context/infraestructura/matriz-rutas.md), [tests/GlobalMart.IntegrationTests/RoutePrefixIntegrationTests.cs](https://github.com/rpedrazav/TallerIntegracion3/blob/0fe41f9793f9422c2c57ec5566160c74bc20f3e8/tests/GlobalMart.IntegrationTests/RoutePrefixIntegrationTests.cs).

**Posible repregunta:** ¿Qué comprobación te permitió distinguir ruta incorrecta de servicio caído?

---

<a id="l-30"></a>

**Pregunta N.º:** 30 (L-30)

**Categoría:** Logros personales, aprendizajes y mejoras

**Dificultad:** Avanzada.

**Prioridad de estudio:** Alta.

**Pregunta del profesor:** ¿Qué mejorarías en tus verificaciones operativas antes del próximo cierre?

**Respuesta modelo:** Haría que los scripts produzcan un resumen basado en resultados reales, exijan un número esperado de casos y distingan outbox persistido de publicación Kafka. Validaría modo de API y configuración efectiva antes del smoke. También consolidaría capacidades IPC con el equipo y registraría el entorno de cada ejecución. Son propuestas, no garantías ya presentes en los mensajes finales de los scripts.

**Explicación para estudiar:** Una automatización debe fallar cuando no puede verificar su promesa. La calidad del reporte es parte de la confiabilidad de la herramienta.

**Evidencia del repositorio:** [2c68eba](https://github.com/rpedrazav/TallerIntegracion3/commit/2c68eba442848ed1f6adf31ad15313f52716b851) (integrado en dev): [scripts/smoke-tarjeta.sh](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/scripts/smoke-tarjeta.sh), [scripts/smoke-simuladores-frontend.sh](https://github.com/rpedrazav/TallerIntegracion3/blob/2c68eba442848ed1f6adf31ad15313f52716b851/scripts/smoke-simuladores-frontend.sh); [512f2b7](https://github.com/rpedrazav/TallerIntegracion3/commit/512f2b71419c11dc4925120245a78a8e16d95637) (integrado en dev): [.github/workflows/ci.yml](https://github.com/rpedrazav/TallerIntegracion3/blob/512f2b71419c11dc4925120245a78a8e16d95637/.github/workflows/ci.yml).

**Posible repregunta:** ¿Qué condición debería convertir en fallo una corrida que omitió el caso de timeout?

---


## 11. Las diez preguntas más importantes para cada integrante

Selección para priorizar estudio, basada en autoría, riesgos del dominio y dependencias. No agrega preguntas nuevas ni cambia los totales. Los enlaces apuntan a las respuestas completas.

### Sebastián

Prioriza autoridad del servidor, reserva y finalización del cobro, coherencia pesable y el pendiente de anulación.

1. **[S-02](05-sebastian.md#s-02)** — ¿Qué entregaste en S2-001 y qué todavía dependía de S2-002?
2. **[S-08](05-sebastian.md#s-08)** — ¿Cómo llega el IVA correcto a la primera venta sin editar después un ítem?
3. **[S-09](05-sebastian.md#s-09)** — ¿Por qué validaste EFECTIVO tanto en el controlador como en VentaService?
4. **[S-11](05-sebastian.md#s-11)** — ¿Por qué no basta el índice único de IdempotencyKey para evitar dos cobros?
5. **[S-12](05-sebastian.md#s-12)** — ¿Qué se confirma atómicamente cuando la pasarela aprueba?
6. **[S-13](05-sebastian.md#s-13)** — ¿Cómo decide ReglasCantidadItem la cantidad facturable de un pesable?
7. **[S-17](05-sebastian.md#s-17)** — ¿Qué garantía ofrece la actualización condicional del intento frente a una reconciliación concurrente?
8. **[S-19](05-sebastian.md#s-19)** — ¿Qué fallo concreto revelaron tus pruebas de fronteras de peso?
9. **[S-20](05-sebastian.md#s-20)** — ¿Cómo resolviste el conflicto entre FALLIDO y PENDIENTE para una autorización incierta?
10. **[S-30](05-sebastian.md#s-30)** — ¿Qué mejora técnica priorizarías en MS-5 después de este sprint y cómo la aceptarías?

### Nicolás

Prioriza contrato y durabilidad del evento, coordinación de workers, incertidumbre de pago y límites del consumidor.

1. **[N-02](06-nicolas.md#n-02)** — ¿Qué definiste en S2-006 antes de crear la tabla outbox?
2. **[N-06](06-nicolas.md#n-06)** — ¿Qué guarda OutboxMessage y por qué guarda el JSON completo?
3. **[N-07](06-nicolas.md#n-07)** — ¿Cómo cambió CompletarAsync con tu incorporación del outbox?
4. **[N-08](06-nicolas.md#n-08)** — ¿Cómo selecciona el worker mensajes sin romper el aislamiento por tenant?
5. **[N-10](06-nicolas.md#n-10)** — ¿Qué hace PaymentGatewayClient cuando se agota el tiempo de autorización?
6. **[N-11](06-nicolas.md#n-11)** — ¿Por qué tu prueba de recuperación no demuestra exactamente una vez en Kafka real?
7. **[N-12](06-nicolas.md#n-12)** — ¿Cómo garantizas que un worker antiguo no marque una reserva que ya perdió?
8. **[N-17](06-nicolas.md#n-17)** — ¿Qué límite tiene la idempotencia del simulador de pagos que implementaste?
9. **[N-25](06-nicolas.md#n-25)** — ¿Qué dependencia de tu outbox identificaste en el consumidor de MS-4?
10. **[N-30](06-nicolas.md#n-30)** — ¿Qué mejorarías primero en la operación del outbox y cómo lo validarías?

### Martín

Prioriza baja lógica, contrato de terminal, autoridad de MS-5, IPC y diferencia entre bloqueo local y seguridad durable.

1. **[M-02](07-martin.md#m-02)** — ¿Qué diferencia hay entre tus commits TI3-642 y TI3-643?
2. **[M-06](07-martin.md#m-06)** — ¿Qué devuelve DELETE de producto cuando se repite y por qué?
3. **[M-07](07-martin.md#m-07)** — ¿Por qué mantuviste dos formas de buscar producto por ID?
4. **[M-10](07-martin.md#m-10)** — ¿Qué información transporta TerminalPort sin manejar datos de tarjeta?
5. **[M-11](07-martin.md#m-11)** — ¿Cómo impide synchronizeCardPayment aceptar la respuesta de otro intento?
6. **[M-12](07-martin.md#m-12)** — ¿Por qué la sincronización no debe considerar approved del simulador como autoridad final?
7. **[M-16](07-martin.md#m-16)** — ¿Por qué el IPC del terminal rechaza propiedades extra en vez de confiar en los tipos?
8. **[M-17](07-martin.md#m-17)** — ¿Qué garantiza single-flight en tu rama y por qué aún necesitas idempotencia backend?
9. **[M-24](07-martin.md#m-24)** — ¿Qué dependías del backend para que tu desactivación de usuario funcionara después de recargar?
10. **[M-30](07-martin.md#m-30)** — ¿Qué mejora priorizarías en el contrato de cancelación del terminal?

### Rodrigo

Prioriza sincronización del POS, recuperación, captura pesable y coherencia del cierre administrativo con el backend.

1. **[R-07](08-rodrigo.md#r-07)** — ¿Cómo cambió el carrito al incorporar POST /ventas?
2. **[R-08](08-rodrigo.md#r-08)** — ¿Cómo manejas agregar, cambiar cantidad y eliminar sin dejar totales locales inconsistentes?
3. **[R-10](08-rodrigo.md#r-10)** — ¿Cómo se recupera en el POS un intento de tarjeta después de recargar?
4. **[R-11](08-rodrigo.md#r-11)** — ¿Cómo evita tu flujo de peso agregar un ítem al leer o al cancelar?
5. **[R-12](08-rodrigo.md#r-12)** — ¿Cómo se relacionan roles propios, permisos base y el JWT final?
6. **[R-13](08-rodrigo.md#r-13)** — ¿Qué valida manualWeightForCart antes de habilitar el respaldo?
7. **[R-16](08-rodrigo.md#r-16)** — ¿Qué protege el bootstrap administrativo y por qué no es un registro público?
8. **[R-19](08-rodrigo.md#r-19)** — ¿Qué problema resolviste al unificar users y Usuarios?
9. **[R-20](08-rodrigo.md#r-20)** — ¿Cómo evitaste que el timeout de tarjeta se convirtiera en un doble cobro por la UI?
10. **[R-25](08-rodrigo.md#r-25)** — ¿Cómo integraste la sincronización de Martín con la recuperación de tarjeta existente?

### Daniel

Prioriza qué ejercita realmente cada suite, cómo prueba aislamiento/idempotencia y cómo integrar evidencia pendiente.

1. **[D-06](09-daniel.md#d-06)** — ¿Qué construye TwoTenantFixture para las pruebas de aislamiento?
2. **[D-08](09-daniel.md#d-08)** — ¿Cómo ejercitan tus tests RN-01 la autenticación real?
3. **[D-09](09-daniel.md#d-09)** — ¿Por qué añadiste pruebas directas de repositorios además de casos HTTP?
4. **[D-10](09-daniel.md#d-10)** — ¿Qué observas en S2-052 después de un timeout y de la respuesta tardía?
5. **[D-11](09-daniel.md#d-11)** — ¿Cómo demuestra S2-053 idempotencia sin inventar una respuesta de pasarela?
6. **[D-12](09-daniel.md#d-12)** — ¿Qué prueba realmente tu E2E Electron de tarjeta y qué deja fuera?
7. **[D-16](09-daniel.md#d-16)** — ¿Qué valor y qué limitación tiene tu prueba entre servicios S2-084?
8. **[D-17](09-daniel.md#d-17)** — ¿Por qué tu prueba de fallback basada en AST debe describirse con especial cuidado?
9. **[D-19](09-daniel.md#d-19)** — ¿Qué dificultad encontraste al seleccionar la ventana de Electron en S2-024?
10. **[D-30](09-daniel.md#d-30)** — ¿Qué priorizarías para que tu evidencia pendiente sea útil al cierre integrado?

### Diego

Prioriza configuración efectiva, rutas, CI, observabilidad, límites de simulación y confiabilidad de los scripts.

1. **[L-06](10-diego.md#l-06)** — ¿Qué hace buildServiceUrl cuando la base ya termina en /api?
2. **[L-07](10-diego.md#l-07)** — ¿Cómo se elige el destino de identidad, catálogo y POS?
3. **[L-08](10-diego.md#l-08)** — ¿Por qué añadiste alias /api a controladores cuando Kong usa strip_path=false?
4. **[L-09](10-diego.md#l-09)** — ¿Qué añade el job frontend-typecheck al CI?
5. **[L-11](10-diego.md#l-11)** — ¿Qué identificadores agregaste a los logs de cobro y cómo ayudan a seguir una operación?
6. **[L-12](10-diego.md#l-12)** — ¿Qué seguridad aporta Electron y qué queda fuera de esas opciones?
7. **[L-16](10-diego.md#l-16)** — ¿Qué limitación tiene exponer el simulador por Kong sin JWT en esas rutas?
8. **[L-18](10-diego.md#l-18)** — ¿Qué riesgo de arquitectura ves en tener DeviceManager y puertos específicos de balanza/terminal?
9. **[L-20](10-diego.md#l-20)** — ¿Por qué el mensaje final del smoke de tarjeta no debe tomarse como evidencia suficiente?
10. **[L-30](10-diego.md#l-30)** — ¿Qué mejorarías en tus verificaciones operativas antes del próximo cierre?

## 12. Quince preguntas grupales especialmente plausibles para la defensa

Se seleccionan por centralidad en las metas, reglas obligatorias, cambios y brechas observadas. Es una estimación cualitativa de relevancia; no se asignan porcentajes ni se afirma conocer las intenciones del profesor.

1. **[G-11](04-preguntas-grupales.md#g-11) — ¿Cuál es el recorrido de una venta desde que entra el cajero al POS?**

   **Justificación:** Recorre el objetivo central del POS y une identidad, turno, catálogo, impuestos y persistencia.

2. **[G-12](04-preguntas-grupales.md#g-12) — ¿Cómo evita el servidor que un cliente compre un producto a un precio inventado?**

   **Justificación:** Evalúa una corrección central del sprint: impedir que el cliente sea autoridad del precio.

3. **[G-15](04-preguntas-grupales.md#g-15) — ¿Qué diferencia hay entre tarjeta aprobada, rechazada e incierta?**

   **Justificación:** Distingue los tres resultados que determinan seguridad y experiencia del cobro.

4. **[G-29](04-preguntas-grupales.md#g-29) — ¿Cómo impiden que dos instancias de MS-5 autoricen simultáneamente la misma venta?**

   **Justificación:** Exige explicar por qué el doble cobro necesita coordinación durable y no solo un botón bloqueado.

5. **[G-24](04-preguntas-grupales.md#g-24) — ¿Por qué usar un outbox en PostgreSQL para sale.completed?**

   **Justificación:** Explica la decisión arquitectónica que conserva el evento junto a la venta.

6. **[G-28](04-preguntas-grupales.md#g-28) — ¿La implementación garantiza exactamente una entrega y un descuento de stock?**

   **Justificación:** Evita una afirmación incorrecta de exactamente una vez y muestra comprensión de los límites de MS-4.

7. **[G-16](04-preguntas-grupales.md#g-16) — ¿Cómo ingresa un producto pesable y qué cantidad se factura?**

   **Justificación:** Conecta una meta visible del sprint con contrato de unidades, validación y cálculo.

8. **[G-19](04-preguntas-grupales.md#g-19) — ¿Por qué una aprobación del terminal simulado no basta para vaciar el carrito?**

   **Justificación:** Evalúa la integración del terminal con la autoridad comercial de MS-5.

9. **[G-35](04-preguntas-grupales.md#g-35) — ¿Cómo evita la prueba RN-01 un falso éxito por rechazar todas las solicitudes?**

   **Justificación:** Comprueba comprensión de RN-01 y de las pruebas que realmente la respaldan.

10. **[G-34](04-preguntas-grupales.md#g-34) — ¿Qué se resolvió en el merge de cobro seguro y recuperación de pagos?**

   **Justificación:** Es un ejemplo documentado de trabajo compartido y resolución de conflicto semántico.

11. **[G-18](04-preguntas-grupales.md#g-18) — ¿Qué cambió en la administración al final del sprint?**

   **Justificación:** Cubre las ampliaciones administrativas del último corte, incluidos roles propios y coherencia de login.

12. **[G-01](04-preguntas-grupales.md#g-01) — ¿Cuáles fueron las cinco metas del Sprint 2 y qué problema concreto resolvía cada una?**

   **Justificación:** Relaciona los resultados con las cinco metas y el alcance acordado, antes de profundizar en código.

13. **[G-39](04-preguntas-grupales.md#g-39) — ¿Por qué distinguir UI con HTTP simulado, integración backend y E2E Electron?**

   **Justificación:** Permite evaluar si el grupo entiende sus resultados de pruebas sin exagerar E2E, hardware o clúster.

14. **[G-54](04-preguntas-grupales.md#g-54) — ¿Qué pendientes deberían reconocer sin minimizar los logros?**

   **Justificación:** Conecta el cierre con pendientes concretos del plan y previene presentar diseño como implementación.

15. **[G-59](04-preguntas-grupales.md#g-59) — ¿Qué experimento diseñarían para demostrar recuperación completa desde cobro hasta inventario?**

   **Justificación:** Exige razonar sobre fallos reales en la cadena cobro→outbox→Kafka→stock y proponer aceptación verificable.
