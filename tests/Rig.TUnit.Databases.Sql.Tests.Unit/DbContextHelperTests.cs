using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Databases.Sql.Helpers;

namespace Rig.TUnit.Databases.Sql.Tests.Unit;

public sealed class DbContextHelperTests
{
    public sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options)
    {
        public DbSet<Widget> Widgets => Set<Widget>();
    }

    public sealed class Widget
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private static TestContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"helper-{Guid.NewGuid()}")
            .Options);

    [Test]
    public async Task InsertAsync_SavesEntity()
    {
        await using var helper = new DbContextHelper<TestContext>(CreateContext());

        await helper.InsertAsync(new Widget { Id = 1, Name = "w1" });

        var result = await helper.QueryAsync(ctx => ctx.Widgets);
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task SeedAsync_Sync_SavesEntities()
    {
        await using var helper = new DbContextHelper<TestContext>(CreateContext());

        await helper.SeedAsync(ctx =>
        {
            ctx.Widgets.Add(new Widget { Id = 1, Name = "a" });
            ctx.Widgets.Add(new Widget { Id = 2, Name = "b" });
        });

        var result = await helper.QueryAsync(ctx => ctx.Widgets);
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task DeleteAsync_RemovesEntity()
    {
        await using var helper = new DbContextHelper<TestContext>(CreateContext());
        await helper.InsertAsync(new Widget { Id = 1, Name = "gone" });

        var w = helper.Context.Widgets.First();
        await helper.DeleteAsync(w);

        var result = await helper.QueryAsync(ctx => ctx.Widgets);
        await Assert.That(result.Count).IsEqualTo(0);
    }
}
