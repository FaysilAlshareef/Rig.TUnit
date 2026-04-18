using Rig.TUnit.Core;
using Rig.TUnit.Databases.Contracts;
using Rig.TUnit.Databases.NoSql.Contracts;
using Rig.TUnit.Databases.NoSql.Tests.Contract;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration;

[InheritsTests]
public sealed class KurrentDbContract : NoSqlRigContract
{
    protected override async ValueTask<INoSqlRig> CreateNoSqlRigAsync(CancellationToken ct)
        => await SharedKurrentDbFixture.GetAsync().ConfigureAwait(false);

    protected override ValueTask DisposeRigAsync(IDbRig rig) => ValueTask.CompletedTask;

    public override async Task Fixture_DatabaseName_IsUniquePerRun()
    {
        var k1 = IsolationKey.FromName(Guid.NewGuid().ToString());
        var k2 = IsolationKey.FromName(Guid.NewGuid().ToString());
        await Assert.That(k1.ForPostgresDatabase()).IsNotEqualTo(k2.ForPostgresDatabase());
    }
}
