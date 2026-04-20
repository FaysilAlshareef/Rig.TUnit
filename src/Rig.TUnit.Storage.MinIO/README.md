# Rig.TUnit.Storage.MinIO

> Testcontainers-backed MinIO fixture (`minio/minio`) with `IMinioClient` and pure-function `MinIOSasBuilder` for presigned-URL construction.

## What this package is

The Rig.TUnit MinIO provider. `MinIOFixture` spins the `minio/minio`
container via Testcontainers and exposes a ready `IMinioClient`.
`MinIOSasBuilder` is a pure function that builds presign-request
parameters (bucket + object + verb + TTL) — unit-testable without
hitting the server.

MinIO is wire-compatible with S3, so this is also the fast path for
testing S3 behaviour without LocalStack overhead.

## When to use it

- Integration tests for object storage with S3-compatible semantics.
- Presigned-URL construction + download/upload verification.
- **Not for**: S3 features not implemented by MinIO (S3 Select, Glacier
  tiers, Intelligent Tiering).

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (MinIO image ~150 MB)
- `Minio` SDK 6.x (transitive, Apache-2.0).

## Quick start

```csharp
using Minio.DataModel.Args;
using Rig.TUnit.Storage.MinIO.Fixtures;

await using var fx = new MinIOFixture();
await fx.InitializeAsync();

await fx.Client.MakeBucketAsync(new MakeBucketArgs().WithBucket("demo"));
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"minio/minio:latest"` | MinIO image |
| `StartupTimeoutSeconds` | `int` | `60` | MinIO boot |
| `RootUser` | `string` | `"rigtunit"` | Default admin user |
| `RootPassword` | `string` | `"rigtunit-minio"` | Default admin password (8+ chars required) |

## Fixture + helper APIs

- `Rig.TUnit.Storage.MinIO.Fixtures.MinIOFixture`
- `Rig.TUnit.Storage.MinIO.Options.MinIOFixtureOptions`
- `Rig.TUnit.Storage.MinIO.Builder.MinIORigBuilder`
- `Rig.TUnit.Storage.MinIO.Helpers.MinIOSasBuilder`

## Per-test isolation

Per-test bucket: `test-{IsolationKey:short}`. Teardown removes the
bucket recursively (`RemoveBucketAsync` requires the bucket be empty —
the fixture clears it first).

## Parallelism + performance

- First-run pull: ~20 s.
- Warm startup: ~3 s.
- Per-test bucket create + delete: ~60 ms.
- Parallelism: 8+ concurrent tests.

## Troubleshooting

- **`AccessDenied` on presign** — clock skew between the fixture and
  signature builder; MinIO enforces ±15 min. Ensure both use UTC.
- **`BucketNotEmpty` on teardown** — test wrote an object after the
  fixture's cleanup pass. Ensure `using` / `await using` scopes are
  tight.

See [docs/troubleshooting.md#minio](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- MinIO is S3-wire-compatible but not byte-identical — S3-specific
  error codes can differ, and some advanced features (S3 Select,
  Intelligent Tiering) are absent. Use LocalStack for those.
- Default region is `us-east-1`; presigned URLs without explicit
  region use that.
- Root password must be at least 8 characters (MinIO enforced).

## Benchmarks

See [`MinIOBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/MinIOBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Storage`](../Rig.TUnit.Storage/README.md)
- Sibling: [`Rig.TUnit.Storage.S3`](../Rig.TUnit.Storage.S3/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
