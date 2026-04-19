# Rig.TUnit.Databases.Sql

SQL base layer. Owns the cross-provider `SqlRigBuilder<TSelf>` (with `ReplaceDbContext<T>`), the EF-agnostic `DbContextHelper<TContext>`, `InMemoryDbExtensions`, and SQL-specific assertions (`RawSqlAssert`). Concrete providers: `SqlServer`, `Sqlite`, `Postgresql`, `MySql`, `Oracle`.

## Install

```
dotnet add package Rig.TUnit.Databases.Sql.SqlServer   # or .Sqlite
```

## Example

```csharp
var rig = new RigBuilder(services)
    .UseSqlServer(RigConnect.FromContainer(sqlFixture), sql => sql
        .ReplaceDbContext<OrderDb>())
    .Build();
```

## Dependencies

`Rig.TUnit.Databases`
