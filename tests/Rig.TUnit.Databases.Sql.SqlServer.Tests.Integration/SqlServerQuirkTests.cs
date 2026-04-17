using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration;

/// <summary>
/// Provider-specific quirk tests: rowversion as binary(8), native DateTimeOffset,
/// SequentialGuid ordering. Uses the assembly-wide <see cref="SharedSqlServerFixture"/>
/// — each test gets a unique database on that container so state does not leak.
/// </summary>
public sealed class SqlServerQuirkTests
{
    public sealed class QuirkContext(DbContextOptions<QuirkContext> options) : DbContext(options)
    {
        public DbSet<VersionedEntity> Versioned => Set<VersionedEntity>();
        public DbSet<TemporalEntity> Temporal => Set<TemporalEntity>();
        public DbSet<SequentialEntity> Sequential => Set<SequentialEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VersionedEntity>(b =>
            {
                b.ToTable("Versioned");
                b.Property(e => e.RowVersion).IsRowVersion();
            });
            modelBuilder.Entity<TemporalEntity>(b =>
            {
                b.ToTable("Temporal");
                b.Property(e => e.OccurredAt).HasColumnType("datetimeoffset");
            });
            modelBuilder.Entity<SequentialEntity>(b =>
            {
                b.ToTable("Sequential");
                b.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            });
        }
    }

    public sealed class VersionedEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }

    public sealed class TemporalEntity
    {
        public int Id { get; set; }
        public DateTimeOffset OccurredAt { get; set; }
    }

    public sealed class SequentialEntity
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = "";
    }

    private static async Task<QuirkContext> OpenContextAsync(CancellationToken ct)
    {
        var fixture = await SharedSqlServerFixture.GetAsync().ConfigureAwait(false);
        var masterCs = fixture.ConnectionString;
        var dbName = $"quirks_{Guid.NewGuid():N}";

        await using (var master = new Microsoft.Data.SqlClient.SqlConnection(masterCs))
        {
            await master.OpenAsync(ct).ConfigureAwait(false);
            await using var create = master.CreateCommand();
            create.CommandText = $"CREATE DATABASE [{dbName}]";
            await create.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        var scopedCs = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(masterCs)
        {
            InitialCatalog = dbName,
        }.ConnectionString;

        var opts = new DbContextOptionsBuilder<QuirkContext>()
            .UseSqlServer(scopedCs)
            .Options;
        var context = new QuirkContext(opts);
        await context.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);
        return context;
    }

    [Test]
    public async Task Quirk_RowVersion_IsBinary8_AndAutoIncrementsOnUpdate(CancellationToken ct)
    {
        await using var context = await OpenContextAsync(ct);

        var e = new VersionedEntity { Name = "v1" };
        context.Versioned.Add(e);
        await context.SaveChangesAsync(ct);

        var v1 = e.RowVersion!.ToArray();
        await Assert.That(v1.Length).IsEqualTo(8);

        e.Name = "v2";
        await context.SaveChangesAsync(ct);

        await Assert.That(e.RowVersion!.SequenceEqual(v1)).IsFalse();
    }

    [Test]
    public async Task Quirk_DateTimeOffset_RoundTripsWithOffsetIntact(CancellationToken ct)
    {
        await using var context = await OpenContextAsync(ct);

        var original = new DateTimeOffset(2026, 4, 17, 12, 30, 45, TimeSpan.FromHours(5));
        context.Temporal.Add(new TemporalEntity { OccurredAt = original });
        await context.SaveChangesAsync(ct);

        var read = await context.Temporal.AsNoTracking().FirstAsync(ct);

        await Assert.That(read.OccurredAt).IsEqualTo(original);
        await Assert.That(read.OccurredAt.Offset).IsEqualTo(TimeSpan.FromHours(5));
    }

    [Test]
    public async Task Quirk_SequentialGuid_ProducesIncreasingValues(CancellationToken ct)
    {
        await using var context = await OpenContextAsync(ct);

        var a = new SequentialEntity { Label = "a" };
        var b = new SequentialEntity { Label = "b" };
        var c = new SequentialEntity { Label = "c" };
        context.Sequential.AddRange(a, b, c);
        await context.SaveChangesAsync(ct);

        var ordered = await context.Sequential
            .AsNoTracking()
            .OrderBy(e => e.Id)
            .Take(10)
            .Select(e => e.Label)
            .ToListAsync(ct);

        await Assert.That(ordered).IsEquivalentTo(new[] { "a", "b", "c" });
    }
}
