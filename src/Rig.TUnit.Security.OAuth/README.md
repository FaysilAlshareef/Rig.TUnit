# Rig.TUnit.Security.OAuth

> In-process mock OAuth 2.0 / OIDC server with `/authorize`, `/token`, `/jwks`, `/.well-known/openid-configuration` — client-credentials + auth-code + PKCE S256 + refresh.

## What this package is

`MockOAuthServer` is an in-process OAuth 2.0 / OpenID Connect mock that
exposes the four endpoints client code expects (`/authorize`, `/token`,
`/jwks`, `/.well-known/openid-configuration`) and issues JWTs signed
with a rotating key set. Tokens are accepted by a real
`JwtBearerHandler` via JWKS discovery — the production validation
pipeline runs unchanged. Supports the three grant flows tests need:
client-credentials, authorization-code + PKCE (S256), and refresh-token.

## When to use it

- Integration tests for services that authenticate via OAuth / OIDC.
- Verifying PKCE flow and refresh-token rotation.
- Testing JWKS key rollover without touching a real identity provider.
- **Not for**: end-to-end tests against a real IDP — `MockOAuthServer`
  does not replicate provider-specific quirks (Azure AD B2C claim
  mappings, Auth0 rules, etc.).

## Prerequisites

- .NET 10 SDK
- ASP.NET Core host (fixture uses `FrameworkReference Microsoft.AspNetCore.App`).

## Quick start

```csharp
using Rig.TUnit.Security.OAuth.Fixtures;

await using var mock = new MockOAuthServer(new MockOAuthServerOptions
{
    Issuer = "https://mock",
});
await mock.StartAsync();
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Issuer` | `string` | `"https://mock.oauth"` | OIDC issuer URL |
| `DefaultScopes` | `string[]` | `["openid", "profile"]` | Tokens minted with these by default |
| `AccessTokenTtl` | `TimeSpan` | `15m` | TTL for access tokens |
| `RefreshTokenTtl` | `TimeSpan` | `7d` | TTL for refresh tokens |
| `EnablePkce` | `bool` | `true` | Require PKCE for auth-code flow |

## Fixture + helper APIs

- `Rig.TUnit.Security.OAuth.Fixtures.MockOAuthServer`
- `Rig.TUnit.Security.OAuth.Options.MockOAuthServerOptions`
- `Rig.TUnit.Security.OAuth.Builder.OAuthRigBuilder`

## Per-test isolation

Each `MockOAuthServer` binds a random ephemeral port; multiple fixtures
run concurrently without collision. Signing keys are per-fixture
(rotatable via `mock.RotateSigningKey()`).

## Parallelism + performance

- Startup: ~25 ms per fixture.
- `/token` endpoint latency: ~5 ms (JWT signing dominant).
- Safe under full parallelism.

## Troubleshooting

- **JWKS discovery fails** — the API under test cached the JWKS
  endpoint at startup; rotating keys mid-test requires resetting the
  cache or raising `TokenValidationParameters.RefreshInterval`.
- **PKCE `code_verifier` mismatch** — ensure the verifier on `/token`
  matches the S256 hash sent on `/authorize`; use the helper's PKCE
  pair generator to avoid manual errors.

See [docs/troubleshooting.md#oauth](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `/.well-known/openid-configuration` is served at the issuer base URL;
  misconfigured `Authority` values on consumer fixtures will fail
  discovery silently.
- Refresh-token rotation: each `/token` call with `grant_type=refresh_
  token` issues a new refresh and invalidates the old. Tests asserting
  "old refresh still works" will fail — by design.

## Benchmarks

See [`OAuthBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/OAuthBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Security`](../Rig.TUnit.Security/README.md)
- Sibling: [`Rig.TUnit.Security.Jwt`](../Rig.TUnit.Security.Jwt/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
