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
