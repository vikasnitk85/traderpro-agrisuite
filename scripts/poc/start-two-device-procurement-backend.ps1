[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string] $BindAddress = '0.0.0.0',

    [ValidateRange(1, 65535)]
    [int] $Port = 5000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($BindAddress.Contains('/') -or $BindAddress.Contains(':')) {
    throw 'BindAddress must be an IPv4 address or host name without a port.'
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$apiProject = Join-Path `
    $repositoryRoot `
    'services\backend\src\TraderPro.Api'
$keyRingPath = Join-Path `
    $repositoryRoot `
    'artifacts\development-procurement-poc-key-ring'
$signingKey = [Convert]::ToBase64String(
    [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))

Write-Warning 'Development Only: temporary workspace/device IDs and HTTP are not authentication.'
Write-Host "Binding the development API to http://${BindAddress}:$Port"
Write-Host 'No database credential is created, changed, or printed by this launcher.'
Write-Host 'Stop the process after the field test to disable the two spike flags.'

$environmentChanges = [ordered]@{
    DOTNET_ENVIRONMENT = 'Development'
    TraderPro__Spikes__Enabled = 'true'
    TraderPro__Spikes__ProcurementPoc__Enabled = 'true'
    TraderPro__Spikes__IdentityBootstrap__Enabled = 'false'
    TraderPro__Authentication__Issuer = 'TraderPro.Development.Poc'
    TraderPro__Authentication__Audience = 'TraderPro.Development.Poc.Client'
    TraderPro__Authentication__SigningKey = $signingKey
    TraderPro__Authentication__DataProtectionKeyRingPath = $keyRingPath
    TraderPro__Authentication__RequireHttps = 'false'
    ASPNETCORE_URLS = "http://${BindAddress}:$Port"
}
$previousEnvironment = @{}
$locationPushed = $false

foreach ($name in $environmentChanges.Keys) {
    $item = Get-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue
    $previousEnvironment[$name] = [pscustomobject]@{
        Existed = $null -ne $item
        Value = if ($null -eq $item) { $null } else { $item.Value }
    }
}

try {
    foreach ($name in $environmentChanges.Keys) {
        Set-Item -LiteralPath "Env:$name" -Value $environmentChanges[$name]
    }
    Push-Location $repositoryRoot
    $locationPushed = $true
    dotnet run --no-launch-profile --project $apiProject
    if ($LASTEXITCODE -ne 0) {
        throw "The development backend exited with code $LASTEXITCODE."
    }
}
finally {
    $signingKey = $null
    if ($locationPushed) {
        Pop-Location
    }
    foreach ($name in $environmentChanges.Keys) {
        $previous = $previousEnvironment[$name]
        if ($previous.Existed) {
            Set-Item -LiteralPath "Env:$name" -Value $previous.Value
        }
        else {
            Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue
        }
    }
}
