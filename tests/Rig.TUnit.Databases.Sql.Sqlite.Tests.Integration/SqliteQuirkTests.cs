using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Databases.Sql.Sqlite.Fixtures;

namespace Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration;

/// <summary>
/// SQLite provider quirks: NOCASE collation, dynamic type affinity (TEXT-affinity
/// column coerces numeric input into TEXT storage), foreign-key pragma honored when
/// enabled, and WITHOUT ROWID tables.
/// </summary>
public sealed class SqliteQuirkTests
{
    public sealed class QuirkContext(DbContextOptions<QuirkContext> options) : DbContext(options)
    {
        public DbSet<Parent> Parents => Set<Parent>();
        public DbSet<Child> Children => Set<Child>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Parent>(b =>
            {
                b.ToTable("Parents");
                b.HasKey(e => e.Id);
            });
            modelBuilder.Entity<Child>(b =>
            {
                b.ToTable("Children");
                b.HasKey(e => e.Id);
                b.HasOne<Parent>().WithMany().HasForeignKey(e => e.ParentId);
            });
        }
    }

    public sealed class Parent
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class Child
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
    }

    private static async Task<(SqliteFixture fx, QuirkContext ctx)> BootAsync(CancellationToken ct)
    {
        var fixture = new SqliteFixture();
        await fixture.InitializeAsync();
        var options = new DbContextOptionsBuilder<QuirkContext>()
            .UseSqlite(fixture.ConnectionString)
            .Options;
        var context = new QuirkContext(options);
        await context.Database.EnsureCreatedAsync(ct);
        return (fixture, context);
    }

    [Test]
    public async Task Quirk_TextComparison_UsesCaseInsensitiveCollationWhenRequested(CancellationToken ct)
    {
        var (fixture, context) = await BootAsync(ct);
        await using var _ = fixture;
        await using var __ = context;

        context.Parents.AddRange(
            new Parent { Id = 1, Name = "Alpha" },
            new Parent { Id = 2, Name = "BRAVO" });
        await context.SaveChangesAsync(ct);

        await using var conn = new SqliteConnection(fixture.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Parents WHERE Name = @name COLLATE NOCASE";
        cmd.Parameters.AddWithValue("@name", "alpha");
        var match = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);
        await Assert.That(match).IsEqualTo(1L);
    }

    [Test]
    public async Task Quirk_TypeAffinity_StoresNumericValueAsIntegerInTextColumn(CancellationToken ct)
    {
        var (fixture, context) = await BootAsync(ct);
        await using var _ = fixture;
        await using var __ = context;

        await using var conn = new SqliteConnection(fixture.ConnectionString);
        await conn.OpenAsync(ct);

        await using (var insert = conn.CreateCommand())
        {
            insert.CommandText = "INSERT INTO Parents (Id, Name) VALUES (@id, @name)";
            insert.Parameters.AddWithValue("@id", 42);
            insert.Parameters.AddWithValue("@name", 123);
            await insert.ExecuteNonQueryAsync(ct);
        }

        await using var read = conn.CreateCommand();
        read.CommandText = "SELECT typeof(Name), Name FROM Parents WHERE Id = @id";
        read.Parameters.AddWithValue("@id", 42);
        await using var reader = await read.ExecuteReaderAsync(ct);
        var advanced = await reader.ReadAsync(ct);
        await Assert.That(advanced).IsTrue();

        var storedType = reader.GetString(0);
        var storedValue = reader.GetValue(1);
        await Assert.That(storedType).IsEqualTo("text");
        await Assert.That(storedValue).IsEqualTo("123");
    }

    [Test]
    public async Task Quirk_ForeignKeys_EnforcedWhenPragmaEnabled(CancellationToken ct)
    {
        var (fixture, context) = await BootAsync(ct);
        await using var _ = fixture;
        await using var __ = context;

        context.Children.Add(new Child { Id = 1, ParentId = 999 });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync(ct));
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Quirk_WithoutRowId_TableSupported(CancellationToken ct)
    {
        var fixture = new SqliteFixture();
        await fixture.InitializeAsync();
        await using var _ = fixture;

        await using var conn = new SqliteConnection(fixture.ConnectionString);
        await conn.OpenAsync(ct);

        await using (var create = conn.CreateCommand())
        {
            create.CommandText = "CREATE TABLE rowless (id INTEGER PRIMARY KEY, label TEXT NOT NULL) WITHOUT ROWID";
            await create.ExecuteNonQueryAsync(ct);
        }

        await using (var insert = conn.CreateCommand())
        {
            insert.CommandText = "INSERT INTO rowless (id, label) VALUES (1, 'one'), (2, 'two')";
            await insert.ExecuteNonQueryAsync(ct);
        }

        await using var count = conn.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM rowless";
        var result = (long)(await count.ExecuteScalarAsync(ct) ?? 0L);
        await Assert.That(result).IsEqualTo(2L);
    }
}
