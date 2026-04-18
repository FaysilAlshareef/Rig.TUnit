using Rig.TUnit.Observability.Seq.Fixtures;

namespace Rig.TUnit.Observability.Seq.Assertions;

/// <summary>
/// Fluent assertions over a Seq instance managed by <see cref="SeqFixture"/>.
/// Same shape as <c>LogAssert</c> so tests can swap between in-memory and Seq with a
/// one-line change.
/// </summary>
public static class SeqAssert
{
    /// <summary>Begins an assertion chain for entries matching the given Seq filter.</summary>
    public static SeqQueryAssertion Query(SeqFixture fixture, string filter)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(filter);
        return new SeqQueryAssertion(fixture, filter);
    }
}

public sealed class SeqQueryAssertion
{
    private readonly SeqFixture _fixture;
    private readonly string _filter;
    private int? _expectedCount;

    internal SeqQueryAssertion(SeqFixture fixture, string filter)
    {
        _fixture = fixture;
        _filter = filter;
    }

    public SeqQueryAssertion Count(int expected)
    {
        if (expected < 0) throw new ArgumentOutOfRangeException(nameof(expected));
        _expectedCount = expected;
        return this;
    }

    /// <summary>
    /// Polls Seq up to <paramref name="timeout"/> waiting for <see cref="Count(int)"/> matches.
    /// Uses Seq's HTTP API (<c>/api/events?filter=...</c>). Requires the fixture to be initialised.
    /// </summary>
    public async Task Within(TimeSpan timeout, CancellationToken ct = default)
    {
        if (_expectedCount is null) throw new InvalidOperationException("Call Count(n) before Within(timeout).");
        var deadline = DateTimeOffset.UtcNow + timeout;
        using var http = new HttpClient();

        while (DateTimeOffset.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var url = $"{_fixture.IngestionUrl}/api/events?filter={Uri.EscapeDataString(_filter)}&count={_expectedCount + 1}";
                var body = await http.GetStringAsync(url, ct);
                var count = CountEvents(body);
                if (count == _expectedCount) return;
            }
            catch (HttpRequestException)
            {
                // transient — Seq may still be warming up; retry until deadline.
            }
            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
        }

        throw new SeqAssertionException(
            $"Expected {_expectedCount} events matching filter '{_filter}' within {timeout}, but count never reached.");
    }

    // Minimal JSON array counter for Seq's response envelope. The full payload is
    // an object; events array is under "Events" or similar key. Parse permissively.
    private static int CountEvents(string body)
    {
        // Count top-level objects in the first array we see. Good enough for count assertions.
        int count = 0, depth = 0;
        var inString = false;
        var seenArray = false;
        for (int i = 0; i < body.Length; i++)
        {
            var c = body[i];
            if (inString)
            {
                if (c == '\\' && i + 1 < body.Length) { i++; continue; }
                if (c == '"') inString = false;
                continue;
            }
            switch (c)
            {
                case '"': inString = true; break;
                case '[': if (!seenArray) { seenArray = true; } break;
                case '{': depth++; break;
                case '}': depth--; if (depth == 0 && seenArray) count++; break;
            }
        }
        return count;
    }
}

public sealed class SeqAssertionException : Exception
{
    public SeqAssertionException(string message) : base(message) { }
}
