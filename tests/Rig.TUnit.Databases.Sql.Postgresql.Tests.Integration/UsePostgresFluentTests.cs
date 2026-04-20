using Microsoft.EntityFrameworkCore;
using Npgsql;
using Rig.TUnit.Core;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Postgresql.Builder;
using Rig.TUnit.Databases.Sql.Postgresql.Extensions;
using Rig.TUnit.Databases.Sql.Postgresql.Helpers;

namespace Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration;

/// <summary>
/// T176a retroactive integration coverage: round-trip a real table via a
/// <see cref="DbContext"/> whose options are built through
/// <see cref="PostgresBuilderExtensions.UsePostgres(DbContextOptionsBuilder, string)"/>.
///
/// Feature 005 T003 (RED) / T004 (GREEN): every test creates its own physical database via
/// <see cref="PostgresDbContextHelper.CreateEphemeralDatabaseAsync(string, CancellationToken)"/>
/// so the schema-visibility check in <see cref="UsePostgres_EachTest_SeesOnlyItsOwnSamplesTable"/>
/// passes deterministically under parallel execution.
/// </summary>
public sealed class UsePostgresFluentTests
{
    [Test]
    public async Task UsePostgres_DbContext_PerformsInsertSelectRoundTrip(CancellationToken ct)
    {
        var fx = await SharedPostgresFixture.GetAsync();
        await using var db = await PostgresDbContextHelper.CreateEphemeralDatabaseAsync(fx.ConnectionString, ct);

        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UsePostgres(db.ConnectionString)
            .Options;

        await using var ctx = new SampleDbContext(options);
        await ctx.Database.EnsureCreatedAsync(ct);

        var sample = new SampleEntity($"round-trip-{Guid.NewGuid():N}");
        ctx.Samples.Add(sample);
        await ctx.SaveChangesAsync(ct);

        var reloaded = await ctx.Samples.AsNoTracking()
            .SingleAsync(s => s.Id == sample.Id, ct);

        await Assert.That(reloaded.Name).IsEqualTo(sample.Name);
    }

    [Test]
    public async Task UsePostgres_RigBuilder_IntegratesWithFixtureConnectionSource(CancellationToken ct)
    {
        var fx = await SharedPostgresFixture.GetAsync();
        await using var db = await PostgresDbContextHelper.CreateEphemeralDatabaseAsync(fx.ConnectionString, ct);
        var source = RigConnect.FromValue(db.ConnectionString);

        RigBuilder? captured = null;
        new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddRigTUnit(rig =>
            {
                captured = rig;
                rig.UsePostgres(source, _ => { });
            });

        await Assert.That(captured).IsNotNull();
    }

    /// <summary>
    /// FR-010 / SC-001: each test owns its database, so a table created inside the test's
    /// scope MUST be the only `samples_*` table visible. Proves schema isolation, not just
    /// successful insert/select. Fires deterministically when per-test isolation breaks.
    /// </summary>
    [Test]
    public async Task UsePostgres_EachTest_SeesOnlyItsOwnSamplesTable(CancellationToken ct)
    {
        var fx = await SharedPostgresFixture.GetAsync();
        await using var db = await PostgresDbContextHelper.CreateEphemeralDatabaseAsync(fx.ConnectionString, ct);

        var isolation = IsolationKey.FromExecutionContext(
            $"{typeof(UsePostgresFluentTests).FullName}.{nameof(UsePostgres_EachTest_SeesOnlyItsOwnSamplesTable)}");
        var tableName = $"samples_{isolation.Value}".ToLowerInvariant();

        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync(ct);

        await using (var create = conn.CreateCommand())
        {
            create.CommandText =
                $"CREATE TABLE \"{tableName}\" (id SERIAL PRIMARY KEY, name TEXT NOT NULL); "
                + $"INSERT INTO \"{tableName}\" (name) VALUES ('marker');";
            await create.ExecuteNonQueryAsync(ct);
        }

        await using (var inspect = conn.CreateCommand())
        {
            inspect.CommandText =
                "SELECT COUNT(*) FROM information_schema.tables "
                + "WHERE table_schema = 'public' AND table_name LIKE 'samples_%' AND table_name <> @self";
            inspect.Parameters.AddWithValue("self", tableName);
            var count = (long)(await inspect.ExecuteScalarAsync(ct) ?? 0L);

            await Assert.That(count)
                .IsEqualTo(0L)
                .Because(
                    "Every test MUST own its database — finding foreign 'samples_*' tables means "
                    + "per-test isolation has regressed. FR-010 / SC-001.");
        }
    }

    private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
    {
        public DbSet<SampleEntity> Samples => Set<SampleEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SampleEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();
                e.Property(x => x.Name).IsRequired();
            });
        }
    }

    private sealed class SampleEntity
    {
        private SampleEntity() { }

        public SampleEntity(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));

        public int Id { get; private set; }

        public string Name { get; private set; } = string.Empty;
    }
}
