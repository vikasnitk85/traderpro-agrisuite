# ADR-0014: Production mobile identity and context binding

- Status: Accepted
- Date: 2026-08-17
- Task: 7C2B3

## Context

ADR-0013 freezes the crash-safe server activation contract. ADR-0011 and
ADR-0012 provide a separate encrypted Commercial database and fail-closed
Android startup, but schema 1 deliberately contains no account binding or
mobile credentials. The production Flutter entry therefore needs to consume
the frozen identity API without allowing the POC database, request headers,
JWT claims, cached role, or local installation identifier to become authority.

The client must also survive process death during Device activation and
refresh rotation. A lost successful response cannot cause a new activation
transaction or an unbounded refresh loop, and a late network response after
logout or reactivation cannot resurrect a session.

## Decision

Implement one production identity composition owned by
`CommercialSecureRuntime`:

- validate the required `TRADERPRO_API_BASE_URL` before opening storage;
- require HTTPS outside an explicit debug-only HTTP define;
- use one bounded `dart:io` transport and treat access tokens as opaque,
  memory-only bearer capabilities;
- journal the exact ADR-0013 activation attempt before its first request;
- keep Device and refresh credentials in a dedicated OS-backed namespace,
  `traderpro_commercial_identity_v1`, using versioned A/B slots and a verified
  head promotion;
- preserve the Device credential through logout and retire it only after a
  definite inactive-Device result or successful reactivation;
- use one runtime-owned, single-flight refresh coordinator with a 60-second
  access-token skew, a session epoch, and one exact predecessor replay inside
  the server's 30-second recovery window;
- persist and verify a replacement refresh credential before publishing its
  access token;
- use `GET /api/v1/auth/me`, not JWT decoding, as current identity authority;
- permanently bind the encrypted database to normalized API origin,
  Workspace, Company, default Branch, User, and Device;
- permit display name, role, Device label, and confirmation time to update in
  a separate snapshot while making the immutable tuple update/delete-proof;
- fail closed and clear only the local access/refresh session on mismatch;
- expose a non-authorizing `BOUND_OFFLINE_REVALIDATION_REQUIRED` state when a
  prior binding/snapshot exists but current cloud identity cannot be reached;
- show only minimal activation, login, status, recovery, and logout UI; and
- start no master, Receiving, outbox, event, Inventory, or Finance service.

Commercial Drift schema 2 adds only `commercial_identity_binding` and
`commercial_identity_snapshot`. Migration `1 -> 2` is explicit and
transactional; the B2 foundation contract version remains 1 and is verified
independently of Drift's schema version.

## Crash and race protocol

Activation writes the complete secret-bearing attempt and cryptographically
random idempotency key to secure storage before sending. An ambiguous outcome
retains that exact attempt; recovery sends the same key and body. The attempt
is cleared only after Device credential head promotion and readback, or after
a definitive server rejection.

Before refresh, the coordinator durably marks the current refresh credential
with its own SHA-256 predecessor digest and attempt time. After process death,
that exact predecessor may be replayed once while the 30-second window is
live. At or after the boundary the local refresh session is cleared and an
explicit login is required. The returned replacement is journaled and read
back before access publication. Concurrent callers join one future.

Logout, disposal, and Device credential replacement increment the session
epoch before waiting for in-flight work. Every result checks the epoch before
credential replacement and access publication. Normal logout and logout-all
complete locally even when remote outcome is ambiguous; both retain the
encrypted database, database key, binding, snapshot, and Device credential.

## Security and privacy consequences

- Passwords, activation codes, Device secrets, refresh tokens, and access
  tokens are absent from Drift and diagnostics.
- Only the identity transport writes the Authorization header.
- Android production declares `INTERNET`, disables cleartext and backup, and
  applies `FLAG_SECURE` to the activity.
- The client does not trust the installation reference, cached role, or JWT
  claims for authority.
- Secure-store/key failure does not delete ciphertext or silently create a
  replacement identity.

`FLAG_SECURE`, OS-backed storage, and encrypted SQLite do not protect against
a rooted Device, arbitrary live-process instrumentation, or a compromised
server/key ring. MFA, password reset, hardware attestation, RLS, B4 business
authorization windows, and business sync remain separate work.

## Alternatives rejected

- Persisting access tokens: increases recoverable bearer exposure and is not
  needed because refresh restoration is crash-safe.
- Using JWT claims as context: bypasses current database-revalidated `/me`
  authority and permanent local binding.
- Reusing the B2 database key entry or POC profile: mixes lifecycles and
  violates production/POC isolation.
- Generic POST retry middleware: can duplicate login or exceed the frozen
  activation/refresh replay contracts.
- Deleting the database on mismatch or key failure: could discard offline
  physical facts and violates the fail-closed foundation.

## Acceptance

Accepted on 2026-08-17 after the Task 7C2B3 host, architecture, contract,
schema migration, official API-24 emulator, authorized physical API-35,
release-posture, security, and protected-scope gates passed. The live Device
matrix found and corrected a PostgreSQL timestamp-precision mismatch in exact
activation replay; focused regression tests and both complete Device matrices
then passed against the correction. B4 business behavior remains outside this
decision and was not started.
