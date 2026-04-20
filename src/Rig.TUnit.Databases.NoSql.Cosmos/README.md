# Rig.TUnit.Databases.NoSql.Cosmos

> Testcontainers-backed Linux Cosmos emulator fixture with `RuChargeCapture` and `PartitionKeyDistributionChecker`.

## What this package is

The Rig.TUnit Cosmos DB provider. `CosmosFixture` spins the
`mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview`
Linux emulator via the generic Testcontainers API (the dedicated
`Testcontainers.CosmosDb` module targets the legacy Windows emulator —
incompatible, see testcontainers-dotnet#1306). Ships two novel helpers:
`RuChargeCapture` (per-operation RU budget assertions) and
`PartitionKeyDistributionChecker` (max-share + normalised Shannon entropy
hot-partition detector).

## When to use it

- Integration tests against the Cosmos API surface.
- Asserting per-operation RU budgets stay within design targets.
- Detecting hot-partition drift before production.
- **Not for**: Windows CI runners — the Linux emulator cannot host there.
  Tests marked `[Category("cosmos")]` auto-skip via runtime
  `OperatingSystem.IsWindows()` guard.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (Linux containers only)
- `Microsoft.Azure.Cosmos` 3.x (transitive)

## Quick start

```csharp
using Microsoft.Azure.Cosmos;
using Rig.TUnit.Databases.NoSql.Cosmos.Fixtures;
using Rig.TUnit.Databases.NoSql.Cosmos.Helpers;

await using var fx = new CosmosFixture();
await fx.InitializeAsync();

using var client = new CosmosClient(fx.ConnectionString);
var db = await client.CreateDatabaseIfNotExistsAsync(fx.DatabaseName);
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview"` | Emulator image |
| `StartupTimeoutSeconds` | `int` | `300` | Emulator boot is slow |
| `DatabaseName` | `string` | `"rigtunit"` | Default database |
| `HttpsGatewayPort` | `int` | `8081` | Emulator gateway port |

## Fixture + helper APIs

- `Rig.TUnit.Databases.NoSql.Cosmos.Fixtures.CosmosFixture`
- `Rig.TUnit.Databases.NoSql.Cosmos.Options.CosmosFixtureOptions`
- `Rig.TUnit.Databases.NoSql.Cosmos.Builder.CosmosRigBuilder`
- `Rig.TUnit.Databases.NoSql.Cosmos.Helpers.RuChargeCapture`
- `Rig.TUnit.Databases.NoSql.Cosmos.Helpers.PartitionKeyDistributionChecker`

## Per-test isolation

Container-per-test (`IsolationKey` suffix) is cost-prohibitive — the
emulator takes ~2 min to start. Default is one fixture per test-class
with per-test container names (`test-cosmos-{IsolationKey}`) created via
`CreateContainerIfNotExistsAsync` and deleted on teardown.

## Parallelism + performance

- First-run pull: ~2–3 min (~3 GB image).
- Warm startup: ~90–120 s.
- Per-test container create + delete: ~200 ms + RU cost.
- Parallelism: Linux-only, typically 2–4 concurrent tests; emulator does
  not like high churn.

## Troubleshooting

- **Tests skipped on Windows CI** — expected; Cosmos tests gate on
  `OperatingSystem.IsWindows()` returning false. Run the matrix cell on
  Linux runners.
- **`TransportException`** — emulator still warming up; fixture waits but
  in a busy CI cell the default timeout can be exceeded. Raise
  `StartupTimeoutSeconds`.
- **RU budget assertions flake** — Cosmos RU charges drift with index
  policy and data shape; capture a baseline per-operation first, then
  assert within a 15 % tolerance.

See [docs/troubleshooting.md#cosmos](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Emulator uses a self-signed certificate; the fixture disables cert
  validation on the embedded `HttpClient`. Do not reuse that handler in
  production.
- `RuChargeCapture` is thread-safe; aggregate RU budgets across parallel
  ops with `capture.Total`.
- `PartitionKeyDistributionChecker.IsBalanced(keys, threshold)` uses
  normalised entropy; the default threshold is `0.9` (0=maximally skewed,
  1=uniform).

## Benchmarks

See [`CosmosBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/CosmosBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`. RU-charge assertions are a
common regression vector and are tracked closely.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)
- Family base: [`Rig.TUnit.Databases.NoSql`](../Rig.TUnit.Databases.NoSql/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
