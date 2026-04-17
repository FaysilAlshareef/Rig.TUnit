using Rig.TUnit.Databases.Sql.SqlServer.Fixtures;
using Rig.TUnit.Databases.Sql.Tests.Contract;

namespace Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration;

/// <summary>
/// Binds the generic <see cref="DbContextHelperCrudContract{TFixture}"/> to
/// <see cref="SqlServerFixture"/>. Uses <see cref="SharedSqlServerFixture"/> so no
/// extra container boot is incurred beyond the assembly-wide one.
/// </summary>
[InheritsTests]
public sealed class SqlServerDbContextHelperTests : DbContextHelperCrudContract<SqlServerFixture>
{
    protected override async ValueTask<SqlServerFixture> CreateFixtureAsync(CancellationToken ct)
        => await SharedSqlServerFixture.GetAsync().ConfigureAwait(false);
}
