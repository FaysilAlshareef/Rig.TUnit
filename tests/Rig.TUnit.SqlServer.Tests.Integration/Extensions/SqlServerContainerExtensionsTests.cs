using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.SqlServer.Extensions;
using Rig.TUnit.SqlServer.Fixtures;
using Rig.TUnit.SqlServer.Tests.Integration.TestInfrastructure;

namespace Rig.TUnit.SqlServer.Tests.Integration.Extensions;

public class SqlServerContainerExtensionsTests
{
    [Test]
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public async Task UseSqlServerContainerIsolated_CreatesUniqueDatabase(SqlServerFixture fixture)
    {
        // Arrange
        var services = new ServiceCollection();
        services.UseSqlServerContainerIsolated<TestDbContext>(fixture);
        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var dbName = context.Database.GetDbConnection().Database;

        // Assert — database name should start with test_ prefix
        await Assert.That(dbName).StartsWith("test_");
    }

    [Test]
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public async Task UseSqlServerContainerIsolated_TwoCalls_ProduceIsolatedDatabases(SqlServerFixture fixture)
    {
        // Arrange
        var services1 = new ServiceCollection();
        services1.UseSqlServerContainerIsolated<TestDbContext>(fixture);
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.UseSqlServerContainerIsolated<TestDbContext>(fixture);
        var provider2 = services2.BuildServiceProvider();

        // Act — insert into db1, check db2 is empty
        using var scope1 = provider1.CreateScope();
        var context1 = scope1.ServiceProvider.GetRequiredService<TestDbContext>();
        context1.TestEntities.Add(TestEntity.Create("isolation-test"));
        await context1.SaveChangesAsync();

        using var scope2 = provider2.CreateScope();
        var context2 = scope2.ServiceProvider.GetRequiredService<TestDbContext>();
        var countInDb2 = await context2.TestEntities.CountAsync();

        // Assert — databases are isolated, db2 should have no data
        await Assert.That(countInDb2).IsEqualTo(0);
    }
}
