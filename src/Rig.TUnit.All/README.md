# Rig.TUnit.All

> Kitchen-sink meta-package containing every `Rig.TUnit.*` package. **Discouraged** — prefer per-feature meta-packages.

## What this package is

A single NuGet that transitively references every leaf, family base, and
cross-cutting package in the Rig.TUnit ecosystem. Used internally by the
architecture-test project so rules like `ProviderCompletenessTests` can walk
the full assembly graph with one project reference. Externally it exists as
an escape hatch for polyrepo consumers who want everything installed at once.

## When to use it

- You are the architecture-test author and need a single reference point.
- You are exploring the ecosystem and want IntelliSense over every public API.
- **Not for**: production test projects. Package size and transitive conflict
  surface is huge. Use `Rig.TUnit` (default stack) or
  `Rig.TUnit.Microservices` (microservice stack) instead.

## Prerequisites

- .NET 10 SDK
- Every prerequisite of every referenced provider (Docker, emulators, SDKs).

## Quick start

```csharp
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Helpers;

var rig = new RigBuilder().WithIsolation(IsolationKey.FromExecutionContext()).Build();
await using var _ = rig;
```

## Options

## §6 — N/A: meta-package exposes no options directly; each referenced package
binds its own configuration section.

## Fixture + helper APIs

All types from every referenced `Rig.TUnit.*` package are available. See
individual READMEs for catalogues.

## Per-test isolation

Per-package; `Rig.TUnit.Core.Helpers.IsolationKey` is the common thread.

## Parallelism + performance

## §9 — N/A: meta-package; parallelism is a property of the providers you
actually exercise.

## Troubleshooting

- **Package-size complaints** — install only what you need via the family
  meta-packages (`Rig.TUnit.Databases.Sql`, `Rig.TUnit.Messaging`, etc.)
  instead of `Rig.TUnit.All`.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

## §11 — N/A: meta-package; per-provider quirks are in each leaf README.

## Benchmarks

## §12 — N/A: meta-package; downstream packages have their own Benchmarks.cs
entries under `tests/Rig.TUnit.Benchmarks/`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [CHANGELOG](../../CHANGELOG.md)

## License

MIT. See [LICENSE](../../LICENSE).
