#requires -Version 7.0
<#!
Prueba HTTP contra servicios YA desplegados; no inicia hosts ni sustituye la pasarela.
Requiere un turno abierto y un producto activo del tenant. Crea dos ventas de prueba
que conserva para auditoría. Nunca cierra el turno compartido ni cobra en aprobado.
Salida: 0 aceptación completa (HTTP + PostgreSQL), 1 fallo, 2 persistencia no verificada.
Para PostgreSQL configure PGHOST/PGPORT/PGDATABASE/PGUSER/PGPASSWORD y use -VerifyDatabase.
!#>
[CmdletBinding()]
param(
    [string]$IdentityUrl = 'https://auth-rpedraza.dev.censei.cl',
    [string]$CatalogUrl = 'https://catalog-rpedraza.dev.censei.cl',
    [string]$PosUrl = 'https://pos-rpedraza.dev.censei.cl',
    [guid]$TenantId = 'aaaaaaaa-0000-0000-0000-000000000001',
    [string]$Email = 'cajero@demo.cl',
    [string]$Password = $env:GLOBALMART_TEST_PASSWORD,
    [string]$OutputPath = 'docs/qa/S2-052/resultado.json',
    [switch]$VerifyDatabase
)
$ErrorActionPreference = 'Stop'
if (-not $Password) { throw 'Configure GLOBALMART_TEST_PASSWORD.' }
if ($VerifyDatabase) { $null = Get-Command psql -ErrorAction Stop }
$report = [ordered]@{
    plan = 'S2-052'; started_at = [DateTimeOffset]::UtcNow.ToString('o')
    pos_url = $PosUrl; tenant_id = "$TenantId"; cases = @(); result = 'FAILED'
    persistence = 'NO VERIFICADO'; event_scope = 'Outbox retenido, no lectura directa de Kafka'
}
$script:headers = @{}
function Http([string]$Method, [string]$Url, $Body = $null) {
    $args = @{ Uri=$Url; Method=$Method; Headers=$script:headers; TimeoutSec=100; SkipHttpErrorCheck=$true }
    if ($null -ne $Body) { $args.ContentType='application/json'; $args.Body=($Body | ConvertTo-Json -Depth 10) }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $response = Invoke-WebRequest @args
    $json = $null
    try { $json = $response.Content | ConvertFrom-Json } catch { }
    return @{ code=[int]$response.StatusCode; body=$json; elapsed_ms=$watch.ElapsedMilliseconds }
}
function Check($Case, [string]$Name, [bool]$Passed) {
    $Case.checks += @{ name=$Name; passed=$Passed }
}
function DatabaseSnapshot([guid]$SaleId) {
    # IDs typed as Guid; every query scopes by tenant AND sale. Read-only, no migrations.
    $sql = @"
SELECT json_build_object('estado', v."Estado", 'pagos',
 (SELECT count(*) FROM "Pagos" p WHERE p."VentaId"=v."Id"), 'outbox',
 (SELECT count(*) FROM "OutboxMessages" o WHERE o."TenantId"=v."TenantId" AND o."VentaId"=v."Id"),
 'intentos', (SELECT json_agg(json_build_object('estado', i."Estado", 'clave', i."IdempotencyKey"))
 FROM "IntentosPago" i WHERE i."TenantId"=v."TenantId" AND i."VentaId"=v."Id"))
FROM "Ventas" v WHERE v."TenantId"='$TenantId' AND v."Id"='$SaleId';
"@
    $value = & psql -X -A -t -v ON_ERROR_STOP=1 -c $sql
    if ($LASTEXITCODE -ne 0 -or -not $value) { throw 'No se pudo verificar persistencia.' }
    return ($value | ConvertFrom-Json)
}
try {
    $login = Http POST "$IdentityUrl/auth/login" @{email=$Email; password=$Password; tenantId="$TenantId"}
    if ($login.code -ne 200 -or -not $login.body.token) { throw "Login HTTP $($login.code)" }
    $script:headers = @{Authorization="Bearer $($login.body.token)"}
    $turno = Http GET "$PosUrl/turnos/activo"
    if ($turno.code -ne 200 -or $turno.body.tenantId -ne "$TenantId" -or $turno.body.estado -notin @(0,'ABIERTO')) {
        throw 'Se requiere turno ABIERTO del tenant del cajero.'
    }
    $catalog = Http GET "$CatalogUrl/products"
    if ($catalog.code -ne 200) { throw "Catálogo HTTP $($catalog.code)" }
    $product = $catalog.body.data | Where-Object { $_.isActive -and -not $_.esPesoVariable -and $_.tenantId -eq "$TenantId" -and $_.precioBase -gt 0 } | Select-Object -First 1
    if (-not $product) { throw 'No hay producto unitario activo para esta prueba.' }
    foreach ($scenario in @('sim-declined-v1','sim-timeout-v1')) {
        $case = [ordered]@{ scenario=$scenario; checks=@(); observations=@() }
        $report.cases += $case
        $created = Http POST "$PosUrl/ventas" @{metodo_pago='TARJETA'; items=@(@{producto_id=$product.id; cantidad=1})}
        $case.creation = $created
        if ($created.code -ne 201 -or -not $created.body.id) { throw "Crear venta HTTP $($created.code)" }
        $sale = [guid]$created.body.id
        $key = [guid]::NewGuid().ToString()
        $case.sale_id = "$sale"; $case.request_id = $key
        Check $case 'Venta inicial PENDIENTE del tenant, TARJETA y total positivo' ($created.body.estado -eq 'PENDIENTE' -and $created.body.tenant_id -eq "$TenantId" -and $created.body.metodo_pago -eq 'TARJETA' -and $created.body.total -gt 0)
        $charge = Http POST "$PosUrl/ventas/$sale/cobrar-tarjeta" @{scenario_id=$scenario; idempotency_key=$key}
        $case.charge = $charge
        $expectedCode = if ($scenario -eq 'sim-declined-v1') {422} else {504}
        $expectedStatus = if ($scenario -eq 'sim-declined-v1') {'DECLINED'} else {'UNCERTAIN'}
        Check $case "Respuesta HTTP $expectedCode" ($charge.code -eq $expectedCode)
        Check $case 'Respuesta correlacionada con venta e intento' ($charge.body.sale_id -eq "$sale" -and $charge.body.request_id -eq $key)
        if ($scenario -eq 'sim-declined-v1') {
            Check $case 'Rechazo definitivo del simulador' ($charge.body.status -eq 'DECLINED' -and $charge.body.decline_code -eq 'DO_NOT_HONOR')
        }
        # Observar también después del retardo de 35s del simulador. No confundir un
        # timeout del cliente de pruebas con el 504 emitido por el endpoint de MS-5.
        $observe = [Diagnostics.Stopwatch]::StartNew()
        for ($i=0; $i -lt 2; $i++) {
            if ($i -eq 1) { Start-Sleep -Seconds 36 }
            $read = Http GET "$PosUrl/ventas/$sale"
            $status = Http GET "$PosUrl/ventas/$sale/intento-tarjeta"
            $observation = @{ elapsed_ms=$observe.ElapsedMilliseconds; sale=$read; attempt=$status }
            $case.observations += $observation
            Check $case "Venta pendiente, total conservado (lectura $i)" ($read.code -eq 200 -and $read.body.estado -eq 'PENDIENTE' -and $read.body.total -eq $created.body.total)
            Check $case "Intento $expectedStatus, misma clave (lectura $i)" ($status.code -eq 200 -and $status.body.status -eq $expectedStatus -and $status.body.request_id -eq $key)
            if ($VerifyDatabase) {
                $db = DatabaseSnapshot $sale
                $observation.database = $db
                $expectedAttempt = if ($scenario -eq 'sim-declined-v1') {2} else {0}
                Check $case "Persistencia sin Pago ni evento (lectura $i)" ($db.estado -eq 0 -and $db.pagos -eq 0 -and $db.outbox -eq 0 -and @($db.intentos).Count -eq 1 -and $db.intentos[0].estado -eq $expectedAttempt -and $db.intentos[0].clave -eq $key)
            }
        }
        $repeat = Http POST "$PosUrl/ventas/$sale/cobrar-tarjeta" @{scenario_id=$scenario; idempotency_key=$key}
        $case.repeat = $repeat
        Check $case 'Repetición de clave bloqueada con 409' ($repeat.code -eq 409)
        if ($scenario -eq 'sim-timeout-v1') {
            $other = Http POST "$PosUrl/ventas/$sale/cobrar-tarjeta" @{scenario_id=$scenario; idempotency_key=[guid]::NewGuid().ToString()}
            $case.new_key = $other
            Check $case 'Otra clave bloqueada ante incertidumbre con 409' ($other.code -eq 409)
        }
        $final = Http GET "$PosUrl/ventas/$sale"
        $case.final = $final
        Check $case 'Venta final sigue PENDIENTE' ($final.code -eq 200 -and $final.body.estado -eq 'PENDIENTE')
        if ($VerifyDatabase) {
            $db = DatabaseSnapshot $sale
            $case.final_database = $db
            Check $case 'Sin pago, outbox ni nuevo intento tras repeticiones' ($db.estado -eq 0 -and $db.pagos -eq 0 -and $db.outbox -eq 0 -and @($db.intentos).Count -eq 1)
        }
        Write-Host "$scenario : HTTP $($charge.code); venta $sale; comprobaciones fallidas: $(@($case.checks | Where-Object { -not $_.passed }).Count)"
    }
    $failures = @($report.cases | ForEach-Object { $_.checks } | Where-Object { -not $_.passed })
    if ($failures.Count -gt 0) { $report.result='FAILED' }
    elseif ($VerifyDatabase) { $report.result='PASSED'; $report.persistence='VERIFICADO: venta, intento, cero pagos y cero outbox' }
    else { $report.result='PARTIAL_HTTP_ONLY' }
} catch {
    $report.error = $_.Exception.Message
} finally {
    $report.finished_at = [DateTimeOffset]::UtcNow.ToString('o')
    $null = New-Item -ItemType Directory -Force (Split-Path $OutputPath)
    $report | ConvertTo-Json -Depth 30 | Set-Content -Encoding utf8 $OutputPath
}
if ($report.result -eq 'PASSED') { exit 0 }
if ($report.result -eq 'PARTIAL_HTTP_ONLY') { exit 2 }
exit 1
