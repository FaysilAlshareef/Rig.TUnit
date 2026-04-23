using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Databases.Sql.Assertions;

namespace Rig.TUnit.Databases.Sql.Tests.Unit;

public sealed class RawSqlAssertTests
{
    [Test]
    public async Task Returns_NullContext_ThrowsArgumentNullException()
    {
        await Assert.That(async () => await RawSqlAssert.Returns<int>(null!, "SELECT 1", 1))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Returns_EmptySql_ThrowsArgumentException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SampleDbContext>().UseSqlite(connection).Options;
        await using var context = new SampleDbContext(options);

        await Assert.That(async () => await RawSqlAssert.Returns<int>(context, "", 0))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task Returns_WhenScalarMatchesExpected_DoesNotThrow()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SampleDbContext>().UseSqlite(connection).Options;
        await using var context = new SampleDbContext(options);

        await RawSqlAssert.Returns<long>(context, "SELECT 42", 42L);
    }

    [Test]
    public async Task Returns_WhenScalarDiffersFromExpected_ThrowsInvalidOperationException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SampleDbContext>().UseSqlite(connection).Options;
        await using var context = new SampleDbContext(options);

        await Assert.That(async () => await RawSqlAssert.Returns<long>(context, "SELECT 42", 0L))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Affects_NullContext_ThrowsArgumentNullException()
    {
        await Assert.That(async () => await RawSqlAssert.Affects(null!, "SELECT 1", 0))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Affects_WhenRowCountMatches_DoesNotThrow()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SampleDbContext>().UseSqlite(connection).Options;
        await using var context = new SampleDbContext(options);

        await context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS TempRows (Id INTEGER PRIMARY KEY)");

        await RawSqlAssert.Affects(context, "DELETE FROM TempRows WHERE 1=0", 0);
    }

    [Test]
    public async Task Affects_WhenRowCountDiffers_ThrowsInvalidOperationException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SampleDbContext>().UseSqlite(connection).Options;
        await using var context = new SampleDbContext(options);

        await context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS TempRows2 (Id INTEGER PRIMARY KEY)");

        await Assert.That(async () =>
                await RawSqlAssert.Affects(context, "DELETE FROM TempRows2 WHERE 1=0", 99))
            .ThrowsExactly<InvalidOperationException>();
    }

    private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options);
}
