# S2-062 — Validar peso en agregar ítem

Fecha: 2026-10-24.

**Resultado: 32/32 pruebas aprobadas en Release, sin fallos ni omisiones: 20 de S2-062 y 12 de regresión.**

## Cambio

`POST /ventas/{id}/items` (también `/api/ventas/{id}/items`) consulta `EsPesoVariable` en MS-3. Para productos pesables exige `peso_kg`, mínimo de 0.010 kg, incrementos de 0.001 kg y coincidencia con cantidad cuando esta tiene valor. Rechaza peso no nulo en productos no pesables. Los errores devuelven HTTP 400 antes del cálculo fiscal y de cualquier escritura del ítem o totales.

`AgregarItemRequest.Cantidad` pasa a `decimal?`: omisión/null deriva cantidad del peso para pesables; en no pesables conserva el valor predeterminado de 1. Cantidades explícitas no positivas se rechazan. Un cliente que enviaba `cantidad: 1` junto a `peso_kg: 0.750` debe omitir cantidad o enviar `0.750`, según S2-061.

## Verificación

Suite en [VentaIntegrationTests.cs](../../../tests/GlobalMart.IntegrationTests/VentaIntegrationTests.cs), trait `Plan=S2-062`. Usa HTTP de MS-5, JWT firmado, validación automática, servicios/repositorios reales y PostgreSQL 16 temporal con migraciones. MS-3 y MS-2 se sustituyen mediante handlers HTTP deterministas; no acredita despliegue remoto ni balanza.

Los 20 casos nuevos cubren:

- 12 rechazos: peso ausente, null, cero, negativo, menor al mínimo, fracción de gramo; cantidad distinta, cero o negativa con peso válido; peso en no pesable; cantidades cero/negativa en no pesable.
- 8 aceptaciones: mínimo, peso con cantidad omitida, cantidad coincidente, ceros decimales adicionales, peso de tres decimales y tres variantes de productos no pesables.
- Para cada rechazo se compara un snapshot de la base con consultas nuevas sin tracking: ítems, identificadores, nombre, cantidad, peso, precio, subtotal, impuestos, total y estado idénticos. Se verifica que MS-2 no recibe otra llamada.
- Los aceptados verifican cantidad/peso persistidos, precio de catálogo pese a precio falsificado, subtotales/impuestos/totales y cantidad en respuesta. En todos se intenta falsificar la clasificación del producto desde el request; prevalece catálogo.

Se incluyen las pruebas previas de ventas y del validador de creación como regresión. Resultado de ejecución: ver [S2-062.trx](S2-062.trx).

## Reproducción

Utilizar una base exclusiva de pruebas, nunca una base compartida de desarrollo:

```bash
docker run --rm -d --name globalmart-s2062-postgres \
  -e POSTGRES_PASSWORD=s2062_test -e POSTGRES_DB=s2062 \
  -p 127.0.0.1:55462:5432 postgres:16

docker exec globalmart-s2062-postgres pg_isready -U postgres -d s2062

export ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=55462;Database=s2062;Username=postgres;Password=s2062_test'
dotnet test tests/GlobalMart.IntegrationTests --configuration Release \
  --filter 'FullyQualifiedName~VentaIntegrationTests|FullyQualifiedName~CrearVentaRequestValidatorTests' \
  --logger 'trx;LogFileName=S2-062.trx' --results-directory docs/qa/S2-062

docker stop globalmart-s2062-postgres
```

La contraseña es desechable y exclusiva del ejemplo. En este entorno fue necesario restaurar paquetes porque el SDK instalado no tenía disponible el targeting pack de ASP.NET Core referenciado por los assets anteriores.

## Límites

S2-062 valida agregar ítems. No aplica todas las reglas del contrato a crear venta o modificar ítems, ni implementa conversión de unidades del simulador. Unidad real del catálogo y snapshot del precio con cuatro decimales siguen pendientes según S2-061. No se cambian migraciones ni el precio persistido. La compilación mostró advertencias XML preexistentes en `ProductoController`; la ejecución mostró advertencias de redirección HTTPS y consulta de múltiples colecciones en EF. No hubo errores de compilación ni fallos de pruebas. No se ejecutó la solución completa.
