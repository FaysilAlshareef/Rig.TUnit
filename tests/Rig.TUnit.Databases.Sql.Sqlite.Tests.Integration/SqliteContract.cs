using Rig.TUnit.Databases.Sql.Contracts;
using Rig.TUnit.Databases.Sql.Sqlite.Fixtures;
using Rig.TUnit.Databases.Sql.Tests.Contract;

namespace Rig.TUnit.Databases.Sql.Sqlite.Tests.Integration;

/// <summary>
/// Concrete <see cref="SqlRigContract"/> binding the SQLite provider. Uses a shared
/// in-memory database per fixture instance with isolation keyed by the fixture's
/// <see cref="Rig.TUnit.Core.IsolationKey"/>.
/// </summary>
[InheritsTests]
public sealed class SqliteContract : SqlRigContract
{
    protected override async ValueTask<ISqlRig> CreateSqlRigAsync(CancellationToken ct)
    {
        var fixture = new SqliteFixture();
        await fixture.InitializeAsync();
        return fixture;
    }
}
