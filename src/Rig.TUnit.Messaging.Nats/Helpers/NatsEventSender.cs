using NATS.Client.Core;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Nats.Helpers;

public sealed class NatsEventSender : EventSenderBase, IAsyncDisposable
{
    private readonly string _url;
    private readonly string _subject;
    private NatsConnection? _connection;

    public NatsEventSender(string url, string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        _url = url;
        _subject = subject;
    }

    public async Task SendAsync(
        string body,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additionalHeaders = null,
        CancellationToken ct = default)
    {
        _connection ??= new NatsConnection(new NatsOpts { Url = _url });

        var headers = BuildHeaders(correlationId, causationId, traceparent, additionalHeaders);
        var natsHeaders = new NatsHeaders();
        foreach (var kv in headers)
        {
            natsHeaders.Add(kv.Key, kv.Value);
        }

        await _connection.PublishAsync(_subject, body, headers: natsHeaders, cancellationToken: ct).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }
}
