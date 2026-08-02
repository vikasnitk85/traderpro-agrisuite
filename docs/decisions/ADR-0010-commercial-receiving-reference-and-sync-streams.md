# ADR-0010: Commercial Receiving reference and sync streams

- Status: Accepted and implemented for Task 7C1
- Date: 2026-08-01

## Decision

Commercial Receiving uses a focused automatic company reference policy rather
than a general voucher engine. The default is `RCV-{SEQ:000000}` with one
sequence token, optional year/month tokens, and Never/CalendarYear/Monthly
periods. Allocation commits a focused durable reservation before the Session
transaction under a company-scoped PostgreSQL serialization point.
Reservations are keyed by immutable Start operation ID and request hash, are
never reusable by another operation, and are consumed only with their matching
Session. An exact retry reuses its reservation; failed Start may intentionally
leave a numbering gap, and no reference is returned until Session commit.
Issued references and policy-version snapshots are immutable; policy changes
affect future Sessions only and starting-number checks include reservations.

Commercial mobile recovery uses a new `CommercialMobileSync` outbox stream.
It is neither `Internal` nor the unauthenticated POC `MobileSync` stream.
Commercial event rows use immutable Company/audience metadata stored in the
one-to-one `platform.commercial_outbox_audiences` companion table and use
`OwnerBroadcast` or `TargetDevice`. Keeping those facts out of the base outbox
row preserves pre-7C1 rolling-upgrade compatibility. Writers serialize commit
order by stream contract + Workspace + Company, avoiding global company
contention. Deferred database constraints require exactly one matching audience
for each Commercial row and prohibit audience rows for Internal or POC streams.

Commercial master synchronization uses an immutable dedicated change log, not
Internal outbox payloads. Migration backfill and table triggers create safe
versioned changes through the same explicit per-master builders. The version-1
contract is camel-case, represents Active and Inactive records, encodes exact
decimals as canonical strings, and omits Supplier protected/internal fields.
Bootstrap is bounded by a company high-water sequence and then transitions to
deltas strictly after that boundary.

## Consequences

- Mobile UUIDv7 identity, external reference, Receiving reference, Purchase
  Bill number, and posting number remain distinct.
- Reference configuration remains small and deterministic.
- A committed reservation can leave an auditable gap after failed Start; it is
  never handed to a different operation.
- POC/Internal events cannot leak through authenticated commercial cursors.
- Supplier protected fields are excluded from mobile-safe facts.
- Task 7C2 can replay operations after lease reacquisition because lease
  metadata is outside immutable payloads and canonical hashes.

## Rejected alternatives

Manual/not-required Receiving numbering, gapless promises, arbitrary executable
format strings, a generic accounting voucher engine, reuse of POC `MobileSync`,
and exposure of raw Internal events are rejected for Task 7C1.

## Final hardening consequences

- Lease and expected-cloud-version capabilities are rejected recursively from
  immutable payload JSON; harmless unknown fields remain exact.
- A same-Session follower blocked by an earlier operation is retryable waiting,
  not permanently rejected, and owns no claim/reference/idempotency fact.
- Claim transitions are serialized and terminal; completed claims require
  matching completed idempotency facts.
- Snapshot trigger validation locks every referenced master. Disabled vehicle
  mode cannot carry a vehicle. Ownership has four explicit transitions and
  transfer locks/revalidates its target identity.
- Proven original Task 7C1 operations replay; unproven legacy identities are
  explicitly non-replayable and never advance the reference counter.
- Monitoring uses the later Session/Ownership update and actionable lease
  attention. Claim and reservation client identities must be UUIDv7.
- Task 7C1 database migrations are forward-only; recovery requires backup
  restore rather than an EF downgrade.
- A workspace/company/Session owns at most one reservation. Same-Session Start
  races are serialized under the company series lock and the loser receives
  `RECEIVING_REFERENCE_CONFLICT` without consuming another number.
- Reservation policy version, period, sequence, and rendering are historical.
  Current policy changes affect new reservations only; exact retry consumes
  the existing snapshot.
- Typed Start structure is rejected before allocation. Only business or
  infrastructure failure after valid allocation may create an intentional gap.
- Start and Entry use the same canonical claim/reference/session/master lock
  order in application and PostgreSQL trigger paths.
