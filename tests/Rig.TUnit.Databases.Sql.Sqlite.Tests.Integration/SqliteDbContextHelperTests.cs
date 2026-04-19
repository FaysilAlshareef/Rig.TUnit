using Rig.TUnit.Databases.Sql.Sqlite.Fixtures;
using Rig.TUnit.Databases.Sql.Tests.Contract;

namespace Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration;

/// <summary>
/// Binds <see cref="DbContextHelperCrudContract{TFixture}"/> to the Sqlite fast-path
/// — the middle step in the three-way InMemory / Sqlite / container parity.
/// </summary>
[InheritsTests]
public sealed class SqliteDbContextHelperTests : DbContextHelperCrudContract<SqliteFixture>
{
    protected override async ValueTask<SqliteFixture> CreateFixtureAsync(CancellationToken ct)
    {
        var fixture = new SqliteFixture();
        await fixture.InitializeAsync();
        return fixture;
    }
}
