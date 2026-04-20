using NSubstitute;
using Rig.TUnit.Databases.NoSql.Redis.Helpers;
using StackExchange.Redis;

namespace Rig.TUnit.Databases.NoSql.Redis.Tests.Unit;

/// <summary>
/// Pure unit coverage for <see cref="KeyScanHelper"/> — constructor guards only.
/// </summary>
public sealed class RedisKvUnitTests
{
    [Test]
    public async Task Constructor_WithValidConnection_ReturnsNonNullHelper()
    {
        // Arrange
        var mux = Substitute.For<IConnectionMultiplexer>();
        mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(Substitute.For<IDatabase>());

        // Act
        var helper = new KeyScanHelper(mux);

        // Assert
        await Assert.That(helper).IsNotNull();
    }

    [Test]
    public async Task Constructor_WithNullConnection_ThrowsArgumentNull()
    {
        // Arrange + Act + Assert — ctor validation fires synchronously.
        await Assert.That(() => new KeyScanHelper(null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_WithCustomDatabaseIndex_ForwardsIndexToMultiplexer()
    {
        // Arrange
        var mux = Substitute.For<IConnectionMultiplexer>();
        mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(Substitute.For<IDatabase>());

        // Act
        var helper = new KeyScanHelper(mux, db: 7);

        // Assert
        await Assert.That(helper).IsNotNull();
        mux.Received(1).GetDatabase(7, Arg.Any<object?>());
    }
}
