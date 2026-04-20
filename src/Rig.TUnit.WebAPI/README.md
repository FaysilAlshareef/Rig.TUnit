# Rig.TUnit.WebAPI

> `WebApplicationFactory` extensions, `TestAuthHeaderBuilder`, and problem-details assertion helpers for ASP.NET Core Web API testing.

## What this package is

The HTTP counterpart to `Rig.TUnit.Grpc`. Extends Microsoft's
`WebApplicationFactory<TEntryPoint>` with a Rig-aware builder, a
`TestAuthHeaderBuilder` that crafts JWT / cookie / mTLS auth headers
matching the host's configured schemes, and `ProblemDetailsAssert` that
unpacks RFC 9457 responses for fluent assertion.

Together with `Rig.TUnit.Http` (for raw HttpClient scenarios) this covers
the full ASP.NET Core Web API surface.

## When to use it

- Integration-testing an ASP.NET Core controller or minimal-API project.
- Asserting on `ProblemDetails`-shaped error responses.
- Authenticating test requests with the same JWT / cookie the real flow uses.
- **Not for**: pure gRPC services — use `Rig.TUnit.Grpc`.

## Prerequisites

- .NET 10 SDK
- Project under test has a public `Program` entry point (top-level
  statements expose one implicitly since .NET 6).

## Quick start

```csharp
using Rig.TUnit.WebAPI.Helpers;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Helpers;

var rig = new RigBuilder()
    .WithIsolation(IsolationKey.FromExecutionContext())
    .UseWebApi(cfg => cfg.ConfigureServices(s => s.AddLogging()))
    .Build();

await using var _ = rig;
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Environment` | `string` | `"Testing"` | Sets `IHostEnvironment.EnvironmentName`. |
| `ContentRoot` | `string?` | `null` | Override for static-file-serving tests. |
| `DefaultAcceptLanguage` | `string` | `"en-US"` | Sets `Accept-Language` on the shared client. |

## Fixture + helper APIs

- `Rig.TUnit.WebAPI.Builder.WebApiRigBuilder` — CRTP builder
- `Rig.TUnit.WebAPI.Authentication.TestAuthHeaderBuilder` — JWT / cookie
- `Rig.TUnit.WebAPI.Helpers.ProblemDetailsAssert` — RFC 9457 assertion

## Per-test isolation

Each `WebApplicationFactory` lives in its own `TestServer`; DI services are
fresh per test. Auth helpers generate test-only JWTs with the
`IsolationKey`-derived subject claim so logs are cross-correlatable.

## Parallelism + performance

- Host startup: ~80 ms (`Program.cs` build + service graph).
- First-request warm-up: ~10 ms; steady-state ~0.5 ms per call (in-memory).
- Safe under full parallelism.

## Troubleshooting

- **`Unauthorized` despite a valid test JWT** — confirm the signing key
  returned by `TestAuthHeaderBuilder` matches the one the host's
  `JwtBearerOptions` is configured with; they must share the same test
  certificate.
- **Test server cannot find `Program`** — top-level statements produce an
  implicit `internal` `Program` class; add `[assembly: InternalsVisibleTo("YourTests")]`
  or declare an explicit `public partial class Program;`.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `WebApplicationFactory` runs the host on a `TestServer`, not Kestrel —
  features that require the Kestrel feature-collection (connection-ID
  inspection, raw-socket access) will not work.
- HTTP/2 and HTTP/3 negotiation is simulated — browsers talk HTTP/1.1 in
  tests regardless of the production config.

## Benchmarks

See [`WebApiBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/WebApiBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)

## License

MIT. See [LICENSE](../../LICENSE).
