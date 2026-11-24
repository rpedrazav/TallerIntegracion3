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
