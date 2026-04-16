using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.SqlServer.Builder;
using Rig.TUnit.SqlServer.Fixtures;
using Rig.TUnit.SqlServer.Tests.Integration.TestInfrastructure;

namespace Rig.TUnit.SqlServer.Tests.Integration.Builder;

public class SqlServerRigBuilderTests
{
    [Test]
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public async Task ReplaceDbContext_ViaBuilder_CreatesIsolatedDatabase(SqlServerFixture fixture)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRigTUnit(rig =>
        {
            rig.UseSqlServer(fixture, sql => sql.ReplaceDbContext<TestDbContext>());
        });
        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var canConnect = await ctx.Database.CanConnectAsync();

        // Assert
        await Assert.That(canConnect).IsTrue();
    }

    [Test]
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public async Task ReplaceDbContext_MultipleCalls_ProduceIsolatedDatabases(SqlServerFixture fixture)
    {
        // Arrange — build two service collections each with their own isolated DB
        var services1 = new ServiceCollection();
        services1.AddRigTUnit(rig =>
        {
            rig.UseSqlServer(fixture, sql => sql.ReplaceDbContext<TestDbContext>());
        });

        var services2 = new ServiceCollection();
        services2.AddRigTUnit(rig =>
        {
            rig.UseSqlServer(fixture, sql => sql.ReplaceDbContext<TestDbContext>());
        });

        var provider1 = services1.BuildServiceProvider();
        var provider2 = services2.BuildServiceProvider();

        // Act — insert an entity into DB1 only
        using (var s1 = provider1.CreateScope())
        {
            var ctx = s1.ServiceProvider.GetRequiredService<TestDbContext>();
            ctx.TestEntities.Add(TestEntity.Create("isolated-1"));
            await ctx.SaveChangesAsync();
        }

        // Assert — DB2 should not see the entity (different database name)
        using var s2 = provider2.CreateScope();
        var ctx2 = s2.ServiceProvider.GetRequiredService<TestDbContext>();
        var any = await ctx2.TestEntities.AnyAsync(e => e.Name == "isolated-1");
        await Assert.That(any).IsFalse();
    }
}
