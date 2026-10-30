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
