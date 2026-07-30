[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [uri] $Url = 'http://localhost:5000',

    [ValidateNotNullOrEmpty()]
    [string] $DataProtectionKeyRingPath =
        '.\artifacts\development-identity-key-ring',

    [ValidateNotNullOrEmpty()]
    [string] $Issuer = 'TraderPro.Development',

    [ValidateNotNullOrEmpty()]
    [string] $Audience = 'TraderPro.Development.Client',

    [switch] $EnableIdentityBootstrap,

    [switch] $GenerateTemporarySigningKey
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Url.Scheme -notin @('http', 'https') -or
    [string]::IsNullOrWhiteSpace($Url.Host)) {
    throw 'Url must be an absolute HTTP or HTTPS URL.'
}

if ([string]::IsNullOrWhiteSpace(
        $env:ConnectionStrings__TraderPro)) {
    throw (
        'Set ConnectionStrings__TraderPro explicitly for the local ' +
        'development database. This launcher does not print it.')
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$apiProject = Join-Path `
    $repositoryRoot `
    'services\backend\src\TraderPro.Api'
$keyRingPath = if (
    [System.IO.Path]::IsPathFullyQualified(
        $DataProtectionKeyRingPath)
) {
    [System.IO.Path]::GetFullPath($DataProtectionKeyRingPath)
}
else {
    [System.IO.Path]::GetFullPath(
        (Join-Path $repositoryRoot $DataProtectionKeyRingPath))
}

$signingKey = $env:TraderPro__Authentication__SigningKey
if ($GenerateTemporarySigningKey) {
    $bytes = [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
    $signingKey = [Convert]::ToBase64String($bytes)
    [Array]::Clear($bytes, 0, $bytes.Length)
}
elseif ([string]::IsNullOrWhiteSpace($signingKey)) {
    throw (
        'Set TraderPro__Authentication__SigningKey to a private Base64 ' +
        'key of at least 256 bits, or request -GenerateTemporarySigningKey.')
}

Write-Warning (
    'Development Only: the identity bootstrap is an insecure local setup, ' +
    'not production onboarding.')
Write-Host "Binding the development API to $($Url.AbsoluteUri)"
Write-Host 'No connection string, signing key, password, code, or token is printed.'
if ($EnableIdentityBootstrap) {
    Write-Warning 'The development identity bootstrap endpoint is enabled.'
}
else {
    Write-Host 'The development identity bootstrap endpoint remains disabled.'
}

$environmentChanges = [ordered]@{
    DOTNET_ENVIRONMENT = 'Development'
    ASPNETCORE_URLS = $Url.AbsoluteUri.TrimEnd('/')
    TraderPro__Spikes__Enabled = 'false'
    TraderPro__Spikes__ProcurementPoc__Enabled = 'false'
    TraderPro__Spikes__IdentityBootstrap__Enabled =
        $EnableIdentityBootstrap.IsPresent.ToString().ToLowerInvariant()
    TraderPro__Authentication__Issuer = $Issuer
    TraderPro__Authentication__Audience = $Audience
    TraderPro__Authentication__SigningKey = $signingKey
    TraderPro__Authentication__DataProtectionKeyRingPath = $keyRingPath
    TraderPro__Authentication__RequireHttps = 'false'
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
