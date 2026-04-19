using Microsoft.EntityFrameworkCore;

namespace Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration;

/// <summary>
/// Postgres-specific quirks: JSONB column, generated (computed) columns, xmin system
/// column. Integration tests sharing a single Postgres container; each test drops
/// its own table (not the whole database) so parallel tests don't collide.
/// </summary>
public sealed class PostgresQuirkTests
{
    private sealed class QuirkContext(DbContextOptions opts) : DbContext(opts)
    {
        public DbSet<QuirkDoc> Docs => Set<QuirkDoc>();
        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<QuirkDoc>(e =>
            {
                e.ToTable("quirk_docs");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();
                e.Property(x => x.Payload).HasColumnType("jsonb");
                e.Property(x => x.FullName).HasComputedColumnSql(@"""FirstName"" || ' ' || ""LastName""", stored: true);
            });
        }
    }

    private sealed class QuirkDoc
    {
        public int Id { get; init; }
        public string FirstName { get; init; } = "";
        public string LastName { get; init; } = "";
        public string? FullName { get; init; }
        public string? Payload { get; init; }
    }

    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static async Task<QuirkContext> FreshContextAsync()
    {
        var fx = await SharedPostgresFixture.GetAsync();
        var opts = new DbContextOptionsBuilder().UseNpgsql(fx.ConnectionString).Options;
        var ctx = new QuirkContext(opts);
        await ctx.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS quirk_docs CASCADE");
        await ctx.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE quirk_docs (
                ""Id""        serial  PRIMARY KEY,
                ""FirstName"" text    NOT NULL,
                ""LastName""  text    NOT NULL,
                ""FullName""  text    GENERATED ALWAYS AS (""FirstName"" || ' ' || ""LastName"") STORED,
                ""Payload""   jsonb
            )");
        return ctx;
    }

    [Test]
    public async Task Save_WithJsonbPayload_RoundtripsNativeJson()
    {
        await Gate.WaitAsync();
        try
        {
            using var ctx = await FreshContextAsync();
            ctx.Docs.Add(new QuirkDoc { FirstName = "A", LastName = "B", Payload = "{\"k\":1}" });
            await ctx.SaveChangesAsync();
            var loaded = await ctx.Docs.AsNoTracking().Take(1).SingleAsync();
            await Assert.That(loaded.Payload).Contains("\"k\"");
        }
        finally { Gate.Release(); }
    }

    [Test]
    public async Task Save_WithFirstAndLastName_GeneratedColumnConcatenates()
    {
        await Gate.WaitAsync();
        try
        {
            using var ctx = await FreshContextAsync();
            ctx.Docs.Add(new QuirkDoc { FirstName = "Ada", LastName = "Lovelace" });
            await ctx.SaveChangesAsync();
            var loaded = await ctx.Docs.AsNoTracking().Take(1).SingleAsync();
            await Assert.That(loaded.FullName).IsEqualTo("Ada Lovelace");
        }
        finally { Gate.Release(); }
    }

    [Test]
    public async Task Query_XminSystemColumn_ReturnsTransactionId()
    {
        await Gate.WaitAsync();
        try
        {
            using var ctx = await FreshContextAsync();
            ctx.Docs.Add(new QuirkDoc { FirstName = "X", LastName = "Y" });
            await ctx.SaveChangesAsync();
            var xmin = await ctx.Database
                .SqlQueryRaw<long>("SELECT xmin::text::bigint AS \"Value\" FROM quirk_docs LIMIT 1")
                .FirstAsync();
            await Assert.That(xmin).IsGreaterThan(0L);
        }
        finally { Gate.Release(); }
    }
}
