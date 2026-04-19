using Rig.TUnit.Microservices.Outbox.Fixtures;

namespace Rig.TUnit.Microservices.Outbox.Simulators;

/// <summary>
/// Drains the outbox store in batches, invoking <paramref name="publish"/> on each
/// message. Marks each row as relayed. Exactly-once under concurrent relay runs is
/// achieved via the claim pattern: <see cref="IOutboxStore"/> implementations must
/// use a conditional update so only one worker marks the row relayed.
///
/// Publish failures are captured to the row's <c>FailureReason</c> only for the
/// explicit transient-exception set: <see cref="InvalidOperationException"/>,
/// <see cref="TimeoutException"/>, <see cref="HttpRequestException"/>,
/// <see cref="IOException"/>. Anything else propagates.
/// </summary>
public sealed class OutboxRelaySimulator
{
    private readonly IOutboxStore _store;
    private readonly Func<OutboxEventEnvelope, CancellationToken, Task> _publish;
    private readonly TimeProvider _time;

    public OutboxRelaySimulator(
        IOutboxStore store,
        Func<OutboxEventEnvelope, CancellationToken, Task> publish,
        TimeProvider? timeProvider = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _publish = publish ?? throw new ArgumentNullException(nameof(publish));
        _time = timeProvider ?? TimeProvider.System;
    }

    public async Task<int> DrainAsync(int batchSize = 100, CancellationToken ct = default)
    {
        var pending = await _store.ReadPendingAsync(batchSize, ct);
        if (pending.Count == 0) return 0;

        var now = _time.GetUtcNow();
        var relayed = 0;
        foreach (var row in pending)
        {
            ct.ThrowIfCancellationRequested();
            var envelope = new OutboxEventEnvelope(
                row.Id, row.AggregateId, row.EventType, row.Payload, row.OccurredAt,
                row.CorrelationId, row.CausationId, row.Traceparent);

            var attempt = await TryPublishAsync(envelope, ct);
            if (attempt.Success)
            {
                await _store.MarkRelayedAsync(row.Id, now, ct);
                relayed++;
            }
            else
            {
                await _store.MarkFailedAsync(row.Id, attempt.Reason!, ct);
            }
        }
        return relayed;
    }

    private async Task<PublishAttempt> TryPublishAsync(OutboxEventEnvelope envelope, CancellationToken ct)
    {
        try
        {
            await _publish(envelope, ct);
            return PublishAttempt.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return PublishAttempt.Failed(Format(ex));
        }
        catch (TimeoutException ex)
        {
            return PublishAttempt.Failed(Format(ex));
        }
        catch (HttpRequestException ex)
        {
            return PublishAttempt.Failed(Format(ex));
        }
        catch (IOException ex)
        {
            return PublishAttempt.Failed(Format(ex));
        }
    }

    private static string Format(Exception ex) => $"{ex.GetType().Name}: {ex.Message}";

    private readonly record struct PublishAttempt(bool Success, string? Reason)
    {
        public static PublishAttempt Ok() => new(true, null);
        public static PublishAttempt Failed(string reason) => new(false, reason);
    }
}
