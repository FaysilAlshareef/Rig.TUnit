# Rig.TUnit.Microservices.Contracts

> Pact-style contract-test harness: `ContractPact` + `ContractInteraction` + builders that replay consumer expectations against a provider host.

## What this package is

A lightweight in-process alternative to Pact.Net. A `ContractPact` captures
the set of HTTP or gRPC interactions a consumer expects; the harness replays
them against the provider's actual `WebApplicationFactory` host and verifies
every response matches the expectation. Shipped as a family-level contract
(ADR-005) so every microservice in the estate has the same verifier surface.

It does not broker files with a remote Pact Broker — the assumption is that
contracts are versioned alongside the consumer code in a shared package.

## When to use it

- Provider-side verification: "does my microservice still satisfy what the
  consumer recorded last week?"
- Consumer-side recording: capture the expected interactions inside a unit
  test; commit the `.contract.json`.
- **Not for**: end-to-end tests where you want a real network between parties
  — that's an integration test, not a contract test.

## Prerequisites

- .NET 10 SDK
- Consumer project has `Rig.TUnit.Http` for building interactions.

## Quick start

```csharp
using Rig.TUnit.Microservices.Contracts;

var pact = new ContractPact(
    consumer: "orders-web",
    provider: "orders-api",
    interactions: Array.Empty<ContractInteraction>());

// Verify against the running provider fixture.
await pact.VerifyAsync(providerBaseUri: new Uri("http://localhost:5097"));
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `StrictMatching` | `bool` | `true` | Reject responses with unexpected extra fields. |
| `TimeoutPerInteraction` | `TimeSpan` | `10s` | Per-replay HTTP timeout. |
| `CaseSensitiveHeaders` | `bool` | `false` | Off-by-default per HTTP spec. |

## Fixture + helper APIs

- `Rig.TUnit.Microservices.Contracts.ContractPact`
- `Rig.TUnit.Microservices.Contracts.ContractInteraction`
- `Rig.TUnit.Microservices.Contracts.Helpers.PactFileReader` / `PactFileWriter`

## Per-test isolation

Each `ContractPact` is a value-type recording; verification runs against the
provider-supplied base URI. No shared state.

## Parallelism + performance

- Per-interaction replay: ~2-5 ms (in-process `HttpClient`).
- Safe under full parallelism — each test owns its pact instance.

## Troubleshooting

- **Verification fails with `ExtraField(s) present in response`** — set
  `StrictMatching = false` if the extra field is a deliberate backward-
  compatible addition.
- **`ContractInteraction.Request.Body` is null after read** — call
  `ContractInteraction.ReadBodyAsync()` before asserting; the raw stream is
  read lazily.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- JSON comparison is structural (property-set equality), not byte-exact —
  field ordering is ignored.
- gRPC interactions serialise via Protobuf wire format; message equality is
  byte-exact because that is what the wire sees.

## Benchmarks

See [`ContractsBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/ContractsBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [ADR-005 — family-level contracts](../../docs/adr/ADR-005-family-level-contracts.md)
- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)

## License

MIT. See [LICENSE](../../LICENSE).
