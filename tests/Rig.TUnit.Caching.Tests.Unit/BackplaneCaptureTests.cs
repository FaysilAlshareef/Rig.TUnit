using Rig.TUnit.Caching.Helpers;

namespace Rig.TUnit.Caching.Tests.Unit;

public sealed class BackplaneCaptureTests
{
    [Test]
    public async Task BackplaneMessage_Properties_AreSetCorrectly()
    {
        var ts = DateTimeOffset.UtcNow;
        var msg = new BackplaneMessage("channel-1", "payload-1", ts);

        await Assert.That(msg.Channel).IsEqualTo("channel-1");
        await Assert.That(msg.Payload).IsEqualTo("payload-1");
        await Assert.That(msg.ReceivedAt).IsEqualTo(ts);
    }

    [Test]
    public async Task BackplaneCapture_Messages_InitiallyEmpty()
    {
        var capture = new StubBackplaneCapture();

        await Assert.That(capture.Messages.Count).IsEqualTo(0);
    }

    [Test]
    public async Task BackplaneCapture_Record_AddsMessageToCollection()
    {
        var capture = new StubBackplaneCapture();
        var msg = new BackplaneMessage("ch", "body", DateTimeOffset.UtcNow);

        capture.PublicRecord(msg);

        await Assert.That(capture.Messages.Count).IsEqualTo(1);
        await Assert.That(capture.Messages.First().Channel).IsEqualTo("ch");
    }

    [Test]
    public async Task BackplaneCapture_Record_NullMessage_ThrowsArgumentNullException()
    {
        var capture = new StubBackplaneCapture();

        await Assert.That(() => capture.PublicRecord(null!)).ThrowsExactly<ArgumentNullException>();
    }

    private sealed class StubBackplaneCapture : BackplaneCapture
    {
        public void PublicRecord(BackplaneMessage message) => Record(message);

        public override Task StartAsync(CancellationToken ct) => Task.CompletedTask;
        public override Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
