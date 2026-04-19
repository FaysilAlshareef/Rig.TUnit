using Rig.TUnit.Databases.Sql.Postgresql.Fixtures;
using Rig.TUnit.Databases.Sql.Tests.Contract;

namespace Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration;

/// <summary>Binds <see cref="DbContextHelperCrudContract{TFixture}"/> to <see cref="PostgresFixture"/>.</summary>
[InheritsTests]
public sealed class PostgresDbContextHelperTests : DbContextHelperCrudContract<PostgresFixture>
{
    protected override async ValueTask<PostgresFixture> CreateFixtureAsync(CancellationToken ct)
        => await SharedPostgresFixture.GetAsync().ConfigureAwait(false);
}
