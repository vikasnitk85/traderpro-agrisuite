[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Output
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (
    Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repositoryRoot (
    'services\backend\src\TraderPro.Infrastructure\' +
    'TraderPro.Infrastructure.csproj')
$outputPath = if ([System.IO.Path]::IsPathRooted($Output)) {
    [System.IO.Path]::GetFullPath($Output)
}
else {
    [System.IO.Path]::GetFullPath(
        (Join-Path $repositoryRoot $Output))
}
$outputDirectory = Split-Path -Parent $outputPath

if ([string]::IsNullOrWhiteSpace(
        $env:ConnectionStrings__TraderPro)) {
    throw (
        'Set ConnectionStrings__TraderPro for a non-production ' +
        'development database before generating a migration script.')
}

if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Push-Location $repositoryRoot
try {
    & dotnet ef migrations script `
        --idempotent `
        --project $project `
        --startup-project $project `
        --context TraderProDbContext `
        --output $outputPath
    if ($LASTEXITCODE -ne 0) {
        throw (
            'Idempotent migration script generation failed with ' +
            "exit code $LASTEXITCODE.")
    }
}
finally {
    Pop-Location
}
