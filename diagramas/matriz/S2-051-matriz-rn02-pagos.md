# S2-051 — Matriz RN-02 de pagos

**Plan:** S2-051 · **YouTrack:** TI3-682 · **MoSCoW:** Must · **Dificultad:** Baja.  
**Meta:** Cobro con tarjeta simulado. **Última revisión:** 2026-10-17.  
**Entrega:** definición de casos de aceptación. **Ejecución de esta matriz: PENDIENTE; aceptación E2E de tarjeta: [NO VERIFICADO].**

## Alcance y regla obligatoria

GlobalMart OS **prohíbe capturar localmente PAN o CVV**, incluso de tarjetas de prueba. No debe ofrecer formularios, lectores, canales IPC ni campos de API para ingresarlos. Tampoco permite almacenarlos, transmitirlos o escribirlos en logs, errores, telemetría, capturas o evidencias. Se excluyen además PIN, track, vencimiento, titular y tokens de tarjetas reales. En esta demostración se elige un `scenario_id` no sensible; no se utiliza una tarjeta ni se produce un cargo bancario real.

Todo pago TARJETA debe pasar por MS-5 y la pasarela/simulador. Solo una autorización confirmada y persistida permite mostrar la venta como cobrada. El frontend y un terminal simulado no son autoridad de aprobación. EFECTIVO es control de regresión; MIXTO se mantiene excluido del sprint y debe rechazarse. Débito y crédito quedan representados por TARJETA: no se ha verificado un campo que los distinga.

Esta matriz desarrolla P01–P04 de [S2-021](S2-021-matriz-aceptacion.md). No incorpora reembolsos ni certifica cumplimiento bancario o PCI DSS.

## Preparación y observables comunes

1. Levantar UI, APIs y PostgreSQL reales del flujo, Kafka y simulador local según [guía de arranque](../../docs/context/proyecto/como-ejecutar.md). Registrar versión, rutas, configuración y fecha. Solo pasarela/terminal externos son simulados; los fallos controlados adicionales se identifican en la evidencia.
2. Preparar tenants A/B, cajeros y ventas propias con las [fixtures S2-022](../../tests/GlobalMart.IntegrationTests/Fixtures/README.md). Crear la venta con JWT válido y turno ABIERTO; obtener total autoritativo de MS-5, sin fijar IVA local. Usar UUID nuevos para cada caso independiente.
3. Antes y después de cada acción, observar venta, `IntentosPago`, pagos, outbox `sale.completed`, stock y UI. Contar solicitudes POST/GET a la pasarela y autorizaciones lógicas por `(tenant_id, request_id)`. Una respuesta repetida o entrega Kafka repetida no constituye otro cargo/evento lógico.
4. La solicitud MS-5 → simulador contiene exclusivamente `request_id`, `tenant_id`, `sale_id`, `amount`, `currency`, `scenario_id`. `Idempotency-Key` coincide con `request_id`. El tenant procede de JWT, monto/moneda de la configuración y venta autoritativas.
5. Aplicar los perfiles de logs de la sección siguiente en **todos** los casos. Las referencias a estados inciertos son semánticas: el enum actual no contiene `INCIERTO`; su representación y los códigos HTTP del futuro cobro TARJETA están [NO VERIFICADO]. No inventar un endpoint de tarjeta ni enviar esos campos al contrato de efectivo.

## Datos permitidos en logs

Lista de aceptación, no afirmación de que todas las capas ya la implementen. Solo registrar campos estructurados validados; omitir los que no existan. Los alias C# equivalentes son admisibles.

| Perfil | Lista permitida | Restricción |
|---|---|---|
| L0 — Operación | Fecha UTC, nombre de evento, ID de correlación, tenant/venta/intento como UUID, método, estado controlado, código HTTP y duración | Sin cuerpos ni encabezados completos; no copiar texto arbitrario del cliente |
| L1 — Pasarela | L0 + `request_id`, `scenario_id` permitido, `transaction_id` y `gateway_reference` opacos del simulador | Solo referencias validadas; no registrar automáticamente objetos de respuesta |
| L2 — Error | L0 + código de error/rechazo de una lista controlada y resultado de consulta de estado | Sin mensaje remoto libre, cuerpo inválido ni excepción que exponga datos de entrada |

PAN/CVV (completos, parciales o enmascarados), otros datos de tarjeta, JWT, Authorization, claves API, cookies, secretos y volcados de request/response quedan **prohibidos en todos los perfiles**. `authorization_code` puede persistirse como referencia según el contrato, pero no se necesita en logs. Monto/moneda se verifican en la venta y el pago; no se requieren en la lista mínima de logs.

## Matriz de aceptación

Todos los resultados son requisitos de aceptación; ningún caso se declara aprobado por esta definición. «Sin efectos» significa venta no completada, sin pago confirmado, sin nuevo `sale.completed` ni descuento de stock.

| ID | Método / dado y acción | Resultado esperado verificable | Idempotencia / conteo | Logs |
|---|---|---|---|---|
| RN02-01 | EFECTIVO pendiente; cobrar monto suficiente y repetir sobre la venta completada | Primer cobro completa venta con Pago EFECTIVO y vuelto correcto; segundo se rechaza sin otro pago/evento | Cero llamadas a pasarela; un pago y un evento lógico | L0 |
| RN02-02 | EFECTIVO pendiente; cobrar menos que total | HTTP 400; sin efectos | Cero llamadas a pasarela | L0 + L2 |
| RN02-03 | TARJETA o MIXTO; invocar el cobro actual de efectivo | HTTP 400; no convertir el método a EFECTIVO; sin efectos | Cero llamadas a pasarela | L0 + L2 |
| RN02-04 | TARJETA pendiente; seleccionar `sim-approved-v1` y cobrar desde UI | Pasarela devuelve HTTP 200/APPROVED; intento aprobado, venta COMPLETADA, un Pago TARJETA por total con referencia opaca y outbox; UI coincide con MS-5; stock se descuenta una vez al consumir evento | Una autorización lógica; un pago y un evento lógico | L1 |
| RN02-05 | TARJETA pendiente; usar `sim-declined-v1` | HTTP 200/DECLINED de pasarela, `DO_NOT_HONOR`; intento rechazado y UI informa rechazo; sin efectos | Un POST, cero GET automáticos, cero reautorizaciones automáticas | L1 + L2 |
| RN02-06 | TARJETA; usar `sim-timeout-v1`, timeout cliente 30 s y demora simulador 35 s | Consulta sin estado confirmado: incertidumbre visible, venta PENDIENTE; sin efectos. No tratar timeout como rechazo definitivo | Exactamente un POST y un GET con misma clave; prohibido crear nueva clave automáticamente | L1 + L2 |
| RN02-07 | TARJETA; provocar pérdida de respuesta después de guardar aprobación en pasarela | GET recupera APPROVED con mismos identificadores; completar localmente una sola vez; UI coincide con persistencia | Un POST y un GET, ningún segundo POST. Requiere fallo controlado: `sim-timeout-v1` no almacena aprobación | L1 |
| RN02-08 | TARJETA; doble clic concurrente, reenvío idéntico y recarga durante autorización (ejecutar cada variante) | Recuperar mismo intento/resultado y referencias; no duplicar pago ni outbox | Misma clave persistida por intento; una autorización lógica incluso con solicitudes de transporte repetidas | L1 |
| RN02-09 | TARJETA; reutilizar clave con otra venta, monto, moneda o escenario (una variante por campo); probar encabezado distinto de request_id | Simulador devuelve 409; se conserva resultado original; no confirmar el intento conflictivo | Cero nuevas autorizaciones lógicas | L1 + L2 |
| RN02-10 | TARJETA rechazada; repetir misma solicitud y luego iniciar explícitamente otro intento | Misma clave devuelve mismo rechazo; solo acción explícita tras rechazo definitivo permite nuevo intento, conservando historial | Nueva clave solo para nuevo intento autorizado por usuario; una aprobación máxima por venta | L1 + L2 |
| RN02-11 | TARJETA con resultado incierto; recargar y reiniciar simulador, luego consultar estado | 404 no demuestra ausencia de cargo; conservar incertidumbre y bloquear nuevo cobro hasta reconciliar | Nunca reautorizar automáticamente con clave nueva o antigua por pérdida del almacén en memoria | L1 + L2 |
| RN02-12 | TARJETA; JWT ausente/inválido o JWT B intenta cobrar/consultar intento de A | Sin JWT válido: 401; recurso ajeno no visible ni mutable; datos de A intactos | Cero llamadas de autorización por operación no autorizada | L0 + L2 |
| RN02-13 | TARJETA; A/B usan igual request_id para ventas propias; después B intenta consultar resultado de A | Resultados separados por tenant; B no obtiene datos de A | Dos claves lógicas independientes; sin colisión ni reutilización cruzada | L1 |
| RN02-14 | TARJETA; cobrar venta completada, anulada o crear venta sin turno ABIERTO | Rechazar sin nuevo pago/evento; sin turno no crear venta (409 vigente) | Cero autorizaciones nuevas | L0 + L2 |
| RN02-15 | TARJETA; caída de red, HTTP 5xx, respuesta malformada, request_id distinto o estado desconocido | No interpretar error como aprobación ni rechazo bancario definitivo; sin efectos, mensaje seguro y recuperación trazable | Sin repetición automática del POST; reconciliar el intento antes de habilitar otro cobro | L2 |
| RN02-16 | TARJETA aprobada externamente; forzar fallo al persistir pago/outbox y reiniciar MS-5 | No mostrar cobrada sin persistencia; recuperación del mismo intento sin segundo cargo; venta/pago/outbox se confirman atómicamente al recuperarse | Una autorización lógica y un pago/evento final; índice único local por sí solo no demuestra esta garantía | L1 + L2 |
| RN02-17 | TARJETA; revisar UI, preload/IPC, DTOs, tráfico, almacenamiento y logs en éxito/error | Ninguna captura local de PAN/CVV ni campo para ingresarlos; solo escenarios y referencias opacas; datos prohibidos ausentes de todas las capas | Inspección de todos los intentos anteriores | L0/L1/L2 exclusivamente |
| RN02-18 | TARJETA; enviar campos `pan`, `cvv` y variantes anidadas al simulador con marcadores no bancarios | HTTP 400/CARD_DATA_FORBIDDEN antes de autorización; sin reflejar valores en respuesta/logs. GlobalMart no debe ofrecer esa entrada; rechazo defensivo no autoriza captura | Cero autorizaciones; no guardar payload rechazado | L2 |

## Evidencia y estado real

- [VentasController](../../src/POSCartService/Controllers/VentasController.cs): `/ventas/{id}/cobrar` y alias `/api/ventas/{id}/cobrar` solo cobran EFECTIVO. RN02-04–16 requieren orquestación de tarjeta para acreditar el flujo completo; actualmente esa aceptación está **BLOQUEADA**. RN02-17 exige además inspección integral de UI/IPC y logs.
- [PaymentGatewayClientTests](../../tests/GlobalMart.IntegrationTests/PaymentGatewayClientTests.cs): contrato sin campos sensibles, payload permitido, clave, rechazo y timeout; soporte parcial para RN02-05/06/17. [PaymentGatewayClient](../../src/POSCartService/Services/PaymentGatewayClient.cs) consulta una vez tras timeout y no repite el POST.
- [PaymentSimulatorIntegrationTests](../../tests/GlobalMart.IntegrationTests/PaymentSimulatorIntegrationTests.cs): aprobación, rechazo, campos prohibidos, conflicto y repetición, encabezado y consulta de estado; soporte parcial para RN02-04/05/08/09/18. [Simulador](../../Docker/simulator/server.js) conserva aprobaciones/rechazos en memoria, no sobrevive a reinicio y no almacena resultado para `sim-timeout-v1`.
- [PaymentRetrySafetyTests](../../tests/GlobalMart.IntegrationTests/PaymentRetrySafetyTests.cs): timeout desconocido, aprobación recuperable y rechazo sin consulta; soporte parcial para RN02-05/06/07. No recorre UI → cobro → base → Kafka.
- `IntentosPago` tiene unicidad por tenant/clave según [contrato de pasarela](../../docs/context/integraciones/pasarela-pago.md); esto impide filas duplicadas, pero **no prueba por sí solo ausencia de doble autorización externa**. Concurrencia, recuperación tras fallo y aislamiento del flujo deben ejecutarse.
- El adaptador registra excepciones y cadenas provenientes de respuesta; la sanitización integral exigida por L2 está **[NO VERIFICADO]**. DTOs sin PAN/CVV y rechazo por nombre de campo no prueban ausencia de filtración en campos libres o excepciones.

## Procedimiento de ejecución y cierre

Para cada fila registrar: ID y variante, fecha, ejecutor, commit, configuración, datos iniciales no sensibles, pasos ejecutados, esperado/observado, conteos POST/GET/autorizaciones, snapshots de venta/intento/pago/outbox/stock, logs filtrados y resultado APROBADO/FALLIDO/BLOQUEADO con enlaces. Sanitizar evidencia antes de guardarla; nunca utilizar PAN/CVV reales ni números de tarjetas de prueba para estos casos.

Para RN02-04 y RN02-05 recorrer desde UI real hasta persistencia y eventos; para fallos usar instrumentación identificada. No aprobar E2E usando respuestas simuladas de MS-5 ni solo tests del adaptador. Validar perfiles de logs también en rutas de excepción. La configuración de espera asíncrona y cualquier código HTTP futuro de tarjeta deben documentarse al implementar el contrato, hoy [NO VERIFICADO].

**Cierre de S2-051:** matriz definida, métodos/resultados/idempotencia/logs trazados y prohibición expresa de captura local de PAN/CVV. **Cierre de la meta de tarjeta:** ejecutar y aprobar los casos aplicables, resolver bloqueos Must y adjuntar evidencia; la entrega documental no cierra la brecha E2E. MIXTO solo exige el rechazo RN02-03, no su implementación.


# 
