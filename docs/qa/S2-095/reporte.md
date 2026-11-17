# S2-095 / TI3-726 — Probar anulación y permisos

Fecha: 2026-11-15. Meta: G2 — Carrito y venta funcional.

## Objetivo y criterio de aceptación

- Objetivo: cubrir efectivo, tarjeta aprobada, rechazo de reversa, repetición y tenant ajeno.
- Aceptación: **Las ventas y eventos conservan estado coherente en cada escenario.**
- El [plan Sprint 3](../../../tareas/plan_sprint3.xlsx) conserva S2-095 como arrastre, TI3-726; el criterio completo procede de [plan Sprint 2 revisado](../../../tareas/plan_sprint2_revisado.xlsx), según el nodo de planificación. No se modificó el tablero ni el Excel.

## Qué se probó

| Escenario | Respuesta / estado esperado | Persistencia y eventos |
|---|---|---|
| Efectivo, ADMIN propio y turno abierto | 200; ANULADA | Una auditoría y un sale.reversed, conservando pago, ítems, montos y sale.completed |
| Tarjeta y reversa aprobada | 200; ANULADA, reversa APROBADA | Un sale.reversed ligado a auditoría y venta; autorización y pago originales conservados |
| Reversa rechazada | 409; COMPLETADA, reversa RECHAZADA | Cero auditorías de anulación y cero sale.reversed; caso consultable sin confirmar reembolso |
| Repetición de efectivo | 409 | Snapshot persistido sin cambios |
| Repetición de tarjeta aprobada/rechazada, con otro motivo | 200/409 según resultado original | Snapshot idéntico, incluidos EventId, payload, referencias, fechas, motivo y autorizador; un solo POST al simulador |
| JWT ausente/vencido/sin identidad | 401 | Sin cambios en la BD ni llamadas de reversa |
| CAJERO, REPONEDOR, SUPER_ADMIN sin ADMIN | 403 | Sin cambios en la BD ni llamadas de reversa |
| ADMIN de otro tenant antes de reservar | 404 | Sin cambios en venta ni eventos |
| ADMIN ajeno frente a reversa APROBADA/RECHAZADA/PENDIENTE | 404 en POST y GET | Ambos tenants conservan snapshot; no consulta ni reversa externa, aunque se envíe X-Tenant-ID falsificado |
| Consulta ajena frente a ID inexistente | Mismo 404 y body | Sin revelar existencia de la reversa |
| Timeout/desconexión/respuesta inconsistente | Pendiente; venta COMPLETADA | Sin sale.reversed hasta recuperar una aprobación válida |
| Requests concurrentes | Una anulación lógica | Una auditoría, un sale.reversed y una solicitud externa en tarjeta |
| Fallo de escritura antes de commit | Rollback | Sin anulación/evento parcial; tarjeta conserva reserva recuperable |
| Reintento de publicación outbox | Recuperación tras fallo simulado | Mismos EventId, venta original y JSON |

## Cambios y simplificación

Se ampliaron las suites existentes [AnulacionVentaIntegrationTests](../../../tests/GlobalMart.IntegrationTests/AnulacionVentaIntegrationTests.cs) y [ReversaTarjetaIntegrationTests](../../../tests/GlobalMart.IntegrationTests/ReversaTarjetaIntegrationTests.cs), identificables con `Plan=S2-095`. No se duplicó una suite completa ni se cambió código de producción.

[AnulacionTestSupport](../../../tests/GlobalMart.IntegrationTests/AnulacionTestSupport.cs) reúne los auxiliares duplicados de JWT y POST y comparte la lectura de snapshots. Usa un contexto nuevo y el tenant correspondiente; compara ventas, ítems, pagos, intentos, auditorías, reversas y outbox. Se agregaron seis combinaciones de seguridad y se reforzaron las aserciones de repetición y rechazo.

Flujo HTTP probado, sin cambios de saltos: Controller → VentaService → VentaRepository (efectivo) / ReversaTarjetaService (tarjeta) → DbContext. No se añadió UI ni capas productivas.

## Cómo reproducir

PostgreSQL de pruebas disponible y Node instalado. Ejemplo con la base temporal usada en esta ejecución:

```bash
GLOBALMART_TEST_POSTGRES='Host=127.0.0.1;Port=55495;Database=s2095;Username=postgres;Password=s2095' ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=55495;Database=s2095;Username=postgres;Password=s2095' dotnet test tests/GlobalMart.IntegrationTests --configuration Release --no-restore --filter 'Plan=S2-095|FullyQualifiedName~SaleReversedOutboxIntegrationTests' --logger 'trx;LogFileName=resultados.trx' --results-directory docs/qa/S2-095
```

Si faltan paquetes, ejecutar restore antes de usar `--no-restore`. Ambas conexiones apuntan a una base exclusiva de pruebas. El contenedor temporal se retira al terminar; no se usan datos del proyecto ni del cluster.

## Resultado

- Referencia antes del cambio: 56/56 aprobadas, exit code 0. No se suman a la ejecución final.
- **Ejecución final: 62/62 aprobadas, cero fallos y cero omisiones, exit code 0.**

| Suite | Resultado |
|---|---|
| AnulacionVentaIntegrationTests | 38/38 aprobadas |
| ReversaTarjetaIntegrationTests | 23/23 aprobadas |
| SaleReversedOutboxIntegrationTests | 1/1 aprobadas |

Artefacto: [resultados.trx](resultados.trx), generado por dotnet test en esta carpeta. La ejecución incluye compilación Release; hubo advertencias previas de comentarios XML y analizadores de otras pruebas. Sin cambios de producción ni migraciones. `git diff --check` y enlaces locales comprobados.

## Límites

- Reales: HTTP de MS-5 en TestServer, JWT firmado validado por middleware, PostgreSQL 16 migrado, adaptador HTTP de reversa y proceso Node del simulador.
- Fixtures: ventas completadas y outbox original sembrados en BD. La autorización inicial de tarjeta sí se obtiene del simulador; no se recorre catálogo/IVA ni pantalla de cobro.
- El worker de publicación usa un productor Kafka controlado que falla una vez. Entrega a broker real, efectos MS-4, Electron y cluster: **[NO VERIFICADO]**. No se tocaron pods ni despliegues.
- No implica reembolso bancario real, entrega física de efectivo ni exactamente una vez de extremo a extremo.

## Guion para la reunión

“Completé las pruebas de anulación y permisos. Verifiqué efectivo, tarjeta aprobada, reversa rechazada, repetición y acceso desde otro tenant. Las pruebas comprueban que solo una anulación válida genere su evento, que los reintentos no dupliquen datos y que los accesos no autorizados no modifiquen nada.”
