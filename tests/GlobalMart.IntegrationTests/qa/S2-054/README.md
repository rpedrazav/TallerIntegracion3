# S2-054 — Aislamiento de cobro

Las pruebas persisten en `CobroSeguroIntegrationTests.cs`, filtro `Plan=S2-054`.
Reutilizan su WebApplicationFactory, JWT firmado, siembra, PostgreSQL migrado,
adaptador real y simulador Node real. No requieren un banco ni Kafka.

## Reproducción desde la raíz

Requisitos: .NET 8, Node y Docker disponibles. El contenedor siguiente es exclusivo
de pruebas; no reutilizar una base personal.

```powershell
docker run --rm -d --name globalmart-s2054-postgres -e POSTGRES_PASSWORD=s2054_test -e POSTGRES_DB=s2054 -p 127.0.0.1:55454:5432 postgres:16
# Esperar hasta que pg_isready informe que acepta conexiones:
docker exec globalmart-s2054-postgres pg_isready -U postgres -d s2054
$env:GLOBALMART_TEST_POSTGRES='Host=127.0.0.1;Port=55454;Database=s2054;Username=postgres;Password=s2054_test'
$qaProps=(Resolve-Path tests/GlobalMart.IntegrationTests/S2054.Build.props).Path
dotnet test tests/GlobalMart.IntegrationTests/GlobalMart.IntegrationTests.csproj --configuration Release --artifacts-path tests/GlobalMart.IntegrationTests/.artifacts-s2054 --filter 'FullyQualifiedName~CobroSeguroIntegrationTests' --logger 'trx;LogFileName=S2-054-regression.trx' --results-directory tests/GlobalMart.IntegrationTests/TestResults/S2-054 --disable-build-servers -m:1 -p:RestoreDisableParallel=true "-p:DirectoryBuildPropsPath=$qaProps" -p:ImportDirectoryBuildTargets=false
docker stop globalmart-s2054-postgres
```

Para ejecutar únicamente los cuatro casos nuevos, usar `--filter 'Plan=S2-054'`.
La configuración QA evita los paths `obj` forzados por los archivos
`Directory.Build.props/targets` de la raíz sin modificarlos.

## Evidencia y límite del procedimiento

La primera ejecución dirigida aprobó 4/4 casos, cero fallos y omisiones (TRX adjunto).
La regresión posterior con configuración aislada aprobó 25/25 casos de
`CobroSeguroIntegrationTests`, incluidos los cuatro nuevos, cero fallos y omisiones
en 1 min 10 s (ver `S2-054-regression.trx`). El contenedor temporal fue retirado.
Verifica 404 para otro tenant con turno propio abierto; 409 para cajero sin turno
y turno cerrado; en los tres rechazos, cero POST y GET a la pasarela, venta
pendiente, cero pagos/intentos/outbox y snapshot idéntico de todas las columnas
de siete tablas del esquema exclusivo, leído con conexiones nuevas sin filtro.
El control positivo retorna 200 APPROVED, observa un POST y cero GET y confirma
venta completada, un pago de tarjeta de 119 CLP, un intento y un outbox pendiente.

Se detectó una desviación del límite de escritura en la primera compilación:
`--artifacts-path` no anuló los paths de intermedios forzados por la configuración
raíz. Se generaron archivos en `src/*/obj/Release` fuera del área de tests (por
ejemplo `src/POSCartService/obj/Release/POSCartService.csproj.FileListAbsolute.txt`,
24-10-2026 14:26:07 hora local). No se editó código productivo ni nodos y no se
borraron artefactos previos. La invocación corregida anterior usa configuración
aislada para sus salidas e intermedios. Esta desviación impide certificar el
cumplimiento completo de la restricción de escritura de esta sesión.

Alcance: backend HTTP local con JWT, BD y simulador reales. No acredita Electron,
login MS-1, catálogo/impuestos externos, entrega Kafka ni despliegue remoto.
