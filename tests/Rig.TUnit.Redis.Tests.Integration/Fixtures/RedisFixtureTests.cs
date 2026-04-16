using Rig.TUnit.Redis.Fixtures;
using StackExchange.Redis;

namespace Rig.TUnit.Redis.Tests.Integration.Fixtures;

public class RedisFixtureTests
{
    [Test]
    [ClassDataSource<RedisFixture>(Shared = SharedType.PerTestSession)]
    public async Task InitializeAsync_StartsContainer(RedisFixture fixture)
    {
        await Assert.That(fixture.Container.State).IsEqualTo(DotNet.Testcontainers.Containers.TestcontainersStates.Running);
    }

    [Test]
    [ClassDataSource<RedisFixture>(Shared = SharedType.PerTestSession)]
    public async Task ConnectionString_AfterInit_IsValid(RedisFixture fixture)
    {
        await Assert.That(fixture.ConnectionString).IsNotNull();
        await Assert.That(fixture.ConnectionString.Length).IsGreaterThan(0);
    }

    [Test]
    [ClassDataSource<RedisFixture>(Shared = SharedType.PerTestSession)]
    public async Task ConnectionString_AfterInit_CanConnect(RedisFixture fixture)
    {
        // Arrange & Act
        var multiplexer = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);

        // Assert
        await Assert.That(multiplexer.IsConnected).IsTrue();
        await multiplexer.DisposeAsync();
    }
}
