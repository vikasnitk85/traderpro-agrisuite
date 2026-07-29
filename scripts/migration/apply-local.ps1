[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (
    Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repositoryRoot (
    'services\backend\src\TraderPro.Infrastructure\' +
    'TraderPro.Infrastructure.csproj')

if ([string]::IsNullOrWhiteSpace(
        $env:ConnectionStrings__TraderPro)) {
    throw (
        'Set ConnectionStrings__TraderPro explicitly for the local ' +
        'development database. This script never selects production.')
}

Push-Location $repositoryRoot
try {
    & dotnet ef database update `
        --project $project `
        --startup-project $project `
        --context TraderProDbContext
    if ($LASTEXITCODE -ne 0) {
        throw "Local migration failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
