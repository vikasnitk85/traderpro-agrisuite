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

    Write-Host 'Running focused Domain/Application command-probe tests...'
    dotnet test `
        .\services\backend\tests\TraderPro.UnitTests\TraderPro.UnitTests.csproj `
        --configuration Debug `
        --no-restore `
        --filter 'FullyQualifiedName~CommandProbeTests|FullyQualifiedName~PlatformFoundationTests'
    if ($LASTEXITCODE -ne 0) {
        throw 'Focused command-probe unit tests failed.'
    }

    Write-Host 'Running PostgreSQL 18 and API spike integration tests...'
    dotnet test `
        .\services\backend\tests\TraderPro.IntegrationTests\TraderPro.IntegrationTests.csproj `
        --configuration Debug `
        --no-restore `
        --filter 'FullyQualifiedName~CloudCommand|FullyQualifiedName~Outbox_event_facts|FullyQualifiedName~Outbox_status_and_attempts'
    if ($LASTEXITCODE -ne 0) {
        throw 'Cloud-command PostgreSQL/API integration tests failed.'
    }

    Write-Host 'Running architecture tests...'
    dotnet test `
        .\services\backend\tests\TraderPro.ArchitectureTests\TraderPro.ArchitectureTests.csproj `
        --configuration Debug `
        --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw 'Architecture tests failed.'
    }

    Write-Host 'Cloud-command spike verification passed.'
}
finally {
    Pop-Location
}
