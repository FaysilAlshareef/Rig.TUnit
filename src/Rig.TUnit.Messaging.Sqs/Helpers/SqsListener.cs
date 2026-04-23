using Amazon.SQS;
using Amazon.SQS.Model;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Sqs.Helpers;

public sealed class SqsListener : ListenerBase<Message>, IAsyncDisposable
{
    private readonly IAmazonSQS _client;
    private readonly string _queueUrl;
    private readonly TimeProvider _clock;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public SqsListener(IAmazonSQS client, string queueUrl, TimeProvider? clock = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentException.ThrowIfNullOrWhiteSpace(queueUrl);
        _queueUrl = queueUrl;
        _clock = clock ?? TimeProvider.System;
    }

    public override Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loop = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            _cts.Dispose();
            _cts = null;
        }
        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            _loop = null;
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None).ConfigureAwait(false);

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            ReceiveMessageResponse response;
            try
            {
                response = await _client.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5,
                    MessageAttributeNames = ["All"],
                }, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (response.Messages is null)
            {
                continue;
            }

            foreach (var msg in response.Messages)
            {
                var headers = new Dictionary<string, string>(StringComparer.Ordinal);
                if (msg.MessageAttributes is not null)
                {
                    foreach (var kv in msg.MessageAttributes)
                    {
                        headers[kv.Key] = kv.Value.StringValue ?? "";
                    }
                }

                headers.TryGetValue("x-correlation-id", out var correlationId);

                Record(new CapturedMessage<Message>(
                    msg,
                    _clock.GetUtcNow(),
                    headers,
                    msg.Body ?? string.Empty,
                    correlationId));

                await _client.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, ct).ConfigureAwait(false);
            }
        }
    }
}
