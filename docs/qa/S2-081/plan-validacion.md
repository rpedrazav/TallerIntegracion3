# S2-081 — Separar pruebas simuladas y físicas

Fecha: 2026-10-25. Prioridad MoSCoW: **Must**. Dificultad: **Baja**. Meta: **Balanza con simulador**.

## Objetivo y decisión de alcance

Definir qué puede certificarse en Sprint 2 y qué exige modelo, protocolo y dispositivo prestado. La entrega es este plan de validación con condición física explícita.

**Aceptación: El informe no declara integración física completada por pruebas de mock.**

En Sprint 2 puede acreditarse el comportamiento del software probado con simulador o dobles, identificando cada sustitución. **Integración física: CONDICIONADA / [NO VERIFICADO]**. No se dispone de balanza ni protocolo confirmado; aprobar mocks, fixtures, IPC o un adaptador serial con transporte sustituido no levanta esta condición. «Certificar» aquí significa acreditar el alcance técnico de una prueba académica, sin atribuir certificación metrológica.

## Matriz de validación

| Nivel | Casos y criterio para aprobar | Evidencia exigida | Qué permite concluir | Qué no acredita |
|---|---|---|---|---|
| SIMULADA — contrato y simulador | Conectar, leer y desconectar; 750 g → 0.750 kg; rechazar peso inválido, error y desconexión según contrato S2-061 | Comando, commit, fecha, salida y escenarios del simulador | Comportamiento del contrato con entradas controladas | Puerto, protocolo, precisión o estabilidad física |
| SIMULADA — serial con dobles | Conversión, timeout, trama inválida e inestabilidad con transport/parser inyectados; configuración incompleta falla | Resultado de suite y declaración de dobles utilizados | Respuesta del adaptador ante escenarios sintéticos | Compatibilidad con fabricante o modelo real |
| SIMULADA — IPC | Whitelist, payload válido/inválido y aislamiento de renderer | Salida de pruebas y configuración probada | Contrato y límites del puente bajo prueba | Lectura desde un dispositivo |
| SIMULADA — POS y MS-5 | Producto pesable, peso canónico y cálculo autoritativo; rechazo ante JWT inválido, tenant ajeno o ausencia de turno abierto del cajero; errores no agregan peso inválido | Registro del recorrido, petición/respuesta sin secretos, sustituciones de servicios y resultado por caso | Solo el recorrido y reglas ejercitados con simulador | E2E físico ni servicios sustituidos por fixtures |
| FÍSICA — dispositivo | Ejecutar P1–P6 de S2-085 con balanza identificada, manual y medios de medición disponibles | Puerto/driver, parámetros, tramas crudas, fotos, mediciones y resultados | Comunicación y mediciones del equipo efectivamente probado | Otros modelos o configuraciones |
| FÍSICA — aplicación | Ejecutar P7 y P8 de S2-085 con transporte/parser reales y POS; rechazar lecturas inválidas y verificar peso/precio persistidos | Evidencia enlazada desde trama real hasta ítem de venta y registro de JWT/tenant/turno sin secretos | Integración física del modelo, versión y configuración ensayados | Compatibilidad universal ni exactitud sin patrón certificado |

Los casos son criterios planificados, no resultados aprobados por su inclusión en esta tabla. La ausencia de turno abierto debe impedir crear la venta (RN-06); en todos los niveles se mantienen JWT (RN-07) y aislamiento por tenant (RN-01). Si se incluye cobro con tarjeta, debe pasar por pasarela/simulador (RN-02).

## Evidencias disponibles y pendientes

El [nodo hardware](../../context/integraciones/hardware.md) registra pruebas previas de contrato, simulador, IPC y serial con dobles (S2-066 a S2-069); son antecedentes de software, no ejecuciones nuevas de S2-081. La [fábrica](../../../globalmart-frontend/src/hardware/scale-adapter-factory.ts) selecciona simulador por defecto y exige proveedor serial explícito. El protocolo de fabricante y compatibilidad física permanecen [NO VERIFICADO].

Suites reutilizables: [simulador](../../../globalmart-frontend/tests/simulated-scale-adapter.test.cjs), [serial con dobles](../../../globalmart-frontend/tests/serial-scale-adapter.test.cjs) e [IPC](../../../globalmart-frontend/tests/scale-ipc.test.cjs). Para una nueva ejecución dirigida desde la raíz:

```text
node --test globalmart-frontend/tests/simulated-scale-adapter.test.cjs globalmart-frontend/tests/serial-scale-adapter.test.cjs globalmart-frontend/tests/scale-ipc.test.cjs
```

El resultado de ese comando solo cubre esas suites; no acredita por sí mismo el recorrido completo del POS. Esta entrega documental no ejecuta una balanza ni certifica nuevamente las funcionalidades previas.

## Condición de desbloqueo físico

Antes de reservar la sesión, el responsable de hardware debe completar y adjuntar:

1. **Dispositivo prestado disponible:** marca, modelo, serie, firmware, responsable del préstamo y ventana de acceso. Datos actuales: [NO VERIFICADO].
2. **Protocolo confirmado:** manual y versión, interfaz/cable/driver, parámetros serie, comandos, tramas, unidad y señal de estabilidad del modelo. Datos actuales: [NO VERIFICADO]. No deducirlos desde mocks.
3. **Implementación real:** transporte y parser del modelo habilitados en Electron main, configuración reproducible y lectura real demostrable. La estructura serial existente no satisface este requisito por sí sola.
4. **Preparación de ensayo:** datos de placa Max/Min/e/d, nivelación y calentamiento según manual; masas patrón y fuente de tolerancias. Sin patrón certificado, exactitud queda [NO VERIFICADO — sin patrón]. Resolver incompatibilidades con mínimo/precisión de S2-061 antes del cierre.
5. **Entorno y responsables:** ejecutor y revisor designados (actualmente por asignar), commit, equipo/OS, servicios y datos de prueba por tenant, usuario con JWT válido y turno abierto para el caso positivo.

Si falta cualquier requisito necesario, registrar **BLOQUEADA** la prueba afectada y conservar la integración física **CONDICIONADA**. Usar el [protocolo S2-085](../S2-085/protocolo-balanza-fisica.md), P1–P8 y su registro de evidencias, sin duplicar instrucciones de captura. Su apartado de estado del repositorio corresponde al 2026-10-24: para software posterior consultar el nodo hardware; la existencia actual de simulador/IPC no cambia sus pendientes físicos.

## Ejecución y cierre

1. Clasificar cada caso como SIMULADA o FÍSICA antes de ejecutarlo; registrar componentes reales y sustituidos.
2. Ejecutar los casos de software y conservar salida por caso; aprobar únicamente los realmente ejecutados y satisfactorios.
3. Revisar la condición de desbloqueo; sin equipo/modelo/protocolo no ejecutar ni aprobar el bloque físico.
4. Con requisitos completos, ejecutar S2-085 y vincular capturas físicas con la prueba de aplicación; registrar fallos y repetir solo los casos afectados tras corregirlos.
5. El revisor cierra el bloque físico únicamente con P1–P6 aprobados, P7 con adaptador real y P8-1 a P8-3 aprobados, evidencias completas y sin hallazgos bloqueantes. Un caso omitido, bloqueado o no ejecutado nunca cuenta como aprobado.

Registro obligatorio por caso: ID; nivel; fecha; ejecutor/revisor; commit; modelo o simulador; protocolo/configuración; componentes sustituidos; esperado; observado; resultado (APROBADO/FALLIDO/BLOQUEADO/NO EJECUTADO); enlace a evidencia; hallazgos.

Redacción de cierre permitida: «Validación simulada aprobada para los casos y versión registrados. Integración física CONDICIONADA / [NO VERIFICADO], pendiente de equipo prestado, modelo, protocolo e implementación real». Solo reemplazar la segunda frase con resultados físicos identificados y respaldados. **No declarar “integración física completada” a partir de mocks.**

