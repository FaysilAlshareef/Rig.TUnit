# Rig.TUnit.Storage.AzureBlob

> Azurite-backed Azure Blob Storage fixture with `BlobServiceClient` and pure-function `AzureBlobSasBuilder` for SAS query construction.

## What this package is

The Rig.TUnit Azure Blob provider. `AzureBlobFixture` spins
`mcr.microsoft.com/azure-storage/azurite` via Testcontainers and
exposes a ready `BlobServiceClient`. `AzureBlobSasBuilder` is a pure
function that constructs SAS query strings from container/blob/
permission/TTL — no side effects, unit-testable.

## When to use it

- Integration tests for blob storage upload / download / listing.
- Testing SAS token construction against Azurite.
- Verifying container lifecycle operations.
- **Not for**: production Azure testing — Azurite diverges on CORS,
  soft-delete, and change feed.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (Azurite image ~180 MB)
- `Azure.Storage.Blobs` (transitive)

## Quick start

```csharp
using Rig.TUnit.Storage.AzureBlob.Fixtures;

await using var fx = new AzureBlobFixture();
await fx.InitializeAsync();

var container = fx.Client.GetBlobContainerClient("demo");
await container.CreateIfNotExistsAsync();
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"mcr.microsoft.com/azure-storage/azurite:latest"` | Azurite image |
| `StartupTimeoutSeconds` | `int` | `60` | Azurite boot |
| `AccountName` | `string` | `"devstoreaccount1"` | Azurite default dev account |
| `AccountKey` | `string` | `"Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="` | Well-known Azurite key |

## Fixture + helper APIs

- `Rig.TUnit.Storage.AzureBlob.Fixtures.AzureBlobFixture`
- `Rig.TUnit.Storage.AzureBlob.Options.AzureBlobFixtureOptions`
- `Rig.TUnit.Storage.AzureBlob.Builder.AzureBlobRigBuilder`
- `Rig.TUnit.Storage.AzureBlob.Helpers.AzureBlobSasBuilder`

## Per-test isolation

Per-test container: `test-{IsolationKey:short}`. Teardown deletes the
container recursively. Blob operations go inside that container so
parallel tests cannot collide.

## Parallelism + performance

- First-run pull: ~20 s.
- Warm startup: ~5 s.
- Per-test container create + delete: ~50 ms.
- Per-blob upload/download: ~5–10 ms.
- Parallelism: 8+ concurrent tests.

## Troubleshooting

- **`ArgumentException: Invalid storage account name`** — the
  connection string must be `UseDevelopmentStorage=true` or the
  explicit Azurite format. Don't substitute a real account name.
- **SAS signature mismatch** — Azurite uses the well-known dev key by
  default; `AzureBlobSasBuilder` reads it from options.

See [docs/troubleshooting.md#azureblob](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Azurite diverges from real Azure on: soft-delete, change feed, CORS
  pre-flight. Tests relying on these must run against real Azure.
- SAS timestamps are UTC; building one with local time fails signature
  validation. `AzureBlobSasBuilder` always normalises to UTC.

## Benchmarks

See [`AzureBlobBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/AzureBlobBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Storage`](../Rig.TUnit.Storage/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
