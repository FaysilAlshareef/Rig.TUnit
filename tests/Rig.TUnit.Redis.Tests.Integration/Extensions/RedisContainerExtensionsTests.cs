using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Redis.Extensions;
using Rig.TUnit.Redis.Fixtures;
using StackExchange.Redis;

namespace Rig.TUnit.Redis.Tests.Integration.Extensions;

public class RedisContainerExtensionsTests
{
    [Test]
    [ClassDataSource<RedisFixture>(Shared = SharedType.PerTestSession)]
    public async Task UseRedisContainer_ReplacesMultiplexer(RedisFixture fixture)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConnectionMultiplexer>(_ => null!);

        // Act
        services.UseRedisContainer(fixture);

        // Assert
        var provider = services.BuildServiceProvider();
        var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
        await Assert.That(multiplexer).IsNotNull();
        await Assert.That(multiplexer.IsConnected).IsTrue();
    }

    [Test]
    [ClassDataSource<RedisFixture>(Shared = SharedType.PerTestSession)]
    public async Task UseRedisContainer_CanSetAndGetKeys(RedisFixture fixture)
    {
        // Arrange
        var services = new ServiceCollection();
        services.UseRedisContainer(fixture);
        var provider = services.BuildServiceProvider();
        var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
        var db = multiplexer.GetDatabase();

        // Act
        await db.StringSetAsync("test-key", "test-value");
        var value = await db.StringGetAsync("test-key");

        // Assert
        await Assert.That(value.ToString()).IsEqualTo("test-value");
    }
}
