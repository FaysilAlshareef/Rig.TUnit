using Azure.Messaging.ServiceBus;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.ServiceBus.Helpers;

public sealed class ServiceBusListener : ListenerBase<ServiceBusReceivedMessage>, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly string _topic;
    private readonly string _subscription;
    private readonly TimeProvider _clock;
    private ServiceBusProcessor? _processor;

    public ServiceBusListener(ServiceBusClient client, string topic, string subscription, TimeProvider? clock = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscription);
        _topic = topic;
        _subscription = subscription;
        _clock = clock ?? TimeProvider.System;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        _processor = _client.CreateProcessor(_topic, _subscription);
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

    private Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
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
            args.Message.CorrelationId));

        return args.CompleteMessageAsync(args.Message);
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args) => Task.CompletedTask;
}
