# Rig.TUnit.Storage

> Family-base package for object-storage test fixtures: `StorageFixture`, path-sandbox helpers, and the cross-provider storage contract.

## What this package is

The base package for the Storage family (`Rig.TUnit.Storage.AzureBlob`,
`.FileSystem`, `.MinIO`, `.S3`). It defines the abstract `StorageFixture`
contract — put / get / list / delete with URI-sandbox constraints — plus the
`StorageContract` suite that every leaf provider `[InheritsTests]` from to
prove parity.

Install this one directly only when you are writing a new storage provider
or want the shared assertions without a specific backend.

## When to use it

- Authoring a new storage backend (FTP, OCI Object Storage, NFS, …).
- Writing provider-agnostic test code that can be pointed at any backend.
- **Not for**: concrete storage testing — install one of the four leaf
  packages.

## Prerequisites

- .NET 10 SDK
- `System.IO.Abstractions` for the `FileSystem` provider; other leaves bring
  their own SDKs.

## Quick start

```csharp
using Rig.TUnit.Storage.Builder;
using Rig.TUnit.Storage.Fixtures;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Helpers;

var rig = new RigBuilder()
    .WithIsolation(IsolationKey.FromExecutionContext())
    .Build();

await using var _ = rig;
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `RootPath` | `string` | `$"test-{IsolationKey}"` | Sandbox prefix for all keys written during the test. |
| `MaxObjectSizeBytes` | `long` | `10_000_000` | Guard against accidental big-object uploads. |
| `EnableVersioning` | `bool` | `false` | Require the backend to support object versioning. |

## Fixture + helper APIs

- `Rig.TUnit.Storage.Fixtures.StorageFixture` — abstract contract
- `Rig.TUnit.Storage.Contracts.StorageContract` — family-level TUnit suite
- `Rig.TUnit.Storage.Assertions.StorageAssert` — existence / size / content

## Per-test isolation

`RootPath` defaults to `$"test-{IsolationKey}"` so every test writes under a
unique prefix; teardown deletes the prefix recursively. Concrete providers
(S3, AzureBlob) append the key verbatim to their bucket / container name
with sandbox enforcement via path-normalisation helpers.

## Parallelism + performance

## §9 — N/A: family-base contract; parallelism profile depends on the
concrete provider. See each leaf README.

## Troubleshooting

- **`StoragePathEscapedSandbox`** — a test tried to read a key outside the
  per-test root. The sandbox helper blocked it; fix the test.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Path separators differ (`/` cloud vs `\` Windows filesystem); the base
  uses `/` as the canonical separator and `FileSystemFixture` translates.

## Benchmarks

## §12 — N/A: family-base; concrete providers have individual
`*Benchmarks.cs` entries under `tests/Rig.TUnit.Benchmarks/`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [ADR-005 — family-level contracts](../../docs/adr/ADR-005-family-level-contracts.md)
- [Glossary](../../docs/glossary.md)

## License

MIT. See [LICENSE](../../LICENSE).
