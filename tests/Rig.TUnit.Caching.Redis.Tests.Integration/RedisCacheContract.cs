using Rig.TUnit.Caching.Contracts;
using Rig.TUnit.Caching.Tests.Contract;

namespace Rig.TUnit.Caching.Redis.Tests.Integration;

/// <summary>
/// Concrete <see cref="CacheRigContract"/> binding the Redis cache-role provider.
/// Shares the assembly-wide <c>redis:7-alpine</c> container so only one boot.
/// </summary>
[InheritsTests]
public sealed class RedisCacheContract : CacheRigContract
{
    protected override async ValueTask<ICacheRig> CreateCacheRigAsync(CancellationToken ct)
        => await SharedRedisFixture.GetAsync().ConfigureAwait(false);

    protected override ValueTask DisposeRigAsync(ICacheRig rig) => ValueTask.CompletedTask;

    public override async Task Fixture_KeyPrefix_IsUniquePerRun()
    {
        var k1 = Rig.TUnit.Core.IsolationKey.FromName(Guid.NewGuid().ToString());
        var k2 = Rig.TUnit.Core.IsolationKey.FromName(Guid.NewGuid().ToString());
        await Assert.That(k1.ForRedisKeyPrefix()).IsNotEqualTo(k2.ForRedisKeyPrefix());
    }
}
