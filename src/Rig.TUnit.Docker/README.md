# Rig.TUnit.Docker

> Generic Testcontainers-backed `ContainerFixture` for bespoke / third-party / exploratory container testing. Escape hatch when no provider-specific fixture fits.

## What this package is

A general-purpose container fixture for situations where none of the
provider-specific packages (SqlServer, Redis, Kafka, …) apply. Spins
an arbitrary image through Testcontainers, exposes the raw
`IContainer` for bespoke control, and wires the same
`IsolationKey`-based naming convention every other fixture uses.

Use when you are prototyping against a third-party service without a
Rig.TUnit package, writing a custom test-harness container, or
exploring a new backend before graduating to its own provider package.

## When to use it

- Testing a new third-party service that has no Rig.TUnit package yet.
- Custom test-harness containers (your own Docker image).
- Exploratory testing before a full provider package lands.
- **Not for**: production-shape testing of services with a dedicated
  Rig.TUnit package — use that instead.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima
- `Testcontainers` (transitive)

## Quick start

```csharp
using Rig.TUnit.Docker.Fixtures;

await using var fx = new ContainerFixture(
    image: "alpine:3",
    env: new Dictionary<string, string> { ["FOO"] = "bar" },
    exposedPorts: new[] { 8080 });
await fx.InitializeAsync();
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultImage` | `string` | `"alpine:3"` | Fallback image |
| `IsolatePerTestNetwork` | `bool` | `true` | Each fixture gets its own Docker network |
| `ReuseImageCache` | `bool` | `true` | Cache pulled images across fixtures |
| `DefaultStartupTimeoutSeconds` | `int` | `300` | Readiness deadline |

## Fixture + helper APIs

- `Rig.TUnit.Docker.Fixtures.ContainerFixture`
- `Rig.TUnit.Docker.Options.DockerFixtureOptions`
- `Rig.TUnit.Docker.Builder.DockerRigBuilder`

## Per-test isolation

Each `ContainerFixture` owns its container and network. Container names
include the `IsolationKey` suffix so parallel tests do not collide.

## Parallelism + performance

- First-run pull: dominated by image size (alpine:3 ~8 MB).
- Warm startup: seconds (image-specific).
- Parallelism: bounded by Docker daemon capacity.

## Troubleshooting

- **Container exits immediately** — check logs via
  `fx.Container.GetLogsAsync()`; the fixture surfaces them for
  diagnosis.
- **Port conflicts** — Testcontainers allocates ephemeral host ports;
  fixed-port usage breaks parallelism.

See [docs/troubleshooting.md#docker](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Docker-in-Docker is supported (Testcontainers's `tc.host` resolution
  handles CI nesting), but port mapping differs — use
  `fx.Container.GetMappedPublicPort(port)` for reliability.
- `Ductus.FluentDocker` is kept as a fallback escape hatch if the
  native Testcontainers compose backend regresses; activation
  criteria are documented per-incident.

## Benchmarks

See [`DockerBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/DockerBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [ADR-001 — Testcontainers over Compose](../../docs/adr/ADR-001-testcontainers-over-compose.md)

## License

MIT. See [LICENSE](../../LICENSE).
