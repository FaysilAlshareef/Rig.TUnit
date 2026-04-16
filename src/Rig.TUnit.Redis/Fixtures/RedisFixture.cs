using Rig.TUnit.Core.Builder;
using Testcontainers.Redis;
using TUnit.Core.Interfaces;

namespace Rig.TUnit.Redis.Fixtures;

public sealed class RedisFixture : IAsyncInitializer, IAsyncDisposable, IRigConnectionSource
{
    public RedisContainer Container { get; } = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync() => await Container.StartAsync();
    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
