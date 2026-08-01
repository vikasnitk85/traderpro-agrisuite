# TPRUN-004: Commercial Supplier and Product Catalog

## Development-only prerequisites

Use local PostgreSQL 18, .NET 10, and the Task 7A development identity
bootstrap. Never place real passwords, tokens, activation codes, device
secrets, signing keys, or connection strings in commands, documentation, or
logs. Keep development secrets in private variables and never print them.

## Start PostgreSQL and the authenticated backend

From the repository root, start PostgreSQL without deleting or resetting its
volume:

```powershell
docker compose `
  --env-file .\infrastructure\docker\.env `
  -f .\infrastructure\docker\compose.yml `
  up -d postgres
```

Privately set `ConnectionStrings__TraderPro`, then run the reviewed local
migration helper:

```powershell
.\scripts\migration\apply-local.ps1
```

Start the Task 7A authenticated Development backend:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\poc\start-production-identity-backend.ps1 `
  -Url 'http://localhost:5000' `
  -DataProtectionKeyRingPath `
    '.\artifacts\development-identity-key-ring' `
  -GenerateTemporarySigningKey `
  -EnableIdentityBootstrap
```

Follow `TPRUN-002-Development-Identity-Bootstrap.md` to bootstrap Task 7A,
activate the pre-created Owner and Operator Devices, and log in. Keep both
access tokens private. Temporary POC workspace/device headers are irrelevant
to the commercial routes.

## Configure the catalog as Owner

Use `Authorization: Bearer <Owner access token>`. Give each logical mutation a
fresh `Idempotency-Key`; reuse a key only for an identical retry. Updates and
status/default/scope commands use the latest positive
`X-Expected-Version`.

1. Confirm Task 7B1 Bag Types exist with
   `GET /api/v1/procurement/bag-types`, or create representative Jute and
   Plastic Bag Types.
2. Create Product Groups with
   `POST /api/v1/catalog/product-groups`.
3. Create Aman, H Aman, and P Aman purchasable products with
   `POST /api/v1/catalog/products`; give all three the Processing Family
   `AMAN`.
4. Create one Rice Product and representative non-purchasable ByProduct
   records. Product Type and `isPurchasable` must be explicit.
5. Create product-specific standards with
   `POST /api/v1/catalog/products/{productId}/bag-standards`. Send
   `standardContentWeightKg` as a decimal string. This is net content, not the
   Bag Type tare.
6. Select one Active default with
   `POST /api/v1/catalog/products/{productId}/bag-standards/{id}/set-default`.
7. Create an Unrestricted Supplier with
   `POST /api/v1/procurement/suppliers`.
8. Create a Restricted Supplier and include at least one unique Active
   purchasable Product ID in `initialProductIds`.
9. Add, deactivate, or reactivate scope rows through
   `/api/v1/procurement/suppliers/{supplierId}/product-scopes`.

Do not put WorkspaceId, CompanyId, BranchId, UserId, or DeviceId in bodies.
Do not add balances, prices, rates, settlement, stock, or accounting data;
Task 7B2 has no such fields or effects.

Do not send `code` on Supplier, Product Group, or Product updates. Do not send
`productId`, `bagTypeId`, or `isDefault` on bag-standard updates. Presence of
one of these immutable or command-only fields returns
`MASTER_IMMUTABLE_FIELD_SUPPLIED`; use the set-default endpoint for default
selection.

## Verify access and protections

With the Operator token, collection and item GETs succeed. POST, PUT,
deactivate, reactivate, set-default, and scope mutations return 403
`OWNER_ROLE_REQUIRED`. Without a bearer token, routes return 401. A revoked
Task 7A session also fails authentication.

With the Owner token, verify:

- default lists show Active rows; explicit status, search, filters, and opaque
  cursor pagination remain stable;
- a cursor cannot move across endpoint, tenant, parent, or changed filters;
- identical idempotent retry returns the original result and changed meaning
  returns `IDEMPOTENCY_PAYLOAD_CONFLICT`;
- stale expected versions return `MASTER_VERSION_CONFLICT`;
- a Restricted Supplier cannot be created without an initial Product and
  cannot lose its final Active scope;
- an inactive/non-purchasable Product cannot enter a scope or bag standard;
- an Active Product prevents Group deactivation;
- an Active scope or standard prevents Product deactivation;
- an Active standard prevents Bag Type deactivation;
- selecting a new default leaves exactly one Active default and advances the
  prior default revision;
- deactivating a default clears it, while reactivation leaves it non-default;
- no Supplier/Catalog event appears in the temporary POC MobileSync cursor;
- Supplier protected-field audits contain only deterministic changed-field
  names, never contact, email, address, tax, or notes values;
- Restricted creation with two Products writes one Supplier fact and two scope
  facts in Product ID order, and an identical replay adds no duplicates;
- an injected initial-scope outbox failure leaves no Supplier, scope, audit,
  outbox, or completed idempotency row;
- direct SQL rejects non-canonical Supplier emails with multiple/edge `@`,
  whitespace, control characters, or uppercase/surrounding whitespace.

## Automated verification

Run:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-commercial-supplier-product-catalog.ps1
```

The script runs focused unit and PostgreSQL/API tests, architecture tests, and
Task 7B1, Task 7A, and Task 6A regressions. Testcontainers use disposable
PostgreSQL 18 databases and do not modify the Compose development volume.

Apply `20260801030919_HardenCommercialSupplierProductCatalogContracts` after
the original Task 7B2 migration. It only strengthens the Supplier email check;
do not edit or regenerate
`20260731115429_AddCommercialSupplierAndProductCatalog`.

After manual verification, stop the backend and clear all private secret and
token variables from the shell.
