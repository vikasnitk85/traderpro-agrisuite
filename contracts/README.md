# TraderPro contract fixtures

These versioned, machine-readable fixtures freeze the reviewed Task 7C2A mobile boundary. They are implementation inputs, not a second server specification. Backend drift tests must fail when source routes, operation hashes, status/error codes, event audiences, or redacted master payloads diverge.

- `openapi/traderpro-commercial-mobile.v1.json` covers authentication, mobile sync, receiving ownership, Owner reads, and reference-policy HTTP examples. Secret and capability fields use explicit non-secret placeholders.
- `golden-vectors/commercial-receiving-operations.v1.json` covers Start, Record Entry, Submit, canonical v1 request hashing, replay, waiting, NeedsAttention, and Rejected results.
- `events/commercial-receiving-events.v1.json` covers exact event envelopes/payloads and the immutable `OwnerBroadcast` / `TargetDevice` audience model, including both sides of ownership transfer.
- `golden-vectors/commercial-master-changes.v1.json` covers all ten redacted master payload types and decimal-string formatting.

All identities and business values are synthetic. The fixtures intentionally contain no password, refresh token, activation code, lease ID, database key, access token, contact data, address, tax identifier, or free-form note.
