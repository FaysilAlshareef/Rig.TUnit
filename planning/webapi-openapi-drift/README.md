# Planning — WebAPI OpenAPI drift + ProblemDetails consistency (F-049)

**Feature ID**: F-049
**Family**: WebAPI
**Status**: planned
**Depends on**: —
**Target release**: v0.13
**Estimated tasks**: ~24 (Phase 0: 7 · 1 package × 12 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.WebAPI` doesn't catch the silent breakages that shipping API teams hit:

- An OpenAPI route / parameter / response schema changed without bumping the version or updating the client.
- A 4xx / 5xx exit returns a non-RFC9457 body — clients can't deserialize.
- Model-binding edge cases: `TimeOnly`, `DateOnly`, `decimal` precision, enum string vs int, null-vs-missing.
- Minimal-API endpoint route conventions (verb prefix, kebab-case path).

## What we deliver

```csharp
public abstract partial class WebApiRigBuilder
{
    public WebApiRigBuilder WithOpenApiBaseline(string filePath);
}

public static class OpenApiAssert
{
    public static OpenApiDiffAssertion Diff(string baselinePath, string currentSpec);
}

public sealed class OpenApiDiffAssertion
{
    public OpenApiDiffAssertion BreakingChanges().Empty();
    public OpenApiDiffAssertion AdditiveChanges().AllowedExceptVersionBump();
}

public static class WebApiAssert
{
    public static ProblemDetailsAssertion AllErrors(WebApiFixture fixture);
    public static ModelBindingAssertion ModelBinding(WebApiFixture fixture);
    public static EndpointConventionAssertion Endpoints(WebApiFixture fixture);
}

public sealed class ProblemDetailsAssertion
{
    public ProblemDetailsAssertion AreProblemDetails();
    public ProblemDetailsAssertion IncludeTraceId();
    public ProblemDetailsAssertion IncludeStatusInBody();
}

public sealed class ModelBindingAssertion
{
    public ModelBindingAssertion HandlesNull(string field).As(NullSemantic semantic);
    public ModelBindingAssertion DecimalPrecision(int places);
    public ModelBindingAssertion EnumSerialization(EnumStyle style);
}
```

## Gaps closed (from WEBAPI-1 in the gap analysis)

- OpenAPI breaking-change detection.
- ProblemDetails consistency on every error.
- Model-binding edge-case coverage.
- Endpoint convention enforcement.

## Providers in scope

1: `src/Rig.TUnit.WebAPI`.

## Exit criteria

- `OpenApiAssert.Diff`, `WebApiAssert.*` ship with 100 % line coverage.
- ≥ 5 RED scenarios.
- `docs/providers/webapi.md` updated with OpenAPI baseline workflow.

## Dependencies on other planned features

- Upstream: none.
- Downstream: F-041 (consumer-driven contracts can ingest the OpenAPI baseline).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 049-webapi-openapi-drift

Read first:
- planning/webapi-openapi-drift/README.md
- src/Rig.TUnit.WebAPI/* (current state)
- RFC 9457 (Problem Details), OpenAPI 3.1 spec
- Microsoft.OpenApi.NET / Asp.Versioning

Generate a feature spec that:
1. Introduces WithOpenApiBaseline + OpenApiAssert.Diff.
2. WebApiAssert.AllErrors / ModelBinding / Endpoints.
3. ≥ 5 RED scenarios.

Constraints:
- Baseline file checked into repo; PR fails if breaking changes vs baseline.
- ProblemDetails assertion covers RFC 9457 fields + traceparent.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
