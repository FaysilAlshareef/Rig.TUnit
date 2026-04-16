using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.SqlServer.Builder;
using Rig.TUnit.SqlServer.Fixtures;
using Rig.TUnit.SqlServer.Helpers;
using Rig.TUnit.SqlServer.Tests.Integration.TestInfrastructure;

namespace Rig.TUnit.SqlServer.Tests.Integration.Helpers;

public class DbContextHelperTests
{
    private static IServiceProvider BuildProviderFor(SqlServerFixture fixture)
    {
        var services = new ServiceCollection();
        services.AddRigTUnit(rig =>
        {
            rig.UseSqlServer(fixture, sql => sql.ReplaceDbContext<TestDbContext>());
        });
        return services.BuildServiceProvider();
    }

    [Test]
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public async Task InsertAsync_ThenQuery_ReturnsEntity(SqlServerFixture fixture)
    {
        // Arrange
        var provider = BuildProviderFor(fixture);
        var helper = new DbContextHelper<TestDbContext>(provider);

        // Act
        var inserted = await helper.InsertAsync(TestEntity.Create("test-entity"));
        var found = await helper.Query(async ctx =>
            await ctx.TestEntities.FirstOrDefaultAsync(e => e.Name == "test-entity"));

        // Assert
        await Assert.That(found).IsNotNull();
        await Assert.That(found!.Name).IsEqualTo(inserted.Name);
    }

    [Test]
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public async Task InsertAsync_ClearsChangeTracker(SqlServerFixture fixture)
    {
        // Arrange
        var provider = BuildProviderFor(fixture);
        var helper = new DbContextHelper<TestDbContext>(provider);

        // Act
        await helper.InsertAsync(TestEntity.Create("tracked-entity"));

        // Assert — after insert, change tracker should be clear in a new scope
        var hasTracked = await helper.Query(ctx =>
            Task.FromResult(ctx.ChangeTracker.Entries().Any()));
        await Assert.That(hasTracked).IsFalse();
    }

    [Test]
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public async Task Query_ExecutesInFreshScope(SqlServerFixture fixture)
    {
        // Arrange
        var provider = BuildProviderFor(fixture);
        var helper = new DbContextHelper<TestDbContext>(provider);

        await helper.InsertAsync(TestEntity.Create("scope-test"));

        // Act — two queries should use independent scopes
        var count1 = await helper.Query(async ctx =>
            await ctx.TestEntities.CountAsync());
        var count2 = await helper.Query(async ctx =>
            await ctx.TestEntities.CountAsync());

        // Assert
        await Assert.That(count1).IsEqualTo(count2);
    }

    [Test]
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public async Task InsertAsync_ThenQuery_UseSeparateScopes(SqlServerFixture fixture)
    {
        // Arrange
        var provider = BuildProviderFor(fixture);
        var helper = new DbContextHelper<TestDbContext>(provider);

        // Act — insert creates its own scope, query creates another
        await helper.InsertAsync(TestEntity.Create("separate-scope-entity"));

        // Query in a fresh scope should still find the entity (persisted to DB)
        var found = await helper.Query(async ctx =>
            await ctx.TestEntities.AnyAsync(e => e.Name == "separate-scope-entity"));

        // Assert
        await Assert.That(found).IsTrue();
    }
}
