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
