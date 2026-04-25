using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Tests.Unit.Helpers;

public sealed class SendContextTests
{
    [Test]
    public async Task SendContext_Default_IsAllNulls()
    {
        var context = default(SendContext);

        await Assert.That(context.SessionKey).IsNull();
        await Assert.That(context.PartitionKey).IsNull();
        await Assert.That(context.DeduplicationKey).IsNull();
    }

    [Test]
    public async Task BuildHeaders_DefaultSendContext_ProducesSameHeadersAsLegacyOverload()
    {
        var sender = new TestEventSender();
        const string correlationId = "corr-1";
        const string causationId = "caus-1";
        const string traceparent = "00-11111111111111111111111111111111-2222222222222222-01";

        var legacy = sender.InvokeLegacy(correlationId, causationId, traceparent, additional: null);
        var contextual = sender.InvokeContextual(
            default,
            correlationId,
            causationId,
            traceparent,
            additional: null);

        await Assert.That(contextual.Count).IsEqualTo(legacy.Count);
        foreach (var kv in legacy)
        {
            await Assert.That(contextual.ContainsKey(kv.Key)).IsTrue();
            await Assert.That(contextual[kv.Key]).IsEqualTo(kv.Value);
        }
    }

    [Test]
    public async Task BuildHeaders_WithSendContext_PreservesLegacyHeaderPropagation()
    {
        var sender = new TestEventSender();
        var context = new SendContext(SessionKey: "s1", PartitionKey: "p1", DeduplicationKey: "d1");
        var additional = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["x-extra"] = "value",
        };

        var headers = sender.InvokeContextual(
            context,
            correlationId: "corr-X",
            causationId: "caus-X",
            traceparent: "00-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-bbbbbbbbbbbbbbbb-01",
            additional: additional);

        await Assert.That(headers["x-correlation-id"]).IsEqualTo("corr-X");
        await Assert.That(headers["x-causation-id"]).IsEqualTo("caus-X");
        await Assert.That(headers["traceparent"]).IsEqualTo("00-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-bbbbbbbbbbbbbbbb-01");
        await Assert.That(headers["x-extra"]).IsEqualTo("value");
    }

    private sealed class TestEventSender : EventSenderBase
    {
        public IReadOnlyDictionary<string, string> InvokeLegacy(
            string? correlationId,
            string? causationId,
            string? traceparent,
            IReadOnlyDictionary<string, string>? additional)
            => BuildHeaders(correlationId, causationId, traceparent, additional);

        public IReadOnlyDictionary<string, string> InvokeContextual(
            SendContext context,
            string? correlationId,
            string? causationId,
            string? traceparent,
            IReadOnlyDictionary<string, string>? additional)
            => BuildHeaders(context, correlationId, causationId, traceparent, additional);
    }
}
