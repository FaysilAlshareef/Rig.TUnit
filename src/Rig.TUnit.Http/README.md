# Rig.TUnit.Http

> In-memory HTTP mock with matcher matrix, response builders, scenario state machine, delay / jitter / failure injection, and record/replay.

## What this package is

A rich in-memory HTTP mock. Supports:
- Matcher matrix — method, path, headers, query, JSONPath, SOAP body.
- Response builders — status, headers, JSON, binary, server-sent events.
- Scenario state machine — per-call sequential responses (first call
  returns A, second B, …).
- Failure injection — delay + jitter + intermittent-5xx for resilience
  testing.
- Record / replay — capture real exchanges and replay offline.
- Call verification — `mock.Verify(method, path).Called(N)`.

Every setup **must** end with `.And()` so the fluent chain materialises
into the mock.

## When to use it

- Testing any HTTP client code — service SDK wrappers, retry policies,
  circuit breakers.
- Replay-testing production-shape responses without hitting the real
  service.
- Verifying resilience policies (retries, timeouts) against synthetic
  failure patterns.
- **Not for**: integration tests requiring real HTTP semantics (TLS,
  HTTP/2, connection pooling) — use `WebApplicationFactory`.

## Prerequisites

- .NET 10 SDK
- Consumer uses `HttpClient` with an injected `HttpMessageHandler` or
  named `IHttpClientFactory` registration.

## Quick start

```csharp
using Rig.TUnit.Http;
using System.Net;

var mock = new HttpMock();
mock.When.Post().Path("/orders")
    .JsonPathEquals("customer.id", "c-1")
    .Responds()
    .WithStatus(HttpStatusCode.Created)
    .WithJson("{\"id\":42}")
    .And();

var client = mock.CreateClient();
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultBaseAddress` | `Uri` | `new Uri("http://mock")` | Base URI for `CreateClient` |
| `StrictMatching` | `bool` | `true` | Fail on request with no matching setup |
| `CaseSensitiveHeaders` | `bool` | `false` | Match headers case-insensitively per HTTP spec |

## Fixture + helper APIs

- `Rig.TUnit.Http.HttpMock`
- `Rig.TUnit.Http.Builders.HttpMockSetupBuilder`
- `Rig.TUnit.Http.Builders.HttpMockResponseBuilder`
- `Rig.TUnit.Http.Helpers.HttpMockRecorder`

## Per-test isolation

Each `HttpMock` instance is scoped. No shared state; parallel tests
each own their mock. Safe under full parallelism.

## Parallelism + performance

- Zero container startup.
- Per-request: matcher evaluation + response build ~500 µs.
- Safe under full parallelism.

## Troubleshooting

- **404 / handler returns default** — missing `.And()` terminator.
  Every setup chain must end with `.And()` to register.
- **`UnmatchedRequestException`** — a request arrived with no matching
  setup. Either add a matcher or disable `StrictMatching`.
- **Record mode captures nothing** — `HttpMock.RecordAgainst(handler)`
  must be called BEFORE the test's HTTP calls; recording a completed
  exchange log is not supported.

See [docs/troubleshooting.md#http](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Scenario state machine resets on `mock.Reset()`; between tests is
  implicit (each test owns its mock).
- JSONPath matching uses the `System.Text.Json` evaluator — it accepts
  a narrower subset than `Newtonsoft.Json`'s JSONPath; stick to basic
  dotted-path + array-index forms.
- Delay injection is driven by `TimeProvider`; freeze time via
  `FakeTimeProvider` for deterministic retry-policy tests.

## Benchmarks

See [`HttpMockBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/HttpMockBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Sibling: [`Rig.TUnit.Resilience`](../Rig.TUnit.Resilience/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
