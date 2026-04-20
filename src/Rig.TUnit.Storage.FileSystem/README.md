# Rig.TUnit.Storage.FileSystem

> In-process filesystem fixture on `System.IO.Abstractions` with traversal-safe `PathSandboxHelper`. No container required.

## What this package is

The zero-container storage provider. `FileSystemFixture` creates a
unique temp sandbox directory (`Path.GetTempPath() + prefix +
IsolationKey`) and cleans it up on dispose. `PathSandboxHelper.Resolve
(root, relative)` ensures the resolved path stays inside the root —
throws on `../` traversal attempts. This is the foundation for safe
per-test filesystem manipulation.

## When to use it

- Fast-path storage testing without Docker.
- Testing code that writes / reads files (log rotation, report export,
  upload staging).
- Regression-guarding path-traversal security boundaries.
- **Not for**: cloud-storage semantic testing — use `.S3`, `.AzureBlob`,
  or `.MinIO`.

## Prerequisites

- .NET 10 SDK
- `System.IO.Abstractions` (transitive)
- Write access to `Path.GetTempPath()` on the test host.

## Quick start

```csharp
using Rig.TUnit.Storage.FileSystem.Fixtures;

await using var fx = new FileSystemFixture();
await fx.InitializeAsync();

var path = Path.Combine(fx.Root, "greeting.txt");
await File.WriteAllTextAsync(path, "hello");
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `RootPathPrefix` | `string` | `"rigtunit-fs"` | Temp-dir prefix |
| `CleanupOnDispose` | `bool` | `true` | Recursively delete sandbox on dispose |
| `UseMockFileSystem` | `bool` | `false` | Use `MockFileSystem` instead of real disk |

Section name: `RigTUnit:FileSystem`.

## Fixture + helper APIs

- `Rig.TUnit.Storage.FileSystem.Fixtures.FileSystemFixture`
- `Rig.TUnit.Storage.FileSystem.Options.FileSystemFixtureOptions`
- `Rig.TUnit.Storage.FileSystem.Builder.FileSystemRigBuilder`
- `Rig.TUnit.Storage.FileSystem.Helpers.PathSandboxHelper`

## Per-test isolation

Each fixture owns a unique temp directory `{TempPath}/{prefix}-
{IsolationKey}`. Teardown deletes recursively. `PathSandboxHelper`
ensures resolved paths stay inside the sandbox.

## Parallelism + performance

- Zero container.
- Fixture construction: ~2 ms (directory create).
- Teardown: ~5 ms for typical test-size directory.
- Safe under full parallelism.

## Troubleshooting

- **`UnauthorizedAccessException: path escapes sandbox`** — exactly
  what you want — `PathSandboxHelper` caught a traversal. Fix the test
  or input validation at the callsite.
- **Files remain after test** — `CleanupOnDispose=false` was set, or
  the test crashed before `Dispose`. Temp cleanup is a safety net, not
  a guarantee; prefer explicit `using` / `await using`.

See [docs/troubleshooting.md#filesystem](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Path separators: `PathSandboxHelper` normalises to OS-native
  separators internally; relative path inputs should use `/` for
  portability.
- `UseMockFileSystem=true` switches to `MockFileSystem` from
  `System.IO.Abstractions.TestingHelpers` — no real disk I/O, useful
  for speed but diverges on edge cases (file locking, case-sensitivity).

## Benchmarks

See [`FileSystemBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/FileSystemBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Storage`](../Rig.TUnit.Storage/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
