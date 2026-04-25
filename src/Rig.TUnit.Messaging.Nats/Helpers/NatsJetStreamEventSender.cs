using NATS.Client.JetStream;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Nats.Helpers;

public sealed class NatsJetStreamEventSender : EventSenderBase, IAsyncDisposable
{
    private readonly INatsJSContext _jetStream;
    private readonly string _subject;

    public NatsJetStreamEventSender(INatsJSContext jetStream, string subject)
    {
        _jetStream = jetStream ?? throw new ArgumentNullException(nameof(jetStream));
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        _subject = subject;
    }

    public async Task SendAsync(
        string body,
        SendContext context = default,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additionalHeaders = null,
        CancellationToken ct = default)
    {
        var extra = context.SessionKey is not null
            ? MergeHeader(additionalHeaders, "x-session-key", context.SessionKey)
            : additionalHeaders;
        var headers = BuildHeaders(context, correlationId, causationId, traceparent, extra);

        await _jetStream.PublishAsync(
            _subject,
            body,
            headers: BuildNatsHeaders(headers),
            cancellationToken: ct).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static IReadOnlyDictionary<string, string> MergeHeader(
        IReadOnlyDictionary<string, string>? existing, string key, string value)
    {
        var merged = existing is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(existing, StringComparer.Ordinal);
        merged[key] = value;
        return merged;
    }

    private static NATS.Client.Core.NatsHeaders? BuildNatsHeaders(
        IReadOnlyDictionary<string, string> headers)
    {
        if (headers.Count == 0) return null;
        var natsHeaders = new NATS.Client.Core.NatsHeaders();
        foreach (var kv in headers)
        {
            natsHeaders.Add(kv.Key, kv.Value);
        }
        return natsHeaders;
    }
}
