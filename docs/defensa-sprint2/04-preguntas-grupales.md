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
