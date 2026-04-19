using KurrentDB.Client;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Assertions;

/// <summary>
/// Fluent stream-level assertions against a KurrentDB (post-rebrand EventStore) stream.
/// Currently exposes <see cref="EventsAppendedAsync"/> — reads the stream forwards from
/// the start and returns the total event count. A missing stream returns 0 (not an error)
/// so callers can write <c>await Assert.That(await StreamAssert.EventsAppendedAsync(c, s)).IsEqualTo(3)</c>.
/// </summary>
public static class StreamAssert
{
    public static async Task<long> EventsAppendedAsync(
        KurrentDBClient client,
        string streamName,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (string.IsNullOrEmpty(streamName))
        {
            throw new ArgumentException("streamName is required.", nameof(streamName));
        }

        var result = client.ReadStreamAsync(
            Direction.Forwards,
            streamName,
            StreamPosition.Start,
            cancellationToken: ct);

        var readState = await result.ReadState.ConfigureAwait(false);
        if (readState == ReadState.StreamNotFound)
        {
            return 0L;
        }

        var count = 0L;
        await foreach (var _ in result.WithCancellation(ct).ConfigureAwait(false))
        {
            count++;
        }
        return count;
    }
}
