# TPTECH-001.18: Production Identity and Device Sessions

## Objective and scope

Task 7A establishes the first production identity boundary for future
commercial TraderPro APIs. A workspace user authenticates with a password and
a secret for one pre-created active Device row. The API issues a short-lived
JWT access token and a rotating refresh token, then reconstructs commercial
authority from the validated token and current database state on every
authenticated request.

This task does not add commercial Procurement, supplier or product masters,
Inventory, Sales, Finance, Purchase Bills, subscriptions, MFA, password reset,
multi-company or branch switching, production onboarding, or Flutter
authentication screens.

## Commercial context and POC context

`IAuthenticatedTraderProContext` is the Application abstraction for
commercial authority. The API middleware parses only validated bearer claims,
and Infrastructure revalidates the workspace, user, device, credential
versions, refresh-token family, company, default branch, and current role
before binding it.

Commercial selection is endpoint-metadata driven, not URL-prefix driven.
`RequireTraderProCommercialAuthorization(...)` attaches the required
commercial policy, whether authenticated context is required, whether a
revoked family is allowed for an idempotent operation, whether the response
may contain secrets, and whether secure transport is required. A future
commercial endpoint outside `/api/v1/auth` and `/api/v1/devices` therefore
receives the same authenticated context automatically. The revoked-family
exception belongs only to current-session logout metadata, so
`/api/v1/auth/logout` and `/api/v1/auth/logout/` behave identically.

The Task 5 and Task 6 temporary context remains separate. Its
`X-TraderPro-Workspace-ID` and `X-TraderPro-Device-ID` headers are accepted
only on explicitly enabled Development/Testing POC routes. Those headers are
ignored by commercial identity routes and are never authentication. An
`Authorization` header alone does not opt a POC endpoint into commercial
context. Anonymous login, refresh, activation, and bootstrap endpoints use
metadata to establish correlation and secret-response safeguards without
requesting authenticated commercial context.

## Persistent model

The `AddProductionIdentityAndDeviceSessions` migration adds:

- `WorkspaceCode` and `NormalizedWorkspaceCode` to Workspace;
- `Owner` and `Operator` role values to Platform User;
- one `UserCredential` per workspace/user;
- one current `DeviceCredential` per workspace/device;
- hash-only, expiring `DeviceActivationCode` records;
- user/device/version-bound `RefreshTokenFamily` records;
- hash-only, rotating `RefreshToken` records.

Composite foreign keys enforce workspace ownership. Unique indexes cover
workspace code, workspace/login, credentials, activation-code hash, and
refresh-token hash. Checks enforce positive versions, expiry ordering,
revocation shape, activation one-time state, and refresh rotation shape.
PostgreSQL triggers protect immutable ownership/facts, controlled transitions,
and physical deletion of identity/session records.

The follow-up `HardenProductionIdentitySessionSecurity` migration preserves the
original Task 7A migration and adds:

- a composite refresh-token alternate key and self-foreign key, requiring
  every replacement to remain in the same workspace and family;
- a unique filtered predecessor index, so one replacement has at most one
  predecessor;
- checks against self-rotation and invalid consumed/replay states;
- controlled-update enforcement for immutable ownership, family identity,
  hashes, terminal timestamps, and rotation facts;
- successor creation-at-or-after-consumption enforcement;
- a filtered unique activation-code index allowing at most one unconsumed,
  unrevoked code per workspace/device.

Before the activation index is created, duplicate active codes are resolved
deterministically: expired or older rows are revoked and the newest valid row
is retained.

The forward-only `CrashSafeDeviceActivation` migration adds four nullable
recovery columns to `platform.device_activation_codes`, a filtered unique
`(workspace_id, redemption_idempotency_key_hash)` index, a maximum one-day
database replay bound (configuration defaults to 15 minutes), and controlled
transition rules. Historical consumed rows remain valid without recovery
material; new consumption must create the complete hashed/protected state in
one transition. Hashes/deadline are immutable, and ciphertext can only move
from present to retired.

## Workspace code

Commercial login uses a 3-64 character code containing uppercase letters,
numbers, and single hyphen-separated segments. Normalization is deterministic
and invariant: lowercase input is uppercased, while leading, trailing, or
embedded whitespace is rejected.

Existing Task 2-6 rows are backfilled as `TP-` followed by the uppercase
32-character workspace UUID without hyphens. The value is deterministic,
collision-safe, valid under the new constraint, and unrelated to Workspace
Name. New non-commercial POC workspaces receive the same UUID-derived shape.
An explicit commercial workspace can choose a reviewed human code. Database
and EF controls make the code immutable.

## Password credentials and lockout

Passwords are validated at creation with a focused policy: 10-256 characters,
not empty or whitespace-only, with no arbitrary category rules. ASP.NET Core
`PasswordHasher<PlatformUser>` creates and verifies the stored password hash.
Plaintext and reversible password storage are prohibited.

Login names normalize with invariant uppercase and are unique per workspace.
`CredentialVersion` starts positive and advances when a password changes.
Current access-token and refresh-family versions must match it, so a password
change invalidates old access and refresh authority immediately.

Failed login updates are serialized by a PostgreSQL transaction and advisory
lock. The default threshold is five failures, with a 15-minute lockout based
on injectable UTC server time. Success clears the counter and lockout.
Anonymous failures do not disclose whether the workspace, login, password,
device, or device secret was wrong.

## Device activation, reactivation, and slot reuse

An Owner issues a one-time activation code only for an existing Device in the
current workspace. Public redemption cannot create a Device row. The code and
workspace identify exactly one active row. Redemption requires a bounded
`Idempotency-Key`, returns a random Device secret, and stores only its
lowercase SHA-256 hash as the credential. Secret comparison is constant-time.

Reinstall/reactivation rotates the credential on that same Device row,
increments `SecretVersion`, clears credential revocation, updates safe device
label/platform metadata, revokes existing device refresh families, and makes
old device secrets and access tokens fail. It therefore reuses the licensed
slot and never purchases or consumes another slot.

Activation codes use at least 256 random bits in URL-safe form, although the
minimum required security boundary is 128 bits. Only their SHA-256 hashes are
stored. They have configurable short expiry, are consumed atomically, cannot
be reused, and can be revoked when a newer code is issued. The optional client
installation reference is stored only as a hash and is informational; it does
not prove physical-device identity.

Issuance and redemption take the same transaction-scoped
workspace/device-activation advisory lock. Issuance takes that lock before its
command-idempotency lock, revokes an older unused code, and inserts the new
code. Redemption safely locates a code, takes the device-activation lock,
reloads and revalidates it, then consumes it, rotates the single current
`DeviceCredential`, and revokes device families atomically. Used timestamps
must be between creation and expiry, revoked timestamps cannot predate
creation, used and revoked remain exclusive, and terminal timestamps cannot
be rewritten.

ADR-0013 closes the committed-response-loss gap. The redemption transaction
also stores the SHA-256 idempotency-key hash, a canonical request hash, a UTC
replay deadline, and an ASP.NET Core time-limited Data Protection ciphertext
containing the exact activation result. The protected payload binds Workspace, activation
code, Device, idempotency/request hashes, secret version, activation time, and
deadline. The default recovery window is 15 minutes after redemption, using
the configured activation-code duration. An exact retry under the same lock
returns the same logical result without another rotation, family revocation,
or audit. Another key sees `DEVICE_ACTIVATION_ALREADY_USED`; the same key with
changed input sees `IDEMPOTENCY_PAYLOAD_CONFLICT`; expiry sees
`DEVICE_ACTIVATION_RECOVERY_EXPIRED`; and a live result that cannot be
unprotected sees retryable `DEVICE_ACTIVATION_RECOVERY_UNAVAILABLE`.

The raw Device secret and raw idempotency key are never stored in PostgreSQL.
The ciphertext embeds the same bounded cryptographic lifetime, so an abandoned
row or restored backup cannot be unprotected indefinitely even with old keys.
Successful login confirms possession and clears the ciphertext. Expired retry
and later reactivation retire it. The stored deadline remains authoritative
after backup restore, so backup age cannot extend replay. A disabled Device
blocks both first redemption and recovery. Database failure rolls back the
whole attempt, allowing the same key/request to retry safely.

## Login and access token

Login requires workspace code, normalized login, password, Device ID, and
device secret. Workspace, user, and device must all be active and
workspace-consistent. A successful transaction resets lockout state, marks
the device credential used, creates one refresh family and initial token, and
commits one safe audit event.

The signed HMAC-SHA-256 JWT is short-lived (15 minutes by default), validates
issuer, audience, signature, expiry, and zero clock skew, and contains:

- `sub`: User ID;
- `wid`: Workspace ID;
- `did`: Device ID;
- `cid`: Company ID;
- `bid`: default Branch ID;
- `role`: issued role;
- `ucv`: user credential version;
- `dsv`: device secret version;
- `sid`: refresh-token family ID;
- `jti`: unique token ID.

UUID claims use canonical `D` format. Tokens contain no password, device
secret, activation code, or refresh token.

## Database context revalidation

JWT claims locate authority but are not sufficient authority. Each protected
request reloads current data and fails closed unless:

- the workspace, user, and device are active and co-owned;
- user and device credentials exist and their versions match;
- the refresh family belongs to the same user/device/workspace, is active,
  and has not expired;
- exactly one active Company exists and exactly one active default Branch
  belongs to it;
- the token Company and Branch claims still identify that exact context.

The bound role comes from the current User row, not a stale role claim.
Disabling a principal, changing a password, reactivating a device, or revoking
the family invalidates existing access immediately.

## Refresh rotation and safe replay

Raw refresh tokens are cryptographically random and returned to the client but
stored only as unique SHA-256 hashes. One login creates one family bound to
workspace, user, device, user credential version, and device secret version.
Every normal refresh rotates token A to token B inside a PostgreSQL transaction
protected by a database advisory lock.

For response-loss recovery, consumed A retains only a Data
Protection-protected copy of raw B and a short replay deadline (30 seconds by
default). A retry of A is safe only when the exact B row exists in the same
workspace/family, is active and unconsumed, and the unprotected replacement
verifies against B's stored hash. It returns that same B and does not grow a
second chain or emit a duplicate success audit. Concurrent identical requests
serialize to the same result.

When B is consumed to produce C, A's protected replay ciphertext and deadline
are cleared in the same transaction while A's immutable rotation facts remain.
Expired replay material is also cleared opportunistically during family
activity. Therefore presenting A after B has rotated to C is suspicious reuse,
even if A's former deadline would not yet have elapsed. A missing, revoked,
consumed, cross-family, cross-workspace, or hash-invalid replacement likewise
revokes the family, emits
`Platform.Identity.RefreshTokenReuseDetected` exactly once, and returns HTTP
409 `REFRESH_TOKEN_REUSE_DETECTED`.

The Data Protection application name is
`TraderPro.AgriSuite.Identity`. Its key ring must use a configured persistent
absolute path. Testing may use isolated temporary paths; Production rejects a
missing, relative, or temporary-directory key ring.

## Session lock contract

All security-operation serialization uses PostgreSQL transaction-scoped
advisory locks; process-local locks are not a correctness boundary. Locks are
acquired in this common order:

1. development bootstrap;
2. user session;
3. workspace/device activation;
4. device credential;
5. token family;
6. token identity;
7. command idempotency.

Collections of family IDs are distinct and sorted by UUID before locking.
Login and logout-all share the user-session lock, so logout-all observes and
revokes every family created before its serialization point. Login and
reactivation share the activation-device and device-credential locks. Refresh, logout, logout-all,
reactivation, and suspicious-reuse handling share family locks, so logout
cannot leave a concurrently rotated token usable and reactivation cannot leave
an old device-secret-version family usable. Token-identity locks additionally
serialize presentation of the exact same refresh token.

## Logout and roles

`POST /api/v1/auth/logout` revokes the current family. Retrying that exact
operation with the same otherwise-valid JWT remains a no-op success, while
all other protected endpoints reject the revoked family immediately.
`POST /api/v1/auth/logout-all` revokes active families for only the current
workspace/user. Material audit events are created once.

The policies are:

- `TraderProCommercialUser`: Owner or Operator;
- `TraderProOwner`: Owner only;
- `TraderProOperatorOrOwner`: Owner or Operator.

The Owner activation-code endpoint requires `TraderProOwner`, an existing
same-workspace Device, and `Idempotency-Key`. A successful idempotency record
stores only Device ID/expiry metadata, never the raw activation code. Because
the secret cannot be safely reconstructed from the generic Task 5 response
store, repeating the same completed key returns HTTP 409
`DEVICE_ACTIVATION_CODE_RESPONSE_NOT_REPLAYABLE`, containing only safe
metadata. The original code remains active. Callers may issue a replacement
with a new logical key, which revokes the older unused code.

Only an endpoint that actually requires `TraderProOwner` maps a forbidden
request to `OWNER_ROLE_REQUIRED`; other commercial authorization failures use
`AUTHORIZATION_DENIED`. Authentication and commercial-context failures remain
HTTP 401 and disclose no cross-workspace state.

Commercial middleware accepts a supplied `X-Correlation-ID` only when it is a
valid UUID. That UUID is preserved across success, validation, authentication,
authorization, conflict, and refresh-reuse responses and material audits.
Invalid input is replaced with a generated safe UUID and is never reflected.
Problem handling resolves commercial correlation before the independent spike
correlation, preserving existing POC behavior.

Activation redemption, login, refresh, activation-code issuance, and
development bootstrap responses always carry `Cache-Control: no-store` and
`Pragma: no-cache`, including relevant errors. This prevents shared or private
caches from retaining raw activation codes, device secrets, access tokens,
refresh tokens, or bootstrap credentials.

## Configuration and startup rules

Configuration keys under `TraderPro:Authentication` are:

- `Issuer`;
- `Audience`;
- `AccessTokenMinutes`;
- `RefreshTokenDays`;
- `RefreshReplaySeconds`;
- `SigningKey`;
- `DataProtectionKeyRingPath`;
- `LockoutFailureLimit`;
- `LockoutMinutes`;
- `ActivationCodeMinutes`;
- `RateLimitingEnabled`;
- `RequireHttps`;
- `ForwardedHeadersEnabled`;
- `TrustedProxyAddresses`.

Standard environment names replace colons with double underscores. No signing
key or Data Protection material is committed. The Base64 signing key must
decode to at least 256 bits. Production startup fails for missing issuer,
audience, strong signing key, or safe persistent key-ring path. It also fails
if HTTPS enforcement is disabled, if trusted forwarding is enabled without
explicit valid proxy addresses, or if the identity bootstrap or Task 5/6 spike
flags are enabled.

Secret-bearing identity endpoints reject insecure Production requests before
accepting or returning secrets. Development and Testing may explicitly set
`RequireHttps=false` for local work. TLS termination at a reverse proxy is
recognized only when forwarded-header processing is enabled and the immediate
proxy's exact IP is allowlisted; arbitrary client
`X-Forwarded-Proto` values are not trusted. The built-in middleware processes
only the forwarded protocol with a one-hop limit.

Built-in fixed-window rate limits apply to login, activation redemption, and
refresh when enabled. A rejection uses the normal safe TraderPro envelope and
HTTP 429. Rate limiting is independent of credential lockout.

## Development bootstrap and audit

`POST /api/v1/spikes/identity/bootstrap` is false by default, available only
in Development/Testing when explicitly enabled, and never mapped in
Production. It idempotently creates one local workspace, company/default
branch, Owner, Operator, and two pre-existing Device rows, and returns fresh
one-time codes. Passwords must be supplied privately in the request; no
default password is committed.

Material audit actions are DeviceActivated, DeviceReactivated,
LoginSucceeded, RefreshTokenReuseDetected, SessionLoggedOut,
AllSessionsLoggedOut, and DeviceActivationCodeIssued. Audits include only
minimal identifiers/correlation. They never include raw credentials or
tokens. Routine refresh is intentionally not noisy.
Activation-result recovery is also intentionally not audited as a second
logical activation; the first committed activation/reactivation has exactly
one material audit.

## Threat boundary and limitations

This foundation prevents request bodies and temporary POC headers from
selecting commercial authority, but it is not a standalone identity server.
It does not provide MFA, password-reset delivery, breached-password checks,
hardware attestation, automatic device fingerprint trust, RLS, or onboarding
UI. A client installation reference is explicitly not a hardware identity.

Task 7C2B3 consumes this identity boundary in the production Flutter entry.
It journals the exact ADR-0013 activation transaction before send, keeps
Device and refresh credentials in a dedicated OS-backed namespace, retains
access only in memory, serializes refresh with one 30-second predecessor
replay, and resolves authority through `/auth/me`. The encrypted Commercial
database permanently binds API origin, Workspace, Company, default Branch,
User, and Device; cached role/display fields are not authority. Logout retains
the Device/database/key/binding, while a definite inactive Device retires the
local secret. Task 6B behavior remains separate and unchanged.
