using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Rig.TUnit.Databases.Sql.Postgresql.Extensions;

namespace Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit;

/// <summary>
/// Retroactive unit coverage for <see cref="PostgresBuilderExtensions.UsePostgres"/> —
/// the EF Core wrapper that routes to <c>UseNpgsql</c>. Backfilled under T176a.
/// </summary>
public sealed class UsePostgresDbContextOptionsExtensionsTests
{
    private const string SampleConnectionString = "Host=localhost;Database=test;Username=u;Password=p";

    [Test]
    public async Task UsePostgres_NonGeneric_NullOptions_ThrowsArgumentNullException()
    {
        await Assert.That(() =>
                ((DbContextOptionsBuilder)null!).UsePostgres(SampleConnectionString))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UsePostgres_NonGeneric_NullConnectionString_ThrowsArgumentException()
    {
        var options = new DbContextOptionsBuilder();
        await Assert.That(() => options.UsePostgres(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UsePostgres_NonGeneric_EmptyConnectionString_ThrowsArgumentException()
    {
        var options = new DbContextOptionsBuilder();
        await Assert.That(() => options.UsePostgres(string.Empty))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task UsePostgresNonGeneric_WithValidConnectionString_RegistersNpgsqlExtension()
    {
        var options = new DbContextOptionsBuilder();

        options.UsePostgres(SampleConnectionString);

        var npgsqlExtension = options.Options
            .Extensions
            .OfType<NpgsqlOptionsExtension>()
            .FirstOrDefault();

        await Assert.That(npgsqlExtension).IsNotNull();
        await Assert.That(npgsqlExtension!.ConnectionString).IsEqualTo(SampleConnectionString);
    }

    [Test]
    public async Task UsePostgres_Generic_NullOptions_ThrowsArgumentNullException()
    {
        await Assert.That(() =>
                ((DbContextOptionsBuilder<TestDbContext>)null!).UsePostgres(SampleConnectionString))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UsePostgres_Generic_NullConnectionString_Throws()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>();
        await Assert.That(() => options.UsePostgres(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UsePostgres_Generic_EmptyConnectionString_Throws()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>();
        await Assert.That(() => options.UsePostgres(string.Empty))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task UsePostgres_Generic_ReturnsSameBuilderForFluentChain()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>();

        var returned = options.UsePostgres(SampleConnectionString);

        await Assert.That(returned).IsSameReferenceAs(options);
    }

    [Test]
    public async Task UsePostgresGeneric_WithValidConnectionString_RegistersNpgsqlExtension()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>();

        options.UsePostgres(SampleConnectionString);

        var npgsqlExtension = options.Options
            .Extensions
            .OfType<NpgsqlOptionsExtension>()
            .FirstOrDefault();

        await Assert.That(npgsqlExtension).IsNotNull();
        await Assert.That(npgsqlExtension!.ConnectionString).IsEqualTo(SampleConnectionString);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
