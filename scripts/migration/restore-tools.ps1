[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (
    Join-Path $PSScriptRoot '..\..')).Path
$manifest = Join-Path $repositoryRoot '.config\dotnet-tools.json'

if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw "The repository-local dotnet tool manifest was not found."
}

Push-Location $repositoryRoot
try {
    & dotnet tool restore --tool-manifest $manifest
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool restore failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
