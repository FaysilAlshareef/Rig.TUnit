using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Tests.Unit;

public sealed class MessageAssertTests
{
    [Test]
    public async Task Published_NullListener_ThrowsArgumentNullException()
    {
        await Assert.That(() => MessageAssert.Published<string>(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Published_EmptyListener_ThrowsInvalidOperationException()
    {
        var listener = new StubListener<string>();

        await Assert.That(() => MessageAssert.Published(listener))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public Task Published_ListenerWithMessages_DoesNotThrow()
    {
        var listener = new StubListener<string>();
        listener.PublicRecord(MakeMessage("hello", null));

        MessageAssert.Published(listener);
        return Task.CompletedTask;
    }

    [Test]
    public async Task ExactlyOnce_NullListener_ThrowsArgumentNullException()
    {
        await Assert.That(() => MessageAssert.ExactlyOnce<string>(null!, _ => true))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ExactlyOnce_NullPredicate_ThrowsArgumentNullException()
    {
        var listener = new StubListener<string>();

        await Assert.That(() => MessageAssert.ExactlyOnce<string>(listener, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public Task ExactlyOnce_OneMatch_DoesNotThrow()
    {
        var listener = new StubListener<string>();
        listener.PublicRecord(MakeMessage("match", null));
        listener.PublicRecord(MakeMessage("other", null));

        MessageAssert.ExactlyOnce(listener, m => m == "match");
        return Task.CompletedTask;
    }

    [Test]
    public async Task ExactlyOnce_ZeroMatches_ThrowsInvalidOperationException()
    {
        var listener = new StubListener<string>();
        listener.PublicRecord(MakeMessage("other", null));

        await Assert.That(() => MessageAssert.ExactlyOnce(listener, m => m == "missing"))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task WithCorrelation_NullMessage_ThrowsArgumentNullException()
    {
        await Assert.That(() => MessageAssert.WithCorrelation<string>(null!, "id"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public Task WithCorrelation_MatchingCorrelation_DoesNotThrow()
    {
        var msg = MakeMessage("body", "corr-123");

        MessageAssert.WithCorrelation(msg, "corr-123");
        return Task.CompletedTask;
    }

    [Test]
    public async Task WithCorrelation_WrongCorrelation_ThrowsInvalidOperationException()
    {
        var msg = MakeMessage("body", "corr-123");

        await Assert.That(() => MessageAssert.WithCorrelation(msg, "different"))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task WithHeader_NullMessage_ThrowsArgumentNullException()
    {
        await Assert.That(() => MessageAssert.WithHeader<string>(null!, "key", "value"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public Task WithHeader_MatchingHeader_DoesNotThrow()
    {
        var headers = new Dictionary<string, string> { ["x-tenant"] = "acme" };
        var msg = new CapturedMessage<string>("body", DateTimeOffset.UtcNow, headers, string.Empty, null);

        MessageAssert.WithHeader(msg, "x-tenant", "acme");
        return Task.CompletedTask;
    }

    [Test]
    public async Task WithHeader_WrongHeaderValue_ThrowsInvalidOperationException()
    {
        var headers = new Dictionary<string, string> { ["x-tenant"] = "acme" };
        var msg = new CapturedMessage<string>("body", DateTimeOffset.UtcNow, headers, string.Empty, null);

        await Assert.That(() => MessageAssert.WithHeader(msg, "x-tenant", "other"))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Within_PrePopulatedListener_DoesNotThrow()
    {
        var listener = new StubListener<string>();
        listener.PublicRecord(MakeMessage("msg", null));

        await MessageAssert.Within(listener, TimeSpan.FromSeconds(1), expectedCount: 1);
    }

    [Test]
    public async Task Within_ListenerNeverReachesCount_ThrowsInvalidOperationException()
    {
        var listener = new StubListener<string>();

        await Assert.That(() => MessageAssert.Within(listener, TimeSpan.Zero, expectedCount: 1))
            .ThrowsExactly<InvalidOperationException>();
    }

    private static CapturedMessage<string> MakeMessage(string body, string? correlationId) =>
        new(body, DateTimeOffset.UtcNow, new Dictionary<string, string>(), body, correlationId);

    private sealed class StubListener<T> : ListenerBase<T>
    {
        public void PublicRecord(CapturedMessage<T> message) => Record(message);
        public override Task StartAsync(CancellationToken ct) => Task.CompletedTask;
        public override Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
