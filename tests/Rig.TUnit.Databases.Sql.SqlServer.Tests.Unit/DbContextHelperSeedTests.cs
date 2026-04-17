using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Databases.Sql.Helpers;
using Rig.TUnit.Databases.Sql.SqlServer.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.Databases.Sql.SqlServer.Tests.Unit;

/// <summary>
/// Ported from the original <c>Rig.TUnit.SqlServer.Tests.Unit</c> suite. The
/// <c>DbContextHelper</c> constructor now takes a <see cref="DbContext"/> directly
/// instead of an <c>IServiceProvider</c>; tests are adapted to the new constructor
/// while exercising the same async/sync seed code paths.
/// </summary>
public sealed class DbContextHelperSeedTests
{
    private static DbContextHelper<TestDbContext> BuildHelper()
    {
        var opts = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"seed-{Guid.NewGuid():N}")
            .Options;
        return new DbContextHelper<TestDbContext>(new TestDbContext(opts));
    }

    [Test]
    public async Task SeedAsync_AsyncAction_InsertsDataInIsolatedScope()
    {
        await using var helper = BuildHelper();

        await helper.SeedAsync(async (ctx, ct) =>
        {
            await ctx.TestEntities.AddAsync(TestEntity.Create("async-seed"), ct);
        });

        var entities = await helper.QueryAsync(ctx => ctx.TestEntities);
        await Assert.That(entities.Count).IsEqualTo(1);
    }

    [Test]
    public async Task SeedAsync_SyncAction_InsertsDataAndSaves()
    {
        await using var helper = BuildHelper();

        await helper.SeedAsync(ctx =>
        {
            ctx.TestEntities.Add(TestEntity.Create("sync-seed"));
        });

        var entities = await helper.QueryAsync(ctx => ctx.TestEntities);
        await Assert.That(entities.Count).IsEqualTo(1);
    }
}
