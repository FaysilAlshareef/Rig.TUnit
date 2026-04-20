# Rig.TUnit.Grpc

> In-process gRPC test host, client factory, and error-metadata helpers for `Grpc.AspNetCore` services.

## What this package is

A `WebApplicationFactory`-style host that spins up your gRPC service in the
same process as the test, so calls skip the network stack entirely while
still exercising the full MVC pipeline (authentication, interceptors,
problem-details translation). The `GrpcTestHost` owns the `TestServer`,
wires a gRPC channel over the `HttpMessageHandler`, and exposes strongly-typed
clients for every service you register.

The helpers also include an `RpcException` inspector that unpacks the
binary `problem-details-bin` metadata key Microsoft's error-handling
middleware emits, so assertions can target `ProblemDetails.Status`,
`Detail`, and `Type` directly.

## When to use it

- Testing a gRPC-first service (command side, query side, gateway).
- Verifying authentication policies behave correctly over gRPC.
- Asserting on structured error responses (`RpcException` + problem-details).
- **Not for**: testing the external HTTP/2 surface — use an integration
  test with a real port for that.

## Prerequisites

- .NET 10 SDK
- Project under test references `Grpc.AspNetCore`.

## Quick start

```csharp
using Rig.TUnit.Grpc.Builder;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Helpers;

var rig = new RigBuilder()
    .WithIsolation(IsolationKey.FromExecutionContext())
    .UseGrpc(cfg => cfg.ConfigureServices(s => s.AddLogging()))
    .Build();

await using var _ = rig;
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `EnableDetailedErrors` | `bool` | `true` | Include stack traces in `RpcException.Status.Detail`. |
| `MaxReceiveMessageSize` | `int?` | `null` | Channel-side limit; `null` = gRPC default 4 MB. |
| `ClientTimeout` | `TimeSpan` | `30s` | Default deadline applied to generated clients. |

## Fixture + helper APIs

- `Rig.TUnit.Grpc.Builder.GrpcRigBuilder` — CRTP builder
- `Rig.TUnit.Grpc.Helpers.GrpcTestHost` — in-process host + channel
- `Rig.TUnit.Grpc.Helpers.ProblemDetailsAssert` — unpack `problem-details-bin`

## Per-test isolation

Every `GrpcTestHost` runs in its own `TestServer`; the channel's base URI
includes the `IsolationKey` so traces and logs segregate cleanly. Services
registered with DI are fresh per test.

## Parallelism + performance

- Host startup: ~40 ms (cached assembly scan amortises after first test).
- Per-call overhead: ~200 µs (in-memory pipe — no TCP round-trip).
- Safe under full test parallelism — each test owns its `TestServer`.

## Troubleshooting

- **`Grpc.Core.RpcException: Status(StatusCode="Internal")`** — inspect
  `ex.Trailers` for `problem-details-bin`; `ProblemDetailsAssert.From(ex)`
  does this for you.
- **Host startup slow** — first test in a session pays for gRPC reflection
  assembly scan; subsequent tests reuse the cached result.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `RpcException.Trailers` is empty until the call completes — probe it
  inside the `catch` branch, not during streaming.
- gRPC's exception translation interceptor (`ApplicationExceptionInterceptor`)
  serialises `IProblemDetailsProvider`-typed exceptions into binary metadata
  — non-typed exceptions become generic `Internal` status codes.

## Benchmarks

See [`GrpcHostBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/GrpcHostBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)

## License

MIT. See [LICENSE](../../LICENSE).
