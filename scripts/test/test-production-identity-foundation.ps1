[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

function Invoke-CheckedDotnet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments,

        [Parameter(Mandatory = $true)]
        [string] $FailureMessage
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }
}

Push-Location $repositoryRoot
try {
    Write-Host 'Restoring repository-local .NET tools...'
    Invoke-CheckedDotnet `
        -Arguments @('tool', 'restore') `
        -FailureMessage 'dotnet tool restore failed.'

    Write-Host 'Restoring the .NET solution...'
    Invoke-CheckedDotnet `
        -Arguments @('restore', '.\TraderPro.sln') `
        -FailureMessage 'dotnet restore failed.'

    Write-Host 'Building the .NET solution once for all focused suites...'
    Invoke-CheckedDotnet `
        -Arguments @(
            'build',
            '.\TraderPro.sln',
            '--configuration',
            'Debug',
            '--no-restore'
        ) `
        -FailureMessage 'dotnet build failed.'

    Write-Host 'Running focused production-identity unit tests...'
    Invoke-CheckedDotnet `
        -Arguments @(
            'test',
            '.\services\backend\tests\TraderPro.UnitTests\TraderPro.UnitTests.csproj',
            '--configuration',
            'Debug',
            '--no-build',
            '--filter',
            'FullyQualifiedName~ProductionIdentityTests'
        ) `
        -FailureMessage 'Production-identity unit tests failed.'

    Write-Host 'Running PostgreSQL 18 and API identity tests...'
    Invoke-CheckedDotnet `
        -Arguments @(
            'test',
            '.\services\backend\tests\TraderPro.IntegrationTests\TraderPro.IntegrationTests.csproj',
            '--configuration',
            'Debug',
            '--no-build',
            '--filter',
            'FullyQualifiedName~ProductionIdentity'
        ) `
        -FailureMessage 'Production-identity PostgreSQL/API tests failed.'

    Write-Host 'Running architecture tests...'
    Invoke-CheckedDotnet `
        -Arguments @(
            'test',
            '.\services\backend\tests\TraderPro.ArchitectureTests\TraderPro.ArchitectureTests.csproj',
            '--configuration',
            'Debug',
            '--no-build'
        ) `
        -FailureMessage 'Architecture tests failed.'

    Write-Host 'Running Task 5 command/cursor regressions...'
    Invoke-CheckedDotnet `
        -Arguments @(
            'test',
            '.\services\backend\tests\TraderPro.UnitTests\TraderPro.UnitTests.csproj',
            '--configuration',
            'Debug',
            '--no-build',
            '--filter',
            'FullyQualifiedName~CommandProbeTests'
        ) `
        -FailureMessage 'Task 5 unit regressions failed.'
    Invoke-CheckedDotnet `
        -Arguments @(
            'test',
            '.\services\backend\tests\TraderPro.IntegrationTests\TraderPro.IntegrationTests.csproj',
            '--configuration',
            'Debug',
            '--no-build',
            '--filter',
            'FullyQualifiedName~CloudCommand'
        ) `
        -FailureMessage 'Task 5 PostgreSQL/API regressions failed.'

    Write-Host 'Running Task 6A backend POC regressions...'
    Invoke-CheckedDotnet `
        -Arguments @(
            'test',
            '.\services\backend\tests\TraderPro.UnitTests\TraderPro.UnitTests.csproj',
            '--configuration',
            'Debug',
            '--no-build',
            '--filter',
            'FullyQualifiedName~ReceivingPocTests'
        ) `
        -FailureMessage 'Task 6A unit regressions failed.'
    Invoke-CheckedDotnet `
        -Arguments @(
            'test',
            '.\services\backend\tests\TraderPro.IntegrationTests\TraderPro.IntegrationTests.csproj',
            '--configuration',
            'Debug',
            '--no-build',
            '--filter',
            'FullyQualifiedName~ProcurementPocApiTests'
        ) `
        -FailureMessage 'Task 6A PostgreSQL/API regressions failed.'

    Write-Host 'Production identity foundation verification passed.'
}
finally {
    Pop-Location
}
