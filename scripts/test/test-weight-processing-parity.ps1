[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$vectorPath = Join-Path `
    $repositoryRoot `
    'contracts\golden-vectors\weight-processing.v1.json'
$unitTestProject = Join-Path `
    $repositoryRoot `
    'services\backend\tests\TraderPro.UnitTests\TraderPro.UnitTests.csproj'
$mobileRoot = Join-Path $repositoryRoot 'apps\mobile'

if (-not (Test-Path -LiteralPath $vectorPath -PathType Leaf)) {
    throw "Weight-processing golden vectors were not found at '$vectorPath'."
}

Write-Host "Golden vectors: $vectorPath"
Write-Host 'Running targeted .NET weight-processing tests...'

& dotnet test `
    $unitTestProject `
    --configuration Debug `
    --filter 'FullyQualifiedName~WeightProcess'

if ($LASTEXITCODE -ne 0) {
    throw ".NET weight-processing tests failed with exit code $LASTEXITCODE."
}

Write-Host 'Running targeted Flutter weight-processing tests...'

Push-Location $mobileRoot
try {
    & flutter test 'test/core/measurements'

    if ($LASTEXITCODE -ne 0) {
        throw "Flutter weight-processing tests failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

Write-Host 'Weight-processing parity tests passed for .NET and Flutter.'
