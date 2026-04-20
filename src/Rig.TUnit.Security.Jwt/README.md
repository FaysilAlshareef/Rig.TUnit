# Rig.TUnit.Security.Jwt

> Fluent `JwtBuilder` producing tokens a real `JwtBearerHandler` accepts — no test-only bypass. HS256 / RS256, kid rotation, negative builders.

## What this package is

A JWT testing kit that produces tokens that pass through Microsoft's
real `JwtBearerHandler` validation pipeline. Supports HS256 / RS256
signing, `kid` rotation, issuer/audience binding, and — critically —
negative builders (`BuildExpired`, `BuildNotYetValid`, `BuildTampered`)
so tests can prove the server rejects bad tokens rather than silently
accepting them.

No `AuthenticationScheme.AllowAnonymous` bypass, no `AuthorizeFilter`
override — the production auth code runs unchanged.

## When to use it

- Integration tests where an API requires a valid JWT.
- Verifying negative paths: expired, tampered, not-yet-valid, wrong
  audience.
- Kid-rotation testing for key-rollover scenarios.
- **Not for**: unit tests of claims transformation — use a
  `ClaimsPrincipal` factory directly.

## Prerequisites

- .NET 10 SDK
- Project under test uses `Microsoft.AspNetCore.Authentication.JwtBearer`.

## Quick start

```csharp
using Rig.TUnit.Security.Jwt.Builder;

var token = JwtBuilder.Create(new JwtBuilderOptions
                {
                    DefaultIssuer = "my-issuer",
                    DefaultAudience = "my-audience",
                })
            .Subject("alice")
            .Claim("role", "admin")
            .ExpiresIn(TimeSpan.FromMinutes(15))
            .SignedWithHs256(keyBytes)
            .Build();
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultIssuer` | `string` | `"rig.tunit"` | Issuer claim |
| `DefaultAudience` | `string` | `"test-audience"` | Audience claim |
| `DefaultTtl` | `TimeSpan` | `15m` | TTL when `ExpiresIn` not called |
| `DefaultAlgorithm` | `string` | `"HS256"` | Override in `SignedWith…` |

## Fixture + helper APIs

- `Rig.TUnit.Security.Jwt.Builder.JwtBuilder`
- `Rig.TUnit.Security.Jwt.Options.JwtBuilderOptions`
- `Rig.TUnit.Security.Jwt.Helpers.JwksKeyRotationHelper`

## Per-test isolation

Each `JwtBuilder` instance is standalone; no global state. Keys can be
per-test via `IsolationKey.FromExecutionContext()` as seed material.

## Parallelism + performance

- HS256 signing: ~50 µs.
- RS256 signing: ~2 ms (key generation dominant).
- Safe under full parallelism.

## Troubleshooting

- **Token rejected with `IDX10503` (signature invalid)** — the signing
  key and the handler's validation key don't match. Ensure
  `TokenValidationParameters.IssuerSigningKey` uses the same bytes.
- **`BuildExpired` token accepted** — the handler's `ClockSkew` default
  is 5 minutes; `BuildExpired(TimeSpan.FromMinutes(10))` clears that
  window.

See [docs/troubleshooting.md#jwt](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `kid` rotation: `JwksKeyRotationHelper.SetCurrentKid("new")` updates
  the JWKS endpoint's active key; tokens signed with the old `kid`
  continue validating if the handler caches the full key set.
- HS256 key material must be at least 256 bits (32 bytes); shorter keys
  are padded and the handler rejects them.
- `BuildTampered` flips one bit of the signature — the resulting token
  round-trips the header/claims fine and fails only at signature check.

## Benchmarks

See [`JwtBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/JwtBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Security`](../Rig.TUnit.Security/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
