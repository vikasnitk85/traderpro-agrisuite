# ADR-0013: Crash-safe Device activation and identity contract

- Status: Accepted
- Date: 2026-08-09
- Task: 7C2B3.0

## Context

Activation previously committed the activation-code consumption, Device
credential creation/rotation, refresh-family revocation, and audit before the
HTTP response carrying the only raw Device secret reached durable mobile
storage. If that successful response was lost, retrying the consumed code
returned `DEVICE_ACTIVATION_ALREADY_USED`. The Device had a valid new secret
that neither the server nor client could recover, so an Owner had to issue a
new code and rotate again.

The correction must preserve one-time activation capabilities, hash-only
credential storage, current PostgreSQL serialization, one logical audit, and
the existing non-replayable Owner code-issuance response.

## Options considered

### A. Server-protected replayable activation result

The client supplies one bounded `Idempotency-Key`. The redemption transaction
stores its SHA-256 hash, a canonical request hash, a replay deadline, and an
ASP.NET Core time-limited Data Protection ciphertext containing the exact
result. Exact
retries under the existing advisory locks unprotect and validate that result
against current database authority.

This is the smallest compatible correction. It reuses the persistent Data
Protection key ring already required for refresh response-loss recovery and
does not place raw secrets in PostgreSQL.

### B. Client-generated Device secret

The client could generate and durably store the Device secret before calling
the API, while the server stores only its hash. This naturally survives a lost
response, but it moves credential generation and pre-request journaling into
the not-yet-implemented B3 client, changes the public credential protocol, and
still needs idempotent recovery for server-assigned Device/version metadata.
It is viable only as a future versioned protocol, not as this backend hardening
task.

### C. Separate recovery capability/result resource

A new opaque recovery token and result table could identify an activation
transaction. It adds another bearer capability, endpoint/resource lifecycle,
foreign keys, retention work, and client state while providing no security or
correctness advantage over binding the protected result to the existing
activation-code row.

## Decision

Select option A.

`POST /api/v1/auth/device-activations/redeem` requires exactly one
`Idempotency-Key` of 1-200 non-whitespace characters. The B3 client must create
and journal at least 128 cryptographically random bits for it before sending
the first request. The key is a transaction identifier and recovery
capability; PostgreSQL receives only its SHA-256 hash for durable state and
advisory-lock scope. A canonical SHA-256 request hash
binds the normalized Workspace, activation-code row, Device, hashed optional
installation reference, trimmed label, and trimmed platform.

The first successful redemption stores, in the same atomic transaction:

- the key hash and request hash;
- a Data Protection ciphertext bound by payload and protector purpose to
  Workspace ID, activation-code ID, Device ID, both hashes, Device secret,
  activation timestamp, secret version, and replay deadline;
- a replay deadline 15 minutes after redemption by default, using the same
  configured duration as activation-code issuance;
- the normal credential, activation-code, family-revocation, and single audit
  changes.

PostgreSQL never stores the raw Device secret or raw idempotency key. The
ciphertext is authenticated, embeds a cryptographic lifetime matching the
configured window, and cannot be used without the server key ring.
Recovery also verifies the decrypted secret against the current
`DeviceCredential` hash/version. A restored database backup cannot extend the
stored UTC deadline. Successful login confirms possession and clears protected
activation replay material. Data Protection makes abandoned ciphertext
cryptographically unusable after expiry; expired retries also clear it lazily,
and a later
reactivation clears superseded material. Hashes and the deadline remain as
non-secret transaction evidence; physical deletion of identity rows remains
prohibited.

## Exact semantics

| Situation | Result |
| --- | --- |
| First valid redemption | HTTP 200, one credential create/rotation, one code consumption, one family revocation pass, one activation/reactivation audit. |
| Exact retry after committed success | HTTP 200 with the same Device ID, Device secret, activation time, and secret version while recovery is live. No write or duplicate audit. |
| Identical retry while first request is in flight | Existing transaction-scoped locks serialize both requests; both observe one logical result. |
| Committed success whose response was lost | The same key and exact request recover the committed result. |
| Same key with changed request or another activation code | HTTP 409 `IDEMPOTENCY_PAYLOAD_CONFLICT`; no mutation. |
| Consumed code with another key | HTTP 409 `DEVICE_ACTIVATION_ALREADY_USED`; no secret disclosure. |
| Retry after recovery deadline | HTTP 409 `DEVICE_ACTIVATION_RECOVERY_EXPIRED`; ciphertext is retired and a new Owner code is required. |
| Ciphertext unavailable with a live deadline | HTTP 503 retryable `DEVICE_ACTIVATION_RECOVERY_UNAVAILABLE`; no rotation. This covers a transient wrong/unavailable Data Protection key ring. |
| Wrong/revoked code or Workspace | HTTP 401 `DEVICE_ACTIVATION_INVALID`. |
| Unused expired code | HTTP 409 `DEVICE_ACTIVATION_EXPIRED`. |
| Disabled Device | HTTP 409 `DEVICE_NOT_ACTIVE`; code remains unconsumed. |
| Temporary database failure | HTTP 503 retryable `TEMPORARY_COMMAND_FAILURE`; the transaction rolls back and the same attempt can retry. |
| New Owner-issued code | Existing issuance behavior remains. Redeeming the new code rotates once, revokes old families, and supersedes old recovery material. |

Owner activation-code issuance continues to return the raw code once. An exact
issuance-key retry remains HTTP 409
`DEVICE_ACTIVATION_CODE_RESPONSE_NOT_REPLAYABLE`; changing that separate
capability contract is unnecessary.

## Identity HTTP contract freeze

`contracts/openapi/traderpro-commercial-mobile.v1.json` is executable for
activation, login, refresh, logout, logout-all, `/auth/me`, and Owner code
issuance. It freezes exact routes, methods, route constraints, authorization,
success shapes/statuses, required headers, and endpoint-specific status/error
sets. Tests compare the union of frozen identity codes to active production
identity source, so missing and obsolete codes fail the contract gate. Refresh
contains a real serialized JSON example; Owner issuance contains its exact
response example.

The refresh protocol is unchanged: the exact consumed predecessor can recover
the same replacement for 30 seconds. After 30 seconds the mobile policy is an
explicit login, not another backend recovery behavior.

## B3 mobile decisions recorded, not implemented here

- Permanently bind one profile to the exact API origin, Workspace, Company,
  default Branch, User, and Device.
- V1 has no user, Company, or Branch switching.
- B2 `installationId` may be sent only as the non-secret informational
  `clientInstallationReference`; it is never authentication authority.
- Use secure-store namespace `traderpro_commercial_identity_v1` and journaled
  Device/refresh credential replacement. Access tokens remain memory-only.
- The Device secret persists through normal logout. Logout clears only access
  and refresh credentials.
- Context mismatch blocks without mutation. The offline state is
  `BOUND_OFFLINE_REVALIDATION_REQUIRED`.
- Refresh access 60 seconds before expiry through one serialized coordinator.
  A predecessor failure after the backend 30-second window requires explicit
  login.
- Use `dart:io` HTTP, require the `TRADERPRO_API_BASE_URL` build define, and do
  not add certificate pinning.
- B3 adds production Android `INTERNET` permission and global production
  `FLAG_SECURE`.
- B3 UI is minimal and identity-only. No mobile B3 implementation belongs to
  Task 7C2B3.0.

## Consequences

Activation replay now depends on the already-mandatory persistent Data
Protection key ring. Losing that key ring during a live window fails closed
and retryably. Four nullable columns and one filtered workspace-scoped unique
index are added to the existing activation-code table. The migration is
forward-only because removing the protocol would reintroduce an unrecoverable
credential state.

## Acceptance evidence

Accepted on 2026-08-09 after the disposable PostgreSQL/Testcontainers gates
passed. The Production Identity suite proved committed-response-loss recovery,
five-minute delayed recovery, recovery-window expiry, recreated-server recovery
with the same persistent key ring, wrong-key-ring fail-closed behavior,
concurrent convergence, atomic rollback/retry, disabled/invalid/expired cases,
one logical audit, predecessor-family revocation, and migration application.
The database assertions proved workspace-scoped idempotency uniqueness,
complete and bounded replay state, immutable hashes/deadline, protected-result
present-to-null-only retirement, ciphertext tamper rejection, and no plaintext
Device secret, activation code, idempotency key, or canonical request in
durable identity/audit state. All 31 selected Production Identity cases and all
22 selected Commercial Receiving cases passed.

The refresh policy remains unchanged: exact-predecessor recovery is available
for 30 seconds; after that, the client requires explicit login.
