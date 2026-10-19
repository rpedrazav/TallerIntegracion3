# S2-055 / TI3-686 — Automatizar flujo E2E tarjeta

Validación local: **2026-10-19, Windows, 2/2 aprobadas, 0 fallidas y 0 omitidas**. Comando: `npm run test:e2e:card` desde `globalmart-frontend`, con PostgreSQL desechable configurado. [Resultado xUnit](S2-055.trx).

| Escenario del simulador | HTTP cobro | Venta | Intento | Pago | Evento durable |
|---|---|---|---|---|---|
| `sim-approved-v1` | 200 | COMPLETADA | APROBADO | 1 TARJETA, $119, referencia coincidente | 1 `sale.completed`, $119 |
| `sim-declined-v1` | 422 | PENDIENTE | RECHAZADO | 0 | 0 |

Ambos casos abren turno desde Electron con fondo de $20.000, agregan Leche S2-055 ($100 + $19 IVA), seleccionan tarjeta y crean la venta antes de confirmar el cobro. Se exige una sola autorización externa por caso. Se comparan tenant, venta, monto, moneda y clave de la solicitud externa; respuesta de pasarela, estado del intento y referencia; pago y payload del evento (venta, tenant, cajero, sucursal, producto, subtotal, IVA y total). Al cerrar el resultado, aprobación deja carrito vacío y rechazo permite continuar con la venta pendiente.

## Evidencias de UI y persistencia

- Aprobación: [apertura](aprobado/01-apertura.png), [venta](aprobado/02-venta.png), [resultado](aprobado/03-resultado.png), [verificación](aprobado/verification.json), [respuesta UI](aprobado/ui-result.json).
- Rechazo: [apertura](rechazado/01-apertura.png), [venta](rechazado/02-venta.png), [resultado](rechazado/03-resultado.png), [verificación](rechazado/verification.json), [respuesta UI](rechazado/ui-result.json).

## Alcance y reproducción

Se ejecutan main, preload y renderer productivos, MS-5 con JWT validado, PostgreSQL migrado y el simulador Node real. Identidad, catálogo e IVA son fixtures controladas. El escenario se fija únicamente en un handler de pruebas antes de enviar la autorización al simulador; no se interceptan respuestas de cobro en Playwright. Se comprueba el evento persistido en outbox, **no su entrega a Kafka** (worker deshabilitado). No certifica el despliegue de todos los microservicios, Kong, hardware ni una pasarela bancaria.

Código: [fixture backend](../../../tests/GlobalMart.IntegrationTests/ElectronCardE2ETests.cs), [flujo UI](../../../globalmart-frontend/e2e/electron/card-backend.cjs), [comando automatizado](../../../globalmart-frontend/e2e/electron/run-card.cjs). [Instrucciones de reproducción y dependencias](../../../globalmart-frontend/e2e/electron/README.md#s2-055--ti3-686--tarjeta-con-backend-y-simulador-reales).

Cada caso usa tenant, esquema y perfil propios. El esquema se elimina al terminar; no se modifica información de desarrollo. Capturas y verificaciones se conservan aquí; trazas y perfiles quedan en `globalmart-frontend/test-results/S2-055/` ignorados por Git. La suite backend ordinaria omite estos casos salvo habilitación explícita; el comando dedicado la habilita y falla si falta una dependencia. Ejecución gráfica en CI: [NO VERIFICADA].

No se modificó lógica productiva. La compilación mantiene advertencias existentes `MSB3539` de los proyectos de servicios.
