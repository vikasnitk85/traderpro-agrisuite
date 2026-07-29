# TraderPro AgriSuite Agent Instructions

These instructions apply to the entire repository. Treat the following product invariants as durable rules when designing, implementing, reviewing, or testing TraderPro AgriSuite.

## Durable product rules

1. Cloud-confirmed data is authoritative.
2. Mobile actions save locally before sync.
3. Offline physical facts are never silently discarded.
4. One device edits an active Receiving Session.
5. Official numbers are cloud-assigned.
6. Final posting is atomic and idempotent.
7. Inventory derives from immutable movements.
8. Finance derives from balanced posting lines.
9. Submitted and posted snapshots are immutable.
10. Raw weight is stored unchanged.
11. Processed weight is displayed and used from the captured policy.
12. Customer-owned records are workspace- and company-scoped.
13. One subscription belongs to one company workspace.
14. Backend permissions are authoritative.
15. Flutter screens do not access database tables directly.
16. External messages use a transactional outbox.
17. Do not copy the RicePOS schema or `BillingRepository`.

## Repository guardrails

- Preserve the modular-monolith dependency direction documented in the root README and enforced by architecture tests.
- Do not create production credentials, connect to a production database, or commit secrets.
- Do not inspect, modify, or copy from `D:\Projects\RicePOS_Pro` unless a future task explicitly authorises inspection.
- Do not add speculative dependencies or implement business capabilities outside the reviewed task scope.
- Keep business data cloud-authoritative while preserving offline-captured physical facts for explicit reconciliation.
