# TPRC-101: Commercial Receiving Open Questions

- Status: Open; decisions are required only where indicated before implementation
- Date: 2026-08-01
- Scope: Requirements absent from committed TPCL-101/TPFS-101 evidence

## Status

Open. No listed question has an implied default or authorizes placeholder
implementation.

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
| OQ-01 | What is the final rendered production cloud-reference format and reset scope? | No committed source establishes prefix, width, date/fiscal period, branch component, or gap policy. | Store an immutable company-scoped numeric allocation and opaque rendered reference; never use `RS-POC-*`. | Enabling the production renderer in Task 7C1. |
| OQ-02 | May an in-progress Session be cancelled; who may cancel it; and what happens to already captured Entries? | Task 4 explicitly deferred cancellation and no commercial source supplies the transition. | Do not add `CancelledBeforeSubmission`, a cancellation endpoint, or UI. Preserve Entries. | Implementing cancellation in any task. |
| OQ-03 | How is a submitted Session corrected or returned for correction? | Submitted snapshots must be immutable, but no source chooses reject-and-clone, reversal, amendment, or a new Session. | `SubmittedForSettlementReview` remains immutable and terminal for Task 7C. Do not add `RejectedForCorrection`. | Any post-submission correction workflow. |
| OQ-04 | Who may authorize ownership transfer/recovery, is editor consent required, and what reason/evidence is mandatory? | Only explicit, audited transfer with generation increment is fixed. | No other Device takes ownership automatically. Same-device lease reacquisition is permitted when generation is unchanged. | Exposing a different-device transfer/recovery command in Task 7C1/7C2. |
| OQ-05 | What are the business maxima for active Sessions per Device/company, Entries per Session, bags per Entry, and Session duration? | Existing limits are POC/transport/physical numeric constraints, not approved business limits. | Enforce only safe technical batch/string/numeric limits and make them distinct from business maxima. | Pilot capacity configuration and UI validation. |
| OQ-06 | Is zero raw/processed weight a valid Receiving Entry, and is at least one Entry required to submit? | Task 3 permits zero as a pure function but explicitly defers Receiving validation; the POC one-entry rule is not commercial authority. | Preserve the local raw fact, but do not claim business acceptance. Task 7C1 must resolve this before enabling production capture/submission. | Production Entry and Submit validation. |
| OQ-07 | Which additional Session header fields, if any, are required? | Supplier, destination, policy, and optional vehicle are established; driver, broker, order, remarks, source location, rate, and similar fields are not. | Add none. | Adding any additional production field/API/UI. |
| OQ-08 | What production lease duration, renewal lead time, grace behavior, and recovery escalation SLA apply? | The five-minute POC policy is explicitly prohibited from promotion. | Use server time and configurable values; do not freeze a POC-derived duration. | Pilot configuration in Task 7C1/7C2. |
| OQ-09 | When a captured master version is stale but the same master remains Active, what explicit recovery choices may an authorized user make? | Automatic substitution/reclassification is prohibited; the allowed human decision is not specified. | Return `NeedsAttention`, retain the original immutable payload, and expose no auto-fix. | Implementing stale-master recovery commands/UI. |
| OQ-10 | When an old ownership generation contains queued physical facts, what explicit adoption/re-entry/reconciliation procedure is allowed? | Old-generation operations must be rejected as stale and payloads cannot be rewritten. | Preserve the facts and operation evidence in attention state. | Implementing cross-generation recovery. |
| OQ-11 | May an Operator override the Company Procurement Settings default destination or Weight Policy, and if so under what authority and audit rules? | The settings defaults exist, but no product source defines who may override them, allowed alternatives, reason capture, or whether override is per Session. | Task 7C1 accepts only the configured default destination and Weight Policy from the exact captured settings revision; expose no alternate route, flag, or UI. | Any destination or Weight Policy override behavior. |
| OQ-12 | How may a non-editor Operator discover or monitor Sessions being edited by another Device? | Owner monitoring is defined, but company membership alone is not authority for one Operator to observe another Session. | Operator event reads are limited to events targeted to its Device while it is the active editor; expose no company-wide Operator list/live view. | Any non-editor Operator discovery, broadcast, list, or live-view access. |

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

- OQ-01 and OQ-08 are explicit Task 7C1 enablement gates and require resolution
  or reviewed configuration before pilot activation.
- OQ-04 is required before a different-device transfer route is enabled.
- OQ-06 is required before production Entry/Submit validation is enabled.
- OQ-02, OQ-03, OQ-07, OQ-09, OQ-10, OQ-11, and OQ-12 gate only their respective
  optional behavior; their absence must not be filled by speculation.

## Unresolved questions

OQ-01 through OQ-12 above are the complete open list for this baseline. New
questions must be added with source evidence and an identified implementation
gate.
