using Microsoft.Data.Sqlite;
using Rig.TUnit.Databases.Sql.Sqlite.Fixtures;
using Rig.TUnit.Databases.Sql.Sqlite.Options;

namespace Rig.TUnit.Databases.Sql.Sqlite.Tests.Unit;

/// <summary>
/// Pure unit coverage for <see cref="SqliteFixture"/> + <see cref="SqliteFixtureOptions"/>.
/// Exercises the in-memory SQLite fixture directly.
/// </summary>
public sealed class SqliteFixtureUnitTests
{
    [Test]
    public async Task SqliteFixtureOptions_SectionName_IsStable()
    {
        var actual = SqliteFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:Sqlite");
    }

    [Test]
    public async Task SqliteFixtureOptions_Defaults_AreSane()
    {
        var options = new SqliteFixtureOptions();

        await Assert.That(options.DatabaseName).IsEqualTo("rigtunit");
        await Assert.That(options.CacheMode).IsEqualTo("Shared");
        await Assert.That(options.ForeignKeys).IsTrue();
    }

    [Test]
    public async Task SqliteFixture_Ctor_WithNullOptions_ThrowsArgumentNull()
    {
        SqliteFixtureOptions? options = null;
        await Assert.That(() => new SqliteFixture(options!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task SqliteFixture_ConnectionString_BeforeInitialize_Throws()
    {
        var fixture = new SqliteFixture();
        await Assert.That(() => fixture.ConnectionString).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task SqliteFixture_InitializeAsync_ThenQuery_RoundTrips()
    {
        await using var fixture = new SqliteFixture();
        await fixture.InitializeAsync();

        await using var conn = new SqliteConnection(fixture.ConnectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1;";
        var result = await cmd.ExecuteScalarAsync();

        await Assert.That(result).IsEqualTo((object)1L);
    }

    [Test]
    public async Task SqliteFixture_DatabaseName_IncludesIsolationKey()
    {
        var fixture = new SqliteFixture(new SqliteFixtureOptions { DatabaseName = "myapp" });

        await Assert.That(fixture.DatabaseName).StartsWith("myapp_");
    }

    [Test]
    public async Task SqliteFixture_Ctor_FromIOptions_UsesWrappedValue()
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new SqliteFixtureOptions { DatabaseName = "wrapped" });
        var fixture = new SqliteFixture(options);

        await Assert.That(fixture.DatabaseName).StartsWith("wrapped_");
    }
}
