# Rig.TUnit.Microservices

> Opinionated microservice-testing meta-package: Core + Mediator + Grpc + Outbox + Inbox + EventSourcing + Snapshots + Saga + Contracts + Tracing + Jwt + Seq.

## What this package is

A meta-package wired for the full CQRS / event-sourcing microservice stack
Rig.TUnit is built around. Install this one NuGet and you get the command
side (outbox relay, idempotency), query side (inbox + snapshotting), contract
verification, saga harness, distributed tracing, and JWT-authenticated gRPC
client helpers in one `using` block.

Prefer this over `Rig.TUnit.All` when your repo is a microservice; use
`Rig.TUnit` when it is just a web API.

## When to use it

- Writing integration tests for an event-sourced microservice.
- Validating pact-style contract suites against a real gRPC/HTTP surface.
- Orchestrating saga-step assertions across multiple aggregates.
- **Not for**: a monolithic ASP.NET Core MVC app — `Rig.TUnit` is smaller.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (any transitively-referenced provider you use)

## Quick start

```csharp
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Helpers;

var rig = new RigBuilder()
    .WithIsolation(IsolationKey.FromExecutionContext())
    .Build();

await using var _ = rig;
```

## Options

## §6 — N/A: meta-package. Each transitively-referenced package binds its own
options section (`RigTUnit:Outbox`, `RigTUnit:Inbox`, `RigTUnit:Grpc`, etc.).

## Fixture + helper APIs

Every public API from the referenced packages is exposed; see their READMEs:
- [Outbox](../Rig.TUnit.Microservices.Outbox/README.md)
- [Inbox](../Rig.TUnit.Microservices.Inbox/README.md)
- [EventSourcing](../Rig.TUnit.Microservices.EventSourcing/README.md)
- [Snapshots](../Rig.TUnit.Microservices.Snapshots/README.md)
- [Saga](../Rig.TUnit.Microservices.Saga/README.md)
- [Contracts](../Rig.TUnit.Microservices.Contracts/README.md)

## Per-test isolation

Delegated to each referenced package. The `IsolationKey` threaded through
`RigBuilder` shows up in outbox relay IDs, inbox dedup keys, saga harness
correlation IDs, and gRPC trace tags.

## Parallelism + performance

## §9 — N/A: meta-package; see individual package READMEs for per-feature
parallelism cost. Outbox + Inbox are typically the hot paths.

## Troubleshooting

- **Version drift across referenced packages** — this meta-package pins every
  dependency version; do not re-declare `<PackageVersion>` entries in
  consuming projects.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

## §11 — N/A: meta-package; quirks are in each leaf README.

## Benchmarks

## §12 — N/A: meta-package. Transitively-referenced packages have their own
entries in `tests/Rig.TUnit.Benchmarks/` and `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [ADR-007 — Redis cache/KV split](../../docs/adr/ADR-007-redis-cache-kv-split.md)
- [CHANGELOG](../../CHANGELOG.md)

## License

MIT. See [LICENSE](../../LICENSE).
