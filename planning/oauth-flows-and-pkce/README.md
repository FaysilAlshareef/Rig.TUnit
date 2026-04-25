# Planning — OAuth flows + PKCE + DPoP + refresh rotation (F-032)

**Feature ID**: F-032
**Family**: Security
**Status**: planned
**Depends on**: F-008 (clock for token expiry / refresh windows), F-030 (JWKS fixture for issuer keys)
**Target release**: v0.14
**Estimated tasks**: ~30 (Phase 0: 7 · 1 package × 18 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.Security.OAuth/MockOAuthServer.cs` exists but is not fluently integrated and lacks discoverable client-credentials, authorization-code, PKCE, DPoP, or refresh-token rotation semantics. Real OAuth deployments fail in subtle ways:

- PKCE `code_verifier` length / charset out-of-spec — server accepts.
- Refresh-token reuse (replay) — should invalidate the entire family.
- Redirect-URI exact-match bypassed (substring match).
- Scope downgrade silently accepted.
- DPoP / mTLS-bound tokens used without binding check.
- Token audience binding — issued for `api.foo`, accepted by `api.bar`.

## What we deliver

A `WithOAuthServer(Action<IMockOAuthServerBuilder>)` builder method on RigBuilder; the server hosts:
- Discoverable `/.well-known/openid-configuration`.
- JWKS endpoint (reusing F-030's `JwksFixture`).
- `token` / `authorize` endpoints supporting client_credentials, authorization_code, refresh_token, PKCE, DPoP.

```csharp
public interface IMockOAuthServerBuilder
{
    IMockOAuthServerBuilder WithClientCredentials(string clientId, string secret, params string[] scopes);
    IMockOAuthServerBuilder WithAuthorizationCode(string clientId, Uri redirectUri, params string[] scopes);
    IMockOAuthServerBuilder WithPkce(bool required = true);
    IMockOAuthServerBuilder WithDpop(bool required = true);
    IMockOAuthServerBuilder WithRefreshToken(TimeSpan slidingExpiry, bool rotateOnUse = true);
    IMockOAuthServerBuilder WithCustomClaims(string issuer, string audience, IDictionary<string, object> claims);
}

public static class OAuthAssert
{
    public static FlowAssertion Flow(OAuthGrantType grant);
    public static RefreshAssertion RefreshFamily(string refreshToken);
    public static DpopAssertion Bound(string token);
}

public sealed class RefreshAssertion
{
    public RefreshAssertion Invalidated().BecauseReused();
    public RefreshAssertion RotatedOnUse(int times);
}
```

## Gaps closed (from SEC-2 + SEC-5 in the gap analysis)

- PKCE / code-verifier validation.
- Redirect-URI exact-match.
- Refresh-token rotation + family invalidation on replay.
- DPoP / mTLS-binding checks.
- Discoverable issuer metadata.

## Providers in scope

1: `src/Rig.TUnit.Security.OAuth`.

## Exit criteria

- `WithOAuthServer` and `OAuthAssert` ship with 100 % line coverage.
- ≥ 7 RED-leading scenarios across grant types.
- `docs/providers/security.md` updated with full OAuth section.

## Dependencies on other planned features

- Upstream: F-008, F-030.
- Downstream: F-033 (authz matrix uses OAuthFixture's tokens).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 032-oauth-flows-and-pkce

Read first:
- planning/oauth-flows-and-pkce/README.md
- planning/deterministic-clock/README.md (F-008 must be shipped)
- planning/jwt-attacks-and-jwks/README.md (F-030 must be shipped)
- src/Rig.TUnit.Security.OAuth/* (current state)
- RFC 6749, 7636 (PKCE), 9449 (DPoP)

Generate a feature spec that:
1. Introduces IMockOAuthServerBuilder + WithOAuthServer on RigBuilder.
2. Discoverable issuer with /.well-known/openid-configuration.
3. JWKS endpoint reused from F-030.
4. OAuthAssert.Flow / RefreshFamily / Bound assertions.
5. ≥ 7 RED-leading scenarios.

Constraints:
- F-008 IFakeClock for refresh / access token expiry.
- Pre-release library — no [Obsolete].
- Server lifecycle bound to RigBuilder.

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
