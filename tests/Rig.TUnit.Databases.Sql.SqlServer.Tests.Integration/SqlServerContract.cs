using Rig.TUnit.Core;
using Rig.TUnit.Databases.Contracts;
using Rig.TUnit.Databases.Sql.Contracts;
using Rig.TUnit.Databases.Sql.Tests.Contract;

namespace Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration;

/// <summary>
/// Concrete <see cref="SqlRigContract"/> binding the SQL Server provider. Shares the
/// assembly-wide MSSQL container via <see cref="SharedSqlServerFixture"/> so this
/// class does not boot its own container.
/// </summary>
[InheritsTests]
public sealed class SqlServerContract : SqlRigContract
{
    protected override async ValueTask<ISqlRig> CreateSqlRigAsync(CancellationToken ct)
        => await SharedSqlServerFixture.GetAsync().ConfigureAwait(false);

    protected override ValueTask DisposeRigAsync(IDbRig rig)
        => ValueTask.CompletedTask;

    public override async Task Fixture_DatabaseName_IsUniquePerRun()
    {
        var k1 = IsolationKey.FromName(Guid.NewGuid().ToString());
        var k2 = IsolationKey.FromName(Guid.NewGuid().ToString());
        await Assert.That(k1.ForSqlServerDatabase()).IsNotEqualTo(k2.ForSqlServerDatabase());
    }
}
