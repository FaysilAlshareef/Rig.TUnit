# Planning — Deterministic clock (F-008)

**Feature ID**: F-008
**Family**: Cross-cutting
**Status**: planned
**Depends on**: —
**Target release**: v0.8
**Estimated tasks**: ~56 (Phase 0: 7 · 11 providers × 4 wiring tasks · 5 docs)

---

## Why this feature exists

Every flaky timing test in the rig is a wall-clock leak. Today the rig calls `DateTime.UtcNow` directly and lets each provider's SDK use its own internal clocks. Real-world tests that fail because of this:

- **TTL eviction** in caches (`Rig.TUnit.Caching.*`) — `WaitFor(span)` to "let the entry expire" is brittle.
- **JWT `exp` / `nbf` / `iat` validation** — `Rig.TUnit.Security.Jwt/JwtBuilder.cs` sets `IssuedAt = DateTime.UtcNow`; tests that build then validate after a `Task.Delay` race the clock.
- **Retry / backoff in resilience** — `Rig.TUnit.Resilience` policies rely on real wall-clock waits to assert "exponential backoff matched the schedule".
- **Saga / dedup windows in messaging** — SQS dedup is a 5-minute real window; ServiceBus session timeouts are real.
- **Outbox / Inbox processing intervals** — `Rig.TUnit.Microservices.Outbox` polls real wall-clock.
- **Token rotation, key rotation, schedule expiries** — all wall-clock.

`.claude/rules/coding-style.md` already mandates "Do not use `DateTime.Now` — use an injected time provider". The rule is not enforced because there is no rig-wide primitive to inject.

## What we deliver

A base-library `IFakeClock` interface and a `WithFakeClock(Action<IFakeClock>)` builder method on every `RigBuilder`, backed by .NET 8+'s [`TimeProvider`](https://learn.microsoft.com/en-us/dotnet/api/system.timeprovider). Every provider that today reads wall-clock is rewired to take its time from the rig's `TimeProvider`. Tests get `clock.Advance(TimeSpan.FromMinutes(5))` and the dedup window, JWT expiry, retry timer, cache TTL all advance deterministically.

## Public API surface (sketch)

```csharp
public interface IFakeClock
{
    DateTimeOffset UtcNow { get; }
    void SetUtcNow(DateTimeOffset value);
    void Advance(TimeSpan delta);
    TimeProvider AsTimeProvider();
}

public abstract partial class RigBuilder<TSelf>
{
    public TSelf WithFakeClock(Action<IFakeClock>? configure = null);
}
```

## Gaps closed (from CC-1 in the gap analysis)

- TTL eviction asserts in `Rig.TUnit.Caching.Memory|Redis|Hybrid|Fusion`.
- JWT `exp`/`nbf` / clock-skew tests in `Rig.TUnit.Security.Jwt`.
- Retry/backoff timing in `Rig.TUnit.Resilience`.
- Dedup-window in `Rig.TUnit.Messaging.Sqs` (5-min content-dedup) and `Rig.TUnit.Messaging.ServiceBus` (session timeout).
- Outbox/Inbox poll intervals in `Rig.TUnit.Microservices.Outbox|Inbox`.
- Saga timeouts in `Rig.TUnit.Microservices.Saga`.
- OAuth refresh-token sliding windows in `Rig.TUnit.Security.OAuth`.

## Providers in scope (wiring)

| Package | What rewires |
|---------|--------------|
| `src/Rig.TUnit/` | base `RigBuilder<TSelf>` adds `WithFakeClock` |
| `src/Rig.TUnit.Caching.Memory` | `IMemoryCache` `MemoryCacheOptions.Clock` |
| `src/Rig.TUnit.Caching.Redis` | StackExchange.Redis `ConfigurationOptions` if applicable; expiry tests |
| `src/Rig.TUnit.Caching.Hybrid` | `HybridCache` `TimeProvider` |
| `src/Rig.TUnit.Caching.Fusion` | `FusionCacheOptions.TimeProvider` |
| `src/Rig.TUnit.Security.Jwt` | `JwtBuilder` reads `IssuedAt` from `TimeProvider.UtcNow` |
| `src/Rig.TUnit.Security.OAuth` | refresh-token sliding window |
| `src/Rig.TUnit.Resilience` | Polly v8 `ResiliencePipelineBuilder` `TimeProvider` |
| `src/Rig.TUnit.Microservices.Outbox` | poll loop |
| `src/Rig.TUnit.Microservices.Inbox` | poll loop |
| `src/Rig.TUnit.Microservices.Saga` | timeout scheduling |
| `src/Rig.TUnit.Messaging.Sqs` | dedup window assertions |

## Exit criteria

- `IFakeClock` and `WithFakeClock` ship in `Rig.TUnit` base library, 100 % line coverage in introducing PR.
- `ProviderCompletenessTests` extended with a `Providers_Honour_RigTimeProvider` rule covering every package in scope.
- ≥ 90 % line / ≥ 85 % branch on every touched package.
- `docs/providers/*.md` updated with "Deterministic clock" section per touched provider.
- `CHANGELOG.md` v0.8 entry.
- One ADR (ADR-010, **planned**): "TimeProvider as the rig's clock primitive" — reviewed before Phase 1 starts.

## Dependencies on other planned features

None upstream. F-009 (chaos), F-014 (shuffle-replay), F-025/F-026 (cache stampede / locks), F-030 (JWT clock skew), F-031 (mTLS revocation timing), F-032 (OAuth refresh windows), F-039 (saga timeouts), F-044 (gRPC deadlines), F-046 (HealthCheck startup ramp), F-047 (resilience jitter distribution) all depend on F-008.

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 008-deterministic-clock

Read first:
- planning/deterministic-clock/README.md (this file)
- .claude/rules/coding-style.md (TimeProvider rule)
- src/Rig.TUnit/Builder/* (the base RigBuilder shape)
- src/Rig.TUnit.Security.Jwt/JwtBuilder.cs (sample wall-clock leak)
- planning/messaging-topology-and-sessions/Provider-Enhancement-Matrix.md (gap-table style we want to mirror)

Generate a feature spec that:
1. Introduces IFakeClock + WithFakeClock under the SDD model used by Feature 007.
2. Lists every provider package that reads wall-clock today and rewires each to TimeProvider.
3. Phase 0 lands the base contract + ProviderCompletenessTests parity rule (empty .clock-coverage.txt at first).
4. One provider per phase appends to .clock-coverage.txt; per-provider phases run in parallel.
5. Exit gate per provider: TimeProvider injected, wall-clock callsites = 0, coverage gate green.

Constraints:
- Pre-release library (no [Obsolete] aliases).
- File-scoped namespaces, sealed concrete types, records for value types.
- TUnit framework, AAA naming, no Thread.Sleep / Task.Delay in tests — use IFakeClock.Advance.
- Honour the parity-coverage progressive enforcement pattern from Feature 007.

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
