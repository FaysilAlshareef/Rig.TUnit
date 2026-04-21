namespace Rig.TUnit.Messaging.Tests.Unit;

public sealed class EventEnvelopeTests
{
    [Test]
    public async Task EventEnvelope_Properties_AreSetCorrectly()
    {
        var ts = DateTimeOffset.UtcNow;
        var headers = new Dictionary<string, string> { ["h"] = "v" };
        var envelope = new EventEnvelope("msg-1", "OrderCreated", "{}", "corr-1", "caus-1", "traceparent-1", headers, ts);

        await Assert.That(envelope.MessageId).IsEqualTo("msg-1");
        await Assert.That(envelope.MessageType).IsEqualTo("OrderCreated");
        await Assert.That(envelope.Body).IsEqualTo("{}");
        await Assert.That(envelope.CorrelationId).IsEqualTo("corr-1");
        await Assert.That(envelope.CausationId).IsEqualTo("caus-1");
        await Assert.That(envelope.Traceparent).IsEqualTo("traceparent-1");
        await Assert.That(envelope.Timestamp).IsEqualTo(ts);
        await Assert.That(envelope.Headers["h"]).IsEqualTo("v");
    }
}
