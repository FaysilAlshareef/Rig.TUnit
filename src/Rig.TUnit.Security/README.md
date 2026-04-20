# Rig.TUnit.Security

> Security family-base: `ISecurityRig`, `SecurityFixtureBase`, `SecurityAssert` with HTTP 401/403 helpers.

## What this package is

The shared contract for the Security family (`.Jwt`, `.Mtls`, `.OAuth`,
`.Policies`). Defines the assertion surface every security test expects
— `SecurityAssert.HttpIsUnauthorized(response)`,
`HttpIsForbidden(response)`, `ClaimsPrincipalIsAuthenticated(principal)`
— so test code looks identical whether you're using JWT, mTLS, OAuth,
or ASP.NET Core Policies.

Install one of the leaves directly for concrete testing.

## When to use it

- Authoring a new security-fixture type.
- Writing provider-agnostic security assertions.
- **Not for**: concrete security testing — install a leaf package.

## Prerequisites

- .NET 10 SDK

## Quick start

```csharp
using Rig.TUnit.Security;

var response = await client.SendAsync(request);
await SecurityAssert.HttpIsUnauthorized(response);
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultScheme` | `string` | `"Test"` | Authentication scheme for fixtures that register one |
| `ValidateIssuer` | `bool` | `true` | Propagate to JwtBearerOptions when paired with `.Jwt` |
| `ValidateAudience` | `bool` | `true` | Same for audience |
| `ClockSkew` | `TimeSpan` | `5m` | Leeway on `exp` / `nbf` |

## Fixture + helper APIs

- `Rig.TUnit.Security.ISecurityRig`
- `Rig.TUnit.Security.Fixtures.SecurityFixtureBase`
- `Rig.TUnit.Security.Assertions.SecurityAssert`

## Per-test isolation

Security fixtures typically own ephemeral key material (JWT signing
keys, certificate chains) per-test, keyed by `IsolationKey`. Each leaf
details the specifics.

## Parallelism + performance

## §9 — N/A: family-base; per-provider. JWT signing is cheap; mTLS
certificate generation is expensive (~20 ms) and cached per fixture.

## Troubleshooting

- **`SecurityAssert.HttpIsUnauthorized` fails on expected-401** — check
  the API's `[Authorize]` wiring actually runs before your handler;
  a missing `app.UseAuthorization()` lets requests through as 200.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Every security leaf integrates with the real `JwtBearerHandler` /
  `CertificateAuthentication` middleware — no test-only bypass. That is
  deliberate; bypassed auth is not a valid security test.

## Benchmarks

## §12 — N/A: family-base; concrete leaves have individual
`*Benchmarks.cs` entries under `tests/Rig.TUnit.Benchmarks/`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Concrete: [`Rig.TUnit.Security.Jwt`](../Rig.TUnit.Security.Jwt/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
