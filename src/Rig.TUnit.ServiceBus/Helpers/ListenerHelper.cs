using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Rig.TUnit.Core.Helpers;

namespace Rig.TUnit.ServiceBus.Helpers;

public sealed class ListenerHelper : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSessionProcessor _processor;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentBag<ServiceBusReceivedMessage> _messages = [];
    private readonly ConcurrentBag<ProcessErrorEventArgs> _errors = [];

    public IReadOnlyCollection<ServiceBusReceivedMessage> Messages => _messages;

    public IReadOnlyCollection<ProcessErrorEventArgs> Errors => _errors;

    public ListenerHelper(
        string connectionString,
        string topicName,
        string subscriptionName,
        TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _client = new ServiceBusClient(connectionString);

        var options = new ServiceBusSessionProcessorOptions
        {
            AutoCompleteMessages = false,
            PrefetchCount = 1,
            MaxConcurrentCallsPerSession = 1,
            MaxConcurrentSessions = 5,
        };

        _processor = _client.CreateSessionProcessor(topicName, subscriptionName, options);
        _processor.ProcessMessageAsync += OnMessage;
        _processor.ProcessErrorAsync += OnError;
    }

    public async Task StartAsync() => await _processor.StartProcessingAsync();

    public async Task StopAsync() => await _processor.StopProcessingAsync();

    public async ValueTask DisposeAsync()
    {
        await _processor.CloseAsync();
        await _client.DisposeAsync();
    }

    public async Task WaitForMessagesAsync(
        int expectedCount = 1,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(15);

        try
        {
            await WaitHelper.WaitForAsync(
                () => _messages.Count >= expectedCount,
                effectiveTimeout,
                cancellationToken: ct);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException(
                $"Expected {expectedCount} messages but received {_messages.Count} within {effectiveTimeout}.");
        }
    }

    private async Task OnMessage(ProcessSessionMessageEventArgs args)
    {
        _messages.Add(args.Message);
        await args.CompleteMessageAsync(args.Message);
    }

    private Task OnError(ProcessErrorEventArgs args)
    {
        _errors.Add(args);
        throw args.Exception;
    }
}
