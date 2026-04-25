# Planning — Multi-tenant scope (F-013)

**Feature ID**: F-013
**Family**: Cross-cutting
**Status**: planned
**Depends on**: F-012 (cross-fixture correlation — tenant join)
**Target release**: v0.11
**Estimated tasks**: ~68 (Phase 0: 7 · 14 propagation points × 4 wiring · 5 docs)

---

## Why this feature exists

Every B2B SaaS test needs `WithTenant("acme")`. Today every test re-implements:
- A tenant claim in JWT.
- A tenant header on HTTP / gRPC.
- A tenant property on a domain event published to ServiceBus / Kafka.
- A `SET app.tenant_id = 'acme'` for Postgres RLS, or a tenant-prefix in Cosmos partition key.
- A row-filter in EF Core query interceptors.

Real-world bugs the rig should catch but cannot today:
- A query missing the tenant filter and silently returning cross-tenant rows.
- A saga compensation that runs without the tenant header, breaking RLS.
- An outbox relay that publishes events without the tenant header, polluting downstream.
- A cache key collision across tenants (no tenant-scoped key prefix).

## What we deliver

A `WithTenant(string id, Action<ITenantScope>?)` builder method that wraps an action, propagating tenant context through:

- HTTP outbound headers (`X-Tenant-Id`).
- gRPC metadata.
- Messaging headers (`x-tenant-id`).
- EF Core query filter (global filter via `ITenantContext`).
- Postgres `SET app.tenant_id`.
- Cosmos hierarchical PK prefix.
- Cache key prefix.
- Log scope.
- Tracing baggage.

Plus an assertion surface:

```csharp
public static class TenantAssert
{
    public static TenantScopeAssert Tenant(string id);
}

public sealed class TenantScopeAssert
{
    public TenantScopeAssert AllHttpCallsCarriedHeader();
    public TenantScopeAssert AllSqlQueriesIncludedFilter();
    public TenantScopeAssert AllCosmosReadsScoped();
    public TenantScopeAssert NoCrossTenantLeakage();
    public TenantScopeAssert MessagesCarriedTenantHeader();
}
```

## Gaps closed (from CC-6 in the gap analysis)

- B2B SaaS tenant-isolation test patterns reinvented per project.
- No way to assert "no cross-tenant query slipped through".
- Cache-key collision under multi-tenancy.
- Saga / outbox lose tenant header on compensation.

## Providers in scope (14 propagation points)

| Package | Propagation |
|---------|-------------|
| `src/Rig.TUnit.Http` | request header |
| `src/Rig.TUnit.Grpc` | metadata |
| `src/Rig.TUnit.Messaging.{ServiceBus,Kafka,RabbitMq,Nats,Sqs}` | message header (5) |
| `src/Rig.TUnit.Databases.Sql.{SqlServer,Postgresql,MySql,Oracle,Sqlite}` | session var / EF filter (5) |
| `src/Rig.TUnit.Databases.NoSql.{Cosmos,Mongo}` | PK prefix / collection scope (2) |
| `src/Rig.TUnit.Caching.*` | key prefix |
| `src/Rig.TUnit.Observability.Logging|Tracing` | scope / baggage |

## Exit criteria

- `WithTenant` and `ITenantScope` ship in `Rig.TUnit` base library; 100 % line coverage.
- `TenantAssert.NoCrossTenantLeakage()` is the marquee assertion — RED scenario asserts a missing-filter EF query is caught.
- `ProviderCompletenessTests` extended with `Providers_Honour_TenantScope` rule.
- Per-provider `docs/providers/*.md` updated with multi-tenant section.

## Dependencies on other planned features

- Upstream: F-012 (correlation — tenant scope joins by tenant + trace).
- Downstream: F-019 (SQL provider quirks — RLS on Postgres dovetails with tenant), F-024 (NoSQL hierarchical PK), F-038 (outbox preserves tenant header on relay).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 013-multi-tenant-scope

Read first:
- planning/multi-tenant-scope/README.md
- planning/cross-fixture-correlation/README.md (F-012 must be shipped)
- Postgres RLS docs, Cosmos hierarchical PK docs
- src/Rig.TUnit.Http/* (header injection pattern)

Generate a feature spec that:
1. Introduces WithTenant + ITenantScope on RigBuilder, plus TenantAssert.
2. Each propagation point has its own RED+GREEN pair wiring the tenant id into the transport's native header / filter.
3. Phase 0 lands the contract + ProviderCompletenessTests parity rule.
4. Phase 6 ships docs and an end-to-end RED scenario that asserts cross-tenant leakage is impossible.

Constraints:
- Zero ambient state — tenant context is scoped, never global static.
- Honour AsyncLocal flow through Task.Run / Parallel.ForEachAsync.
- Pre-release library — change Capture* shapes if needed.

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
