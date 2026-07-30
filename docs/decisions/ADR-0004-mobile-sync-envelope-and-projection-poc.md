# ADR-0004: Mobile sync envelope and projection POC

- Status: Accepted for the Task 6B development proof of concept
- Date: 2026-07-29
- Scope: Debug-only Flutter synchronization and two-device field evidence

## Context

Task 4 already persists raw weights, captured processing policy, immutable
receiving entries, immutable operation identity, deterministic payload JSON,
and SHA-256 hashes before any network request. Task 6A adds a cloud lease,
ordered mobile-operation endpoint, durable event cursor, read-only monitoring,
and direct heartbeat, approval, and finalization commands.

Putting the server lease into an already-stored Task 4 payload would change its
bytes and hash after the physical fact was captured. Letting a widget rebuild
an operation or command after a timeout would also defeat server idempotency.
Using owner event data as an editable aggregate would confuse a cloud
projection with Mobile A's local physical facts.

## Decision

Task 6B keeps Task 4 operation identity, operation type, aggregate identity,
local sequence, `payloadJson`, and `payloadHash` immutable. The current lease
is stored in a separate receiving-session cloud-state table. At send time the
sync engine copies the immutable fields into the Task 6A envelope and adds the
lease beside `payloadJson`. It never decodes and re-encodes the stored payload.

`StartReceivingSession` is sent alone and without lease metadata. Only after
the direct response and the operation delivery state are committed together
does the engine send dependent record or submit operations. This preserves the
only channel through which the editor receives its lease; MobileSync events
never contain a lease ID.

An operation UUID remains stable across timeout, process termination, and
restart. Ambiguous delivery moves only mutable delivery state back to
retryable. The next run sends the same UUID, sequence, payload bytes, hash, and
current envelope lease. `Accepted` and `PreviouslyProcessed` complete the same
local operation.

MobileSync events are stored exactly as received in an inbox scoped by
normalized backend base URL and Workspace ID. Event insertion, projection
application, applied marking, and cursor advancement share one Drift
transaction. A malformed known event rolls back the whole transaction, so the
cursor cannot skip it. Unknown future events are retained, marked
`SkippedUnknown`, and advance safely. Aggregate versions never move backward.

Owner data is a read-only local projection, not a receiving aggregate.
Session-list and live-view responses refresh and verify that projection.
At most five remote entry summaries are retained. Owner presentation code has
no path to `LocalReceivingStore.recordWeightLocally()` or local submission.

Heartbeat, Approve, and Finalize use a separate persistent command outbox
because they are not Task 4 mobile operations. Each logical command stores one
UUIDv7 idempotency key and its exact session, expected version, or lease
identity. A timeout reuses the same row and key. The explicit finalization
replay moves the completed command back to pending delivery without changing
its identity or successful-response evidence.

The POC local schema advances to version 3. Direct `1 -> 3` and follow-up
`2 -> 3` migrations preserve Task 4 bytes; the latter preserves version-2 POC
rows while backfilling cloud state and commands from the active source/device.
Migration fails explicitly when that binding is ambiguous. Cloud state is
bound immutably to source and operator device; commands are bound immutably to
source and acting device. SQLite checks and triggers enforce session identity,
lease shape, command shape, and non-regressive cloud versions.

Mobile sync recovery, selection, blocking, and diagnostics are limited to the
three POC ReceivingSession operation types. Successful operation and control
responses are checked semantically below the UI. Malformed or regressive 2xx
responses return the original durable identity to retryable state rather than
recording false completion.

One app-level runtime owns every POC resource and observes lifecycle until the
enabled runtime is disposed. Only `resumed` permits foreground cycles. A
single coordinator gate serializes automatic and manual cycles before profile
lookup; poller and control gates follow the same rule. A neutral completion
callback reloads all Drift-backed controller state after successful or safely
failed cycles, without an Application-to-Presentation dependency.

Finalization replay authority comes from a completed local Finalize command
for the active source/device, not from a remote Finalized projection. Manual
capture accepts only explicit ISO-8601 UTC text; invalid input creates no
physical fact or outbox operation.

Foreground polling is used before SignalR. A three-second development timer
reduces latency, while the durable cursor remains the recovery mechanism.
Foreground-only heartbeat is acceptable for this POC because forced recovery,
background execution, and production lease policy remain explicitly deferred.

The Flutter route is available only when
`TRADERPRO_PROCUREMENT_POC=true` at compile time and the build is not release.
The route guard fails closed as well as hiding the navigation entry. Android
cleartext HTTP is enabled only in the debug manifest. Temporary Workspace and
Device headers are repeatedly labelled as development context, not
authentication.

## Consequences

- Offline physical facts remain byte-for-byte stable across cloud acceptance.
- Response loss can be resolved by Task 6A's stored idempotent result.
- A missing or expired lease leaves dependent operations in Needs Attention;
  it does not create a replacement session or steal a lease.
- Workspace/base-URL cursor state cannot be reused for another event source.
- Cloud leases and control commands cannot cross source/device context.
- Unrelated future-module outbox rows are untouched by this POC engine.
- Device identity cannot change while local work, a lease, or a control
  command remains unresolved.
- Automatic Start, entry, event, and heartbeat results become observable
  without a manual refresh.
- Route removal does not own or accidentally outlive lifecycle control;
  runtime disposal prevents later API starts and notifications.
- Polling and foreground heartbeat are simple and testable, but do not claim
  production background behavior.
- The owner cache is disposable projection data. Mobile A's immutable facts
  remain the local source for its captured work.
- Debug HTTP, temporary identity headers, bootstrap IDs, the POC route, and
  all `Poc` APIs must be removed or replaced before commercial release.

## Alternatives rejected

- Rewriting stored payload JSON with a lease breaks immutable physical-fact
  evidence and changes its hash.
- Sending entries in the same first batch as Start cannot use the direct
  server-created lease safely.
- Generating a new operation or command key after timeout can duplicate work.
- Advancing a cursor before committing the projection can permanently skip
  an event after process failure.
- Making the owner projection editable creates a second unsupported writer.
- SignalR-only monitoring cannot recover events missed while disconnected.
- Android background packages and commercial cleartext configuration exceed
  the reviewed POC scope.
