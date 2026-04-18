using Rig.TUnit.Core;
using Rig.TUnit.Databases.Contracts;
using Rig.TUnit.Databases.Sql.Contracts;
using Rig.TUnit.Databases.Sql.Tests.Contract;

namespace Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration;

[InheritsTests]
public sealed class PostgresContract : SqlRigContract
{
    protected override async ValueTask<ISqlRig> CreateSqlRigAsync(CancellationToken ct)
        => await SharedPostgresFixture.GetAsync().ConfigureAwait(false);

    protected override ValueTask DisposeRigAsync(IDbRig rig) => ValueTask.CompletedTask;

    public override async Task Fixture_DatabaseName_IsUniquePerRun()
    {
        var k1 = IsolationKey.FromName(Guid.NewGuid().ToString());
        var k2 = IsolationKey.FromName(Guid.NewGuid().ToString());
        await Assert.That(k1.ForPostgresDatabase()).IsNotEqualTo(k2.ForPostgresDatabase());
    }
}
