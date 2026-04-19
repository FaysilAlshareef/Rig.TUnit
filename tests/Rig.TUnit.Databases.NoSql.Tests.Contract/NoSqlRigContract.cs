using Rig.TUnit.Databases.Contracts;
using Rig.TUnit.Databases.NoSql.Contracts;
using Rig.TUnit.Databases.Tests.Contract;

namespace Rig.TUnit.Databases.NoSql.Tests.Contract;

/// <summary>
/// Abstract document-store contract: inherits the 13 mandatory database tests from
/// <see cref="DbRigContract"/> and adds document-specific assertions. Concrete
/// provider test projects (Cosmos, Mongo, Redis KV, etc.) implement
/// <see cref="CreateNoSqlRigAsync"/>.
/// </summary>
[InheritsTests]
public abstract class NoSqlRigContract : DbRigContract
{
    protected abstract ValueTask<INoSqlRig> CreateNoSqlRigAsync(CancellationToken ct);

    protected override async ValueTask<IDbRig> CreateRigAsync(CancellationToken ct)
        => await CreateNoSqlRigAsync(ct);

    [Test]
    public virtual async Task NoSqlRig_ExposesNoSqlContract()
    {
        var rig = await CreateNoSqlRigAsync(CancellationToken.None);
        try
        {
            await Assert.That(rig).IsAssignableTo<INoSqlRig>();
        }
        finally
        {
            await DisposeRigAsync(rig);
        }
    }
}
