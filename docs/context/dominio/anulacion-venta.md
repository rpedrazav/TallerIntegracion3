---
id: anulacion-venta
tipo: dominio
titulo: Contrato de anulación total — S2-091
estado: implementado-backend
ultima_revision: 2026-11-09
fuentes: [src/POSCartService/Services/VentaService.cs, src/POSCartService/Models/Anulacion.cs, src/POSCartService/Services/IPaymentGatewayClient.cs, Docker/simulator/server.js]
---

# Contrato de anulación total — S2-091 / TI3-722

**S2-091 definió el contrato; S2-092 implementa el endpoint y sus guardias en backend.** Ver [reporte S2-092](../../qa/S2-092/reporte.md) para evidencia y límites. No incluye UI de anulación ni despliegue remoto.

## Objetivo y alcance

Permitir exclusivamente la anulación total autorizada de una venta completada del tenant autenticado. La devolución parcial (selección de ítems, cantidades o montos), el pago mixto y la devolución automática de inventario quedan fuera de este contrato. Cancelar un carrito PENDIENTE es otra operación; no se confunde con anular una venta cobrada.

El alcance habilitable inicialmente es la venta COMPLETADA en EFECTIVO, con pago efectivo registrado y turno original ABIERTO. Limitar al turno original abierto evita introducir ajustes sobre cajas ya cerradas. La validación se aplica en S2-092 dentro de la transacción de anulación. No se exige que el ADMIN sea el cajero original ni que tenga otro turno propio; sí debe pertenecer al mismo tenant. Anulaciones de turnos cerrados requieren un flujo posterior de ajustes de caja.

Una venta con tarjeta aprobada **no se anula localmente ni se declara reembolsada** mientras el simulador/adaptador no disponga de una operación de devolución con respuesta definitiva verificable. No se inventa esa capacidad en S2-091.

## Implementación y límites

- `VentasController.Anular` exige JWT y ADMIN; valida motivo y rechaza campos adicionales, incluso selectores parciales e identidad enviada por cliente.
- `VentaService.AnularAsync` verifica de nuevo identidad ADMIN mediante el contexto autenticado, normaliza motivo y delega la transición transaccional.
- `VentaRepository.AnularTotalAsync` filtra explícitamente por tenant, bloquea turno y venta, verifica estado/pagos/intentos y confirma estado ANULADA junto con auditoría. Conserva pagos, ítems, importes y outbox históricos.
- No exige turno propio al administrador ni que sea el cajero de la venta.
- `IPaymentGatewayClient` y el simulador aún no ofrecen refund/reversal. No se invocan para esta operación.
- El cuadre actual suma `Pago.Monto` de ventas COMPLETADAS sin restar `Vuelto`; S2-092 no corrige ni certifica ese cálculo. Anular excluye la venta de esa consulta existente; no acredita entrega física de dinero.

## Contrato HTTP

`POST /ventas/{id:guid}/anular`, con alias `POST /api/ventas/{id:guid}/anular`.

Encabezados: `Authorization: Bearer <JWT>` y `Content-Type: application/json`.

```json
{
  "motivo": "Venta registrada por error; devolución total autorizada"
}
```

- `motivo`: obligatorio, texto de 1 a 500 caracteres después de `Trim`; espacios solos no son válidos. El límite es una nueva decisión de contrato, no una restricción actual del modelo.
- No se aceptan ítems, cantidades, monto de devolución, tenant, usuario autorizador ni un estado de reembolso indicado por el cliente. Campos adicionales se rechazan con 400; el endpoint inspecciona el objeto JSON expresamente y también rechaza propiedades duplicadas.
- `id` identifica la venta completa. Montos, pagos y turno se obtienen de la persistencia.

### Permisos y aislamiento

JWT válido con identidad de usuario y `tenant_id`. Exigir rol efectivo `ADMIN` del token (patrón `[Authorize(Roles = "ADMIN")]`); no basta enviar `autorizado_por` o afirmar permiso desde la UI. CAJERO y REPONEDOR sin ADMIN reciben 403. SUPER_ADMIN sin claim ADMIN no obtiene bypass implícito. Los permisos granulares `refund.process` siguen fuera de este contrato.

`AutorizadoPor` se obtiene del identificador autenticado (`cajero_id`, NameIdentifier o `sub`, según el helper existente); representa al ADMIN autorizador, no necesariamente al cajero de la venta. Toda lectura/mutación se limita al tenant autenticado. Para un ADMIN, venta ajena e inexistente producen el mismo 404 sin revelar sus datos. La regla debe proteger también la operación de negocio, no únicamente la pantalla.

### Matriz de decisiones (después de autenticar y autorizar)

| Condición | Resultado del contrato | Mutación |
|---|---|---|
| Body o motivo inválido, campos adicionales | 400 `SOLICITUD_INVALIDA` | Ninguna |
| Venta inexistente o ajena | 404 `VENTA_NO_ENCONTRADA` | Ninguna |
| Venta PENDIENTE o CANCELADA | 409 `ESTADO_NO_ANULABLE` | Ninguna |
| Venta ya ANULADA | 409 `VENTA_YA_ANULADA` | No duplicar anulación ni movimientos |
| Intento de pago PENDIENTE o FALLIDO sin resolución definitiva | 409 `PAGO_INCIERTO` | Ninguna; resolver antes mediante consulta existente |
| Pago/método MIXTO | 409 `METODO_NO_SOPORTADO` | Ninguna |
| Método TARJETA, pago TARJETA o intento APROBADO de tarjeta | 409 `REEMBOLSO_NO_DISPONIBLE` | No anular ni afirmar devolución |
| COMPLETADA en efectivo pero sin pago efectivo coherente | 409 `PAGO_INCONSISTENTE` | Ninguna |
| Turno original no ABIERTO | 409 `TURNO_NO_ANULABLE` | Ninguna |
| COMPLETADA, efectivo confirmado, turno original ABIERTO, sin incertidumbre | 200 | Anulación total local y auditoría atómicas |

Para evitar una clasificación basada solo en `MetodoPago`, consultar también pagos e intentos. Un pago efectivo coherente tiene importe neto (`Monto - Vuelto`) igual al total de la venta y no contiene pagos de otro método. Priorizar incertidumbre sobre método cuando ambas condiciones existan. El índice de anulación por venta y el bloqueo transaccional impiden que dos requests simultáneos registren dos anulaciones.

### Ejemplo de respuesta válida

```json
{
  "venta_id": "c8d94f40-2182-4e85-bfbb-cbcb913cd259",
  "estado": "ANULADA",
  "anulacion": {
    "motivo": "Venta registrada por error; devolución total autorizada",
    "autorizado_por": "4d8edb4a-a8fa-48b6-a0a0-69ad17221c77",
    "created_at": "2026-11-09T15:00:00Z"
  },
  "reembolso_tarjeta": "NO_APLICA",
  "devolucion_efectivo": "GESTION_MANUAL"
}
```

UUIDs y fecha ilustrativos. El 200 confirma una anulación administrativa local en efectivo, **no prueba la entrega física de dinero**. No se borra el pago histórico ni se altera el precio, total o detalle de la venta. Se persisten juntos el estado ANULADA y el registro de autorización/motivo/fecha. El efecto contable y la devolución física requieren su procedimiento; no se crea aquí un reembolso automático.

### Ejemplo de tarjeta aprobada

HTTP 409:

```json
{
  "codigo": "REEMBOLSO_NO_DISPONIBLE",
  "error": "No se puede anular el pago con tarjeta sin una devolución confirmada por la pasarela."
}
```

La venta continúa COMPLETADA y el pago/intento aprobado se conserva (el intento es el registro de la autorización original). Ni el 200 de autorización ni la consulta que devuelve APPROVED acreditan un reembolso. Un futuro flujo de devolución deberá conservar identidad y monto, resolver rechazos/timeouts y verificar la respuesta específica del simulador antes de anunciar dinero devuelto. Ruta/DTO/estados de esa devolución: pendientes de definición en una tarea de integración; no se inventan en este documento.

Los errores de validación de negocio usan `codigo` y `error`. Un JSON malformado o body ausente recibe el 400 estándar de ASP.NET (`ValidationProblemDetails`) antes de ejecutar la acción; 401/403 del middleware pueden no tener body. No se cambian los errores de otros endpoints. JWT ausente, vencido o sin identidad/tenant válido: 401. Usuario autenticado sin ADMIN: 403. Fallo inesperado de persistencia: 500, con rollback y mensaje genérico, sin exponer detalles internos. Un UUID inválido en el segmento no coincide con `{id:guid}` y obtiene 404 de enrutamiento.

## Efectos que no promete este contrato

- No publica de nuevo `sale.completed` ni borra el outbox histórico de una venta completada.
- No define un evento de reversión ni acredita reposición automática en MS-4, reversión de puntos o anulación fiscal. Cada efecto requiere contrato/implementación propios.
- No permite devolución parcial ni dividir pagos. Rechazar solicitudes parciales, no ignorarlas y anular toda la venta accidentalmente.
- No añade estados ficticios al enum de venta ni equipara ANULADA con REEMBOLSADA.

## Matriz de aceptación

| Caso | Evidencia esperada |
|---|---|
| ADMIN, efectivo, COMPLETADA, turno abierto | 200, ANULADA y una auditoría; pagos/ítems/importes históricos intactos |
| Sin JWT / CAJERO / REPONEDOR | 401 / 403 / 403 sin mutaciones |
| ADMIN de otro tenant, en ambos sentidos | 404 indistinguible de inexistente; snapshots de ambos tenants intactos |
| Motivo ausente, vacío, espacios, >500; body con ítems/monto | 400 sin mutaciones |
| ANULADA, CANCELADA, PENDIENTE, turno cerrado o pago inconsistente | 409 sin mutaciones |
| Tarjeta aprobada o intento incierto | 409, sin falsa confirmación de reembolso ni nueva autorización |
| Repetición y dos solicitudes simultáneas | Solo una transición y un registro; la segunda recibe 409 |
| Error de escritura | Estado y auditoría no quedan parcialmente persistidos |

S2-091 entregó esta matriz como especificación. S2-092 incorpora `AnulacionVentaIntegrationTests`: HTTP, JWT firmado, PostgreSQL migrado, snapshots ante rechazo, concurrencia y fallo controlado posterior a SaveChanges para verificar rollback. Resultados concretos en [reporte S2-092](../../qa/S2-092/reporte.md).

## Fuentes y conexiones

- [MS-5](../servicios/ms5-pos.md)
- [Pasarela](../integraciones/pasarela-pago.md)
- [Estado actual](../estado-actual.md)
- [Reglas de negocio, RN-14](reglas-negocio.md)
- [RBAC](rbac-multirol.md)
- [Servicio actual](../../../src/POSCartService/Services/VentaService.cs)
- [Modelo de anulación](../../../src/POSCartService/Models/Anulacion.cs)
- [Cliente de pasarela](../../../src/POSCartService/Services/IPaymentGatewayClient.cs)
- [Simulador](../../../Docker/simulator/server.js)
- [Correspondencia S2-091 → TI3-722](../../../tareas/sprint2_youtrack_resultado.json)
