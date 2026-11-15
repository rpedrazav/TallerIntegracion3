# S2-113 / TI3-744 — E2E de tarjeta y pesaje (entorno integrado)

Validación local: **2026-11-15, Windows, 5/5 aprobadas, 0 fallidas, 0 omitidas, exit code 0**. Comando: `npm run test:e2e:integrado` desde `globalmart-frontend`, que en la misma corrida ejecuta S2-112 (2/2) y S2-113 (5/5): **7/7**. [Resultado Playwright JSON](resultados.json).

**Aceptación:** una falla no completa la venta ni provoca doble cargo. ✅ Se verifica en la BD de MS-5 (`Pagos`, `IntentosPago`, `Ventas`) después de cada caso.

## Qué se ejecuta

Electron real contra MS-1..MS-5 en localhost, PostgreSQL de `docker compose` y el **simulador de pasarela real** (`payment-simulator`, `:5050`). Sin mocks de respuestas. Hay dos intervenciones de prueba, ambas declaradas en la spec:

1. **Escenario de pasarela.** La UI no permite elegirlo, así que Playwright agrega `scenario_id` a la solicitud real `POST /ventas/{id}/cobrar-tarjeta`. Las respuestas de MS-5 y del simulador no se tocan.
2. **Falla de balanza.** Para habilitar la entrada manual, el handler IPC `scale:read-stable` del proceso main devuelve `device_error`. El peso simulado usa el adaptador real `SimulatedScaleAdapter` (0,750 kg estable).

El terminal simulado del main se configura con `MOCK_TERMINAL_SCENARIO=approved`: representa la lectura de la tarjeta, y el resultado del cobro lo decide la pasarela vía MS-5.

| # | Caso | Verificación | Captura |
|---|---|---|---|
| 1 | Tarjeta aprobada | 200, «Cobro aprobado» con referencia opaca; venta COMPLETADA, **1 pago**, intentos `APROBADO` | [01](capturas/01-tarjeta-aprobada.png) |
| 2 | Tarjeta rechazada | 422, «Cobro rechazado»; venta **PENDIENTE**, **0 pagos**, intentos `RECHAZADO` | [02](capturas/02-tarjeta-rechazada.png) |
| 2b | Reintento tras rechazo (clave nueva) | 200; venta COMPLETADA, **1 pago**, intentos `RECHAZADO,APROBADO` | [03](capturas/03-reintento-aprobado.png) |
| 3 | Timeout de pasarela | UI en «Resultado incierto» con «Verificar venta»; verificar no completa la venta; un segundo cobro con otra clave → **409** sin llamar a la pasarela; tras vencer MS-5 (30 s) y el simulador (35 s): venta **PENDIENTE**, **0 pagos**, **1 intento** (PENDIENTE) | [04](capturas/04-timeout-incierto.png) |
| 4 | Peso estable (balanza simulada) | 0,750 kg → una línea en MS-5: `peso_kg 0.75`, $8.990 × 0,75 = 6.742,5 → **$6.743**; IVA **$1.281**; total **$8.024** | [05](capturas/05-peso-simulado.png) |
| 5 | Falla de balanza → entrada manual | Peso `0` no se puede confirmar; 1,234 kg → una línea: subtotal **$11.094**, IVA **$2.108**, total **$13.202** | [06](capturas/06-peso-manual.png) |

Cada prueba parte de un turno nuevo del cajero demo, porque el POS restaura la última venta PENDIENTE del turno. Los productos (`Tarjeta S2-113 …`, `Queso S2-113 …`) se crean por API con el ADMIN demo.

Código: [spec](../../../globalmart-frontend/e2e/electron/tarjeta-pesaje.integrated.spec.ts) y [fixture compartido con S2-112](../../../globalmart-frontend/e2e/electron/integrated-fixture.ts). Reproducción: [README Electron](../../../globalmart-frontend/e2e/electron/README.md#s2-113--ti3-744--tarjeta-y-pesaje-integrados).

## Fallos encontrados

| # | Defecto | Evidencia | Estado |
|---|---|---|---|
| F1 | **El simulador rechazaba al tenant demo.** Exigía UUID RFC versión 1–5 y el tenant sembrado `aaaaaaaa-0000-0000-0000-000000000001` tiene versión 0, así que ningún cobro con tarjeta del tenant demo podía aprobarse (MS-5 respondía 502). S2-055 no lo detectó porque usaba UUID aleatorios. | Log del simulador: `"status":"VALIDATION_ERROR"`; MS-5: `La pasarela devolvió HTTP 400`. | **Corregido**: `server.js` y `reversals.js` aceptan cualquier GUID hexadecimal, como MS-5. `node --test reversals.test.js`: 4/4. |
| F2 | **En local, MS-5 no apuntaba al simulador.** `appsettings.json` trae `PasarelaPago:BaseUrl = https://api.sandbox-pasarela.example.com`, y nadie lo sobreescribía al usar `run-apis`. | El cobro con tarjeta salía hacia un dominio de ejemplo. | **Corregido** solo en los scripts locales: `run-apis.sh`/`.bat` definen `PasarelaPago__BaseUrl=http://localhost:5050` para MS-5. No toca el clúster. |

Observaciones sin impacto en la aceptación:

- **Timeouts desalineados.** El renderer corta a los 15 s (axios), MS-5 espera 30 s a la pasarela y el simulador responde 504 a los 35 s. La UI pasa a «sin confirmar» antes de que MS-5 resuelva. Es seguro (reconcilia por la misma clave y no permite otro cobro), pero el intento queda **PENDIENTE sin resolución automática**: hay que verificar o cancelar el intento a mano.
- **«Reintentar tarjeta»** solo vuelve a habilitar el cobro con una clave nueva; el cajero debe confirmar otra vez. Está bien, pero son dos clics.
- Bajo carga, el relay IPv6 de Docker Desktop (`wslrelay`) dejó colgadas conexiones a PostgreSQL por `localhost`. Para esta corrida, MS-5 se lanzó con `Host=127.0.0.1` por variable de entorno. Es un tema del entorno local, no del código.

## Alcance y límites

- Acredita la pasarela **simulada** (RN-02: sin PAN/CVV en el cliente) y la balanza **simulada**. Terminal y balanza físicos siguen **CONDICIONADOS** (R5 de S2-111).
- No acredita Kong ni la entrega de `sale.completed` a Kafka.
- Escribe en la BD local de desarrollo: productos, turnos y ventas del cajero demo en cada corrida.

## Verificaciones de regresión

- `npm run typecheck`: exit 0.
- `npm run test:e2e:electron` (mock): 2/2.
- S2-112 en la misma corrida: 2/2. Ahora usa el fixture compartido.
- Simulador: `node --test Docker/simulator/reversals.test.js` 4/4.
