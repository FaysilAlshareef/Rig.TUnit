# ADR-002: CRTP for provider RigBuilders

**Status**: Accepted
**Date**: 2026-02 (Feature 002)
**Context**: Every provider ships a `{Provider}RigBuilder` that must expose both
family-level chainable methods (`UseJsonSerialization`, `UseCorrelation`, …) and
provider-specific ones (`WithSchema`, `WithBroker`, …) while keeping fluent chaining
type-safe.

## Decision

Provider builders use the **Curiously Recurring Template Pattern (CRTP)**:

```csharp
public abstract class SqlRigBuilder<TSelf> where TSelf : SqlRigBuilder<TSelf> { … }

public sealed class SqlServerRigBuilder : SqlRigBuilder<SqlServerRigBuilder> { … }
```

Family methods return `TSelf`; provider-specific methods return the concrete class
directly. Consumers always see the leaf type at the end of the chain.

## Rationale

1. **Type-preserving fluent chains** — `builder.UseCorrelation().WithSchema("orders")`
   returns `SqlServerRigBuilder`, not `SqlRigBuilder`. No casts required.
2. **Compile-time enforcement** — forgetting the `TSelf` generic parameter fails at
   compile time, not at runtime.
3. **Same shape across families** — Messaging, Caching, Storage, and Security family
   builders all follow the same template so consumers learn one pattern.

## Consequences

- Generic noise in public API signatures — mitigated by `using` aliases in leaf
  consumer code.
- Architecture tests (`ProviderCompletenessTests`) check for the concrete
  `{Provider}RigBuilder` type, not the abstract base.
- Static-helper `Use{Provider}` extensions hide the generic chain from consumers who
  don't need it.
