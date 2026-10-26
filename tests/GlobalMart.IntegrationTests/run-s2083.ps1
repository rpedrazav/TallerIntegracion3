$ErrorActionPreference = 'Stop'
$s2083Repository = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$s2083Project = Join-Path $PSScriptRoot 'GlobalMart.IntegrationTests.csproj'
$s2083Props = Join-Path $PSScriptRoot 'S2082.Build.props'
$s2083Results = Join-Path $PSScriptRoot 'TestResults/S2083'
Push-Location -LiteralPath $s2083Repository
try {
    & dotnet test $s2083Project --filter 'FullyQualifiedName~ScaleFallbackIntegrationTests' --nologo -v q -m:1 "-p:DirectoryBuildPropsPath=$s2083Props" --logger 'trx;LogFileName=S2083.trx' --results-directory $s2083Results
    $s2083ExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}
exit $s2083ExitCode
