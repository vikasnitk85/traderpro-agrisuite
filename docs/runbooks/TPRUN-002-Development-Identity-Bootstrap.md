# TPRUN-002: Development Identity Bootstrap

## Warning and prerequisites

This runbook is **Development Only**. The bootstrap endpoint is an insecure
local setup aid, not production onboarding. Use no production database,
password, signing key, activation code, device secret, or token. Do not paste
real values into source control, command history, screenshots, or logs.

Prepare PostgreSQL 18, PowerShell, the .NET 10 SDK, a private local connection
string, and the reviewed migrations. The bootstrap requires caller-supplied
Owner and Operator passwords of 10-256 characters.

## Start PostgreSQL and apply migrations

From the repository root:

```powershell
docker compose `
  --env-file .\infrastructure\docker\.env `
  -f .\infrastructure\docker\compose.yml `
  up -d postgres
```

Set the connection privately in the current shell; do not echo it:

```powershell
$env:ConnectionStrings__TraderPro = '<local PostgreSQL connection string>'
.\scripts\migration\apply-local.ps1
```

The migration workflow does not reset or delete the Compose volume.

## Start only the identity bootstrap spike

The launcher can generate an in-process temporary 256-bit signing key without
printing it. The ignored `artifacts` directory is the default persistent
development Data Protection key ring:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\poc\start-production-identity-backend.ps1 `
  -Url 'http://localhost:5000' `
  -DataProtectionKeyRingPath `
    '.\artifacts\development-identity-key-ring' `
  -GenerateTemporarySigningKey `
  -EnableIdentityBootstrap
```

The launcher sets `DOTNET_ENVIRONMENT=Development`, keeps the general and
Procurement POC flags false, uses `--no-launch-profile`, and restores all
changed environment values in `finally`. It never prints the connection
string or signing key. For a persistent development signing key, privately set
`TraderPro__Authentication__SigningKey` to Base64 for at least 32 random bytes
and omit `-GenerateTemporarySigningKey`.

Equivalent configuration names are:

```text
TraderPro__Authentication__Issuer
TraderPro__Authentication__Audience
TraderPro__Authentication__SigningKey
TraderPro__Authentication__DataProtectionKeyRingPath
TraderPro__Authentication__RequireHttps
TraderPro__Authentication__ForwardedHeadersEnabled
TraderPro__Authentication__TrustedProxyAddresses__0
TraderPro__Spikes__IdentityBootstrap__Enabled
```

The launcher explicitly sets `RequireHttps=false` for its local Development
HTTP process. Production cannot disable HTTPS. For a Production deployment
whose immediate reverse proxy terminates TLS, enable forwarded headers and
list that proxy's exact IP address. Do not use a wildcard, trust the request
header directly, or allowlist client networks. Forwarding is disabled by
default and limited to one hop.

## Bootstrap owner, operator, and Device rows

In a second PowerShell session, keep passwords in variables that are never
printed. The placeholders below are examples, not real secrets:

```powershell
$ownerPassword = Read-Host 'Development Owner password' -AsSecureString
$operatorPassword = Read-Host 'Development Operator password' -AsSecureString
$ownerPlain = [System.Net.NetworkCredential]::new(
  '', $ownerPassword).Password
$operatorPlain = [System.Net.NetworkCredential]::new(
  '', $operatorPassword).Password

$setup = Invoke-RestMethod `
  -Method Post `
  -Uri 'http://localhost:5000/api/v1/spikes/identity/bootstrap' `
  -ContentType 'application/json' `
  -Body (@{
    workspaceCode = 'TRADERPRO-DEMO'
    ownerPassword = $ownerPlain
    operatorPassword = $operatorPlain
  } | ConvertTo-Json)
```

`$setup` contains one-time activation codes. Treat the object as secret and do
not format, echo, or serialize it. Repeating bootstrap reuses the one
workspace, company/default branch, users, and two Device rows, while issuing
fresh codes and revoking older unused codes. Issuance is serialized per Device
and the database permits at most one unused, unrevoked code for that Device.
Bootstrap and every other secret-bearing identity response carries
`Cache-Control: no-store` and `Pragma: no-cache`.

## Activate a pre-created Device

Use the returned Owner Device ID/code without printing them:

```powershell
$activation = Invoke-RestMethod `
  -Method Post `
  -Uri 'http://localhost:5000/api/v1/auth/device-activations/redeem' `
  -ContentType 'application/json' `
  -Body (@{
    workspaceCode = $setup.workspaceCode
    activationCode = $setup.ownerActivation.activationCode
    clientInstallationReference = 'local-owner-installation'
    deviceLabel = 'Local Owner Device'
    platform = 'Development'
  } | ConvertTo-Json)
```

The result contains the raw device secret once. Do not print it. Activation
uses `$setup.ownerDeviceId`; it does not create a Device row.

## Login, refresh, and current identity

```powershell
$login = Invoke-RestMethod `
  -Method Post `
  -Uri 'http://localhost:5000/api/v1/auth/login' `
  -ContentType 'application/json' `
  -Body (@{
    workspaceCode = $setup.workspaceCode
    login = 'owner'
    password = $ownerPlain
    deviceId = $setup.ownerDeviceId
    deviceSecret = $activation.deviceSecret
  } | ConvertTo-Json)

$headers = @{ Authorization = "Bearer $($login.accessToken)" }
$me = Invoke-RestMethod `
  -Method Get `
  -Uri 'http://localhost:5000/api/v1/auth/me' `
  -Headers $headers

$refreshed = Invoke-RestMethod `
  -Method Post `
  -Uri 'http://localhost:5000/api/v1/auth/refresh' `
  -ContentType 'application/json' `
  -Body (@{ refreshToken = $login.refreshToken } | ConvertTo-Json)
```

Do not display `$login`, `$headers`, or `$refreshed`. A normal refresh rotates
the token. Retrying the same prior token during the configured replay window
returns the same replacement only while that exact replacement remains active
and unconsumed. Once the replacement rotates again, presenting the older
predecessor is suspicious reuse: the family is revoked and
`REFRESH_TOKEN_REUSE_DETECTED` is returned. Protected replay ciphertext is
cleared when its successor is consumed and after expiry during later family
activity.

Commercial context is selected by endpoint metadata and current database
state, never by URL prefixes or temporary POC headers. A valid supplied
`X-Correlation-ID` UUID is preserved in identity responses and material
audits; invalid input is replaced rather than reflected.

## Logout and disable bootstrap

```powershell
Invoke-WebRequest `
  -Method Post `
  -Uri 'http://localhost:5000/api/v1/auth/logout' `
  -Headers @{
    Authorization = "Bearer $($refreshed.accessToken)"
  } | Out-Null
```

Stop the launcher, then restart it without `-EnableIdentityBootstrap`. Clear
secret variables from the second shell:

```powershell
$ownerPassword = $null
$operatorPassword = $null
$ownerPlain = $null
$operatorPlain = $null
$setup = $null
$activation = $null
$login = $null
$headers = $null
$refreshed = $null
```

## Troubleshooting

- `AUTHENTICATION_CONFIGURATION_INVALID`: supply issuer/audience, a Base64
  signing key that decodes to at least 32 bytes, and an absolute key-ring path
  (the launcher resolves its parameter to absolute).
- API startup cannot reach PostgreSQL: confirm
  `ConnectionStrings__TraderPro` is set privately and the Compose service is
  healthy.
- `DEVICE_ACTIVATION_ALREADY_USED`: bootstrap again for a fresh code or have
  an authenticated Owner issue one with a new idempotency key.
- `AUTHENTICATION_FAILED`: verify the workspace code, password, exact Device
  ID, and current device secret without logging them.
- `AUTHENTICATION_TEMPORARILY_LOCKED`: wait for the configured server-time
  lockout interval; rate limiting and lockout are separate.
- Refresh replay cannot be unprotected after a restart: reuse the same
  persistent Data Protection key-ring path.
- `DEVICE_ACTIVATION_CODE_RESPONSE_NOT_REPLAYABLE`: the same completed
  issuance idempotency key cannot reproduce a raw code. The original code
  remains active; use a new logical key only when intentionally replacing it.
- `HTTPS_REQUIRED`: the request reached a secret-bearing endpoint over an
  insecure transport. Use HTTPS in Production, or ensure the immediate TLS
  proxy is explicitly trusted and forwarding is enabled.
- `OWNER_ROLE_REQUIRED` applies only to an Owner-only endpoint. Other
  authenticated commercial denials return `AUTHORIZATION_DENIED`.
- Production refuses startup: expected. Bootstrap and every Task 5/6 spike are
  forbidden in Production.
