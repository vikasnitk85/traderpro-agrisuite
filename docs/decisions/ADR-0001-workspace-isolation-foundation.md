# ADR-0001: Workspace isolation foundation

- Status: Accepted for the foundation database
- Date: 2026-07-29
- Security gate: PostgreSQL Row-Level Security is mandatory before production

## Context

TraderPro customer records belong to a company workspace. The first database
foundation needs useful tenant isolation while authentication, authorization,
and database session identity are still deferred.

EF Core global query filters can keep ordinary application queries scoped to
the active workspace. SaveChanges validation can also stop accidental
cross-workspace inserts, updates, deletes, and ownership changes. Those
controls are valuable defense in depth, but they execute in the application
process and are not a database security boundary.

## Decision

All workspace-owned Platform entities implement `IWorkspaceScoped`. A scoped
`ICurrentWorkspaceAccessor` supplies the active workspace to
`TraderProDbContext`.

The context applies global query filters to Company, Branch, PlatformUser,
Device, IdempotencyRecord, OutboxMessage, and AuditEvent. Workspace is the
tenant root and is deliberately not filtered.

An absent current workspace is fail-closed: workspace-owned query filters
match no rows, and the write interceptor rejects inserts, updates, and deletes.

`WorkspaceOwnershipInterceptor` applies write controls:

- an empty WorkspaceId on a new workspace-owned record is assigned from the
  active workspace;
- an explicitly different WorkspaceId is rejected;
- an update or delete is rejected when its original workspace differs from
  the active workspace;
- WorkspaceId cannot change after creation.

Backend permissions remain authoritative. These filters and interceptors do
not replace authorization checks.

## Why Row-Level Security is deferred

PostgreSQL Row-Level Security (RLS) needs an authenticated, trustworthy
workspace identity bound to every database transaction or connection checkout.
Authentication and request-to-workspace resolution are outside this task.
Adding a placeholder RLS policy without reliable session context would provide
misleading security and could either deny legitimate operations or leak data
through an incorrectly initialized pooled connection.

RLS is therefore deferred, not rejected. It is a mandatory pre-production
security gate.

## Intended RLS direction

Before production:

1. Resolve the authenticated user's authorized workspace in the backend.
2. Begin a database transaction.
3. Set a transaction-local PostgreSQL setting, for example
   `SET LOCAL app.workspace_id = '<workspace-uuid>'`.
4. Enable and force RLS on each workspace-owned table.
5. Define `USING` and `WITH CHECK` policies comparing `workspace_id` with the
   validated session setting.
6. Ensure pooled connections cannot retain tenant context, and test fail-closed
   behavior when context is absent.
7. Restrict database roles so the application cannot bypass RLS.

## Consequences and risks

Normal EF Core queries and tracked writes now fail closed when no workspace is
available or when ownership conflicts.

Raw SQL, database views, stored procedures, bulk APIs, and queries using
`IgnoreQueryFilters` can bypass application query filters. Such operations
require explicit tenant predicates and security review. `IgnoreQueryFilters`
is used only in focused integration tests that prove the write interceptor.

Until RLS is deployed and verified, a defect in application query construction
or raw SQL remains capable of crossing workspace boundaries. This unresolved
risk blocks production deployment.
