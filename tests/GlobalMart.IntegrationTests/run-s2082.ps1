$ErrorActionPreference = 'Stop'
$s2082Repository = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$s2082Project = Join-Path $PSScriptRoot 'GlobalMart.IntegrationTests.csproj'
$s2082Props = Join-Path $PSScriptRoot 'S2082.Build.props'
$s2082Results = Join-Path $PSScriptRoot 'TestResults/S2082'
Push-Location -LiteralPath $s2082Repository
try {
    & dotnet test $s2082Project --filter 'FullyQualifiedName~ScaleParserRoundingIntegrationTests' --nologo -v q -m:1 "-p:DirectoryBuildPropsPath=$s2082Props" --logger 'trx;LogFileName=S2082.trx' --results-directory $s2082Results
    $s2082ExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}
exit $s2082ExitCode
