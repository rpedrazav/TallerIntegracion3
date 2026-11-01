# S2-052 — Rechazo y timeout por HTTP

Fecha: 2026-10-19. Cobro con tarjeta simulado, sin datos bancarios reales.

**Resultado local: 2/2 aprobadas, 0 fallidas, 0 omitidas**, Release, 39 segundos. Evidencia: [S2-052.trx](S2-052.trx). La compilación mostró advertencias preexistentes de MSBuild/documentación XML; no hubo errores. No se ejecutó la solución completa.

## Nota para el siguiente agente — bloqueo pendiente en el clúster

**Las pruebas locales aprobadas no significan que el despliegue esté libre de errores.** El 2026-10-19, `POST https://pos-rpedraza.dev.censei.cl/ventas` respondió **502** al intentar conectar con MS-3 en `localhost:5203` (`Connection refused`). Esto impidió crear la venta y llegar a `cobrar-tarjeta`. El problema no fue corregido en esta tarea; la evidencia está en [resultado.json](resultado.json).

Los **422 y 504 locales son respuestas esperadas** de los escenarios de rechazo y timeout; el **502 remoto es un bloqueo real previo al cobro**. La configuración efectiva y la causa de que el despliegue use `localhost:5203` están [NO VERIFICADO], porque no se dispone de las credenciales del clúster universitario (las tiene el líder del proyecto).

Para continuar, revisar con acceso autorizado el destino efectivo de MS-3 configurado en MS-5 y su conectividad interna; corregir lo que corresponda y repetir primero la creación de venta y luego los escenarios de tarjeta. Hasta entonces, mantener la aceptación remota como **[NO VERIFICADO]**. No dar por resuelto el bloqueo por el resultado local ni por health checks 200. Las URLs del proyecto siguen apuntando al clúster y la base temporal local ya fue eliminada.

## Alcance y reproducción

Se agregaron dos casos con `Plan=S2-052` a `CobroSeguroIntegrationTests`:

| Escenario | HTTP esperado | Venta | Intento | Pago | Evento outbox |
|---|---|---|---|---|---|
| `sim-declined-v1` | 422, `DECLINED`, `DO_NOT_HONOR` | PENDIENTE | RECHAZADO | 0 | 0 |
| `sim-timeout-v1` | 504 con venta e intento correlacionados | PENDIENTE | PENDIENTE / consulta `UNCERTAIN` | 0 | 0 |

La suite recorre el pipeline HTTP real de MS-5 mediante WebApplicationFactory, JWT validado, PostgreSQL 16 migrado, adaptador HTTP y proceso Node real del simulador. El turno abierto y la venta se preparan en un esquema exclusivo por caso. No sustituye respuestas de la pasarela; el handler cuenta llamadas reales. No es una prueba del frontend ni del flujo de creación vía MS-1/MS-2/MS-3.

Comprueba respuesta, clave/venta correlacionadas, total conservado, ausencia de autorización aprobada y persistencia mediante contextos nuevos. Consulta el intento por HTTP y repite las comprobaciones; para timeout espera 36 segundos, superando la respuesta tardía de 35 segundos del simulador. Exige 409 al repetir la clave y también ante una clave nueva tras incertidumbre, con un único POST externo y sin pago ni evento posterior.

La ausencia de evento se verifica en `OutboxMessages`, origen durable de `sale.completed`. El publicador está deshabilitado en este fixture; **no se acredita lectura del broker Kafka ni del clúster**. Cero pagos es una condición más estricta que ausencia de Pago aprobado.

```powershell
docker run --rm -d --name globalmart-s2052-postgres `
  -e POSTGRES_PASSWORD=s2052_test -e POSTGRES_DB=s2052 `
  -p 127.0.0.1:55452:5432 postgres:16
$env:GLOBALMART_TEST_POSTGRES = 'Host=127.0.0.1;Port=55452;Database=s2052;Username=postgres;Password=s2052_test'
dotnet test tests/GlobalMart.IntegrationTests --configuration Release `
  --filter 'Plan=S2-052' --logger 'trx;LogFileName=S2-052.trx' `
  --results-directory docs/qa/S2-052
docker stop globalmart-s2052-postgres
```

Requiere .NET 8, Node, Docker y que PostgreSQL acepte conexiones antes de ejecutar. La suite elimina sus esquemas y procesos de simulación al terminar; el contenedor es desechable. Las variables de conexión solo se aplican al proceso de prueba. No se editaron destinos de servicios ni configuración del frontend: siguen apuntando al clúster.

## Intento sobre servicios desplegados

Los health checks remotos de MS-1/MS-3/MS-5 respondieron 200; login, catálogo y turno activo también respondieron 200. Sin embargo, `POST /ventas` devolvió **502** con `Error de conexión con MS-3: Connection refused (localhost:5203)`. El escenario no alcanzó el endpoint de cobro. Evidencia: [resultado.json](resultado.json). No se interpreta este 502 como rechazo ni timeout de pasarela.

El usuario confirmó que no dispone de las credenciales del clúster y autorizó validar localmente. La aceptación en ese despliegue queda **[NO VERIFICADO]**; requiere corregir la configuración interna de catálogo en MS-5 y disponer de acceso a persistencia. No se modificó el clúster.

Se entrega además [test-s2-052.ps1](../../../scripts/test-s2-052.ps1) para repetir contra servicios ya levantados. Requiere PowerShell 7, `GLOBALMART_TEST_PASSWORD`, turno abierto y producto activo. Guarda respuestas sin JWT ni contraseña, conserva las ventas de prueba y devuelve 1 ante fallo, 2 si solo pudo verificar HTTP y 0 únicamente si también verifica PostgreSQL con `-VerifyDatabase` (cliente `psql` y variables `PG*`). Esta ruta PostgreSQL del script remoto está **[NO VERIFICADA]**; la evidencia local de persistencia corresponde a xUnit.

```powershell
# Configurar GLOBALMART_TEST_PASSWORD fuera del repositorio.
pwsh -NoProfile -File scripts/test-s2-052.ps1
```
