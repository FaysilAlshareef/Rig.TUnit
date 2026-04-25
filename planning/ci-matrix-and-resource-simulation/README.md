# Planning — CI matrix pin + Docker resource simulation (F-050)

**Feature ID**: F-050
**Family**: CI / Docker
**Status**: planned
**Depends on**: —
**Target release**: v0.16
**Estimated tasks**: ~26 (Phase 0: 5 · 2 packages × 8 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.Ci` and `Rig.TUnit.Docker` ship the rig's container infrastructure but lack assertion surfaces for two real CI concerns:

1. **Image-pin matrix** — assert SUTs run against latest patch + LTS of each provider container image. Today images are pinned in `docker compose` files but no test asserts the pin is fresh.
2. **Resource-limit simulation** — run a fixture under `--memory=256m --cpus=0.5` to catch GC hot paths and OOM kills. Real-world: Postgres healthy at 1 GB / 2 cores fails at 256 MB / 0.5 cores; the rig has no surface for this.

Plus:
- **Network-alias correctness** for cross-provider integration tests.
- **Healthcheck-before-fixture-up**: assert no test runs while container is `(starting)`.

## What we deliver

```csharp
public static class CiAssert
{
    public static ImageMatrixAssertion ImageMatrix();
    public static NetworkAliasAssertion NetworkAlias(string alias);
    public static HealthcheckGateAssertion HealthcheckGate(string containerName);
}

public sealed class ImageMatrixAssertion
{
    public ImageMatrixAssertion CoversLatestPatch();
    public ImageMatrixAssertion CoversLts();
    public ImageMatrixAssertion ExcludesYanked();
}

public abstract partial class DockerRigBuilder
{
    public DockerRigBuilder WithResourceLimits(Action<IResourceLimitConfig> configure);
}

public interface IResourceLimitConfig
{
    IResourceLimitConfig Memory(string size);   // "256m"
    IResourceLimitConfig Cpus(double count);    // 0.5
    IResourceLimitConfig PidsLimit(int max);
}

public static class ResourceAssert
{
    public static ResourceUsageAssertion Container(string name);
}

public sealed class ResourceUsageAssertion
{
    public ResourceUsageAssertion StayedUnderMemory(string limit);
    public ResourceUsageAssertion CpuUsageBelow(double percent);
    public ResourceUsageAssertion DidNotOomKill();
}
```

## Gaps closed (from CI-1 in the gap analysis)

- Image-pin freshness assertion.
- Resource-limit simulation.
- Network-alias correctness across compose stacks.
- Healthcheck-before-fixture-up gating.

## Providers in scope

2: `src/Rig.TUnit.Ci`, `src/Rig.TUnit.Docker`.

## Exit criteria

- `CiAssert.*`, `WithResourceLimits`, `ResourceAssert.Container` ship with 100 % line coverage.
- ≥ 5 RED scenarios.
- `docs/providers/ci-docker.md` (new) covers the matrix-pin workflow.

## Dependencies on other planned features

- Upstream: none.
- Downstream: none (terminal feature in the planned block).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 050-ci-matrix-and-resource-simulation

Read first:
- planning/ci-matrix-and-resource-simulation/README.md
- src/Rig.TUnit.Ci/* and Docker/* (current state)
- Testcontainers .NET docs (resource limits, healthchecks, network aliases)

Generate a feature spec that:
1. Introduces CiAssert.ImageMatrix / NetworkAlias / HealthcheckGate.
2. WithResourceLimits on DockerRigBuilder.
3. ResourceAssert.Container with stayed-under / CPU-below / no-OOM operators.
4. ≥ 5 RED scenarios.

Constraints:
- Image-matrix data sourced from a checked-in YAML; assertion compares against canonical list.
- Resource limits feed Testcontainers' --memory / --cpus.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
