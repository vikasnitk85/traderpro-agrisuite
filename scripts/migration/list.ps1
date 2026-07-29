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
        'Set ConnectionStrings__TraderPro for a non-production ' +
        'development database before listing migrations.')
}

Push-Location $repositoryRoot
try {
    & dotnet ef migrations list `
        --project $project `
        --startup-project $project `
        --context TraderProDbContext
    if ($LASTEXITCODE -ne 0) {
        throw "Migration listing failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
