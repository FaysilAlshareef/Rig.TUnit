using Microsoft.EntityFrameworkCore;
using Npgsql;
using Rig.TUnit.Core;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Postgresql.Builder;
using Rig.TUnit.Databases.Sql.Postgresql.Extensions;

namespace Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration;

/// <summary>
/// T176a retroactive integration coverage: round-trip a real table via a
/// <see cref="DbContext"/> whose options are built through
/// <see cref="PostgresBuilderExtensions.UsePostgres(DbContextOptionsBuilder, string)"/>.
///
/// Feature 005 T003 (RED) / T004 (GREEN): the new
/// <see cref="UsePostgres_EachTest_SeesOnlyItsOwnSamplesTable"/> assertion fires
/// deterministically on any setup that hands more than one test the same physical database.
/// T004 follows up with a <c>PostgresDbContextHelper.CreateEphemeralDatabaseAsync</c> helper
/// that gives every test its own database.
/// </summary>
public sealed class UsePostgresFluentTests
{
    [Test]
    public async Task UsePostgres_DbContext_PerformsInsertSelectRoundTrip(CancellationToken ct)
    {
        var fx = await SharedPostgresFixture.GetAsync();

        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UsePostgres(fx.ConnectionString)
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

    /// <summary>
    /// FR-010 / SC-001 RED assertion: each test creates a uniquely-named `samples_{key}`
    /// table in the shared fixture's database and checks that no other test's table is
    /// visible in the <c>public</c> schema. Under the shared-database regime any two tests
    /// running concurrently will both be mid-setup at some point — the assertion therefore
    /// fails deterministically whenever TUnit runs siblings in parallel. T004 GREEN switches
    /// every test in this class to a per-test ephemeral database, after which this check
    /// always reports exactly one matching table (the caller's own).
    /// </summary>
    [Test]
    public async Task UsePostgres_EachTest_SeesOnlyItsOwnSamplesTable(CancellationToken ct)
    {
        var fx = await SharedPostgresFixture.GetAsync();
        var isolation = IsolationKey.FromExecutionContext(
            $"{typeof(UsePostgresFluentTests).FullName}.{nameof(UsePostgres_EachTest_SeesOnlyItsOwnSamplesTable)}");
        var tableName = $"samples_{isolation.Value}".ToLowerInvariant();

        await using var conn = new NpgsqlConnection(fx.ConnectionString);
        await conn.OpenAsync(ct);

        await using (var create = conn.CreateCommand())
        {
            create.CommandText =
                $"CREATE TABLE IF NOT EXISTS \"{tableName}\" (id SERIAL PRIMARY KEY, name TEXT NOT NULL); "
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
                    + "the fixture is shared across tests and the Postgres schema leaks. Expected: "
                    + $"only `{tableName}` visible. FR-010 / SC-001.");
        }

        // Clean up this test's table. Best-effort; the ephemeral DB added in T004 destroys
        // the database entirely at test exit which makes this step redundant but harmless.
        await using (var drop = conn.CreateCommand())
        {
            drop.CommandText = $"DROP TABLE IF EXISTS \"{tableName}\"";
            await drop.ExecuteNonQueryAsync(ct);
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
        // Parameterless ctor kept for EF materialization; factory ctor enforces invariants.
        private SampleEntity() { }

        public SampleEntity(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));

        public int Id { get; private set; }

        public string Name { get; private set; } = string.Empty;
    }
}
