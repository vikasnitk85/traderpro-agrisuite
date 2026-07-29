[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]*$')]
    [string] $Name
)

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
        'development database before creating a migration.')
}

Push-Location $repositoryRoot
try {
    & dotnet ef migrations add $Name `
        --project $project `
        --startup-project $project `
        --context TraderProDbContext `
        --output-dir 'Persistence\Migrations'
    if ($LASTEXITCODE -ne 0) {
        throw "Migration creation failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
