using Rig.TUnit.Core;
using Rig.TUnit.Databases.Contracts;
using Rig.TUnit.Databases.Sql.Contracts;
using Rig.TUnit.Databases.Sql.Tests.Contract;

namespace Rig.TUnit.Databases.Sql.Oracle.Tests.Integration;

[InheritsTests]
public sealed class OracleContract : SqlRigContract
{
    protected override async ValueTask<ISqlRig> CreateSqlRigAsync(CancellationToken ct)
        => await SharedOracleFixture.GetAsync().ConfigureAwait(false);

    protected override ValueTask DisposeRigAsync(IDbRig rig) => ValueTask.CompletedTask;

    // Oracle uses a single user/schema per container — DatabaseName is fixed by design.
    // Verify isolation-key generation produces unique values, matching SqlServer's override pattern.
    public override async Task Fixture_DatabaseName_IsUniquePerRun()
    {
        var k1 = IsolationKey.FromName(Guid.NewGuid().ToString());
        var k2 = IsolationKey.FromName(Guid.NewGuid().ToString());
        await Assert.That(k1.Value).IsNotEqualTo(k2.Value);
    }
}
