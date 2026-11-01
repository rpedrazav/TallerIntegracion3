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
