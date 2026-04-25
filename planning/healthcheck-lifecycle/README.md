# Planning — HealthCheck lifecycle (F-046)

**Feature ID**: F-046
**Family**: HealthChecks
**Status**: planned
**Depends on**: F-008 (clock for startup ramp / drain timing)
**Target release**: v0.13
**Estimated tasks**: ~26 (Phase 0: 7 · 1 package × 14 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.HealthChecks` ships only assertions on individual `HealthStatus` results. Real health-check correctness is about the **lifecycle**:

- Liveness vs readiness divergence during startup ramp (process is up but not ready).
- Drain mode on graceful shutdown — readiness flips to unhealthy, liveness stays healthy until the active connections finish.
- Dependency cascade — DB unhealthy → readiness degraded; one dep down doesn't fail liveness.
- Probe budget — total health-check time under SLO (a slow check is its own failure).

## What we deliver

```csharp
public abstract partial class HealthCheckRigBuilder
{
    public HealthCheckRigBuilder WithHealthChecks(Action<IHealthCheckBuilder> configure);
}

public interface IHealthCheckBuilder
{
    IHealthCheckBuilder Check(string name, Action<IHealthCheckConfig>? configure = null);
    IHealthCheckBuilder WithStartupRamp(TimeSpan duration);
    IHealthCheckBuilder WithDrainTimeout(TimeSpan timeout);
    IHealthCheckBuilder WithProbeBudget(TimeSpan max);
}

public interface IHealthCheckConfig
{
    IHealthCheckConfig SetStatus(HealthStatus status);
    IHealthCheckConfig WithDependency(string depName, HealthStatus depStatus);
    IHealthCheckConfig SimulateLatency(TimeSpan span);
}

public static class HealthAssert
{
    public static HealthPhaseAssertion Phase(LifecyclePhase phase);
    public static HealthDrainAssertion Drain();
    public static HealthBudgetAssertion Budget();
}

public enum LifecyclePhase { Startup, Steady, Drain, Shutdown }

public sealed class HealthPhaseAssertion
{
    public HealthPhaseAssertion Liveness(HealthStatus expected);
    public HealthPhaseAssertion Readiness(HealthStatus expected);
}

public sealed class HealthDrainAssertion
{
    public HealthDrainAssertion FlipsReadinessIn(TimeSpan span);
    public HealthDrainAssertion KeepsLivenessHealthy();
}
```

## Gaps closed (from HEALTH-1 in the gap analysis)

- Liveness / readiness divergence during startup.
- Drain-mode flip semantics.
- Dependency-cascade evaluation.
- Probe-budget SLO assertion.

## Providers in scope

1: `src/Rig.TUnit.HealthChecks`.

## Exit criteria

- `IHealthCheckBuilder`, `HealthAssert.Phase / Drain / Budget` ship with 100 % line coverage.
- ≥ 5 RED scenarios (startup-ramp divergence, drain flip, dep cascade, probe budget, slow-check fail).
- F-008 fake-clock used for ramp / drain timing.
- `docs/providers/healthchecks.md` updated.

## Dependencies on other planned features

- Upstream: F-008.
- Downstream: F-050 (CI matrix uses health-check phase assertions for container-readiness gating).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 046-healthcheck-lifecycle

Read first:
- planning/healthcheck-lifecycle/README.md
- planning/deterministic-clock/README.md (F-008 must be shipped)
- src/Rig.TUnit.HealthChecks/* (current state)
- Microsoft.Extensions.Diagnostics.HealthChecks docs

Generate a feature spec that:
1. Introduces IHealthCheckBuilder + WithHealthChecks on RigBuilder.
2. HealthAssert.Phase / Drain / Budget.
3. ≥ 5 RED scenarios.

Constraints:
- F-008 IFakeClock for ramp / drain.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
