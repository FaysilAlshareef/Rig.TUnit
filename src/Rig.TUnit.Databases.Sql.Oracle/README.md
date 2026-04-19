# Rig.TUnit.Databases.Sql.Oracle

Testcontainers-backed Oracle fixture for Rig.TUnit, integrated with the
`Oracle.EntityFrameworkCore` EF provider. Uses the `gvenzl/oracle-free:23.5-slim-faststart`
image by default — boots in ~60-90s on a warm Docker daemon (aspire#12036 tracks
further speed-ups).

## Install

```bash
dotnet add package Rig.TUnit.Databases.Sql.Oracle
```

## Quick start

```csharp
using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Oracle.Builder;
using Rig.TUnit.Databases.Sql.Oracle.Extensions;
using Rig.TUnit.Databases.Sql.Oracle.Fixtures;

await using var fx = new OracleFixture();
await fx.InitializeAsync();

var opts = new DbContextOptionsBuilder<TestDb>()
    .UseOracle(fx.ConnectionString)
    .Options;
using var db = new TestDb(opts);
```

## Fluent wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseOracle(RigConnect.FromValue(fx.ConnectionString), sql => sql.ReplaceDbContext<TestDb>())
);
```

## Options

| Property | Default | Purpose |
|---|---|---|
| `Image` | `gvenzl/oracle-free:23.5-slim-faststart` | Container image |
| `StartupTimeoutSeconds` | `300` | Oracle Free startup is slow on first pull |
| `Username` | `rigtunit` | Test schema user |
| `Password` | `rigtunit` | Test schema password |

PL/SQL-specific quirks (sequence reset, NUMBER precision, TIMESTAMP TZ) are
covered in `Rig.TUnit.Databases.Sql.Oracle.Tests.Integration`.
