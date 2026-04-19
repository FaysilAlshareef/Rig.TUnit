using Rig.TUnit.Caching.Redis.Fixtures;
using Rig.TUnit.Databases.NoSql.Fixtures;

namespace Rig.TUnit.Databases.NoSql.Redis.Tests.Integration;

/// <summary>
/// Thin <see cref="DocumentFixtureBase"/> adapter over the cache-owned
/// <see cref="RedisFixture"/>. Lets the KV role participate in the shared container
/// without duplicating the Testcontainers startup logic.
/// </summary>
public sealed class RedisKvFixture : DocumentFixtureBase
{
    private readonly RedisFixture _inner;

    public RedisKvFixture()
    {
        _inner = new RedisFixture();
    }

    public override string ConnectionString => _inner.ConnectionString;

    public override string DatabaseName => IsolationKey.ForRedisKeyPrefix();

    public override Task InitializeAsync() => _inner.InitializeAsync();

    public override ValueTask DisposeAsync() => _inner.DisposeAsync();
}
