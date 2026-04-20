# ADR-006: IsolationKey as the single per-test naming primitive

**Status**: Accepted
**Date**: 2026-01 (Feature 001)
**Context**: Every family needs per-test unique names — databases, schemas,
collections, keyspaces, topics, key prefixes, container names, bucket names.

## Decision

`Rig.TUnit.Core.IsolationKey` — a value-record struct combining a truncated
readable suffix + a SHA-256 hash slice of the full test name. Produces deterministic,
collision-resistant identifiers across parallel runs.

```csharp
public readonly record struct IsolationKey(string Value)
{
    public static IsolationKey FromExecutionContext(string? fullyQualifiedName = null);
    public string ForPostgresDatabase();   // lowercase, 63-byte cap
    public string ForSqlServerDatabase();  // original case, 128-char cap
    public string ForRedisKeyPrefix();     // free-form, 64-char cap
    public string ForDockerContainer();    // lowercase, 63-char cap
}
```

## Rationale

1. **Deterministic** — same test fullyQualifiedName produces the same key across
   process restarts, making replays + triage easier.
2. **Collision-resistant** — SHA-256 prefix makes accidental collision between two
   similarly-named tests effectively impossible (2^64 space).
3. **Shape-per-target** — different backing systems have different identifier rules
   (lowercase, length cap, charset); `IsolationKey` exposes per-target getters.
4. **Lives in Core** — not Databases.Sql or Messaging — so every family can derive
   names without crossing base-to-base boundaries (enforced by
   `DependencyDirectionTests`).

## Consequences

- Fixtures use `IsolationKey.FromExecutionContext()` in their constructor — derived
  from `Environment.GetEnvironmentVariable("RIGTUNIT_TEST_ID")` or a random GUID
  fallback.
- Tests in different contexts (CI matrix runners, local IDE) produce different keys —
  expected and safe.
- Helpers named `{Thing}PerTestHelper` (`CollectionPerTestHelper`,
  `KeyspacePerTestHelper`, etc.) always take an `IsolationKey` parameter.
