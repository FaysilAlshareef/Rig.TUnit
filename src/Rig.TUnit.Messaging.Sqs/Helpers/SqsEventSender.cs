using Amazon.SQS;
using Amazon.SQS.Model;
using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Sqs.Helpers;

public sealed class SqsEventSender : EventSenderBase
{
    private readonly IAmazonSQS _client;
    private readonly string _queueUrl;
    private readonly bool _isFifo;

    public SqsEventSender(IAmazonSQS client, string queueUrl)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentException.ThrowIfNullOrWhiteSpace(queueUrl);
        _queueUrl = queueUrl;
        _isFifo = queueUrl.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase);
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
        var attributes = BuildMessageAttributes(headers);

        await _client.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = body,
            MessageAttributes = attributes,
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends <paramref name="body"/> with optional routing hints from <paramref name="context"/>.
    /// <list type="bullet">
    ///   <item><see cref="SendContext.SessionKey"/> → <c>MessageGroupId</c> (required on FIFO queues)</item>
    ///   <item><see cref="SendContext.DeduplicationKey"/> → <c>MessageDeduplicationId</c></item>
    ///   <item><see cref="SendContext.PartitionKey"/> — ignored (meaningless on SQS)</item>
    /// </list>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the queue URL ends with <c>.fifo</c> and <see cref="SendContext.SessionKey"/> is null.
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
        if (_isFifo && context.SessionKey is null)
        {
            throw new InvalidOperationException(
                "SQS FIFO queues require SendContext.SessionKey (mapped to MessageGroupId). " +
                "Set SessionKey or use a standard (non-FIFO) queue.");
        }

        var headers = BuildHeaders(context, correlationId, causationId, traceparent, additionalHeaders);
        var attributes = BuildMessageAttributes(headers);

        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = body,
            MessageAttributes = attributes,
        };

        if (_isFifo && context.SessionKey is not null)
            request.MessageGroupId = context.SessionKey;

        if (context.DeduplicationKey is not null)
            request.MessageDeduplicationId = context.DeduplicationKey;

        await _client.SendMessageAsync(request, ct).ConfigureAwait(false);
    }

    private static Dictionary<string, MessageAttributeValue> BuildMessageAttributes(
        IReadOnlyDictionary<string, string> headers)
    {
        var attributes = new Dictionary<string, MessageAttributeValue>(StringComparer.Ordinal);
        foreach (var kv in headers)
        {
            attributes[kv.Key] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = kv.Value,
            };
        }
        return attributes;
    }
}
