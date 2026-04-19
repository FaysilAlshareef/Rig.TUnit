# Rig.TUnit.Databases.Sql.MySql

Testcontainers-backed MySQL fixture for Rig.TUnit. Uses the official `mysql`
image (default `mysql:8.4`) and exposes the container connection string and
container database name for direct consumption by `MySqlConnector` or any
EF Core provider the caller installs.

## Install

```bash
dotnet add package Rig.TUnit.Databases.Sql.MySql
```

## Quick start — raw connection

```csharp
using MySqlConnector;
using Rig.TUnit.Databases.Sql.MySql.Fixtures;

await using var fx = new MySqlFixture();
await fx.InitializeAsync();

await using var conn = new MySqlConnection(fx.ConnectionString);
await conn.OpenAsync();
using var cmd = new MySqlCommand("SELECT 1", conn);
var value = (long)(await cmd.ExecuteScalarAsync())!;
```

## Quick start — EF Core (consumer-provided provider)

Pomelo.EntityFrameworkCore.MySql 9.x pins `Microsoft.EntityFrameworkCore.Relational`
to `<= 9.0.999` and is not yet stable on EF Core 10 (Pomelo PR #2019 tracks the
uplift). Install the provider in your own test project:

```bash
dotnet add package Pomelo.EntityFrameworkCore.MySql --prerelease
```

```csharp
using Microsoft.EntityFrameworkCore;

var opts = new DbContextOptionsBuilder<TestDb>()
    .UseMySql(fx.ConnectionString, ServerVersion.AutoDetect(fx.ConnectionString))
    .Options;
using var db = new TestDb(opts);
```

## Fluent wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseMySql(RigConnect.FromValue(fx.ConnectionString), sql => { /* register DbContext yourself */ })
);
```

Calling `sql.ReplaceDbContext<TContext>()` throws `NotSupportedException` until
Pomelo ships an EF Core 10 stable release — by design, so the failure is loud
rather than silent.

## Options

| Property | Default | Purpose |
|---|---|---|
| `ImageTag` | `8.4` | MySQL Docker image tag |
| `StartupTimeoutSeconds` | `180` | MySQL init can be slow on first pull |
| `Username` | `root` | Default container user |
| `Password` | `rigtunit` | Root password |
| `Database` | `rigtunit` | Default database created on startup |
