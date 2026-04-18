using Rig.TUnit.Databases.Contracts;
using Rig.TUnit.Databases.Sql.Contracts;
using Rig.TUnit.Databases.Tests.Contract;

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
