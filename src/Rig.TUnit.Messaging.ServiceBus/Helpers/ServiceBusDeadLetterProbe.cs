using Azure.Messaging.ServiceBus;
using Rig.TUnit.Messaging.Assertions;

namespace Rig.TUnit.Messaging.ServiceBus.Helpers;

/// <summary>
/// Probes the dead-letter sub-queue of a Service Bus topic subscription.
/// Implements <see cref="IDeadLetterProbe"/> for use with <see cref="DeadLetterAssert"/>.
/// </summary>
public sealed class ServiceBusDeadLetterProbe : IDeadLetterProbe, IAsyncDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(300);

    private readonly ServiceBusReceiver _dlqReceiver;

    public ServiceBusDeadLetterProbe(ServiceBusClient client, string topic, string subscription)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscription);

        _dlqReceiver = client.CreateReceiver(topic, subscription,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
    }

    public async Task HasMessageAsync(string expectedReason, TimeSpan timeout, CancellationToken ct)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Probe timeout must be positive.");
        }

        // Linked CTS lets us tell apart "caller cancelled" from "probe deadline elapsed":
        // when only the inner CancelAfter fires, ct.IsCancellationRequested stays false.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cts.Token).ConfigureAwait(false))
            {
                var msg = await _dlqReceiver.PeekMessageAsync(cancellationToken: cts.Token).ConfigureAwait(false);
                if (msg is not null
                    && msg.DeadLetterReason?.Contains(expectedReason, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Inner deadline elapsed; surface a diagnostic instead of the raw OCE.
        }

        ct.ThrowIfCancellationRequested();
        throw new InvalidOperationException(
            $"DeadLetterAssert: no message with reason '{expectedReason}' found on DLQ within {timeout.TotalSeconds:0}s.");
    }

    public async Task IsEmptyAsync(CancellationToken ct)
    {
        var msg = await _dlqReceiver.PeekMessageAsync(cancellationToken: ct).ConfigureAwait(false);
        if (msg is not null)
            throw new InvalidOperationException("DeadLetterAssert: expected DLQ to be empty but found a message.");
    }

    public async ValueTask DisposeAsync()
    {
        await _dlqReceiver.DisposeAsync().ConfigureAwait(false);
    }
}
