# Rig.TUnit

> Convenience meta-package bundling Core + Mediator + Grpc + WebAPI — the default entry point for most projects.

## What this package is

`Rig.TUnit` is a zero-code meta-package that pulls in the four packages most
projects need for integration-grade testing: `Rig.TUnit.Core` (the `RigBuilder`
CRTP + `IsolationKey`), `Rig.TUnit.Mediator` (MediatR fakes + pipeline
inspection), `Rig.TUnit.Grpc` (`WebApplicationFactory`-style gRPC host), and
`Rig.TUnit.WebAPI` (JWT/OAuth test-auth helpers). Reach for it in any new
repo that tests an ASP.NET Core + MediatR + gRPC stack.

It is deliberately smaller than `Rig.TUnit.All` — the 60-package kitchen sink
is almost always overkill.

## When to use it

- You are kicking off a new test project and want the common 80 % of Rig.TUnit
  in one `dotnet add package` call.
- Your service under test is a typical ASP.NET Core host with MediatR handlers
  and optional gRPC endpoints.
- You will add provider-specific packages (`Rig.TUnit.Databases.Sql.Postgresql`,
  `Rig.TUnit.Messaging.Kafka`, etc.) on top as you need them.
- **Not for**: pure domain-unit projects with no HTTP/gRPC surface (just add
  `Rig.TUnit.Core` directly).

## Prerequisites

- .NET 10 SDK
- Docker Desktop or Colima (required by any provider package you layer on)

## Quick start

```csharp
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Helpers;

var isolation = IsolationKey.FromExecutionContext();
var rig = new RigBuilder()
    .WithIsolation(isolation)
    .Build();

await using var _ = rig;
```

## Options

## §6 — N/A: meta-package exposes no `FixtureOptions`; each referenced package
(Core / Mediator / Grpc / WebAPI) ships its own options bound under its own
configuration section.

## Fixture + helper APIs

- `Rig.TUnit.Core.Builder.RigBuilder` — root CRTP builder
- `Rig.TUnit.Core.Helpers.IsolationKey` — per-test naming
- `Rig.TUnit.Mediator.Helpers.MediatorPipelineProbe` — inspection
- `Rig.TUnit.Grpc.Helpers.GrpcTestHost` — in-process gRPC host
- `Rig.TUnit.WebAPI.Helpers.TestAuthHeaderBuilder` — JWT/OAuth headers

## Per-test isolation

Delegates to the referenced packages; `Rig.TUnit.Core` provides `IsolationKey`
which every downstream builder threads into container / host names.

## Parallelism + performance

## §9 — N/A: meta-package; parallelism characteristics are determined by the
specific providers you layer on. See `Rig.TUnit.Core`'s README for the
baseline `RigBuilder` cost.

## Troubleshooting

- **Missing provider package** — `Rig.TUnit` intentionally does NOT pull
  database / messaging / storage packages. Add them explicitly.
- **Version drift** — pin all `Rig.TUnit.*` packages to the same version via
  `Directory.Packages.props` to avoid transitive mismatches.

See [docs/troubleshooting.md](../../docs/troubleshooting.md) for the full catalogue.

## Provider quirks + edge cases

## §11 — N/A: meta-package; provider-specific quirks live in each leaf README.

## Benchmarks

## §12 — N/A: meta-package has no runnable code of its own. The referenced
packages each have their own `tests/Rig.TUnit.Benchmarks/*Benchmarks.cs`
entries tracked in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [CHANGELOG](../../CHANGELOG.md)
- [CONTRIBUTING](../../CONTRIBUTING.md)

## License

MIT. See [LICENSE](../../LICENSE).
