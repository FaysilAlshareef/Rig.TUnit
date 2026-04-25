# Planning — Log redaction + cardinality guard (F-035)

**Feature ID**: F-035
**Family**: Observability
**Status**: planned
**Depends on**: —
**Target release**: v0.13
**Estimated tasks**: ~24 (Phase 0: 7 · 1 package × 12 tasks · 5 docs)

---

## Why this feature exists

GDPR / HIPAA / PCI all require log scrubbing of PII / secrets / card numbers. The rig today has log capture (`Rig.TUnit.Observability.Logging`) but no first-class redaction or cardinality guard. Real-world incidents this would catch:

- A new logger statement accidentally logging an entire request body containing card numbers.
- A typo turning a metric label into a high-cardinality field (`{userId}` instead of `{userTier}`), exploding Prometheus.
- A connection string logged on startup-failure path.

## What we deliver

```csharp
public abstract partial class LoggingFixture
{
    public LoggingFixture WithRedactor(Func<string, string> redact);
    public LoggingFixture WithRedactorPattern(Regex pattern, string mask);
}

public static class LogAssert
{
    public static LogCaptureAssert Captures(LoggingFixture fixture);
}

public sealed class LogCaptureAssert
{
    public LogCaptureAssert NoneContain(Regex pattern);
    public LogCaptureAssert WithCategory(string category).ContainsMessage(string text);
    public LogCaptureAssert StructuredProperty(string name).Cardinality(int max);
}

public static class MetricCardinalityAssert
{
    public static CardinalityAssertion Counter(string name);
}

public sealed class CardinalityAssertion
{
    public CardinalityAssertion Cardinality(int max);
    public CardinalityAssertion LabelValues(string label).Cardinality(int max);
}
```

## Gaps closed (from OBS-2 + OBS-3 in the gap analysis)

- Log redaction.
- Metric label-cardinality guard.
- Structured-property assertions.

## Providers in scope

2: `src/Rig.TUnit.Observability.Logging`, `src/Rig.TUnit.Observability.Metrics`.

## Exit criteria

- `LogAssert.Captures` + redactor surface + `MetricCardinalityAssert` ship with 100 % line coverage.
- ≥ 5 RED scenarios (PII pattern detected, secret pattern, structured-property cardinality, metric-label cardinality, redactor applied).
- `docs/providers/observability.md` updated.

## Dependencies on other planned features

- Upstream: none (standalone).
- Downstream: F-033 (authz matrix shares the secret/PII rules — composable).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 035-log-redaction-and-cardinality

Read first:
- planning/log-redaction-and-cardinality/README.md
- src/Rig.TUnit.Observability.Logging/* (current state)
- src/Rig.TUnit.Observability.Metrics/* (current state)
- Microsoft.Extensions.Compliance.Redaction docs

Generate a feature spec that:
1. Introduces WithRedactor / WithRedactorPattern on LoggingFixture.
2. LogAssert.Captures + structured-property cardinality.
3. MetricCardinalityAssert.Counter(name).
4. ≥ 5 RED scenarios.

Constraints:
- Default redaction patterns (email, SSN, card) included; users can extend.
- Cardinality-guard runs on captured snapshots, not at runtime.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
