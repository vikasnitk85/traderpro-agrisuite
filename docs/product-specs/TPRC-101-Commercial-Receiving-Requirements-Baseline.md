# TPRC-101: Commercial Receiving Requirements Baseline

- Status: Approved baseline for Task 7C0; not a replacement for TPCL-101 or TPFS-101
- Date: 2026-08-01
- Scope: Commercial Receiving through submission for settlement review
- Owners: Product and Architecture

## Status

Approved as the Task 7C0 requirements baseline. It remains subordinate to any
later supplied original TPCL-101/TPFS-101 text and must be revised through an
explicit reviewed delta if that text changes a recorded rule.

## Purpose and source limitation

This document records the requirements that are demonstrably authoritative for
Task 7C0. It is a requirements baseline assembled from the Task 7C0 direction,
repository invariants, accepted architecture decisions, and committed technical
specifications. The complete locked TPCL-101 and TPFS-101 source documents are
not committed in this repository. This baseline does not claim to reconstruct
them.

Where the available sources do not establish business behavior, the matter is
left open in `TPRC-101-Open-Questions.md`. Future implementation must not treat
an open matter as implicitly approved.

## Scope

Task 7C covers this boundary:

```text
Receiving in progress
  -> immutable entry capture
  -> read-only owner monitoring
  -> submission for settlement/review
```

Task 7C0 freezes the design. Task 7C1 will implement the commercial backend and
Task 7C2 will implement secure Flutter Receiving and offline synchronization.

## Non-goals

Task 7C does not define or implement Purchase Settlement, bag reconciliation,
empty-bag deductions, DoublePlastic 0.2/0.4 kg settlement deductions, Goods
Supplied to Supplier accounting, Purchase Bills, supplier payables, Inventory
movements, Sales, Finance postings, or official purchase finalization. It also
does not complete requirements that exist only in the missing TPCL-101 or
TPFS-101 documents.

## Authoritative sources

1. `AGENTS.md`, especially the durable cloud-authority, local-first,
   immutable-fact, one-editor, cloud-numbering, exact-weight, snapshot, tenant,
   backend-authority, outbox, and module-boundary rules.
2. The binding Task 7C0 direction dated 2026-08-01.
3. ADR-0001 through ADR-0007.
4. TPTECH-001.12 through TPTECH-001.20.
5. TPRUN-001 and the Task 6B physical-field corrections in commit `7cc3d29`.
6. The committed Task 7A, Task 7B1, Task 7B2, Task 3, Task 4, Task 5, and Task
   6 implementation foundations, as evidence of existing contracts rather than
   authority to promote POC behavior.

If these sources conflict, explicit durable product rules and later accepted
commercial specifications prevail over temporary POC behavior.

## Binding-rule traceability

The following table records every binding rule explicitly supplied for Task
7C0. `Design` identifies the Task 7C0 contract, `7C1` and `7C2` identify future
implementation responsibility, and `Deferred` identifies work that must remain
outside Task 7C.

| ID | Binding rule | Task 7C0 design | Task 7C1 backend | Task 7C2 mobile | Deferred settlement/posting |
| --- | --- | --- | --- | --- | --- |
| TEN-01 | One workspace contains one company for V1. | Commercial authority and all Receiving facts bind both IDs. | Revalidate the sole active company and constrain ownership. | Bind the encrypted profile and database to the returned context. | Multi-company switching. |
| TEN-02 | One company has one authenticated default branch. | Branch is derived, never selected as authority in a command. | Persist and validate the authenticated default Branch ID. | Cache/display the authenticated branch; never override it. | Branch switching. |
| TEN-03 | A company may have multiple Business Locations. | Task 7C Start captures the Procurement Settings default destination; alternate selection is not yet approved. | Validate the configured default is same company/default branch. | Cache and capture the exact configured default. | Location inventory and destination-override behavior. |
| TEN-04 | One subscription belongs to one company workspace. | Receiving never creates or moves subscription ownership. | Continue Task 7A workspace/company authority. | Fail closed on workspace/company mismatch. | Subscription billing and plan changes. |
| TEN-05 | Commercial APIs derive workspace, company, branch, user, device, and role only from authenticated commercial context. | Authority fields are excluded from commercial payloads. | Use `IAuthenticatedTraderProContext` after database revalidation. | Send bearer credentials; do not send authority overrides. | None. |
| TEN-06 | Temporary POC workspace/device headers are never commercial authority. | POC and commercial routes, hashes, streams, and storage are separate. | Commercial routes ignore POC headers. | Commercial client never emits POC authority headers. | POC removal is separate cleanup. |
| OWN-01 | One device edits an active Receiving Session. | Durable editor ownership and generation are aggregate facts. | Serialize ownership in PostgreSQL. | Bind pending work to the authenticated Device. | None. |
| OWN-02 | Owner monitoring is live and read-only. | Monitoring is a projection with no entry mutation path. | Expose Owner-authenticated list/live-view reads. | Provide a read-only Owner surface. | Settlement controls. |
| OWN-03 | Cloud is authoritative for shared state. | Local state distinguishes immutable capture from cloud projection. | Return authoritative version, generation, lease, and status. | Reconcile cloud projections without rewriting captured facts. | None. |
| OWN-04 | Offline physical facts are preserved. | Entries and operation payloads are immutable; conflicts become attention. | Never substitute or discard a rejected offline payload. | Save entry plus operation before network I/O and retain attention rows. | Approved recovery semantics where business action is required. |
| OWN-05 | A different device must not silently take over editing. | Device changes require an explicit audited transfer/recovery. | Reject automatic acquisition by another Device. | Never infer ownership from visibility or an expired lease. | Final approval rules for transfer. |
| OWN-06 | Recovery or transfer is explicit and auditable. | Generation increments on every accepted ownership change. | Commit ownership change and audit atomically. | Present the result and keep old-generation work visible. | Product approval policy for transfer/recovery. |
| OWN-07 | The short POC heartbeat lease is not copied unchanged. | Production separates durable ownership, generation, and renewable lease; duration is configurable and unresolved. | Implement server-clock lease policy after review. | Treat expiry as reacquisition/attention, not lost work. | Final operational duration policy. |
| OWN-08 | Commercial event visibility follows least privilege. | Owners may read safe company-monitoring broadcasts; any active editor Device may read events targeted to it, and an Operator has no broader entitlement. | Enforce audience kind, target Device, role, Session, and generation after database revalidation. | Store/apply only events authorized for the bound profile. | Any discovery/monitoring of other Sessions by non-editor Operators. |
| SYN-01 | Ordered mobile operations share one global command scope. | Start/Record/Submit all use `Procurement.CommercialReceiving.MobileSyncOperation`; actual type is hashed. | The same UUID under another type is `IDEMPOTENCY_PAYLOAD_CONFLICT` and does not execute. | Generate one immutable operation UUID/type and never reclassify it. | None. |
| SYN-02 | Mutable cloud version is not offline operation identity. | Start has no generation/lease; Record/Submit require generation/lease; all omit expected cloud version. | Serialize by authenticated Device, generation, exact sequence, status, and aggregate/ownership lock. | Keep cloud version only in mutable cloud projection, never immutable outbox payload/hash. | Direct approved transfer/recovery may use expected Session/ownership versions. |
| WGT-01 | Raw weight is stored unchanged. | Entry contract stores the original decimal text. | Verify and persist exact text. | Persist exact text before sync. | None. |
| WGT-02 | Processed weight uses the captured policy snapshot. | Precision/method are immutable Session and Entry facts. | Reprocess on acceptance and compare exactly. | Process with Task 3 and submit captured policy facts. | None. |
| WGT-03 | Supported decimal places are 1, 2, and 3. | Contract uses only these values. | Enforce in domain and database. | Enforce with the shared processor. | None. |
| WGT-04 | Standard is decimal half-up. | Uses Task 3 `Standard`. | Use the shared exact processor. | Use the shared exact processor. | None. |
| WGT-05 | Floor and Ceiling are supported. | Uses Task 3 exact methods. | Use the shared exact processor. | Use the shared exact processor. | None. |
| WGT-06 | No floating-point arithmetic. | Decimal JSON values are strings; storage uses exact text/`numeric(20,6)`. | Never use `double`. | Never use `double`/SQLite `REAL`. | None. |
| WGT-07 | Product-specific Standard Bag Weights are supported. | Optional Product/Bag standard identity and facts are snapshotted. | Validate Active matching association. | Cache and snapshot the selected standard. | Use of standard content in settlement. |
| WGT-08 | Bag Type tare is distinct from Product Standard Bag content weight. | Both are separate snapshot fields; no calculation combines them in Task 7C. | Persist separately. | Display and capture separately. | Tare/content deductions and reconciliation. |
| MST-01 | Supplier must be Active. | Validate at Session start. | Return stable inactive/not-found outcomes. | Exclude inactive selections while retaining history. | None. |
| MST-02 | Restricted Supplier scope must permit the Product. | Validate and snapshot safe scope evidence at Entry acceptance. | Check current Active association and captured versions. | Cache scope and retain the original selection/payload. | Recovery decision for stale scope. |
| MST-03 | Product must be Active and purchasable. | Validate at Entry acceptance. | Reject inactive or non-purchasable selections distinctly. | Exclude from active selection lists. | None. |
| MST-04 | Bag Type must be Active. | Validate at Entry acceptance. | Enforce company ownership and status. | Exclude inactive types from new selection. | None. |
| MST-05 | Selected Standard Bag Weight belongs to the Product and Bag Type. | Validate identity, ownership, status, Product, and Bag Type. | Enforce all relationships in one acceptance transaction. | Filter candidates by selected Product and Bag Type. | None. |
| MST-06 | Destination defaults from Company Procurement Settings. | Session Start accepts only the configured destination until overrides are approved. | Validate exact settings revision and configured default; no alternate route/payload behavior. | Cache and capture the configured default without offering override. | Destination movement and override behavior. |
| MST-07 | Vehicle is optional or disabled according to settings. | The exact settings revision's mode is snapshotted; Disabled rejects a vehicle and Optional permits null or an Active vehicle. | Validate and snapshot the authoritative mode. | Render from the captured/cache settings mode. | Fleet behavior. |
| MST-08 | Weight Policy defaults from Company Procurement Settings. | Session Start accepts only the configured Active policy until overrides are approved. | Validate exact settings revision/default; no alternate route/payload behavior. | Cache and capture the configured default without offering override. | Weight Policy override behavior. |
| MST-09 | Selected Session/Entry master data is snapshotted. | Required safe identity, code/name/classification/version facts are immutable. | Capture authoritative facts only after version/status validation. | Persist the capture-time cache identity/version and display snapshot. | None. |
| MST-10 | Later master changes never rewrite historical Receiving facts. | Submitted facts never use mutable joins for reconstruction. | Query snapshots, not current names, for history. | Cache refresh never rewrites Session/Entry snapshots. | None. |
| MST-11 | Session captures the exact Procurement Settings revision. | Store `CompanyProcurementSettingsId`, `ProcurementSettingsVersionSnapshot`, and `VehicleSelectionModeSnapshot`. | Stale revision returns `NeedsAttention`; authoritative facts come only from the exact row. | Start payload carries settings ID/version and local Session retains the snapshot. | Operator override policy. |
| NUM-01 | Mobile-generated UUIDv7 is aggregate identity. | `CommercialReceivingSession.Id` is supplied by mobile and validated as UUIDv7. | Preserve it as the primary aggregate ID. | Generate once before local save. | None. |
| NUM-02 | Production cloud reference is cloud-assigned. | A database allocator and renderer are separate from aggregate identity. | Allocate once during accepted Start. | Display only after cloud acceptance. | None. |
| NUM-03 | No official commercial number is generated offline. | Local references are explicitly temporary/non-official if shown. | Ignore client attempts to choose a cloud reference. | Never populate an official number locally. | None. |
| NUM-04 | `RS-POC-000001` and other POC references are not reused. | Production format remains open pending source. | Use the new allocator/renderer only. | Do not recognize POC format as commercial. | None. |
| NUM-05 | Purchase Bill and final-posting numbers are outside Task 7C. | Receiving cloud reference is explicitly not either number. | Create neither. | Display neither. | Purchase Bill/final-posting numbering. |
| SUB-01 | Task 7C ends at submission for settlement/review. | `SubmittedForSettlementReview` is the terminal Task 7C state. | Submission atomically closes editing. | Submit once and show read-only cloud state. | Settlement review outcome. |
| SUB-02 | Entry capture is immutable. | Entry insert is append-only. | Reject update/delete and post-submission insertion. | Never update/delete captured Entry payload facts. | Approved correction/reversal behavior. |
| SUB-03 | Purchase Settlement is not implemented. | Explicit non-goal. | No settlement aggregate/effect. | No settlement UI/state. | Purchase Settlement. |
| SUB-04 | Bag reconciliation and empty-bag deductions are not implemented. | Explicit non-goal. | No deduction/movement. | No controls or calculations. | Bag settlement/reconciliation. |
| SUB-05 | DoublePlastic 0.2/0.4 kg settlement deductions are not implemented. | Construction class remains a snapshot only. | No special calculation. | No special calculation. | Settlement policy. |
| SUB-06 | Goods Supplied to Supplier accounting is not implemented. | Explicit non-goal. | No accounting side effect. | No accounting state. | Accounting design. |
| SUB-07 | Purchase Bills and supplier payables are not implemented. | Explicit non-goal. | Create neither. | Show neither. | Purchase and payable workflows. |
| SUB-08 | Inventory movements are not implemented. | Explicit non-goal. | Create none. | Store no Inventory state. | Inventory posting. |
| SUB-09 | Sales and Finance postings are not implemented. | Explicit non-goal. | Create none. | Store no Sales/Finance state. | Sales and balanced Finance posting. |
| SUB-10 | Official purchase finalization is not implemented. | No Approved or Finalized Receiving state is introduced. | Stop at submitted. | Stop at submitted. | Finalization/posting. |

## Derived contract decisions

- The only persisted Task 7C lifecycle states are
  `ReceivingInProgress` and `SubmittedForSettlementReview`.
- `OwnershipRecoveryRequired` is an attention/lease-health condition, not a
  Receiving business state.
- `CancelledBeforeSubmission` and `RejectedForCorrection` are not added until
  their business transitions and permissions are supplied.
- Every Session and Entry is owned by Workspace and Company. The Session is
  also bound to the authenticated default Branch. No request body owns those
  values.
- Entry acceptance, Session projection change, audit, `CommercialMobileSync`
  event, and idempotent result form one cloud transaction.
- Submission, lease clearing, audit, event, and idempotent result form one
  cloud transaction.
- Owners may read safe company-monitoring event broadcasts. An Operator Device
  cannot read unrelated Session events merely because it belongs to the same
  company; only events targeted to that Device while it is active editor are visible.
- Existing Active and Inactive Task 7B1/7B2 masters are exposed through a
  deterministic migration-backed safe change log. Bootstrap captures a scoped
  high-water mark and then resumes strictly after it, so concurrent mutations
  are neither skipped nor duplicated; raw `Internal` payloads are never used.

## Data ownership

Cloud-confirmed shared state is authoritative. The mobile encrypted database
owns the durable evidence of locally captured physical facts until cloud
acceptance and retains those facts for history/attention afterward. Master
records remain owned by their existing commercial modules; Receiving stores
immutable safe snapshots and does not take ownership of mutable master rows.
Owner-monitoring rows on mobile are disposable read-only projections.

## Transaction boundaries

- Mobile capture: local Entry + immutable operation + Session exact projection
  update commit in one encrypted SQLite transaction.
- Cloud Start: Session + ownership generation/lease + cloud reference + audit +
  event + idempotent result commit in one PostgreSQL transaction.
- Cloud Entry: immutable Entry + Session count/total/sequence/version + audit +
  event + idempotent result commit in one PostgreSQL transaction.
- Cloud Submit: status/timestamp + ownership/lease closure + audit + event +
  idempotent result commit in one PostgreSQL transaction.
- Mobile event apply: inbox insert + projection update + applied status + cursor
  advancement commit in one encrypted SQLite transaction.

## Failure behavior

No failure silently deletes or rewrites an offline physical payload. Reversible
conditions such as an expired lease for the unchanged owner or a stale master/
Procurement Settings revision produce `NeedsAttention` without consuming the
immutable operation. Permanent
identity conflicts such as another Device or an old ownership generation are
rejected, while the local fact remains available for an explicit future
recovery decision. Database failures roll back the whole cloud or local
transaction. Ambiguous network responses reuse the same operation ID and
payload.

## Security implications

Commercial authority requires a bearer access token and current database
revalidation. POC headers, profiles, routes, event cursors, and plaintext
SQLite are not commercial controls. Commercial customer data must be stored in
an encrypted local database whose key is held outside that database using
OS-backed secure storage. PostgreSQL RLS remains a mandatory pre-production
gate under ADR-0001.

## Migration implications

Task 7C0 creates documentation only. Task 7C1 requires new production tables,
constraints, event audience metadata, and reviewed migrations; it must not
rename or reuse POC entities/tables. Task 7C2 requires a new encrypted
production local database/schema and explicit non-destructive Drift migrations;
it must not promote or mutate the unencrypted POC database.

## Future implementation dependencies

- Task 7C1 depends on Task 7A, Task 7B1, Task 7B2, Task 3, Task 5 transaction
  patterns, ADR-0008, ADR-0009, and resolution of any question that changes an
  endpoint or lifecycle transition.
- Task 7C2 depends on Task 7C1 contracts, a reviewed encrypted-SQLite/secure-
  storage compatibility spike, Task 7A mobile integration, and the same source
  resolutions.
- Settlement/posting work depends on the missing product sources and a separate
  atomic posting design.

## Source-gap register

| Gap | Evidence available | What must not be inferred | Required source/decision | Impact |
| --- | --- | --- | --- | --- |
| Complete TPCL-101 | No committed full document; only name/search references are absent. | Additional header fields, lifecycle behavior, limits, numbering format, settlement behavior. | Original locked TPCL-101 or approved replacement delta. | Blocks affected details, not the two-state core. |
| Complete TPFS-101 | No committed full document; only Task 7C0 supplied rules are available. | UI workflow, correction/cancellation UX, field optionality, approval rules. | Original locked TPFS-101 or approved replacement delta. | Blocks affected Flutter acceptance criteria. |
| Production display-reference format | Only POC `RS-POC-*` exists and is prohibited. | Prefix, width, fiscal-year reset, branch segment, or gaplessness. | Product/operations numbering decision. | Renderer configuration; numeric allocator can proceed. |
| Cancellation before submission | Task 4 defers cancellation; Task 7C0 requires explicit treatment. | Whether allowed, who authorizes, and treatment of captured entries. | TPCL/TPFS or approved lifecycle decision. | No cancellation state/endpoint/UI in 7C1/7C2 until resolved. |
| Correction after submission | Task 4 reserves reversal fields but implements no correction. | Reopen, reject, clone, reverse, or amend behavior. | TPCL/TPFS and settlement-boundary decision. | Submitted snapshots remain immutable. |
| Ownership transfer authorization | Transfer must be explicit/audited; approver policy is unspecified. | Owner-only vs editor consent, reason requirements, or emergency rules. | TPCL/TPFS or approved security/product decision. | Basic no-takeover rule is fixed; transfer endpoint waits. |
| Capacity and physical validation limits | Task 3 bounds decimal syntax and numeric capacity; business session limits are absent. | Maximum Sessions, Entries, bags, zero weight, or submission minimum. | TPCL/TPFS and operational capacity review. | Task 7C1 must establish safe technical request limits without inventing business limits. |
| Optional Receiving header fields | Supplier, configured destination/policy, and optional vehicle are established. | Driver, vehicle free text, source location, remarks, purchase order, broker, or rate. | TPCL/TPFS. | No speculative fields. |
| Procurement default overrides | Procurement Settings define default destination and Weight Policy, but no source authorizes operator deviation. | Whether an override exists, who may use it, allowed values, reasons, or audit requirements. | TPCL/TPFS or approved product decision. | Task 7C1 accepts configured defaults only. |
| Non-editor Operator visibility | Owner monitoring is established, but no source grants Operators discovery of Sessions edited by another Device. | Company-wide Operator event, list, or live-view visibility. | TPCL/TPFS or approved product/security decision. | Operator event cursor remains targeted to its active-editor Device only. |
| Lease duration and recovery SLA | POC uses five minutes and explicitly cannot be copied unchanged. | Production duration, grace window, escalation timing. | Operational/security review. | Policy remains configurable with safe server defaults reviewed in 7C1. |

## Unresolved questions

The normative open-question list is
`docs/product-specs/TPRC-101-Open-Questions.md`. No question in that document
has an implied default.
