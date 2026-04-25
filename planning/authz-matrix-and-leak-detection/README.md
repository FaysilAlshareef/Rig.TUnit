# Planning — Authz matrix + secrets/PII leak detection (F-033)

**Feature ID**: F-033
**Family**: Security
**Status**: planned
**Depends on**: F-030 (JWKS / token shape)
**Target release**: v0.15
**Estimated tasks**: ~24 (Phase 0: 7 · 1 package × 12 tasks · 5 docs)

---

## Why this feature exists

Two adjacent gaps remain after F-030/F-031/F-032:

1. **Authorization matrix coverage** — `(role × resource × action × tenant)` is a cross-product. Today users hand-write 50 tests; the rig has no matrix runner. Real bugs ship because one cell is silently uncovered.
2. **Secrets / PII leak detection** — connection strings, tokens, emails, SSNs, card numbers must NOT appear in logs, trace attributes, or HTTP error bodies. There is no rig assertion for this.

## What we deliver

A `[AuthMatrix]` data-driven attribute that generates the cross-product, and a `LeakAssert` family that asserts no secret patterns leaked into any captured artefact.

```csharp
[AuthMatrix(
    Roles = new[] { "admin", "user", "guest" },
    Resources = new[] { "/api/orders", "/api/users" },
    Actions = new[] { "GET", "POST", "PUT", "DELETE" })]
public class OrderEndpointAuthTests
{
    [Test]
    public async Task Endpoint_Auth_Matches_Policy(string role, string resource, string action) { /* ... */ }
}

public static class LeakAssert
{
    public static LeakScope Captures(RigFixture fixture);
}

public sealed class LeakScope
{
    public LeakScope Contain(NoSecretRule rule);
    public LeakScope Contain(NoPiiRule rule);
    public LeakScope NoneContain(string pattern);
}

public static class NoSecretRule
{
    public static NoSecretRule Default();          // tokens, conn strings
    public static NoSecretRule WithCustom(Regex);
}

public static class NoPiiRule
{
    public static NoPiiRule WithFields(params string[] fieldNames);
}
```

## Gaps closed (from SEC-6 + SEC-7 in the gap analysis)

- Authz matrix cross-product coverage.
- Log / trace / HTTP-body leak detection.
- Compliance-grade redaction assertions.

## Providers in scope

1: `src/Rig.TUnit.Security.Policies`, plus integration with capture fixtures (Logging, Tracing, Http, Grpc, Messaging).

## Exit criteria

- `[AuthMatrix]` attribute and `LeakAssert` ship with 100 % line coverage.
- ≥ 4 RED scenarios (authz matrix coverage proof; PII pattern detected; secret pattern detected; custom rule).
- `docs/providers/security.md` updated.

## Dependencies on other planned features

- Upstream: F-030.
- Downstream: F-035 (log redaction patterns share the secret/PII rules).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 033-authz-matrix-and-leak-detection

Read first:
- planning/authz-matrix-and-leak-detection/README.md
- planning/jwt-attacks-and-jwks/README.md (F-030 must be shipped)
- src/Rig.TUnit.Security.Policies/* (current state)

Generate a feature spec that:
1. Introduces [AuthMatrix] TUnit data-driven attribute.
2. LeakAssert + NoSecretRule + NoPiiRule.
3. ≥ 4 RED-leading scenarios.

Constraints:
- LeakAssert reads from existing capture fixtures (no new captures).
- Default rule patterns documented and configurable.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
