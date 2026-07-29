[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

Push-Location $repositoryRoot
try {
    Write-Host 'Restoring repository-local .NET tools...'
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet tool restore failed.'
    }

    Write-Host 'Restoring the .NET solution...'
    dotnet restore .\TraderPro.sln
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet restore failed.'
    }

    Write-Host 'Running focused Procurement POC unit tests...'
    dotnet test `
        .\services\backend\tests\TraderPro.UnitTests\TraderPro.UnitTests.csproj `
        --configuration Debug `
        --no-restore `
        --filter 'FullyQualifiedName~ReceivingPocTests'
    if ($LASTEXITCODE -ne 0) {
        throw 'Focused Procurement POC unit tests failed.'
    }

    Write-Host 'Running PostgreSQL 18 and true API Procurement POC tests...'
    dotnet test `
        .\services\backend\tests\TraderPro.IntegrationTests\TraderPro.IntegrationTests.csproj `
        --configuration Debug `
        --no-restore `
        --filter 'FullyQualifiedName~ProcurementPocApiTests'
    if ($LASTEXITCODE -ne 0) {
        throw 'Procurement POC PostgreSQL/API tests failed.'
    }

    Write-Host 'Running architecture tests...'
    dotnet test `
        .\services\backend\tests\TraderPro.ArchitectureTests\TraderPro.ArchitectureTests.csproj `
        --configuration Debug `
        --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw 'Architecture tests failed.'
    }

    Write-Host 'Running Task 5 command/cursor regression tests...'
    dotnet test `
        .\services\backend\tests\TraderPro.UnitTests\TraderPro.UnitTests.csproj `
        --configuration Debug `
        --no-restore `
        --filter 'FullyQualifiedName~CommandProbeTests'
    if ($LASTEXITCODE -ne 0) {
        throw 'Task 5 unit regressions failed.'
    }

    dotnet test `
        .\services\backend\tests\TraderPro.IntegrationTests\TraderPro.IntegrationTests.csproj `
        --configuration Debug `
        --no-restore `
        --filter 'FullyQualifiedName~CloudCommand'
    if ($LASTEXITCODE -ne 0) {
        throw 'Task 5 PostgreSQL/API regressions failed.'
    }

    Write-Host 'Two-device Procurement backend POC verification passed.'
}
finally {
    Pop-Location
}
