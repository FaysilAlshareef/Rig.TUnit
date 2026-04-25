# Planning — JWT attacks + JWKS + key rotation (F-030)

**Feature ID**: F-030
**Family**: Security
**Status**: planned
**Depends on**: F-008 (deterministic clock — exp / nbf / iat)
**Target release**: v0.12
**Estimated tasks**: ~28 (Phase 0: 7 · 1 package × 16 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.Security.Jwt/JwtBuilder.cs` builds tokens, but the real security regressions ship in the **validation** path:

- **`alg: none`** acceptance — historic OWASP top-10 JWT bug.
- **RSA-vs-HMAC key confusion** — the server expects RS256, attacker resigns with HS256 using the public key as the secret.
- **`kid` confusion** — wrong key id picked from the JWKS endpoint.
- **Token replay** — same token reused outside its expected single-use window.
- **Embedded JWK header attack** — token carries its own self-signing key.
- **`exp` skew** — accepting tokens past their expiry by N seconds.
- **`aud` / `iss` mismatch** — accepting a token issued for service A on service B.

There's also no JWKS-endpoint fixture, no key-rotation topology, no `IFakeClock` integration.

## What we deliver

A JWT attack-scenario surface, a JWKS fixture, and rotation topology — all wired through F-008's fake clock so `exp`/`nbf` assertions are deterministic.

```csharp
public sealed class JwtAttack
{
    public static string AlgNone(JwtBuilder template);
    public static string KeyConfusion(JwtBuilder template, RsaPublicKey publicKey);
    public static string KidConfusion(JwtBuilder template, string fakeKid);
    public static string EmbeddedJwk(JwtBuilder template);
    public static string Replay(string token, TimeSpan after);
}

public sealed class JwksFixture
{
    public Uri JwksUri { get; }
    public Task<JwksKey> AddKeyAsync(string kid, KeyAlgorithm alg, CancellationToken ct);
    public Task RotateAsync(JwksKey newKey, JwksKey? retired, CancellationToken ct);
}

public static class AuthAssert
{
    public static TokenAssertion Token(string token);
    public static RejectionAssertion Rejected();
}

public sealed class RejectionAssertion
{
    public RejectionAssertion With(RejectReason reason);
    public RejectionAssertion AtSkew(TimeSpan tolerance);
}
```

## Gaps closed (from SEC-1, SEC-3 in the gap analysis)

- JWT attack-scenario reproduction.
- JWKS endpoint as a real fixture.
- Key-rotation testing under `IFakeClock`.
- Clock-skew tolerance assertions.

## Providers in scope

1: `src/Rig.TUnit.Security.Jwt`.

## Exit criteria

- `JwtAttack`, `JwksFixture`, `AuthAssert` ship with 100 % line coverage.
- ≥ 6 RED-leading scenarios cover the attack surface above.
- `docs/providers/security.md` (extended) covers the attack scenarios with explicit "is this a defence we provide?" guidance.

## Dependencies on other planned features

- Upstream: F-008.
- Downstream: F-032 (OAuth flows reuse JWKS), F-033 (authz matrix).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 030-jwt-attacks-and-jwks

Read first:
- planning/jwt-attacks-and-jwks/README.md
- planning/deterministic-clock/README.md (F-008 must be shipped)
- src/Rig.TUnit.Security.Jwt/JwtBuilder.cs (existing)
- OWASP "JSON Web Token Cheat Sheet"

Generate a feature spec that:
1. Introduces JwtAttack static surface (AlgNone, KeyConfusion, KidConfusion, EmbeddedJwk, Replay).
2. JwksFixture starting an in-process Kestrel endpoint serving /.well-known/jwks.json.
3. AuthAssert.Token / Rejected with reason enum.
4. ≥ 6 RED-leading scenarios.

Constraints:
- F-008 IFakeClock for all exp / nbf / iat.
- JWKS fixture lifecycle bound to RigBuilder.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
