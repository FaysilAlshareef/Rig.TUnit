using NATS.Client.Core;
using NATS.Client.JetStream;
using NSubstitute;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Nats.Helpers;

namespace Rig.TUnit.Messaging.Nats.Tests.Unit;

// T052-RED: compile-fail until T052-GREEN adds NatsJetStreamEventSender.
public sealed class NatsJetStreamEventSenderTests
{
    [Test]
    public async Task Ctor_NullJetStream_ThrowsArgumentNullException()
    {
        await Assert.That(() => new NatsJetStreamEventSender(null!, "events.>"))  // CS0246 RED
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_NullSubject_ThrowsArgumentException()
    {
        var mockJs = Substitute.For<INatsJSContext>();
        await Assert.That(() => new NatsJetStreamEventSender(mockJs, null!))  // CS0246 RED
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Ctor_WhitespaceSubject_ThrowsArgumentException()
    {
        var mockJs = Substitute.For<INatsJSContext>();
        await Assert.That(() => new NatsJetStreamEventSender(mockJs, "  "))  // CS0246 RED
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task SendAsync_WithSessionKey_IncludesHeaderInPublish(CancellationToken ct)
    {
        // Arrange
        var mockJs = Substitute.For<INatsJSContext>();
        var sender = new NatsJetStreamEventSender(mockJs, "events.test");
        var context = new SendContext(SessionKey: "session-42");

        // Act — will throw (mock returns default ValueTask) but we just verify call was made
        try
        {
            await sender.SendAsync("body", context, ct: ct);
        }
        catch { /* mock doesn't return real ack — exception is expected */ }

        // Assert — PublishAsync was called with the correct subject and a
        // NatsHeaders bag containing the x-session-key header from SendContext.
        await mockJs.Received(1).PublishAsync(
            Arg.Is<string>(s => s == "events.test"),
            Arg.Any<string>(),
            headers: Arg.Is<NatsHeaders?>(h =>
                h != null
                && h.ContainsKey("x-session-key")
                && h["x-session-key"].ToString() == "session-42"),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendAsync_DefaultSendContext_BehavesLikeLegacyOverload(CancellationToken ct)
    {
        var mockJs = Substitute.For<INatsJSContext>();
        var sender = new NatsJetStreamEventSender(mockJs, "events.test");

        try
        {
            await sender.SendAsync("payload", ct: ct);
        }
        catch { /* mock — expected */ }

        // Default SendContext: no x-session-key header, but the W3C traceparent
        // header is auto-emitted by EventSenderBase — so headers is non-null
        // but does not carry session metadata.
        await mockJs.Received(1).PublishAsync(
            Arg.Is<string>(s => s == "events.test"),
            Arg.Any<string>(),
            headers: Arg.Is<NatsHeaders?>(h =>
                h != null
                && !h.ContainsKey("x-session-key")
                && h.ContainsKey("traceparent")),
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
