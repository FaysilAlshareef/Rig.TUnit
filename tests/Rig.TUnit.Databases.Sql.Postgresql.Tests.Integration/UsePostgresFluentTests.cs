using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Postgresql.Builder;
using Rig.TUnit.Databases.Sql.Postgresql.Extensions;

namespace Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration;

/// <summary>
/// T176a retroactive integration coverage: round-trip a real table via a
/// <see cref="DbContext"/> whose options are built through
/// <see cref="PostgresBuilderExtensions.UsePostgres(DbContextOptionsBuilder, string)"/>.
/// Proves the EF wrapper actually routes to a working Npgsql connection — not just that
/// the extension is present. Pairs with the live Postgres container.
/// </summary>
public sealed class UsePostgresFluentTests
{
    [Test]
    public async Task UsePostgres_DbContext_PerformsInsertSelectRoundTrip()
    {
        var fx = await SharedPostgresFixture.GetAsync();

        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UsePostgres(fx.ConnectionString)
            .Options;

        await using var ctx = new SampleDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        var entity = new SampleEntity { Name = $"round-trip-{Guid.NewGuid():N}" };
        ctx.Samples.Add(entity);
        await ctx.SaveChangesAsync();

        var reloaded = await ctx.Samples.AsNoTracking()
            .SingleAsync(s => s.Id == entity.Id);

        await Assert.That(reloaded.Name).IsEqualTo(entity.Name);
    }

    [Test]
    public async Task UsePostgres_RigBuilder_IntegratesWithFixtureConnectionSource()
    {
        var fx = await SharedPostgresFixture.GetAsync();
        var source = RigConnect.FromValue(fx.ConnectionString);

        RigBuilder? captured = null;
        new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddRigTUnit(rig =>
            {
                captured = rig;
                rig.UsePostgres(source, _ => { });
            });

        await Assert.That(captured).IsNotNull();
    }

    private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
    {
        public DbSet<SampleEntity> Samples => Set<SampleEntity>();
    }

    private sealed class SampleEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
