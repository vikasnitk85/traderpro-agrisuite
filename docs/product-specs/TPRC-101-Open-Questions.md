# TPRC-101: Commercial Receiving Open Questions

- Status: Partially resolved; OQ-01, OQ-06, OQ-08, and focused OQ-04 resolved for Task 7C1
- Date: 2026-08-01
- Scope: Requirements absent from committed TPCL-101/TPFS-101 evidence

## Status

OQ-01, OQ-06, OQ-08, and the focused OQ-04 transfer authority are resolved by
the approved Task 7C1 decisions below. The remaining questions have no implied
default or placeholder authorization.

## Purpose

This register contains only genuinely unresolved Commercial Receiving matters.
It is intentionally not a list of ideas or enhancements. A future answer must
cite the original TPCL-101/TPFS-101 text or an explicitly approved replacement
decision. Until then, implementations must preserve the fixed two-state core
and omit the unresolved behavior.

## Scope

Questions cover the production display reference, lifecycle exceptions,
ownership transfer/recovery approval, capacity/validation limits, and optional
header fields.

## Non-goals

This document does not ask whether Task 7C should implement settlement,
Inventory, Finance, Purchase Bills, payables, Sales, or official purchase
finalization; those capabilities are already outside scope. It does not reopen
authenticated authority, exact weight processing, immutable snapshots,
one-device editing, cloud numbering, or encrypted-storage requirements.

## Authoritative sources

- `AGENTS.md`
- The Task 7C0 binding direction dated 2026-08-01
- ADR-0001 through ADR-0009
- TPTECH-001.12 through TPTECH-001.21
- TPRC-101 requirements baseline

The missing full TPCL-101 and TPFS-101 documents remain the expected primary
business source.

## Questions requiring product-source resolution

| ID | Question | Why unresolved | Safe behavior until resolved | Required before |
| --- | --- | --- | --- | --- |
| OQ-02 | May an in-progress Session be cancelled; who may cancel it; and what happens to already captured Entries? | Task 4 explicitly deferred cancellation and no commercial source supplies the transition. | Do not add `CancelledBeforeSubmission`, a cancellation endpoint, or UI. Preserve Entries. | Implementing cancellation in any task. |
| OQ-03 | How is a submitted Session corrected or returned for correction? | Submitted snapshots must be immutable, but no source chooses reject-and-clone, reversal, amendment, or a new Session. | `SubmittedForSettlementReview` remains immutable and terminal for Task 7C. Do not add `RejectedForCorrection`. | Any post-submission correction workflow. |
| OQ-05 | What are the business maxima for active Sessions per Device/company, Entries per Session, bags per Entry, and Session duration? | Existing limits are POC/transport/physical numeric constraints, not approved business limits. | Enforce only safe technical batch/string/numeric limits and make them distinct from business maxima. | Pilot capacity configuration and UI validation. |
| OQ-07 | Which additional Session header fields, if any, are required? | Supplier, destination, policy, and optional vehicle are established; driver, broker, order, remarks, source location, rate, and similar fields are not. | Add none. | Adding any additional production field/API/UI. |
| OQ-09 | When a captured master version is stale but the same master remains Active, what explicit recovery choices may an authorized user make? | Automatic substitution/reclassification is prohibited; the allowed human decision is not specified. | Return `NeedsAttention`, retain the original immutable payload, and expose no auto-fix. | Implementing stale-master recovery commands/UI. |
| OQ-10 | When an old ownership generation contains queued physical facts, what explicit adoption/re-entry/reconciliation procedure is allowed? | Old-generation operations must be rejected as stale and payloads cannot be rewritten. | Preserve the facts and operation evidence in attention state. | Implementing cross-generation recovery. |
| OQ-11 | May an Operator override the Company Procurement Settings default destination or Weight Policy, and if so under what authority and audit rules? | The settings defaults exist, but no product source defines who may override them, allowed alternatives, reason capture, or whether override is per Session. | Task 7C1 accepts only the configured default destination and Weight Policy from the exact captured settings revision; expose no alternate route, flag, or UI. | Any destination or Weight Policy override behavior. |
| OQ-12 | How may a non-editor Operator discover or monitor Sessions being edited by another Device? | Owner monitoring is defined, but company membership alone is not authority for one Operator to observe another Session. | Operator event reads are limited to events targeted to its Device while it is the active editor; expose no company-wide Operator list/live view. | Any non-editor Operator discovery, broadcast, list, or live-view access. |

## Resolved for Task 7C1

- **OQ-01:** Commercial Receiving uses cloud-assigned automatic references.
  The default template is `RCV-{SEQ:000000}`. V1 permits exactly one sequence
  token, optional year/month tokens, positive starting numbers, and `Never`,
  `CalendarYear`, or `Monthly` reset. Calendar-year templates require a year;
  monthly templates require year and month. Issued references and captured
  policy versions never change; manual/not-required numbering is not enabled.
- **OQ-06:** A saved Entry requires positive raw and processed weight and a
  positive bag count. Zero live-scale observations are not Entries. Submit
  requires an in-progress Session, exact next sequence, current/reacquired
  lease, at least one accepted Entry, and a positive processed total.
- **OQ-08:** Lease duration is 60 minutes and the foreground-online heartbeat
  target is 10 minutes. Server time is authoritative. Expiry preserves durable
  ownership and physical facts. The same Device/generation reacquires a new
  lease; another Device requires Owner-authorized transfer.
- **OQ-04 (focused Task 7C1 authority):** A different-device transfer is
  Owner-only, requires an active credentialed target Device, a safe reason,
  expected Session version and ownership generation, and commits generation
  advancement, lease clearing, audit, and Owner/old/new Device events atomically.
  The Owner never receives a target lease. The target Device authenticates as
  an Operator and acquires its own new lease without changing generation; an
  already-valid lease cannot be arbitrarily rotated. Broader emergency/consent
  policy remains future scope.

## Resolved in Task 7C0 and therefore not open

- Persisted lifecycle states are `ReceivingInProgress` and
  `SubmittedForSettlementReview` only.
- Ownership lease expiry is an attention condition, not a business state.
- Same-device lease reacquisition may occur only while the ownership generation
  is unchanged.
- Every different-device ownership change is explicit, audited, PostgreSQL-
  serialized, and increments the generation.
- Active Owners may read safe company-monitoring `CommercialMobileSync`
  broadcasts. An authenticated Device may read events targeted to it while it
  is the active editor for the related Session; an Operator has no broader
  entitlement, and same-company membership does not reveal unrelated Session
  events.
- The operation batch maximum is 50; this is a transport safety limit, not a
  business Entry maximum.
- POC headers, POC event streams, POC references, and the unencrypted POC
  database are never promoted to commercial authority.

## Data ownership

The Product owner controls answers to business questions. Architecture and
Security may set safety constraints but must not silently choose commercial
behavior. Existing cloud and mobile records remain under the ownership rules
in TPRC-101 while a question is open.

## Transaction boundaries

No open answer may weaken the already frozen atomic Start, Entry, Submit,
ownership-change, local-capture, or event-application transactions. A future
decision that adds a command must define its own audit, idempotency, and
rollback boundary before implementation.

## Failure behavior

If required source text is unavailable, fail the affected implementation gate
closed and omit the behavior. Do not use the POC as a default. Existing local
facts remain immutable and visible for reconciliation.

## Security implications

Transfer authorization, stale-fact recovery, lease timing, and optional PII
fields require security review. Any future field added to an event must pass a
safe-snapshot/PII allowlist review. No open question permits plaintext local
storage or caller-supplied tenant/device authority.

## Migration implications

The two-state core can be migrated independently. Cancellation, correction,
extra header fields, and cross-generation recovery must not receive placeholder
columns or enum values. Add them only through later reviewed, forward-only
migrations after source resolution.

## Future implementation dependencies

- OQ-01, OQ-06, and OQ-08 are satisfied for Task 7C1 and now constrain Task
  7C2 implementation.
- OQ-04 is resolved for Task 7C1 as Owner-only transfer with an explicit safe
  reason, expected Session version, and expected ownership generation.
- OQ-02, OQ-03, OQ-07, OQ-09, OQ-10, OQ-11, and OQ-12 gate only their respective
  optional behavior; their absence must not be filled by speculation.

## Unresolved questions

OQ-02, OQ-03, OQ-05, OQ-07, and OQ-09 through OQ-12 remain open. OQ-04 has the
focused Task 7C1 Owner-transfer resolution. New questions require source
evidence and an identified implementation gate.

## Task 7C1 finalization clarifications

The following are closed implementation contracts, not open product defaults:
recursive immutable-payload capability rejection; retryable waiting outcomes
for blocked same-Session batch items; serialized and terminal claim states;
locked master snapshot validation; the exact ownership transition state
machine; legacy replay versus explicit non-replayable classification; the
later Session/Ownership monitoring timestamp; acquisition/reacquisition
attention; UUIDv7 claim/reservation checks; and forward-only Task 7C1 database
recovery by backup restore. These clarifications add no settlement or posting
behavior and do not resolve the remaining commercial-policy questions.

Reference sealing is likewise closed implementation behavior: one reservation
per Session, structural Start rejection before allocation, immutable
reservation policy snapshots across later policy updates, and canonical
claim/reference/master lock ordering. A numbering gap is intentional only
after a structurally valid command has reserved a number and then encounters a
business or infrastructure failure.
