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

    /// <summary>
    /// Sends <paramref name="body"/> with optional routing hints from <paramref name="context"/>.
    /// <list type="bullet">
    ///   <item><see cref="SendContext.SessionKey"/> → <see cref="ServiceBusMessage.SessionId"/></item>
    ///   <item><see cref="SendContext.PartitionKey"/> → <see cref="ServiceBusMessage.PartitionKey"/>
    ///     (must equal <see cref="SendContext.SessionKey"/> when both are set)</item>
    ///   <item><see cref="SendContext.DeduplicationKey"/> → <see cref="ServiceBusMessage.MessageId"/></item>
    /// </list>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="SendContext.SessionKey"/> and <see cref="SendContext.PartitionKey"/> are
    /// both non-null but not equal — Service Bus requires them to match on session-enabled entities.
    /// </exception>
    public async Task SendAsync(
        string body,
        SendContext context,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additionalHeaders = null,
        CancellationToken ct = default)
    {
        if (context.SessionKey is not null &&
            context.PartitionKey is not null &&
            context.SessionKey != context.PartitionKey)
        {
            throw new InvalidOperationException(
                $"SessionKey ('{context.SessionKey}') and PartitionKey ('{context.PartitionKey}') " +
                "must be equal on session-enabled Service Bus entities.");
        }

        _sender ??= _client.CreateSender(_topic);
        var headers = BuildHeaders(context, correlationId, causationId, traceparent, additionalHeaders);

        var msg = new ServiceBusMessage(body)
        {
            CorrelationId = correlationId,
            MessageId = context.DeduplicationKey ?? Guid.NewGuid().ToString(),
        };

        if (context.SessionKey is not null)
            msg.SessionId = context.SessionKey;

        if (context.PartitionKey is not null)
            msg.PartitionKey = context.PartitionKey;
        else if (context.SessionKey is not null)
            msg.PartitionKey = context.SessionKey;

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
