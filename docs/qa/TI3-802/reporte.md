# TI3-802 — Recepción de mercadería en Admin

Fecha 2026-11-24. Rama `dev`.

## Objetivo y criterio

ADMIN activo registra mercadería en MS-4 y ve el stock de la sucursal actualizado tras el 201. Los fallos de envío se explican y conservan las entradas. Un GET fallido después del 201 conserva la confirmación y permite reintentar la lectura.

## Cambios y contrato

- `AdminInventario.tsx` comparte las listas paginadas, la selección de sucursal y el GET de stock con `RecepcionMercaderiaForm.tsx`. El formulario ofrece solo productos y sucursales activos y se muestra con `active_role=ADMIN`.
- El POST envía Bearer y `productoId`, `sucursalId`, `cantidad`, `fechaVencimiento` y `ubicacion` opcional. Valida referencias seleccionadas, cantidad positiva hasta 99.999.999 con cuatro decimales como máximo, fecha desde hoy UTC y hasta 100 caracteres de ubicación. La guarda síncrona evita el doble envío.
- Tras 201 se consulta `GET /api/stock?sucursal_id=…` y se muestra la confirmación. La API acepta recepciones repetidas, por lo que la UI no reenvía automáticamente un POST. Ante 400, 401, 403, 404, 503 o error de red conserva las entradas.

## Contexto para compañeros

- `src/WarehouseInventoryService/Controllers/RecepcionesController.cs` agrega `/api/recepciones` porque la URL de inventario del frontend ya incluye `/api`. `POST /recepciones` sigue disponible para clientes directos. Ambas rutas apuntan a la misma acción, autorización ADMIN/REPONEDOR, validación, respuesta 201 y persistencia de lote, stock y movimiento.
- `Docker/config/kong.yaml` agrega `recepciones-route` para POST y OPTIONS. Su plugin JWT sigue el patrón de stock. `strip_path: false` conserva `/api/recepciones` al reenviar al upstream `http://host.docker.internal:5205`. `/api/stock` conserva su ruta. Para probar en el entorno de equipo, MS-4 debe ejecutar una versión que incluya el alias y Kong debe cargar la configuración declarativa actualizada. Confirmar una recepción con JWT y sucursal de pruebas y luego consultar el stock. El clúster y su configuración quedan fuera de esta validación.

## Cómo reproducir

Desde la raíz:

```powershell
dotnet build src/WarehouseInventoryService/WarehouseInventoryService.csproj --no-restore
dotnet test tests/GlobalMart.Tests/GlobalMart.Tests.csproj --no-restore --filter 'FullyQualifiedName~MS4_Warehouse'
cd globalmart-frontend
node node_modules/typescript/bin/tsc --noEmit --incremental false --pretty false
$env:DEBUG='pw:webserver'
node node_modules/playwright/cli.js test e2e/admin.spec.ts --reporter=list --output=test-results/ti3-802
```

Para reproducir el smoke directo, ejecutar el ensamblado MS-4 desde su carpeta con `ASPNETCORE_ENVIRONMENT=Testing`, `Kafka__Enabled=false`, `Logging__EventLog__LogLevel__Default=None`, `ASPNETCORE_URLS=http://127.0.0.1:15205` y una clave JWT efímera en `Jwt__Key`. Firmar tokens HS256 de pruebas con `tenant_id`, `sub`, rol, expiración e issuer/audience de MS-4. Enviar `POST` con Bearer, `Content-Type: application/json` y cuerpo `{}` a `/recepciones`, `/api/recepciones` y `/ruta-inexistente`. La entrada inválida evita cualquier escritura.

## Resultados

| Validación | Resultado | Exit code |
|---|---|---|
| TypeScript | Sin errores | 0 |
| MS-4 build | Compiló con advertencia MSB3539 | 0 |
| Unitarias MS-4 | 44/44 aprobadas | 0 |
| Regresión Admin y recepción | 17/17 aprobadas en 34,0 s, con resumen final | 0 |
| HTTP directo MS-4 | Ambas rutas devuelven 400 con ADMIN y REPONEDOR, y 403 con CAJERO. Ruta de control 404 | 0 |
| Kong, inspección estática | YAML parseado. Ruta, métodos, JWT, upstream, `strip_path` y ruta de stock comprobados | 0 |

Playwright cubre 201, payload y JWT, bloqueo de doble envío, GET posterior para la sucursal seleccionada, cambio visible de stock, 400/401/403/404/503 y red, fallo del GET con reintento exclusivo de lectura, y visibilidad por rol. La línea base aprobó los ocho casos previos. En la primera corrida tras añadir el formulario aprobaron 16/17 (exit code 1) porque el selector de producto de la prueba de ajuste coincidía con dos formularios. Se acotó la misma aserción a «Registrar ajuste». Windows dejó abierto el servidor del puerto 4175 en el teardown. Después de que Playwright entró en esa fase se cerró su servidor por PID y el runner emitió su resumen y código de salida.

## Límites

- El navegador usa fixtures HTTP. La prueba directa verifica routing, roles y rechazo de entrada inválida en MS-4 real, sin usar PostgreSQL ni dependencias de validación. El primer arranque directo encontró acceso denegado a EventLog y se repitió con configuración de logging efímera.
- En la validación anterior, el puerto 8000 estaba libre y Docker no estaba en PATH. La verificación aislada posterior cubre Kong en ejecución y recepción 201 con servicios reales. El despliegue remoto y Electron contra el gateway quedan [NO VERIFICADOS].

## Verificación real aislada con Kong y PostgreSQL

Fecha 2026-11-24. Docker Desktop estaba activo en `desktop-linux`. `docker ps -a` terminó con exit code 0 y devolvió una lista vacía. Tampoco había escucha en los puertos habituales de Kong ni de los tres MS. Compose declara los PostgreSQL persistentes `tenant_db`, `catalog_db` y `warehouse_db` con volúmenes `pgdata-tenant`, `pgdata-catalog` y `pgdata-warehouse`. Las APIs se ejecutan en el host y Kong las alcanza mediante `host.docker.internal`. Arrancar solo el Kong habitual no habría proporcionado sus upstreams.

- Se crearon dos contenedores con las imágenes locales `postgres:16-alpine` y `kong:3.6`, una red y un volumen propios, identificados con `ti3-802-67dd2e4047`. PostgreSQL se publicó en `127.0.0.1:15432` con las bases exclusivas `ti3_identity`, `ti3_catalog` y `ti3_inventory`. Kong se publicó en `127.0.0.1:18000` con administración deshabilitada.
- MS-1, MS-3 y MS-4 se ejecutaron desde sus DLL locales en 15124, 15203 y 15205, con conexiones explícitas a esas bases y clave JWT efímera. MS-1 inicializó su usuario ADMIN de desarrollo en la base de prueba. MS-4 validó la sucursal y el producto contra los servicios reales de la corrida. Kafka quedó deshabilitado en MS-4 para esta sesión.
- Kong cargó una copia temporal de la configuración declarativa con el servicio MS-4. En ella solo se adaptaron el puerto del upstream y el secreto JWT de pruebas en lo relativo a MS-4. La ruta probada conservó `/api/recepciones`, POST/OPTIONS, el plugin JWT y `strip_path: false`.

| Paso | HTTP | Evidencia |
|---|---|---|
| `GET /health` en MS-1, MS-3 y MS-4 | 200 cada uno | Servicios disponibles |
| `POST /auth/login` en MS-1 | 200 | JWT ADMIN emitido por MS-1 |
| `POST /sucursales` en MS-1 y `POST /products` en MS-3 | 201 cada uno | Sucursal y producto activos de la corrida |
| `GET /api/stock?sucursal_id=…` por Kong | 200 | Lista vacía antes de recibir |
| `POST /api/recepciones` por Kong | 201 | Devuelve ID de lote y los datos de la recepción de 2,125 unidades |
| `GET /api/stock?sucursal_id=…` por Kong | 200 | Una fila para el producto y la sucursal. `cantidadActual = 2.125` |
| SQL en `ti3_inventory` | — | Un lote coincide en ID, tenant, producto, sucursal, cantidad, vencimiento y ubicación. Un movimiento positivo `RECEPCION` coincide en tenant, producto, cantidad y referencia a lote y sucursal |

El orquestador temporal `node .ti3-802-integration.cjs` terminó con exit code **0**. Los comandos Docker se ejecutaron con `--context desktop-linux` y utilizaron `network create`, `volume create`, `run -d --pull=never` y `exec` con `pg_isready` y `createdb -U qa` para las tres bases de prueba. Las APIs se iniciaron mediante `dotnet <proyecto>/bin/Debug/net8.0/<proyecto>.dll` con `ConnectionStrings__DefaultConnection` y puertos propios. Las dos consultas de persistencia se ejecutaron con `docker exec <contenedor-qa> psql -U qa -d ti3_inventory -At -v ON_ERROR_STOP=1 -c <SQL>` y devolvieron `1` y `1`, exit code **0**.

Para reproducir las tres operaciones de negocio en un entorno equivalente con datos y JWT de prueba:

```powershell
$gateway = 'http://127.0.0.1:18000'
$headers = @{ Authorization = "Bearer $token" }
$stockUrl = "$gateway/api/stock?sucursal_id=$sucursalId"
Invoke-RestMethod -Uri $stockUrl -Headers $headers
$recepcion = @{
  productoId = $productoId
  sucursalId = $sucursalId
  cantidad = 2.125
  fechaVencimiento = [DateTime]::UtcNow.AddDays(10).ToString('yyyy-MM-dd')
  ubicacion = 'Estante QA'
} | ConvertTo-Json
Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$gateway/api/recepciones" -Headers $headers -ContentType 'application/json' -Body $recepcion
Invoke-RestMethod -Uri $stockUrl -Headers $headers
```

Se cerraron los tres procesos .NET y se eliminaron por sus identificadores los dos contenedores propios, la red, el volumen de la corrida y la copia temporal de Kong. El listado por nombre y etiqueta confirmó que ya no existen esos recursos y que los volúmenes habituales de MS-1/MS-3/MS-4 y el volumen anterior de pruebas de MS-1 siguen presentes. Los cinco puertos de la corrida quedaron libres.
