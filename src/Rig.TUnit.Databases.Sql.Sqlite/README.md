# Rig.TUnit.Databases.Sql.Sqlite

Real SQLite provider using an in-memory shared-cache connection (no Testcontainers). Fast-path for tests that need real SQL semantics without container startup cost.

## Install

```
dotnet add package Rig.TUnit.Databases.Sql.Sqlite
```

## Example

```csharp
var rig = new RigBuilder(services)
    .UseSqlite(RigConnect.FromContainer(sqliteFixture), sql => sql
        .ReplaceDbContext<OrderDb>())
    .Build();
```

## Dependencies

`Rig.TUnit.Databases.Sql`, `Microsoft.EntityFrameworkCore.Sqlite`
