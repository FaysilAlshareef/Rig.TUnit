using Rig.TUnit.Core;
using Rig.TUnit.Databases.Contracts;
using Rig.TUnit.Databases.NoSql.Contracts;
using Rig.TUnit.Databases.NoSql.Tests.Contract;

namespace Rig.TUnit.Databases.NoSql.Redis.Tests.Integration;

/// <summary>
/// Concrete <see cref="NoSqlRigContract"/> binding the Redis key-value role. Shares
/// the assembly-wide container with the cache-role test project via the adapter.
/// </summary>
[InheritsTests]
public sealed class RedisKvContract : NoSqlRigContract
{
    protected override async ValueTask<INoSqlRig> CreateNoSqlRigAsync(CancellationToken ct)
        => await SharedRedisKvFixture.GetAsync().ConfigureAwait(false);

    protected override ValueTask DisposeRigAsync(IDbRig rig) => ValueTask.CompletedTask;

    public override async Task Fixture_DatabaseName_IsUniquePerRun()
    {
        var k1 = IsolationKey.FromName(Guid.NewGuid().ToString());
        var k2 = IsolationKey.FromName(Guid.NewGuid().ToString());
        await Assert.That(k1.ForRedisKeyPrefix()).IsNotEqualTo(k2.ForRedisKeyPrefix());
    }
}
