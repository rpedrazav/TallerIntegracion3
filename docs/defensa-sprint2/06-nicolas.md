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
