using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Redis.Builder;
using Rig.TUnit.Redis.Fixtures;
using StackExchange.Redis;

namespace Rig.TUnit.Redis.Tests.Integration.Builder;

public class RedisRigBuilderTests
{
    private sealed class CustomClient(IDatabase db)
    {
        public Task<bool> SetAsync(string k, string v) => db.StringSetAsync(k, v);
        public async Task<string?> GetAsync(string k) => (await db.StringGetAsync(k)).ToString();
    }

    [Test]
    [ClassDataSource<RedisFixture>(Shared = SharedType.PerTestSession)]
    public async Task ReplaceMultiplexer_ViaBuilder_ReplacesConnection(RedisFixture fixture)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConnectionMultiplexer>(_ => null!);

        // Act
        services.AddRigTUnit(rig => rig.UseRedis(fixture));

        // Assert
        var provider = services.BuildServiceProvider();
        var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
        await Assert.That(multiplexer).IsNotNull();
        await Assert.That(multiplexer.IsConnected).IsTrue();
    }

    [Test]
    [ClassDataSource<RedisFixture>(Shared = SharedType.PerTestSession)]
    public async Task ReplaceClient_CustomFactory_ReturnsCustomType(RedisFixture fixture)
    {
        // Arrange + Act
        var services = new ServiceCollection();
        services.AddRigTUnit(rig =>
            rig.UseRedis(fixture, redis =>
                redis.ReplaceClient(mux => new CustomClient(mux.GetDatabase()))));

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<CustomClient>();

        await client.SetAsync("builder-key", "builder-value");
        var value = await client.GetAsync("builder-key");

        // Assert
        await Assert.That(value).IsEqualTo("builder-value");
    }
}
