# Planning — HTTP cookies / redirects / CORS / negotiation (F-043)

**Feature ID**: F-043
**Family**: HTTP
**Status**: planned
**Depends on**: —
**Target release**: v0.14
**Estimated tasks**: ~24 (Phase 0: 5 · 1 package × 14 tasks · 5 docs)

---

## Why this feature exists

After F-042 (streaming protocols), the remaining HTTP gaps are about correctness of small details that bite hard in production:

- **Cookies** — domain / path scoping, `SameSite=None|Lax|Strict`, `HttpOnly`, `Secure`, partitioned cookies.
- **Redirects** — redirect-loop guard, max-redirect cap, method-preservation on 307/308.
- **Connection-pool exhaustion** — `HttpClient` factory limits, lifetime.
- **CORS preflight** — `OPTIONS` precedes `GET`, `Access-Control-Allow-*` headers honoured.
- **Content negotiation** — 406 path, charset fallback, brotli/gzip/deflate auto-decode.
- **HTTP problem details** consistency on every error path.

## What we deliver

```csharp
public static class HttpAssert
{
    public static CookieAssertion Cookies(HttpFixture fixture);
    public static RedirectAssertion Redirects(HttpFixture fixture);
    public static PoolAssertion Pool(HttpFixture fixture);
    public static CorsAssertion Cors(HttpFixture fixture);
    public static NegotiationAssertion Negotiation(HttpFixture fixture);
}

public sealed class CookieAssertion
{
    public CookieAssertion Cookie(string name).ScopedTo(domain).WithSameSite(SameSiteMode);
    public CookieAssertion Secure(bool expected);
    public CookieAssertion Partitioned(bool expected);
}

public sealed class RedirectAssertion
{
    public RedirectAssertion Followed(int count);
    public RedirectAssertion StoppedAtLimit(int max);
    public RedirectAssertion PreservedMethodOn(int statusCode);
}

public sealed class PoolAssertion
{
    public PoolAssertion Saturated().AfterRequests(int n);
    public PoolAssertion ReusedConnection(bool expected);
}

public sealed class CorsAssertion
{
    public CorsAssertion PreflightOptionsSent();
    public CorsAssertion AllowedOrigin(string origin);
    public CorsAssertion DeniedOrigin(string origin);
}
```

## Gaps closed (from HTTP-2 in the gap analysis)

- Cookie scoping / `SameSite` / partitioned cookies.
- Redirect-loop / max-redirects.
- Pool exhaustion under load.
- CORS preflight correctness.
- Content-negotiation 406 path.

## Providers in scope

1: `src/Rig.TUnit.Http`.

## Exit criteria

- `HttpAssert.Cookies / Redirects / Pool / Cors / Negotiation` ship with 100 % line coverage.
- ≥ 6 RED scenarios.
- `docs/providers/http.md` updated.

## Dependencies on other planned features

- Upstream: none.
- Downstream: F-049 (WebAPI ProblemDetails consistency reuses these assertions).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 043-http-cookies-redirects-cors

Read first:
- planning/http-cookies-redirects-cors/README.md
- src/Rig.TUnit.Http/* (current state)
- RFC 6265 (cookies), RFC 9110 (HTTP semantics), CORS spec

Generate a feature spec that:
1. Introduces HttpAssert.Cookies / Redirects / Pool / Cors / Negotiation.
2. ≥ 6 RED scenarios.

Constraints:
- HttpFixture exposes pool metrics; assertion reads, doesn't poke internals.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
