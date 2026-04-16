using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Rig.TUnit.ServiceBus.Helpers;

public sealed class ServiceBusEventSender : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusEventSender(string connectionString, string topicName)
    {
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(topicName);
    }

    public async Task SendAsync<T>(T eventData, string? sessionId = null, CancellationToken ct = default)
    {
        var json = JsonConvert.SerializeObject(eventData);
        var message = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            SessionId = sessionId ?? Guid.NewGuid().ToString("N"),
        };

        await _sender.SendMessageAsync(message, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
