# Rig.TUnit.Storage.S3

> LocalStack-backed Amazon S3 fixture with `IAmazonS3` and pure-function `S3SasBuilder` for presigned-URL construction.

## What this package is

The Rig.TUnit AWS S3 provider. `S3Fixture` spins LocalStack with the
S3 feature enabled and exposes a ready `IAmazonS3` pointing at it.
`S3SasBuilder` builds presign-request parameters purely — no server
call required — so tests can assert on URL shape and expiry before
actually hitting the signing endpoint.

Pick this when you need AWS-specific quirks (S3 Select, Intelligent
Tiering, IAM policy stubs); pick `.MinIO` for the faster wire-compatible
path.

## When to use it

- Integration tests using the full AWS SDK surface.
- Verifying S3 Select / intelligent-tiering / lifecycle policies.
- Cross-cutting tests that exercise IAM role assumptions.
- **Not for**: simple put/get — `.MinIO` is faster.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (LocalStack image ~400 MB)
- `AWSSDK.S3` (transitive)

## Quick start

```csharp
using Amazon.S3.Model;
using Rig.TUnit.Storage.S3.Fixtures;

await using var fx = new S3Fixture();
await fx.InitializeAsync();

await fx.Client.PutBucketAsync("demo");
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"localstack/localstack:3"` | LocalStack image |
| `StartupTimeoutSeconds` | `int` | `120` | LocalStack boot |
| `Region` | `string` | `"us-east-1"` | AWS region label |
| `AccessKeyId` | `string` | `"test"` | LocalStack dev |
| `SecretAccessKey` | `string` | `"test"` | LocalStack dev |

## Fixture + helper APIs

- `Rig.TUnit.Storage.S3.Fixtures.S3Fixture`
- `Rig.TUnit.Storage.S3.Options.S3FixtureOptions`
- `Rig.TUnit.Storage.S3.Builder.S3RigBuilder`
- `Rig.TUnit.Storage.S3.Helpers.S3SasBuilder`

## Per-test isolation

Per-test bucket: `test-{IsolationKey:short}`. Teardown clears and
deletes. LocalStack handles bucket churn well.

## Parallelism + performance

- First-run pull: ~30 s.
- Warm startup: ~10 s.
- Per-test bucket create + delete: ~80 ms.
- Parallelism: 6+ concurrent tests.

## Troubleshooting

- **`PermanentRedirect` on get/put** — LocalStack forces path-style
  addressing; set `ForcePathStyle=true` on the AWSSDK client (the
  fixture does this by default).
- **`InvalidBucketName`** — bucket names must be lowercase, 3–63 chars,
  no underscores; the `IsolationKey` suffix is filesystem-safe and
  shouldn't break this, but custom prefixes can.

See [docs/troubleshooting.md#s3](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- LocalStack's S3 diverges from real AWS on: rate limiting (never
  throttles), consistency (always strong), IAM policy enforcement
  (permissive by default). Production parity tests must run against
  real AWS.
- Presigned URLs expire in GMT; clock-skewed hosts produce 403.
- S3 Select is supported in LocalStack but with a subset of SQL — test
  expression compatibility first.

## Benchmarks

See [`S3Benchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/S3Benchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Storage`](../Rig.TUnit.Storage/README.md)
- Sibling: [`Rig.TUnit.Storage.MinIO`](../Rig.TUnit.Storage.MinIO/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
