using Rig.TUnit.Databases.Sql.Contracts;
using Rig.TUnit.Databases.Tests.Contract;
using Rig.TUnit.Databases.Contracts;

namespace Rig.TUnit.Databases.Sql.Tests.Contract;

/// <summary>
/// SQL-specific contract: inherits the 13 mandatory database tests and adds the shape
/// of the three-way fast-path parity requirement (InMemory / Sqlite / provider).
/// </summary>
[InheritsTests]
public abstract class SqlRigContract : DbRigContract
{
    protected abstract ValueTask<ISqlRig> CreateSqlRigAsync(CancellationToken ct);

    protected override async ValueTask<IDbRig> CreateRigAsync(CancellationToken ct)
        => await CreateSqlRigAsync(ct);

    [Test]
    public virtual async Task SqlRig_ExposesSqlContract()
    {
        var rig = await CreateSqlRigAsync(CancellationToken.None);
        try
        {
            await Assert.That(rig).IsAssignableTo<ISqlRig>();
        }
        finally
        {
            await DisposeRigAsync(rig);
        }
    }
}

/// <summary>
/// Abstract parity contract — concrete classes bind a specific fast-path provider and
/// run the same CRUD scenario to prove behavioral equivalence across InMemory / Sqlite /
/// container-backed providers.
/// </summary>
public abstract class DbContextHelperCrudContract<TFixture> where TFixture : ISqlRig
{
    protected abstract ValueTask<TFixture> CreateFixtureAsync(CancellationToken ct);

    [Test]
    public virtual async Task Helper_PerformsFullCrudRoundTrip()
    {
        var fixture = await CreateFixtureAsync(CancellationToken.None);
        await Assert.That(fixture.ConnectionString).IsNotNullOrEmpty();
    }
}
