<#
.SYNOPSIS
  S2-085 - Herramienta de captura para el protocolo de prueba fisica de balanza.

.DESCRIPTION
  No depende del modelo de balanza. Permite:
    -Listar : enumerar puertos COM y dispositivos serie/USB conectados (paso P1).
    -Puerto : abrir un puerto, enviar opcionalmente un comando y registrar los bytes
              recibidos en CSV (marca de tiempo, hex, ASCII) para pasos P2-P6.
  No interpreta tramas: el formato depende del manual del fabricante [NO VERIFICADO].

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File capturar-balanza.ps1 -Listar
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File capturar-balanza.ps1 -Puerto COM3 -Baudios 9600 -Comando "W\r" -Segundos 60 -Salida evidencia\P4-estabilidad-1kg.csv
.EXAMPLE
  # Comando en hexadecimal (p. ej. ENQ = 0x05), con repeticion cada 500 ms
  powershell -ExecutionPolicy Bypass -File capturar-balanza.ps1 -Puerto COM3 -ComandoHex 05 -IntervaloMs 500 -Segundos 30 -Salida evidencia\P3-enq.csv
#>
[CmdletBinding(DefaultParameterSetName = 'Listar')]
param(
    [Parameter(ParameterSetName = 'Listar')][switch]$Listar,
    [Parameter(ParameterSetName = 'Capturar', Mandatory = $true)][string]$Puerto,
    [Parameter(ParameterSetName = 'Capturar')][int]$Baudios = 9600,
    [Parameter(ParameterSetName = 'Capturar')][ValidateSet('None', 'Even', 'Odd', 'Mark', 'Space')][string]$Paridad = 'None',
    [Parameter(ParameterSetName = 'Capturar')][ValidateSet(7, 8)][int]$BitsDatos = 8,
    [Parameter(ParameterSetName = 'Capturar')][ValidateSet('One', 'Two')][string]$BitsParada = 'One',
    [Parameter(ParameterSetName = 'Capturar')][string]$Comando,
    [Parameter(ParameterSetName = 'Capturar')][string]$ComandoHex,
    [Parameter(ParameterSetName = 'Capturar')][int]$IntervaloMs = 0,
    [Parameter(ParameterSetName = 'Capturar')][int]$Segundos = 30,
    [Parameter(ParameterSetName = 'Capturar')][string]$Salida = "captura-$(Get-Date -Format 'yyyyMMdd-HHmmss').csv"
)

$ErrorActionPreference = 'Stop'

function Get-Bytes {
    if ($ComandoHex) {
        $limpio = ($ComandoHex -replace '[^0-9A-Fa-f]', '')
        if ($limpio.Length % 2 -ne 0) { throw "ComandoHex debe tener un numero par de digitos." }
        return [byte[]](0..($limpio.Length / 2 - 1) | ForEach-Object { [Convert]::ToByte($limpio.Substring($_ * 2, 2), 16) })
    }
    if ($Comando) {
        $texto = $Comando.Replace('\r', "`r").Replace('\n', "`n")
        return [System.Text.Encoding]::ASCII.GetBytes($texto)
    }
    return $null
}

function Format-Ascii([byte[]]$b) {
    -join ($b | ForEach-Object { if ($_ -ge 32 -and $_ -le 126) { [char]$_ } else { '<{0:X2}>' -f $_ } })
}

if ($PSCmdlet.ParameterSetName -eq 'Listar') {
    Write-Output "== Puertos COM (System.IO.Ports) =="
    $nombres = [System.IO.Ports.SerialPort]::GetPortNames()
    if (-not $nombres) { Write-Output "(ninguno)" } else { $nombres | Sort-Object | ForEach-Object { Write-Output $_ } }

    Write-Output ""
    Write-Output "== Dispositivos PnP (Ports / USB serie) =="
    try {
        $disp = Get-CimInstance Win32_PnPEntity |
            Where-Object { $_.PNPClass -eq 'Ports' -or $_.Name -match '\(COM\d+\)' -or $_.Name -match 'Serial|UART|CH340|FTDI|CP210|PL2303' }
        if (-not $disp) { Write-Output "(ninguno)" }
        else { $disp | Select-Object Name, Status, DeviceID, Manufacturer | Format-Table -AutoSize | Out-String -Width 250 | Write-Output }
    } catch {
        Write-Output "No fue posible consultar Win32_PnPEntity: $($_.Exception.Message)"
    }
    exit 0
}

$bytesCmd = Get-Bytes
$dir = Split-Path -Parent $Salida
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }

$sp = New-Object System.IO.Ports.SerialPort $Puerto, $Baudios, ([System.IO.Ports.Parity]$Paridad), $BitsDatos, ([System.IO.Ports.StopBits]$BitsParada)
$sp.ReadTimeout = 200
$sp.WriteTimeout = 1000
$sp.Handshake = [System.IO.Ports.Handshake]::None
$sp.DtrEnable = $true
$sp.RtsEnable = $true

"timestamp_iso,ms_desde_inicio,direccion,bytes,hex,ascii" | Set-Content -Path $Salida -Encoding UTF8
$cfg = "$Puerto $Baudios $Paridad $BitsDatos $BitsParada"
Write-Output "Abriendo $cfg -> $Salida"

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$ultimoEnvio = -1
$totalRx = 0
try {
    $sp.Open()
    while ($sw.Elapsed.TotalSeconds -lt $Segundos) {
        if ($bytesCmd -and ($ultimoEnvio -lt 0 -or ($IntervaloMs -gt 0 -and ($sw.ElapsedMilliseconds - $ultimoEnvio) -ge $IntervaloMs))) {
            $sp.Write($bytesCmd, 0, $bytesCmd.Length)
            $ultimoEnvio = $sw.ElapsedMilliseconds
            $hex = ($bytesCmd | ForEach-Object { '{0:X2}' -f $_ }) -join ' '
            "{0},{1},TX,{2},""{3}"",""{4}""" -f (Get-Date -Format o), $sw.ElapsedMilliseconds, $bytesCmd.Length, $hex, (Format-Ascii $bytesCmd).Replace('"', '""') |
                Add-Content -Path $Salida -Encoding UTF8
        }
        $n = $sp.BytesToRead
        if ($n -gt 0) {
            $buf = New-Object byte[] $n
            $leidos = $sp.Read($buf, 0, $n)
            $buf = $buf[0..($leidos - 1)]
            $totalRx += $leidos
            $hex = ($buf | ForEach-Object { '{0:X2}' -f $_ }) -join ' '
            $ascii = (Format-Ascii $buf).Replace('"', '""')
            "{0},{1},RX,{2},""{3}"",""{4}""" -f (Get-Date -Format o), $sw.ElapsedMilliseconds, $leidos, $hex, $ascii |
                Add-Content -Path $Salida -Encoding UTF8
            Write-Output ("[{0,7} ms] RX {1}" -f $sw.ElapsedMilliseconds, (Format-Ascii $buf))
        } else {
            Start-Sleep -Milliseconds 20
        }
    }
} catch {
    "{0},{1},ERROR,0,"""",""{2}""" -f (Get-Date -Format o), $sw.ElapsedMilliseconds, $_.Exception.Message.Replace('"', "'") |
        Add-Content -Path $Salida -Encoding UTF8
    Write-Output "ERROR: $($_.Exception.Message)"
    exit 2
} finally {
    if ($sp.IsOpen) { $sp.Close() }
    $sp.Dispose()
}
Write-Output "Fin. Bytes recibidos: $totalRx. Registro: $Salida"
if ($totalRx -eq 0) { exit 3 }
exit 0
