using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.SqlServer.Extensions;
using Rig.TUnit.SqlServer.Helpers;
using Rig.TUnit.SqlServer.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.SqlServer.Tests.Unit.Helpers;

public class DbContextHelperSeedTests
{
    private static DbContextHelper<TestDbContext> BuildHelper(out IServiceProvider provider)
    {
        var services = new ServiceCollection();
        services.UseInMemoryDatabase<TestDbContext>();
        provider = services.BuildServiceProvider();
        return new DbContextHelper<TestDbContext>(provider);
    }

    [Test]
    public async Task SeedAsync_AsyncAction_InsertsDataInIsolatedScope()
    {
        // Arrange
        var helper = BuildHelper(out var provider);

        // Act
        await helper.SeedAsync(async ctx =>
        {
            await ctx.TestEntities.AddAsync(TestEntity.Create("async-seed"));
        });

        // Assert
        var count = await helper.Query(ctx => ctx.TestEntities.CountAsync());
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task SeedAsync_SyncAction_InsertsDataAndSaves()
    {
        // Arrange
        var helper = BuildHelper(out _);

        // Act
        await helper.SeedAsync(ctx =>
        {
            ctx.TestEntities.Add(TestEntity.Create("sync-seed"));
        });

        // Assert
        var any = await helper.Query(ctx =>
            ctx.TestEntities.AnyAsync(e => e.Name == "sync-seed"));
        await Assert.That(any).IsTrue();
    }

    [Test]
    public async Task SeedAsync_AutoCallsSaveChangesAsync()
    {
        // Arrange — no manual SaveChangesAsync inside the seed action
        var helper = BuildHelper(out _);

        // Act
        await helper.SeedAsync(ctx =>
        {
            ctx.TestEntities.Add(TestEntity.Create("auto-save"));
        });

        // Assert — data persists across scopes because SeedAsync saved it for us
        var found = await helper.Query(ctx =>
            ctx.TestEntities.FirstOrDefaultAsync(e => e.Name == "auto-save"));
        await Assert.That(found).IsNotNull();
    }
}
