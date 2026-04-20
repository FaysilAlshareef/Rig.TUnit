# ADR-007: Split Redis into cache (Caching.Redis) + KV store (Databases.NoSql.Redis)

**Status**: Accepted
**Date**: 2026-03 (Feature 003)
**Context**: Redis is used as both a cache backplane (with TTL semantics, eviction,
cross-node invalidation) and as a KV store (persistent, SCAN-based enumeration,
secondary-index-free lookup).

## Decision

Two separate packages:

- **`Rig.TUnit.Caching.Redis`** — `RedisFixture` + `RedisCacheRigBuilder` +
  `UseRedisCache` extension + `RedisBackplaneCapture` helper. Cache-focused API.
- **`Rig.TUnit.Databases.NoSql.Redis`** — `RedisKvRigBuilder` + `UseRedisKv` extension
  + `KeyScanHelper` for SCAN-based enumeration. KV-focused API.

Both share the same underlying Redis container (by-design reuse, audited in A005).

## Rationale

1. **Role-specific APIs** — cache tests want pub/sub channel capture + TTL assertions;
   KV tests want SCAN + multi-key batch operations. Folding both into one package
   bloats the public surface.
2. **Documentation clarity** — README §2 "What this package is" differs between the
   cache role and the KV role. Separate packages produce focused docs.
3. **Shared container, different semantics** — the same `StackExchange.Redis`
   `ConnectionMultiplexer` serves both roles, so the fixture reuse is safe. A005 audit
   explicitly documents this as an `Intentional reuse`.

## Consequences

- Consumers who use Redis for both roles take both packages.
- `TestCompletenessTests`' skip list has `Rig.TUnit.Databases.NoSql.Redis` documented
  as "Integration shares the Caching.Redis suite; Contract in NoSqlRigContract".
- `SharedRedisFixture.cs` and `SharedRedisKvFixture.cs` both carry the `Intentional
  reuse per 003 §4.4` rationale (enforced by `SharedFixtureGuardTests`).
