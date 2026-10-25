#requires -Version 7.0
# Crea y conserva una venta de prueba remota. Nunca registra JWT ni contraseña.
# Salida 1: fallo/bloqueo; 2: HTTP correcto, unicidad en DB/pasarela no verificada.
[CmdletBinding()]
param(
    [string]$IdentityUrl = 'https://auth-rpedraza.dev.censei.cl',
    [string]$CatalogUrl = 'https://catalog-rpedraza.dev.censei.cl',
    [string]$PosUrl = 'https://pos-rpedraza.dev.censei.cl',
    [guid]$TenantId = 'aaaaaaaa-0000-0000-0000-000000000001',
    [string]$Email = 'cajero@demo.cl',
    [string]$Password = $env:GLOBALMART_TEST_PASSWORD,
    [string]$OutputPath = 'docs/qa/S2-053/remoto.json'
)
$ErrorActionPreference = 'Stop'
if (-not $Password) { throw 'Configure GLOBALMART_TEST_PASSWORD.' }
$report = [ordered]@{
    plan='S2-053'; started_at=[DateTimeOffset]::UtcNow.ToString('o')
    pos_url=$PosUrl; tenant_id="$TenantId"; result='BLOCKED'
    persistence='NO VERIFICADO'; logical_authorizations='NO VERIFICADO'; checks=@()
}
$script:headers = @{}
function Http([string]$Method, [string]$Url, $Body = $null) {
    $options = @{Uri=$Url; Method=$Method; Headers=$script:headers; TimeoutSec=60; SkipHttpErrorCheck=$true}
    if ($null -ne $Body) { $options.ContentType='application/json'; $options.Body=($Body | ConvertTo-Json -Depth 10) }
    $response = Invoke-WebRequest @options
    $json = $null
    try { $json = $response.Content | ConvertFrom-Json } catch { }
    return @{code=[int]$response.StatusCode; body=$json}
}
function Check([string]$Name, [bool]$Passed) { $report.checks += @{name=$Name; passed=$Passed} }
try {
    $report.health = @()
    foreach ($url in @($IdentityUrl,$CatalogUrl,$PosUrl)) {
        $report.health += @{url="$url/health"; response=(Http GET "$url/health")}
    }
    $login = Http POST "$IdentityUrl/auth/login" @{email=$Email; password=$Password; tenantId="$TenantId"}
    $report.login_code = $login.code
    if ($login.code -ne 200 -or -not $login.body.token) { throw "Login HTTP $($login.code)" }
    $script:headers = @{Authorization="Bearer $($login.body.token)"}
    $turno = Http GET "$PosUrl/turnos/activo"
    $report.turno = $turno
    if ($turno.code -ne 200 -or $turno.body.tenantId -ne "$TenantId" -or $turno.body.estado -notin @(0,'ABIERTO')) {
        throw 'Se requiere turno ABIERTO del tenant del cajero.'
    }
    $catalog = Http GET "$CatalogUrl/products"
    $report.catalog_code = $catalog.code
    if ($catalog.code -ne 200) { throw "Catálogo HTTP $($catalog.code)" }
    $product = $catalog.body.data | Where-Object { $_.isActive -and -not $_.esPesoVariable -and $_.tenantId -eq "$TenantId" -and $_.precioBase -gt 0 } | Select-Object -First 1
    if (-not $product) { throw 'No hay producto unitario activo para la prueba.' }
    $report.product_id = $product.id
    $created = Http POST "$PosUrl/ventas" @{metodo_pago='TARJETA'; items=@(@{producto_id=$product.id; cantidad=1})}
    $report.creation = $created
    if ($created.code -ne 201 -or -not $created.body.id) { throw "Crear venta HTTP $($created.code)" }
    if ($created.body.tenant_id -ne "$TenantId" -or $created.body.estado -ne 'PENDIENTE' -or $created.body.metodo_pago -ne 'TARJETA') {
        throw 'La venta no cumple las precondiciones de cobro.'
    }
    $sale = [guid]$created.body.id
    $key = [guid]::NewGuid().ToString()
    $report.sale_id = "$sale"; $report.request_id = $key
    $payload = @{scenario_id='sim-approved-v1'; idempotency_key=$key}
    $first = Http POST "$PosUrl/ventas/$sale/cobrar-tarjeta" $payload
    $report.first = $first
    if ($first.code -ne 200 -or $first.body.status -ne 'APPROVED') { throw "No se obtuvo aprobación: HTTP $($first.code)" }
    $repeat = Http POST "$PosUrl/ventas/$sale/cobrar-tarjeta" $payload
    $report.repeat = $repeat
    $read = Http GET "$PosUrl/ventas/$sale"
    $attempt = Http GET "$PosUrl/ventas/$sale/intento-tarjeta"
    $report.sale = $read; $report.attempt = $attempt
    Check 'Duplicado rechazado con 409' ($repeat.code -eq 409)
    Check 'Venta completada y total conservado' ($read.code -eq 200 -and $read.body.estado -eq 'COMPLETADA' -and $read.body.total -eq $created.body.total)
    Check 'Misma clave aprobada' ($attempt.code -eq 200 -and $attempt.body.status -eq 'APPROVED' -and $attempt.body.request_id -eq $key)
    Check 'Referencia conservada' ($first.body.gateway_reference -and $attempt.body.gateway_reference -eq $first.body.gateway_reference)
    $report.result = if (@($report.checks | Where-Object { -not $_.passed }).Count) { 'FAILED' } else { 'PARTIAL_HTTP_ONLY' }
} catch {
    $report.error = $_.Exception.Message
} finally {
    $report.finished_at = [DateTimeOffset]::UtcNow.ToString('o')
    $null = New-Item -ItemType Directory -Force (Split-Path $OutputPath)
    $report | ConvertTo-Json -Depth 25 | Set-Content -LiteralPath $OutputPath -Encoding utf8
    Write-Host "$($report.result): $($report.error)"
}
if ($report.result -eq 'PARTIAL_HTTP_ONLY') { exit 2 }
exit 1
