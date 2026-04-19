using System.Text;
using KurrentDB.Client;
using Rig.TUnit.Databases.NoSql.KurrentDb.Assertions;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration;

/// <summary>
/// T038-RED live integration tests — append events to a fresh stream via the
/// rebranded <c>kurrentplatform/kurrentdb:25.1</c> container, then assert the
/// event count via <see cref="StreamAssert.EventsAppendedAsync"/>.
/// </summary>
public sealed class KurrentDbLiveTests
{
    private static EventData NewEvent(string type, int payload)
    {
        var json = Encoding.UTF8.GetBytes($"{{\"value\":{payload}}}");
        return new EventData(Uuid.NewUuid(), type, json);
    }

    [Test]
    public async Task EventsAppendedAsync_AfterThreeAppends_ReturnsThree()
    {
        // Arrange
        var fx = await SharedKurrentDbFixture.GetAsync();
        var streamName = $"stream-{Guid.NewGuid():N}";

        await fx.Client.AppendToStreamAsync(
            streamName,
            StreamState.NoStream,
            new[] { NewEvent("probe", 1), NewEvent("probe", 2), NewEvent("probe", 3) });

        // Act
        var count = await StreamAssert.EventsAppendedAsync(fx.Client, streamName);

        // Assert
        await Assert.That(count).IsEqualTo(3L);
    }

    [Test]
    public async Task EventsAppendedAsync_AfterSingleAppend_ReturnsOne()
    {
        // Arrange
        var fx = await SharedKurrentDbFixture.GetAsync();
        var streamName = $"stream-{Guid.NewGuid():N}";

        await fx.Client.AppendToStreamAsync(
            streamName,
            StreamState.NoStream,
            new[] { NewEvent("probe", 1) });

        // Act
        var count = await StreamAssert.EventsAppendedAsync(fx.Client, streamName);

        // Assert
        await Assert.That(count).IsEqualTo(1L);
    }

    [Test]
    public async Task EventsAppendedAsync_UnknownStream_ReturnsZero()
    {
        // Arrange
        var fx = await SharedKurrentDbFixture.GetAsync();
        var streamName = $"stream-missing-{Guid.NewGuid():N}";

        // Act
        var count = await StreamAssert.EventsAppendedAsync(fx.Client, streamName);

        // Assert
        await Assert.That(count).IsEqualTo(0L);
    }
}
