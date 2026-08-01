[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (
    Join-Path $PSScriptRoot '..\..')).Path

Push-Location $repositoryRoot
try {
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool restore failed with exit code $LASTEXITCODE."
    }

    & dotnet restore '.\TraderPro.sln'
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE."
    }

    & dotnet build '.\TraderPro.sln' `
        --configuration Debug `
        --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }

    & dotnet test `
        '.\services\backend\tests\TraderPro.UnitTests\TraderPro.UnitTests.csproj' `
        --configuration Debug `
        --no-build `
        --filter 'FullyQualifiedName~CommercialSupplierProductCatalogTests'
    if ($LASTEXITCODE -ne 0) {
        throw "Focused Supplier/Product catalog unit tests failed."
    }

    & dotnet test `
        '.\services\backend\tests\TraderPro.IntegrationTests\TraderPro.IntegrationTests.csproj' `
        --configuration Debug `
        --no-build `
        --filter 'FullyQualifiedName~CommercialSupplierProductCatalogApiTests'
    if ($LASTEXITCODE -ne 0) {
        throw "Focused Supplier/Product PostgreSQL/API tests failed."
    }

    & dotnet test `
        '.\services\backend\tests\TraderPro.ArchitectureTests\TraderPro.ArchitectureTests.csproj' `
        --configuration Debug `
        --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "Architecture tests failed."
    }

    & dotnet test `
        '.\services\backend\tests\TraderPro.IntegrationTests\TraderPro.IntegrationTests.csproj' `
        --configuration Debug `
        --no-build `
        --filter 'FullyQualifiedName~CommercialOperationalMasterDataApiTests'
    if ($LASTEXITCODE -ne 0) {
        throw "Task 7B1 operational-master regression tests failed."
    }

    $identityAndPocFilter = (
        'FullyQualifiedName~ProductionIdentity' +
        '|FullyQualifiedName~ProcurementPocApiTests'
    )
    & dotnet test `
        '.\services\backend\tests\TraderPro.IntegrationTests\TraderPro.IntegrationTests.csproj' `
        --configuration Debug `
        --no-build `
        --filter $identityAndPocFilter
    if ($LASTEXITCODE -ne 0) {
        throw "Task 7A identity/Task 6A backend regression tests failed."
    }
}
finally {
    Pop-Location
}
