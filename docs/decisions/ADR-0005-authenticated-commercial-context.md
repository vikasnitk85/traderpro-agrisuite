# ADR-0005: Authenticated commercial context

- Status: Accepted for the Task 7A identity foundation
- Date: 2026-07-30
- Scope: Commercial API identity, device sessions, and current authority

## Context

Task 5 and Task 6 deliberately use temporary workspace/device headers so
development POCs can prove idempotency, event cursors, leases, and two-device
sync before authentication exists. Those caller-controlled headers are not
authentication and cannot become the authority source for commercial
Procurement or other customer data.

TraderPro also licenses pre-created Device rows. Reinstalling an app must not
silently create another row and consume another company slot. Password,
device, role, workspace, and session state can change while an access token is
still cryptographically valid, so token claims alone cannot remain
authoritative.

## Decision

Commercial API authority comes from a validated signed bearer token followed
by current database revalidation. The Application layer exposes
`IAuthenticatedTraderProContext`; API middleware parses canonical claims; an
Infrastructure resolver loads and validates the active workspace, user,
Device, password credential, device credential, refresh-token family, sole
active Company, and sole active default Branch. It binds the current database
role. Any mismatch fails closed.

Endpoint metadata selects this behavior. Commercial authorization conventions
attach the policy and context requirements to the endpoint itself instead of
maintaining a path-prefix allowlist. Metadata also scopes the revoked-family
exception to idempotent current-session logout and marks secret-bearing
endpoints for transport and cache protection. Future commercial routes receive
context by declaring a commercial policy, while POC routes never receive
commercial context merely because a bearer header is present.

User passwords use the supported ASP.NET Core password hasher. Device secrets,
activation codes, and refresh tokens are random and persisted only as
SHA-256 hashes. Hash comparison uses fixed-time comparison where a caller
supplies a raw device secret.

Devices must exist before activation. An Owner issues a short-lived one-time
code tied to one Device row. Redemption creates or rotates only that row's
credential. Reactivation increments its secret version and revokes its
sessions, but never creates another Device or subscription slot. The optional
installation reference remains informational and is not trusted hardware
identity.

Access tokens are short-lived signed JWTs. They carry exact IDs and credential
versions for efficient lookup, but those claims are locators rather than
sufficient authority. Current database role and status win over stale claims.

Each login creates a refresh-token family bound to one
workspace/user/device/version tuple. Refresh tokens rotate. PostgreSQL
transactions and transaction-scoped advisory locks serialize login,
logout-all, refresh, logout, reactivation, and identical-token presentation
with one hierarchy: bootstrap, user session, activation device, device
credential, token family, token identity, then command idempotency. Multiple
family locks are acquired in deterministic UUID order.

A short retry window protects the replacement token with ASP.NET Core Data
Protection so a lost response can be recovered without plaintext persistence.
Replay is safe only while the exact same-family replacement is active,
unconsumed, and hash-verified. Consuming the replacement clears its
predecessor's protected replay material. Presenting an older predecessor after
its successor was consumed revokes the family and records suspicious reuse
exactly once.

The database independently constrains refresh-token links to the same
workspace/family, prevents self-links and duplicate predecessors, enforces
successor timing, and keeps ownership, hashes, and established rotation facts
immutable. Activation issuance and redemption share a per-device lock, and a
filtered unique index permits only one active code per device.

Valid commercial correlation UUIDs survive every response and related
material audit; invalid values are replaced safely. Owner-only authorization
failures use `OWNER_ROLE_REQUIRED`, while other forbidden commercial
operations use `AUTHORIZATION_DENIED`. Secret-bearing identity endpoints set
`no-store`/`no-cache` on success and error responses.

Production requires HTTPS. Forwarded TLS state is accepted only through the
built-in forwarded-header middleware when it is explicitly enabled and the
immediate proxy IP is allowlisted. Development/Testing may explicitly allow
HTTP. Arbitrary forwarded-protocol headers are not an authority boundary.

The existing POC context remains an independent, feature-gated
Development/Testing mechanism. Commercial identity code does not depend on
temporary context resolvers or the current-device POC accessor. Production
refuses startup if any spike would be exposed.

## Consequences

- Request bodies and POC headers cannot override commercial workspace, user,
  device, company, branch, or role.
- Disabling users/devices/workspaces, changing a password, reactivating a
  device, or logging out invalidates current access through database checks.
- Reinstall uses an existing licensed Device slot.
- Data Protection keys must persist across API instances and restarts or safe
  replay recovery will fail; Production therefore requires an explicit safe
  key-ring path.
- A strong shared HMAC signing key must be supplied outside committed
  configuration. Key rotation is an operational follow-up.
- Database revalidation adds reads to protected requests. Caching cannot be
  introduced later without preserving immediate revocation semantics.
- Repeating a completed Owner code-issuance idempotency key cannot reproduce
  the raw secret from generic stored response data; it returns
  `DEVICE_ACTIVATION_CODE_RESPONSE_NOT_REPLAYABLE` without revoking the
  original active code. A new key may intentionally replace it.
- Login cannot be missed by a concurrent logout-all, logout cannot lose a
  concurrent refresh rotation, and device reactivation leaves no old-version
  family usable.
- Protected replay ciphertext has a bounded lifetime and is cleared once its
  successor is consumed or its replay deadline expires.
- Production deployments behind TLS termination must configure exact trusted
  proxy addresses; forwarding is disabled by default.
- Task 5/6 POC behavior stays available only behind its existing development
  gates and must not ship as commercial authentication.

## Rejected alternatives

- Trusting workspace/device headers was rejected because callers control them.
- Trusting JWT role and status until expiry was rejected because authorization
  changes and session revocation must take effect immediately.
- Creating a Device during public activation was rejected because it would
  bypass licensed-slot control.
- Storing raw refresh tokens or replacement tokens was rejected because a
  database disclosure would become an active session disclosure.
- Process-local refresh locks were rejected because correctness must hold
  across API instances.
- URL-prefix based commercial-context selection was rejected because it is
  fragile for future routes and accidentally couples authorization to routing
  layout.
- Blind trust in `X-Forwarded-Proto` was rejected because an untrusted client
  could otherwise bypass the HTTPS boundary.
- Building or adding a separate OAuth/IdentityServer product was rejected as
  unnecessary scope for this modular-monolith foundation.
