# ADR-008: Adopt Kurrent rebrand — EventStore → KurrentDb

**Status**: Accepted
**Date**: 2026-03 (Feature 003, confirmed 2026-04 Feature 004)
**Context**: EventStore Ltd renamed to Kurrent Inc in 2025; the client SDK and
container image were renamed (`EventStore.Client.Grpc.Streams` →
`KurrentDB.Client`, `Testcontainers.EventStoreDb` → `Testcontainers.KurrentDb`).
Rig.TUnit had a `Rig.TUnit.Databases.NoSql.EventStore` package.

## Decision

Follow the upstream rebrand — ship `Rig.TUnit.Databases.NoSql.KurrentDb` and
retire `Rig.TUnit.Databases.NoSql.EventStore`. No compatibility shim; migration is
documented in `CHANGELOG.md` § 0.3.0 and `docs/migration-001-to-004.md`.

## Rationale

1. **Upstream clarity** — the .NET SDK Namespace is `KurrentDB.Client`; keeping our
   package named EventStore creates permanent confusion in NuGet search + intellisense.
2. **Obsolescence is honest** — `Testcontainers.EventStoreDb` is marked obsolete from
   4.9. Silently upgrading the transitive pin would break binary compat without
   consumers knowing.
3. **Zero-compat-shim philosophy** — Rig.TUnit's renames are `chore:` events in the
   changelog, not semver-minor deprecations. Consumers in motion benefit from a clean
   break.

## Consequences

- Consumer migration: update `<PackageReference>` from `.EventStore` to `.KurrentDb`;
  update namespace imports from `Rig.TUnit.Databases.NoSql.EventStore` to
  `Rig.TUnit.Databases.NoSql.KurrentDb`; update C# using aliases.
- `Directory.Packages.props` pins `KurrentDB.Client 1.3.1` +
  `Testcontainers.KurrentDb 4.11.0`; the EventStore pins are removed with a comment
  "Do NOT reintroduce the old pin."
- `CHANGELOG.md` 0.3.0 entry explicitly lists the rebrand as a breaking rename.
