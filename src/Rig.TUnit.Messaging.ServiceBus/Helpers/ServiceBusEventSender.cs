using Azure.Messaging.ServiceBus;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.ServiceBus.Helpers;

public sealed class ServiceBusEventSender : EventSenderBase, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly string _topic;
    private ServiceBusSender? _sender;

    public ServiceBusEventSender(ServiceBusClient client, string topic)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        _topic = topic;
    }

    public async Task SendAsync(
        string body,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additionalHeaders = null,
        CancellationToken ct = default)
    {
        _sender ??= _client.CreateSender(_topic);
        var headers = BuildHeaders(correlationId, causationId, traceparent, additionalHeaders);

        var msg = new ServiceBusMessage(body)
        {
            CorrelationId = correlationId,
            MessageId = Guid.NewGuid().ToString(),
        };
        foreach (var kv in headers)
        {
            msg.ApplicationProperties[kv.Key] = kv.Value;
        }

        await _sender.SendMessageAsync(msg, ct).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender is not null)
        {
            await _sender.DisposeAsync().ConfigureAwait(false);
            _sender = null;
        }
    }
}
