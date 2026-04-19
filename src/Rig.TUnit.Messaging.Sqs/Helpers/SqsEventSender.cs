using Amazon.SQS;
using Amazon.SQS.Model;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Sqs.Helpers;

public sealed class SqsEventSender : EventSenderBase
{
    private readonly IAmazonSQS _client;
    private readonly string _queueUrl;

    public SqsEventSender(IAmazonSQS client, string queueUrl)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentException.ThrowIfNullOrWhiteSpace(queueUrl);
        _queueUrl = queueUrl;
    }

    public async Task SendAsync(
        string body,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additionalHeaders = null,
        CancellationToken ct = default)
    {
        var headers = BuildHeaders(correlationId, causationId, traceparent, additionalHeaders);

        var attributes = new Dictionary<string, MessageAttributeValue>(StringComparer.Ordinal);
        foreach (var kv in headers)
        {
            attributes[kv.Key] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = kv.Value,
            };
        }

        await _client.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = body,
            MessageAttributes = attributes,
        }, ct).ConfigureAwait(false);
    }
}
