# ADR-0003: Two-device Procurement proof of concept

- Status: Accepted for the Task 6A development proof of concept
- Date: 2026-07-29
- Scope: Non-commercial backend evidence only

## Context

Task 6A must prove that one intermittently connected mobile device can own and
order physical receiving work while a second device monitors, approves, and
finalizes it. Authentication, production authorization, SignalR, subscription
enforcement, Inventory, settlement, Purchase Bills, and Finance remain outside
this task.

The proof must reuse the Task 5 PostgreSQL idempotency and MobileSync cursor
contracts. It must also accept Task 4-style mobile operation identity and exact
weight facts without silently discarding them.

## Decision

The mobile-created UUIDv7 becomes the cloud `ReceivingSessionPoc.Id`. This
keeps one durable aggregate identity across offline capture, retries, and cloud
acceptance. PostgreSQL independently generates a display-only sequence rendered
as `RS-POC-000001`; this is not the production official-number service.

Every aggregate operation carries a positive local sequence. Start is sequence
1 and the cloud aggregate next expects sequence 2. Exact next-sequence matching
detects missing and reused offline operations without using device timestamps
as ordering evidence. Operation UUIDs are idempotency keys in one global
`Procurement.Poc.MobileSyncOperation` command scope, and the canonical hash
includes actual operation type and authoritative current device. A UUID reused
for another operation type, or replayed from another device, conflicts instead
of returning the first device's result.

Editing ownership is a renewable server-clock lease. The cloud selects the
editor device and creates the lease; request bodies cannot choose a trusted
device. Recording and submission require the same active device, matching
lease, unexpired server time, and in-progress state. Forced lease recovery is
deferred.

Lease ID is transport capability metadata, not an immutable physical fact.
Task 4 `payloadJson` and `payloadHash` remain byte-for-byte unchanged and
contain no lease ID. Start rejects envelope lease metadata; record and submit
require it. Task 6B will enrich the outgoing network envelope from local
session state without updating the Drift outbox payload. A supplied
`cloudSessionId` is accepted only when canonical and equal to the aggregate ID.

The second device has workspace-scoped read-only live view and the existing
durable event cursor. Cursor polling is used before SignalR because a durable
cursor can recover committed events after disconnection; SignalR alone cannot.

Submission clears the active lease. Approval and finalization require a
different active device from the original editor. Finalization creates one
immutable `ReceivingFinalizationPoc` completion record only. It deliberately
creates no Purchase Bill, stock movement, supplier obligation, settlement,
payable, journal, or other business posting.

The Task 5 PostgreSQL advisory-lock/idempotency transaction was extracted into
one shared focused infrastructure service. CommandProbe behavior remains
unchanged. POC aggregate change, audit, captured MobileSync payload, and
completed result commit in one PostgreSQL transaction.

Batch preflight checks only identities needed to correlate a result. Payload,
hash, type, lease, version, weight, and lifecycle checks run per operation in
request order. A failure blocks later operations for that aggregate only;
earlier commits remain and other aggregates continue.

Lease capability data is never published to MobileSync. The started event
contains editor and lease expiry for monitoring, while the new lease ID is
returned only by the direct start response.

Session workspace and original editor are immutable, and POC sessions cannot
be physically deleted. A follow-up migration adds PostgreSQL triggers and a
full state-shape constraint for in-progress, submitted, approved, and finalized
rows. Total weight overflow is a stable business conflict and rolls back the
entry, projection, audit, outbox, version, and idempotency result.

Temporary `X-TraderPro-Workspace-ID` and `X-TraderPro-Device-ID` headers are
accepted only when the general spike and Procurement POC flags are both enabled
outside Production. They are request context, not authentication. Disabled POC
routes fall through to 404 before temporary context, while the Task 5 event
cursor retains workspace-only behavior when its general spike flag is on.

## Consequences

- Offline operation identity and order can survive lost responses and client
  restarts.
- Immutable Task 4 physical payloads remain compatible while lease capability
  data stays outside durable local facts and MobileSync events.
- One device writes while another monitors without granting the monitor edit
  ownership.
- Raw strings remain unchanged and the server verifies the captured Task 3
  processing result before accepting an entry.
- MobileSync returns the exact payload committed with each command.
- PostgreSQL, not process memory, resolves concurrent identical finalization.
- The POC cannot ship commercially. Its temporary context, bootstrap, strings
  in place of masters, display reference, polling, and completion record must
  be removed or evolved through reviewed migrations and specifications.

## Alternatives rejected

- Server-replacing the client UUID would split offline and cloud identity.
- Timestamp ordering cannot safely detect missing offline operations.
- A device-controlled lease or body-supplied device identity is not
  authoritative.
- Process-local locks do not coordinate replicas or restarts.
- SignalR-only monitoring cannot recover missed events.
- Creating fake stock, bills, payables, or journals would claim business
  behavior that Task 6A does not implement.
