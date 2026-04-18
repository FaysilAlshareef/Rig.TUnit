using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Rig.TUnit.Http.Matching;

namespace Rig.TUnit.Http.Handlers;

/// <summary>
/// <see cref="DelegatingHandler"/> implementation that serves responses from an
/// <see cref="HttpMock"/>. Also supports record/replay modes.
/// </summary>
public sealed class HttpMockDelegatingHandler : DelegatingHandler
{
    private readonly HttpMock _mock;
    private int _globalCallSequence;

    public HttpMockDelegatingHandler(HttpMock mock)
    {
        _mock = mock ?? throw new ArgumentNullException(nameof(mock));
        InnerHandler = _mock.PassthroughInner ?? new NoopHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Snapshot the request once (buffer body; parse query & headers).
        var snap = await BuildSnapshotAsync(request).ConfigureAwait(false);

        if (_mock.Mode == HttpMockMode.Record)
        {
            var upstream = await base.SendAsync(request, ct).ConfigureAwait(false);
            var body = upstream.Content is null ? string.Empty : await upstream.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var headers = upstream.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
            _mock.Recorded.Add(new RecordedExchange(request.Method, snap.Uri?.AbsolutePath ?? "/", upstream.StatusCode, body, headers));
            return upstream;
        }

        var expectation = FindMatch(request, snap);
        if (expectation is null)
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent($"HttpMock: no matching expectation for {request.Method} {request.RequestUri}", Encoding.UTF8, "text/plain")
            };
        }

        var response = expectation.NextResponse();
        var seq = Interlocked.Increment(ref _globalCallSequence);

        if (response.IntermittentFailureEvery is int every && every > 0 && seq % every == 0)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

        if (response.Delay > TimeSpan.Zero)
        {
            var jitter = response.Jitter == TimeSpan.Zero ? TimeSpan.Zero
                : TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * response.Jitter.TotalMilliseconds);
            await Task.Delay(response.Delay + jitter, ct).ConfigureAwait(false);
        }

        return BuildHttpResponse(response);
    }

    private HttpMockExpectation? FindMatch(HttpRequestMessage request, HttpRequestSnapshot snap)
    {
        foreach (var candidate in _mock.MatchAll(request))
        {
            if (candidate.MatchesSnapshot(snap)) return candidate;
        }
        return null;
    }

    private static async Task<HttpRequestSnapshot> BuildSnapshotAsync(HttpRequestMessage request)
    {
        string? body = null;
        if (request.Content is not null)
        {
            body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        var query = HttpMockPathParser.ParseQuery(request.RequestUri?.Query);
        var headers = request.Headers
            .Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
            .ToDictionary(h => h.Key, h => h.Value.ToArray());
        return new HttpRequestSnapshot(
            request.Method,
            request.RequestUri,
            query,
            new HttpMockHeaders(headers),
            body);
    }

    private static HttpResponseMessage BuildHttpResponse(HttpMockResponse r)
    {
        var msg = new HttpResponseMessage(r.Status);
        HttpContent? content = null;
        if (r.SseBody is { } sse)
        {
            content = new StringContent(sse, Encoding.UTF8);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/event-stream");
        }
        else if (r.BinaryBody is { } bytes)
        {
            content = new ByteArrayContent(bytes);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(r.ContentType);
        }
        else if (r.Body is { } body)
        {
            content = new StringContent(body, Encoding.UTF8);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(r.ContentType);
        }
        msg.Content = content ?? new StringContent(string.Empty);

        if (r.Headers is not null)
        {
            foreach (var kv in r.Headers)
            {
                if (!msg.Headers.TryAddWithoutValidation(kv.Key, kv.Value))
                {
                    msg.Content.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }
            }
        }
        return msg;
    }

    private sealed class NoopHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
