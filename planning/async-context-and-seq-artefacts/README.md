# Planning — Async-context flow + Seq artefacts + AppInsights mock (F-037)

**Feature ID**: F-037
**Family**: Observability
**Status**: planned
**Depends on**: F-034 (cross-boundary propagation)
**Target release**: v0.15
**Estimated tasks**: ~42 (Phase 0: 7 · 3 packages × 10 tasks · 5 docs)

---

## Why this feature exists

Three remaining observability gaps after F-034/F-035/F-036:

1. **Async-context flow** — `ExecutionContext` / `Activity.Current` must flow through `Task.Run`, `Parallel.ForEachAsync`, channels, `IAsyncEnumerable`. Real bug: a logging scope or trace gets lost in `Task.Run` and produces orphan spans.
2. **Seq fixture deepening** — `SeqFixture` exists (`src/Rig.TUnit.Observability.Seq`) but no fluent query builder, saved-query support, or dashboard-artefact capture on test failure.
3. **AppInsights mock** — `Rig.TUnit.Observability.AppInsights` is minimal/nonexistent. No fluent mock server, no telemetry assertion surface.

## What we deliver

```csharp
public static class AsyncContextAssert
{
    public static AsyncContextAssertion Flowed(string scopeKey);
}

public sealed class AsyncContextAssertion
{
    public AsyncContextAssertion Through(AsyncBoundary boundary);
    public AsyncContextAssertion AllSpans().HaveParent().NoOrphans();
}

public enum AsyncBoundary { TaskRun, ParallelForEachAsync, ChannelReader, AsyncEnumerable, ConfigureAwaitFalse }

public abstract partial class SeqFixture
{
    public Task<SeqQueryResult> RunSavedQueryAsync(string name, CancellationToken ct);
    public Task CaptureArtifactAsync(string testName, ArtifactFormat format, CancellationToken ct);
}

public sealed class MockAppInsightsServer
{
    public Uri IngestionUri { get; }
    public IReadOnlyList<TelemetryEnvelope> CapturedEvents { get; }
}

public static class AppInsightsAssert
{
    public static AppInsightsScope Events(MockAppInsightsServer server);
}

public sealed class AppInsightsScope
{
    public AppInsightsScope OfType(TelemetryType type);
    public AppInsightsScope WithName(string name);
    public AppInsightsScope Count(int expected);
}
```

Plus a TUnit `[CaptureSeqOnFail("query")]` attribute that snapshots a Seq query into TestResults on failure.

## Gaps closed (from OBS-6 + OBS-7 in the gap analysis)

- Async-context flow assertions.
- Seq artefact capture on failure.
- AppInsights mock telemetry assertions.

## Providers in scope

3: `src/Rig.TUnit.Observability.Tracing` (async-context), `src/Rig.TUnit.Observability.Seq`, `src/Rig.TUnit.Observability.AppInsights`.

## Exit criteria

- `AsyncContextAssert`, extended `SeqFixture`, `MockAppInsightsServer` + `AppInsightsAssert` ship with 100 % line coverage.
- `[CaptureSeqOnFail]` TUnit attribute integrated.
- ≥ 6 RED scenarios across the three packages.
- `docs/providers/observability.md` updated.

## Dependencies on other planned features

- Upstream: F-034.
- Downstream: F-048 (concurrency fuzzing — async-context flow assertions deepen).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 037-async-context-and-seq-artefacts

Read first:
- planning/async-context-and-seq-artefacts/README.md
- planning/otel-cross-boundary-propagation/README.md (F-034 must be shipped)
- src/Rig.TUnit.Observability.Seq/* (current state)
- src/Rig.TUnit.Observability.AppInsights/* (current state)
- ApplicationInsights ingestion-endpoint contract (telemetry envelopes)

Generate a feature spec that:
1. Introduces AsyncContextAssert.Flowed + AsyncBoundary enum.
2. Extends SeqFixture with saved-query + artefact capture.
3. Adds MockAppInsightsServer + AppInsightsAssert.
4. [CaptureSeqOnFail] TUnit attribute.
5. ≥ 6 RED scenarios.

Constraints:
- MockAppInsightsServer in-process Kestrel; no external dependency.
- Seq artefact files written under TestResults/seq-artefacts/.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
