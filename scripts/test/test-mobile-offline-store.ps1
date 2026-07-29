[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$mobileRoot = Join-Path $repositoryRoot 'apps\mobile'
$parityScript = Join-Path `
    $repositoryRoot `
    'scripts\test\test-weight-processing-parity.ps1'

if (-not (Test-Path -LiteralPath $mobileRoot -PathType Container)) {
    throw "The Flutter mobile project was not found at '$mobileRoot'."
}

Write-Host 'Generating the Drift database source...'

Push-Location $mobileRoot
try {
    & dart run build_runner build
    if ($LASTEXITCODE -ne 0) {
        throw "Drift generation failed with exit code $LASTEXITCODE."
    }

    Write-Host 'Running local database and restart-recovery tests...'
    & flutter test 'test/core/database'
    if ($LASTEXITCODE -ne 0) {
        throw "Core local-store tests failed with exit code $LASTEXITCODE."
    }

    Write-Host 'Running Receiving local-store transaction tests...'
    & flutter test 'test/features/receiving'
    if ($LASTEXITCODE -ne 0) {
        throw "Receiving local-store tests failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $parityScript -PathType Leaf)) {
    throw "The weight-processing parity script was not found at '$parityScript'."
}

Write-Host 'Running the existing cross-platform weight-processing parity tests...'
& powershell -ExecutionPolicy Bypass -File $parityScript
if ($LASTEXITCODE -ne 0) {
    throw "Weight-processing parity tests failed with exit code $LASTEXITCODE."
}

Write-Host 'Mobile offline-store generation and tests passed.'
