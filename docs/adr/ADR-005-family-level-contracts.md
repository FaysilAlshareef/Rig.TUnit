# ADR-005: Family-level contract suites via [InheritsTests]

**Status**: Accepted
**Date**: 2026-04 (Feature 004)
**Context**: Every SQL/NoSQL/Cache/Messaging provider should satisfy the same
invariants — idempotent writes, concurrent-read isolation, cancellation propagation.
Duplicating those tests per provider is wasteful and drifts.

## Decision

Abstract `{Family}RigContract` classes live in `tests/Rig.TUnit.{Family}.Tests.Contract/`.
Provider suites derive with `[InheritsTests]` and plug their own fixture factory:

```csharp
[InheritsTests]
public sealed class PostgresContract : SqlRigContract<PostgresFixture>
{
    protected override async ValueTask<PostgresFixture> CreateFixtureAsync(…)
        => await SharedPostgresFixture.GetAsync();
}
```

## Rationale

1. **One test, many providers** — adding a new contract method fires it against every
   derived provider automatically.
2. **Drift detection** — a provider that breaks the family invariant fails its contract
   suite independently, even if the derived class doesn't mention the specific assertion.
3. **No code duplication** — Postgres, SqlServer, MySql, Oracle, Sqlite all share the
   same `SqlRigContract` surface.

## Consequences

- `TestCompletenessTests` requires either a `{Provider}Contract.cs` inside the
  provider's Integration project OR the provider appears in the family contract suite.
- Contracts must be careful with timing — the same assertion runs against Postgres
  (fast) and Oracle (slow) — timeouts are per-provider-overridable.
- Phase 6c README rewrites point consumers to the family contract when documenting
  per-provider behaviour.
