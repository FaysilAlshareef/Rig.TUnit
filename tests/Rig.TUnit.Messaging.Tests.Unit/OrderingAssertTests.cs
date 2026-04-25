using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Tests.Unit;

public sealed class OrderingAssertTests
{
    [Test]
    public async Task PerKeyMonotonic_NullListener_ThrowsArgumentNullException()
    {
        await Assert.That(() => OrderingAssert.PerKeyMonotonic<string>(null!, m => m, m => 0))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task PerKeyMonotonic_NullKeyExtractor_ThrowsArgumentNullException()
    {
        var listener = new StubListener<string>();

        await Assert.That(() => OrderingAssert.PerKeyMonotonic<string>(listener, null!, m => 0))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task PerKeyMonotonic_NullSequenceExtractor_ThrowsArgumentNullException()
    {
        var listener = new StubListener<string>();

        await Assert.That(() => OrderingAssert.PerKeyMonotonic<string>(listener, m => m, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public Task PerKeyMonotonic_MonotonicOrder_DoesNotThrow()
    {
        var listener = new StubListener<string>();
        listener.PublicRecord(MakeMessage("key", 1));
        listener.PublicRecord(MakeMessage("key", 2));
        listener.PublicRecord(MakeMessage("key", 3));

        OrderingAssert.PerKeyMonotonic<string>(listener, _ => "key", m => long.Parse(m));
        return Task.CompletedTask;
    }

    [Test]
    public async Task PerKeyMonotonic_NonMonotonicOrder_ThrowsInvalidOperationException()
    {
        var listener = new StubListener<string>();
        listener.PublicRecord(MakeMessage("key", 2));
        listener.PublicRecord(MakeMessage("key", 1));

        await Assert.That(() => OrderingAssert.PerKeyMonotonic<string>(listener, _ => "key", m => long.Parse(m)))
            .ThrowsExactly<InvalidOperationException>();
    }

    private static CapturedMessage<string> MakeMessage(string key, long seq) =>
        new(seq.ToString(), DateTimeOffset.UtcNow, new Dictionary<string, string>(), string.Empty, null);

    private sealed class StubListener<T> : ListenerBase<T>
    {
        public void PublicRecord(CapturedMessage<T> message) => Record(message);
        public override Task StartAsync(CancellationToken ct) => Task.CompletedTask;
        public override Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
