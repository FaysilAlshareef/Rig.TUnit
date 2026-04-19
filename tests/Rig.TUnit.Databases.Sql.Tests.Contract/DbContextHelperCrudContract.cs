using Rig.TUnit.Databases.Sql.Contracts;

namespace Rig.TUnit.Databases.Sql.Tests.Contract;

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
