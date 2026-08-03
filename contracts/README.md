# TraderPro contract fixtures

These versioned, machine-readable fixtures record the reviewed Task 7C2A mobile boundary. They are implementation inputs, not a second server specification. Backend drift tests compare every listed identity/Commercial Receiving route with endpoint-registration source, compare the complete reviewed Commercial Receiving error-code vocabulary with active source, and check typed operation payloads against production Web JSON serialization. Event/master tests enforce the fixture envelopes, payload property sets, audiences, decimal-string kinds, and recursive sensitive-property exclusions.

- `openapi/traderpro-commercial-mobile.v1.json` covers authentication, mobile sync, receiving ownership, Owner reads, and reference-policy HTTP examples. Accessible authentication request/response DTO examples are checked for production serializer shape and casing; the refresh response is a documented reference to the login-response shape rather than an independently serialized example. Secret and capability fields use explicit non-secret placeholders.
- `golden-vectors/commercial-receiving-operations.v1.json` covers Start, Record Entry, Submit, canonical v1 request hashing, replay, waiting, NeedsAttention, and Rejected results.
- `events/commercial-receiving-events.v1.json` covers exact event envelopes/payloads and the immutable `OwnerBroadcast` / `TargetDevice` audience model, including both sides of ownership transfer.
- `golden-vectors/commercial-master-changes.v1.json` covers all ten redacted master payload types and decimal-string formatting.

All identities and business values are synthetic. The fixtures contain no real password, refresh token, activation code, lease ID, database key, access token, contact data, address, tax identifier, or free-form note. Secret-bearing HTTP fields contain explicit non-secret placeholders; event and master fixtures recursively prohibit sensitive property names.
