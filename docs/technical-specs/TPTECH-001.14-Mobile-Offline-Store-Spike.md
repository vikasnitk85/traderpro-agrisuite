# TPTECH-001.14 — Mobile Offline Store Spike

## Status and objective

This specification records the Task 4 Flutter/Drift proof of concept. The spike
demonstrates that an Android-first TraderPro client can accept a Receiving
Session and immutable weight fact locally before cloud synchronisation, commit
the fact and its future-sync operation atomically, close, and recover the same
accepted work from a file-backed SQLite database.

The spike is deliberately not the complete Receiving module. Cloud-confirmed
data remains authoritative, no official Receiving Session number is created
locally, and no UI or network synchronisation is wired to this store.

## Schema version 1

`TraderProLocalDatabase` owns exactly four tables:

| Table | Purpose |
| --- | --- |
| `local_receiving_sessions` | Local editable session identity, cloud linkage placeholders, sequence/version allocation, and rebuildable projection |
| `local_receiving_entries` | Immutable captured physical facts and their capture-time processing policy |
| `local_outbox_operations` | Immutable future-sync payloads plus an explicit delivery state machine |
| `local_sync_state` | Minimal cursor and last-successful-sync foundation for a later sync engine |

Primary keys, temporary references, operation IDs, and per-aggregate local
sequences are constrained by SQLite. Check constraints protect positive bag
counts, non-negative projection/count fields, valid precisions, controlled
processing/source/status values, and non-negative retry counts. Foreign keys
are enabled on every database open. Supporting indexes cover session entry
ordering and pending-outbox reads. Schema-version-1 triggers prevent physical
session deletion, entry updates/deletion, queued outbox deletion, and outbox
identity mutation.

Schema version 1 creates its tables, indexes, and immutability triggers through
a Drift `MigrationStrategy`. A future version must add an explicit,
non-destructive `onUpgrade` path. Destructive recreation is not an acceptable
production migration strategy.

## Exact weight representation

The raw weight string is saved exactly as supplied to the existing Task 3
`WeightProcessor`; it is never parsed through `double`, normalised, or replaced
by a display value. The processor's `processedWeightKg` is saved as canonical
six-decimal TEXT, and `displayWeightKg` is saved separately using the captured
precision.

SQLite `REAL` is not used for raw, processed, display, or cumulative weight.
Projection totals are calculated by converting canonical six-decimal strings
to `BigInt` micro-kilogram units, adding them exactly, and formatting the result
back to six-decimal TEXT.

## Local identity, sequence, and version behavior

Session and operation/entry IDs use UUID version 7. A local session receives a
temporary reference in the form
`TMP-RCV-<short-install-reference>-<compact-full-UUID>`. The 32-character UUID
component avoids relying on the former final-eight-character fragment and
retains the complete local session identity. This reference is not an official
cloud number.

The start operation uses local sequence 1. After creation,
`next_local_sequence` is 2. Each accepted entry consumes exactly one subsequent
sequence. A successful entry increments `local_version`, the active-entry
projection, the exact processed-total projection, and
`next_local_sequence`. Two sessions allocate their sequences independently.

When an aggregate ID is supplied, pending operations are always returned by
`local_sequence` ascending; device timestamps never participate in that order.
The unfiltered global query is deliberately conservative for this spike: it
returns at most one safe head per aggregate, ordered by aggregate ID. A pending
operation is eligible only when every lower sequence for that aggregate is
already `Accepted`. An earlier `Sending`, `NeedsAttention`, `Rejected`,
`Superseded`, or `Pending` operation blocks later operations, so aggregates can
progress independently without processing one aggregate out of sequence.

## Transaction boundaries

`createLocalReceivingSession()` uses one Drift transaction to insert the
session, insert its `StartReceivingSession` outbox operation at sequence 1, and
advance the next sequence. A failed outbox constraint leaves neither record.

`recordWeightLocally()` validates and processes the command, then uses one Drift
transaction to:

1. validate the editable local session and allocate its next sequence;
2. insert the immutable entry fact;
3. insert the immutable `RecordReceivingEntry` outbox operation;
4. update only the session's local version, next sequence, update timestamp,
   active count, and exact total.

An insert or projection failure rolls back all database changes. A caller may
report "Saved on device" only after this transaction returns successfully.

## Outbox payload and hashing

Record payloads use deterministic camelCase JSON in this property order:

1. `operationId`
2. `localSessionId`
3. `cloudSessionId`
4. `localSequence`
5. `productReference`
6. `bagTypeReference`
7. `bagCount`
8. `rawWeightKg`
9. `processedWeightKg`
10. `displayWeightKg`
11. `decimalPlaces`
12. `processingMethod`
13. `weightSource`
14. `capturedAtDeviceUtc`

Dart's insertion-ordered map encoding creates the JSON string. SHA-256 over the
UTF-8 JSON bytes creates a lowercase 64-character payload hash.

SQLite treats the outbox identity as immutable after insertion:
`operation_id`, `aggregate_id`, `aggregate_type`, `operation_type`,
`local_sequence`, `expected_cloud_version`, `payload_json`, `payload_hash`, and
`created_at_device_utc` cannot be updated. Only delivery metadata may change:
`status`, `attempt_count`, `last_attempt_at_utc`, `accepted_at_utc`, and
`last_error_code`.

Outbox rows in `Pending`, `Sending`, or `NeedsAttention` cannot be physically
deleted. Final retention and cleanup behavior for terminal states remains
deferred.

## Duplicate operations

A caller may supply a previously generated operation ID when retrying the same
local action. When that ID and its recomputed deterministic payload hash match,
the store returns the original entry and outbox record without allocating
another entry, sequence, or projection increment. The result identifies that it
was a duplicate.

If the operation ID exists with another aggregate, operation type, or payload,
the store throws `LOCAL_OPERATION_PAYLOAD_CONFLICT`. Changed input is never
silently treated as the original operation.

## Projection rebuilding

Receiving entries are the source of truth. The cached session projection can be
compared with an exact projection derived from active entries that are not
reversal facts. Rebuilding updates only `active_entry_count` and
`processed_total_weight_kg`; it does not consume a sequence, increment the local
version, or rewrite timestamps or entry facts.

The schema includes `entry_status` and nullable `reversal_of_entry_id` so a
later reviewed correction design can use reversal facts. This spike does not
implement a correction workflow. Until that workflow is reviewed, every entry
column—including `entry_status` and `reversal_of_entry_id`—is immutable, and
entry deletion is rejected.

Receiving Sessions also cannot be physically deleted. Formal cancellation,
archival, and cleanup semantics remain deferred.

## Outbox transitions

The focused state methods implement:

- `Pending -> Sending`
- `Sending -> Accepted`
- `Sending -> Pending` for an explicit retryable failure
- `Sending -> NeedsAttention` for an explicit non-automatic resolution path

Only failure transitions increment `attempt_count`. `Accepted` operations
cannot be restarted. Payloads and hashes never change, and all delivery state
survives close/reopen. The remaining controlled status values (`Rejected` and
`Superseded`) are reserved for a future reviewed sync/conflict workflow.

## Opening and restart recovery

The mobile opener resolves the application-support directory and opens a
file-backed ordinary SQLite database. Test openers provide real in-memory and
explicit file-backed SQLite connections. Restart tests write sessions, facts,
projections, and pending or transitioned outbox records, close the database,
open the same file through a new database instance, and verify every value.

All mapped timestamps pass through one stored-UTC parser. It rejects malformed
ISO-8601 text and parsed values without a UTC offset instead of silently
constructing a local-time `DateTime`.

## Security limitation

This spike uses **ordinary, unencrypted SQLite**. It is not production-ready
for sensitive customer data. Selecting and validating encrypted local storage
remains a mandatory pre-pilot security task.

The schema and store accept a Drift query executor rather than owning an
encryption implementation. This keeps the access layer replaceable with a
future reviewed encrypted SQLite connection without changing business
transactions or weight representations. No encryption capability is claimed
or implied by this spike.

## Explicitly deferred work

The following remain outside Task 4:

- encrypted SQLite selection, key management, and migration;
- BLE, Phoenix or other scale integration;
- HTTP APIs, SignalR, cloud sync, background workers, and outbox transmission;
- device lease/one-editor enforcement and full conflict resolution;
- authentication, device enrolment, subscription enforcement, and backend
  permission checks;
- supplier/product masters and complete Receiving UI/workflows;
- inventory movements, settlement, bills, finance, and atomic cloud posting;
- PostgreSQL or EF Core schema changes.
