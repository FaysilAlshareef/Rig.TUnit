# Rig.TUnit.Databases.Sql.SqlServer

Testcontainers-backed SQL Server provider. Ships `SqlServerFixture`, `SqlServerFixtureOptions`, `SqlServerRigBuilder`, and `UseSqlServer(source, sql => ...)`.

## Install

```
dotnet add package Rig.TUnit.Databases.Sql.SqlServer
```

## Example

```csharp
public sealed class OrderTests
{
    private readonly SqlServerFixture _db = new();

    [Before(Test)] public Task Init() => _db.InitializeAsync();
    [After(Test)]  public ValueTask Disp() => _db.DisposeAsync();

    [Test]
    public async Task CreateOrder_PersistsRow()
    {
        await using var helper = new DbContextHelper<OrderDb>(new OrderDb(
            new DbContextOptionsBuilder<OrderDb>().UseSqlServer(_db.ConnectionString).Options));

        await helper.InsertAsync(new Order(id: 1, customerId: 42));

        var rows = await helper.QueryAsync(ctx => ctx.Orders);
        Assert.That(rows.Count).IsEqualTo(1);
    }
}
```

## Dependencies

`Rig.TUnit.Databases.Sql`, `Testcontainers.MsSql`, `Microsoft.EntityFrameworkCore.SqlServer`
