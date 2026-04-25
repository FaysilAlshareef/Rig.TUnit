using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.ServiceBus.Helpers;

/// <summary>
/// Captures messages delivered via a session-enabled subscription, populating
/// <see cref="CapturedMessage{T}.SessionKey"/> from the broker's <c>SessionId</c>.
/// Uses <see cref="ServiceBusSessionProcessor"/> for native per-session FIFO ordering.
/// </summary>
public sealed class ServiceBusSessionListener
    : ListenerBase<ServiceBusReceivedMessage>, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly string _topic;
    private readonly string _subscription;
    private readonly ServiceBusSessionProcessorOptions? _options;
    private readonly TimeProvider _clock;
    private ServiceBusSessionProcessor? _processor;
    private readonly ConcurrentBag<string> _observedSessions = new();
    private readonly ConcurrentQueue<Exception> _observedErrors = new();

    public ServiceBusSessionListener(
        ServiceBusClient client,
        string topic,
        string subscription,
        ServiceBusSessionProcessorOptions? options = null,
        TimeProvider? clock = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscription);
        _topic = topic;
        _subscription = subscription;
        _options = options;
        _clock = clock ?? TimeProvider.System;
    }

    /// <summary>The distinct session IDs observed since <see cref="StartAsync"/> was called.</summary>
    public IReadOnlyCollection<string> ObservedSessions => _observedSessions.Distinct().ToArray();

    /// <summary>
    /// Errors surfaced by <see cref="ServiceBusSessionProcessor"/> (lost session locks, auth failures,
    /// missing subscription, etc.) since <see cref="StartAsync"/>. Tests can assert on these instead of
    /// only observing the symptom "no messages received".
    /// </summary>
    public IReadOnlyCollection<Exception> ObservedErrors => _observedErrors.ToArray();

    /// <summary>The most recent error surfaced by the processor, or <see langword="null"/> if none.</summary>
    public Exception? LastError => _observedErrors.LastOrDefault();

    public override async Task StartAsync(CancellationToken ct)
    {
        _processor = _options is not null
            ? _client.CreateSessionProcessor(_topic, _subscription, _options)
            : _client.CreateSessionProcessor(_topic, _subscription);

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;
        await _processor.StartProcessingAsync(ct).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(ct).ConfigureAwait(false);
            await _processor.DisposeAsync().ConfigureAwait(false);
            _processor = null;
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None).ConfigureAwait(false);

    private Task HandleMessageAsync(ProcessSessionMessageEventArgs args)
    {
        var sessionId = args.SessionId;
        _observedSessions.Add(sessionId);

        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in args.Message.ApplicationProperties)
        {
            headers[kv.Key] = kv.Value?.ToString() ?? "";
        }

        Record(new CapturedMessage<ServiceBusReceivedMessage>(
            args.Message,
            _clock.GetUtcNow(),
            headers,
            args.Message.Body.ToString(),
            args.Message.CorrelationId,
            SessionKey: sessionId));

        return args.CompleteMessageAsync(args.Message);
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        if (args.Exception is not null)
        {
            _observedErrors.Enqueue(args.Exception);
        }
        return Task.CompletedTask;
    }
}
