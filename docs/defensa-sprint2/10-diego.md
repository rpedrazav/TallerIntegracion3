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
