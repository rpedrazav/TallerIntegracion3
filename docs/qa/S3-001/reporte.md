# S3-001 — Inventario de código muerto en el flujo de caja MS-5

Fecha: 2026-11-16. Meta: M2 — Funcionalidad de punta a punta.

## Objetivo y aceptación

Recorrer `VentasController`, `VentaService` y `TurnosController`; identificar métodos, rutas y ramas sin uso y justificar cada candidato con evidencia de llamadas, rutas o alcanzabilidad. Fuente: `tareas/plan_sprint3.xlsx`, S3-001.

**Alcance de esta entrega:** revisión estática amplia, sin eliminar código ni cambiar comportamiento. No se editaron fuentes productivas. “Sin llamadas” significa sin llamadores de producción dentro del repositorio; reflexión o consumidores externos quedan [NO VERIFICADO].

## Candidatos comprobados

| # | Candidato | Evidencia y alcance |
|---|---|---|
| 1 | Wrapper `VentaService.RegistrarIntentoPagoAsync` y alias de repositorio | [VentaService.cs](../../../src/POSCartService/Services/VentaService.cs#L358), firma en `IVentaService.cs:143`, implementación/firma en `VentaRepository.cs:347` y `IVentaRepository.cs:71`. No hay llamada de producción: el método solo reenvía a `VentaRepository.RegistrarIntentoPagoAsync`, que solo reenvía a `ActualizarIntentoPagoAsync`. El controller usa directamente `ActualizarIntentoPagoAsync` ([VentasController.cs:1058,1159](../../../src/POSCartService/Controllers/VentasController.cs#L1058)). La limpieza completa debe retirar la cadena redundante y las declaraciones/fake asociadas; conservar `ActualizarIntentoPagoAsync`. |
| 2 | Wrapper `VentaService.ReservarIntentoPagoAsync` | [VentaService.cs:354](../../../src/POSCartService/Services/VentaService.cs#L354), declarado también en `IVentaService.cs:137`. No hay llamadas al wrapper del servicio. **No retirar la reserva del repositorio:** `VentaRepository.IniciarIntentoPagoAsync` la invoca internamente (`VentaRepository.cs:181`) y es parte activa de la idempotencia. |
| 3 | Parámetro `kafkaProducer` del constructor de `VentaService` | [VentaService.cs:19,27](../../../src/POSCartService/Services/VentaService.cs#L19). Se recibe y descarta (`_ = kafkaProducer`); el comentario indica que la publicación directa fue reemplazada por outbox. Hay pruebas/manuales que aún lo pasan (`OutboxBrokerRecoveryIntegrationTests:37`, `VentaOutboxTransactionTests:25`, `POSCartService.CobroCuadreTest/Program.cs:27`, `POSCartService.AgregarItemTest/Program.cs:693`). Se puede retirar junto con esas llamadas, revisando las pruebas que todavía esperan publicación directa. No retirar `IKafkaProducerService`: lo usan otros componentes. |
| 4 | Alias de ruta `[Route("turnos")]` | [TurnosController.cs:12](../../../src/POSCartService/Controllers/TurnosController.cs#L12) duplica las mismas acciones bajo `/turnos/...` y `/api/turnos/...`. El frontend usa `/api/turnos/...` en todos sus modos (`config/api.ts:35–58`, `client.ts:157–174`); Kong y el Ingress remoto enrutan el prefijo `/api/turnos` (`Docker/config/kong.yaml:154–165`, `new-ingresses.yaml:68–73`). No se encontró cliente de producto usando `/turnos/...`. `RoutePrefixIntegrationTests` sí comprueba ambos prefijos, así que retirar el alias requiere ajustar esa expectativa. Es candidato de simplificación sujeto a conservar el contrato si existen consumidores directos externos [NO VERIFICADO]. |
| 5 | `IVentaRepository.GetByCajeroAsync` y `VentaRepository.GetByCajeroAsync` (adyacente al alcance) | [VentaRepository.cs:95](../../../src/POSCartService/Repositories/VentaRepository.cs#L95), firma `IVentaRepository.cs:26`. La búsqueda global encontró solo definición e implementación de producción; no lo llama controller, service ni otro código productivo. El fake de `PaymentEventSequenceContractTests` solo implementa la interfaz. No pertenece a los tres archivos solicitados, pero sí es una operación de consulta de ventas sin consumidor dentro del repositorio revisado. Confirmar que no sea punto de extensión externa antes de retirarla. |

Los puntos 1 y 2 son métodos distintos del servicio; el punto 1 abarca además el alias redundante en el repositorio. Los puntos 4 y 5 amplían el inventario original: el 4 está en un controller solicitado y el 5 se encontró al seguir sus dependencias.

## Métodos y rutas que sí están en uso

- **VentaService:** `GetByIdAsync`, `CambiarMetodoPagoAsync`, `GetByTurnoAsync`, `CrearAsync`, `CompletarAsync`, `AnularAsync`, agregar/modificar/eliminar ítem, completar tarjeta e iniciar/consultar/actualizar intento tienen callers desde `VentasController`. La búsqueda también revisó interfaces, pruebas y llamadas de producción. Solo los dos wrappers de intento enumerados arriba quedan sin llamadas al servicio.
- **VentasController:** las acciones de creación, edición y consulta del carrito, cobro en efectivo/tarjeta, selección del método de pago, anulación, consulta del intento y reconciliación tienen rutas y/o llamadas del frontend y pruebas de integración. `Anular` y `ConsultarReversa` aún no tienen llamada desde `client.ts`, pero sí contrato y pruebas de integración (S2-092/093/095); su falta de botón no las hace código muerto.
- **Prefijos de ventas:** conservar `[Route("ventas")]` y `[Route("api/ventas")]`. En modo remoto/local, la API del frontend usa `/ventas` sobre una base sin `/api`; en modo gateway la base ya termina en `/api`. El router remoto expone raíz `/`, Kong expone `/api/ventas`, y las pruebas cubren ambas formas.
- **TurnosController:** `Abrir`, `Activo`, `Cerrar` y `Cuadre` son invocados por `posApi` y/o pruebas. Solo el prefijo alternativo sin `/api` aparece como candidato (punto 4); las acciones mismas están activas.

## Ramas y duplicaciones

- Se inspeccionaron todas las condiciones de ambos controllers. No se encontró una rama que pueda declararse **inalcanzable** con evidencia estática. Las validaciones de identidad/tenant, estado, pago, peso, catálogo, errores de servicios externos y resultados de pasarela representan entradas o fallos posibles; no se deben borrar como código muerto.
- `TurnosController` repite cuatro veces la lectura y parseo de claims. Es duplicación activa, no código muerto: una futura simplificación podría extraer un helper manteniendo la respuesta HTTP y los claims admitidos.
- `VentaService.CompletarAsync` y `CompletarTarjetaAsync` construyen el mismo formato de evento/outbox (`sale.completed`), y ambos están activos. Es candidato de refactor para compartir construcción, no una función prescindible; las transacciones de persistencia deben seguir diferenciadas.
- No se marcó como muerto que `ConsultarReversa` todavía no aparezca en la UI: las pruebas de integración la llaman y cubre la consulta/reconciliación del resultado persistido.

## Evidencia y reproducción

Inventario basado en búsquedas globales de símbolos y rutas, lectura de los tres archivos completos y seguimiento de interfaces, implementación del repositorio, configuración API, Kong, Ingress y pruebas. Búsquedas reproducibles:

```bash
rg -n 'RegistrarIntentoPagoAsync|ReservarIntentoPagoAsync|kafkaProducer:' src tests globalmart-frontend scripts
rg -n 'GetByCajeroAsync\(' --glob '*.cs' src tests
rg -n 'turnos/(abrir|activo|cerrar|cuadre)|/api/turnos' globalmart-frontend/src globalmart-frontend/e2e tests Docker new-ingresses.yaml
rg -n 'ventas|turnos|anular|reversa-tarjeta|intento-tarjeta' globalmart-frontend/src/api/client.ts globalmart-frontend/src/config/api.ts tests Docker/config/kong.yaml new-ingresses.yaml
rg -n '\[Http|\[Route|\bif\s*\(' src/POSCartService/Controllers/VentasController.cs src/POSCartService/Controllers/TurnosController.cs
```

No se ejecutaron pruebas ni build: esta entrega es inventario documental y no cambia código. No se inspeccionaron consumidores externos al repositorio ni se verificó el despliegue del clúster.

## Guion breve

“Al revisar de nuevo encontré cinco candidatos: dos métodos puente de intentos de pago que no se llaman, el parámetro Kafka que `VentaService` descarta, el prefijo alternativo `/turnos` y una consulta del repositorio sin consumidor. Separé los dos últimos porque uno requiere ajustar una prueba de rutas y el otro está fuera de los tres archivos principales. También confirmé que no hay ramas de negocio demostrablemente inalcanzables y que no conviene borrar rutas de ventas ni la reserva idempotente.”
