# Undo Log: 003-rig-tunit-ecosystem-expansion

## T064 — SqlServer integration test project + concrete contract + 3 quirks
**Timestamp**: 2026-04-17T20:00Z
**Repo**: primary
**Status**: OK

- created: tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration/Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration.csproj
- created: tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration/SharedSqlServerFixture.cs
- created: tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration/SqlServerContract.cs
- created: tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration/SqlServerQuirkTests.cs
- modified: Rig.TUnit.slnx (added SqlServer.Tests.Integration project)

Container-sharing optimisation: one `SqlServerFixture` is lazy-initialised in
`SharedSqlServerFixture` and consumed by all three test classes in this assembly,
so the MSSQL container boots once (~20s) instead of 18 times. Quirk tests
(rowversion, DateTimeOffset, SequentialGuid) each create a unique database on
the shared container. `Fixture_DatabaseName_IsUniquePerRun` is overridden to
assert against two fresh `IsolationKey` values instead of two fixtures.

## T065 — SqlServer fast-path parity
**Timestamp**: 2026-04-17T20:00Z
**Repo**: primary
**Status**: OK

- created: tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration/SqlServerDbContextHelperTests.cs

Inherits `DbContextHelperCrudContract<SqlServerFixture>` via `[InheritsTests]`
and pulls the shared fixture from `SharedSqlServerFixture`.

## T074 — Sqlite integration test project + concrete contract + 4 quirks
**Timestamp**: 2026-04-17T20:00Z
**Repo**: primary
**Status**: OK

- created: tests/Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration/Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration.csproj
- created: tests/Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration/SqliteContract.cs
- created: tests/Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration/SqliteQuirkTests.cs
- modified: Rig.TUnit.slnx (added Sqlite.Tests.Integration project)

Quirks: NOCASE collation, TEXT-affinity coerces numeric bind to TEXT storage,
FK pragma enforcement, WITHOUT ROWID support. Each test owns a fresh
`SqliteFixture` (in-memory SQLite is cheap — no container sharing needed).

## T075 — Sqlite fast-path parity
**Timestamp**: 2026-04-17T20:00Z
**Repo**: primary
**Status**: OK

- created: tests/Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration/SqliteDbContextHelperTests.cs

## T076 — InMemory fast-path parity (closes 3-way parity)
**Timestamp**: 2026-04-17T20:00Z
**Repo**: primary
**Status**: OK

- created: tests/Rig.TUnit.Databases.Sql.Tests.Unit/InMemoryDbContextHelperTests.cs
- modified: tests/Rig.TUnit.Databases.Sql.Tests.Unit/Rig.TUnit.Databases.Sql.Tests.Unit.csproj (added Rig.TUnit.Databases.Sql.Tests.Contract reference)

Defines a minimal `InMemoryFixture : SqlFixtureBase` inside the test file and
binds `DbContextHelperCrudContract<InMemoryFixture>` via `[InheritsTests]`.
Closes the three-way parity chain: InMemory / Sqlite / SqlServer.

## Verification
- `dotnet build Rig.TUnit.slnx`: 0 Warning(s), 0 Error(s)
- SqlServer.Tests.Integration: 17/17 passed (37s with shared container)
- Sqlite.Tests.Integration: 19/19 passed
- Architecture.Tests: 10/10 passed
- Core.Tests.Unit: 56/56 passed
- Mediator.Tests.Unit: 6/6 passed
- Grpc.Tests.Unit: 10/10 passed
- WebAPI.Tests.Unit: 34/34 passed
- Databases.Tests.Unit: 3/3 passed
- Databases.Sql.Tests.Unit: 4/4 passed (includes the new InMemory parity test)
