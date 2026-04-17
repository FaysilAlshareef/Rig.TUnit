# Rig.TUnit.Databases

Base package for the Rig.TUnit database testing ecosystem. Defines `IDbRig`, `DbFixtureBase`, `DatabaseRigBuilder<TSelf>`, seeding, and provider-agnostic assertions (`DatabaseAssert`, `MigrationAssert`). Concrete providers live in `Rig.TUnit.Databases.Sql.*` and `Rig.TUnit.Databases.NoSql.*`.

## Install

```
dotnet add package Rig.TUnit.Databases
```

## Example

```csharp
public sealed class MyRig : CompositeFixture
{
    public SqlServerFixture Db { get; } = new();  // from .Sql.SqlServer provider
}

[Test]
public async Task MyHandler_WhenInserted_IsFound()
{
    await using var rig = new MyRig();
    await rig.InitializeAsync();

    await SeedBuilder<Customer>.Create()
        .Generate(5, f => f.CustomInstantiator(x => new Customer(x.UniqueIndex, x.Name.First())))
        .BuildInto(rig.Db);
}
```

## See also

- [spec.md](../../.dotnet-ai-kit/features/003-rig-tunit-ecosystem-expansion/spec.md)
- [plan.md](../../.dotnet-ai-kit/features/003-rig-tunit-ecosystem-expansion/plan.md)

## Dependencies

`Rig.TUnit.Core`
