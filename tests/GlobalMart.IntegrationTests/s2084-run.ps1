$ErrorActionPreference = 'Stop'
$s2084Repository = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$s2084Props = Join-Path $PSScriptRoot 'S2082.Build.props'
Push-Location -LiteralPath $s2084Repository
try {
    & dotnet test (Join-Path $PSScriptRoot 'GlobalMart.IntegrationTests.csproj') --filter 'FullyQualifiedName~PesableEntreServiciosIntegrationTests' --nologo -v q -m:1 -p:NuGetAudit=false "-p:DirectoryBuildPropsPath=$s2084Props" --logger 'trx;LogFileName=S2084.trx' --results-directory (Join-Path $PSScriptRoot 'TestResults/S2084')
    $s2084ExitCode = $LASTEXITCODE
}
finally { Pop-Location }
exit $s2084ExitCode

