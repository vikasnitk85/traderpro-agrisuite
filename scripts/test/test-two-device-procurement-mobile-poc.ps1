[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$mobileRoot = Join-Path $repositoryRoot 'apps\mobile'
$offlineStoreScript = Join-Path `
    $repositoryRoot `
    'scripts\test\test-mobile-offline-store.ps1'
$weightParityScript = Join-Path `
    $repositoryRoot `
    'scripts\test\test-weight-processing-parity.ps1'
$backendPocScript = Join-Path `
    $repositoryRoot `
    'scripts\test\test-two-device-procurement-backend-poc.ps1'

Push-Location $repositoryRoot
try {
    Write-Host 'Confirming Task 6B did not change the frozen backend source or tests...'
    $backendChanges = @(git status --porcelain --untracked-files=all -- .\services\backend)
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to inspect the frozen Task 6A backend worktree.'
    }
    if ($backendChanges.Count -ne 0) {
        $backendChanges | ForEach-Object { Write-Error $_ }
        throw 'Task 6B must not change the frozen Task 6A backend.'
    }

    Push-Location $mobileRoot
    try {
        Write-Host 'Resolving Flutter dependencies...'
        flutter pub get
        if ($LASTEXITCODE -ne 0) {
            throw 'Flutter dependency resolution failed.'
        }

        Write-Host 'Generating checked-in Drift source...'
        $generatedDriftSource = Join-Path `
            $mobileRoot `
            'lib\core\database\trader_pro_local_database.g.dart'
        if (-not (Test-Path -LiteralPath $generatedDriftSource -PathType Leaf)) {
            throw 'The checked-in generated Drift source is missing.'
        }
        $generatedHashBefore = (
            Get-FileHash -LiteralPath $generatedDriftSource -Algorithm SHA256
        ).Hash
        dart run build_runner build
        if ($LASTEXITCODE -ne 0) {
            throw 'Drift generation failed.'
        }
        $generatedHashAfter = (
            Get-FileHash -LiteralPath $generatedDriftSource -Algorithm SHA256
        ).Hash
        if ($generatedHashBefore -ne $generatedHashAfter) {
            throw (
                'Generated Drift source was stale. Review the regenerated ' +
                'file, then rerun this verification.'
            )
        }

        Write-Host 'Running focused Task 6B tests...'
        flutter test .\test\features\procurement_poc
        if ($LASTEXITCODE -ne 0) {
            throw 'Focused Task 6B tests failed.'
        }

        Write-Host 'Running full Flutter analysis...'
        flutter analyze
        if ($LASTEXITCODE -ne 0) {
            throw 'Flutter analysis failed.'
        }

        Write-Host 'Running the complete Flutter test suite...'
        flutter test
        if ($LASTEXITCODE -ne 0) {
            throw 'Flutter tests failed.'
        }
    }
    finally {
        Pop-Location
    }

    Write-Host 'Running the Task 4 offline-store regression...'
    & powershell -ExecutionPolicy Bypass -File $offlineStoreScript
    if ($LASTEXITCODE -ne 0) {
        throw 'Task 4 offline-store regression failed.'
    }

    Write-Host 'Running the Task 3 weight parity regression...'
    & powershell -ExecutionPolicy Bypass -File $weightParityScript
    if ($LASTEXITCODE -ne 0) {
        throw 'Task 3 weight parity regression failed.'
    }

    Write-Host 'Running the frozen Task 6A backend regression...'
    & powershell -ExecutionPolicy Bypass -File $backendPocScript
    if ($LASTEXITCODE -ne 0) {
        throw 'Task 6A backend regression failed.'
    }

    Write-Host 'Two-device Procurement mobile POC verification passed.'
}
finally {
    Pop-Location
}
