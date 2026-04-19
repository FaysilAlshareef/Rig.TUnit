using Rig.TUnit.Databases.NoSql.KurrentDb.Assertions;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Unit;

/// <summary>
/// T038-RED unit tests for <see cref="StreamAssert.EventsAppendedAsync"/>. KurrentDBClient
/// is sealed with transport plumbing — unit tests cover null/empty-arg guards only; live
/// stream behaviour is in StreamAssertLiveTests.
/// </summary>
public sealed class StreamAssertTests
{
    [Test]
    public async Task EventsAppendedAsync_NullClient_Throws()
    {
        await Assert.That(async () => await StreamAssert.EventsAppendedAsync(null!, "stream-1"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task EventsAppendedAsync_EmptyStreamName_Throws()
    {
        // Offline client — never contacted because guard fires first.
        var client = TestClients.Offline();
        await Assert.That(async () => await StreamAssert.EventsAppendedAsync(client, string.Empty))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task EventsAppendedAsync_NullStreamName_Throws()
    {
        var client = TestClients.Offline();
        await Assert.That(async () => await StreamAssert.EventsAppendedAsync(client, null!))
            .ThrowsExactly<ArgumentException>();
    }
}
