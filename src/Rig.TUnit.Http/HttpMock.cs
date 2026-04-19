using System.Collections.Concurrent;
using System.Net;
using Rig.TUnit.Http.Handlers;
using Rig.TUnit.Http.Matching;

namespace Rig.TUnit.Http;

/// <summary>
/// In-memory HTTP mock. Configure expectations with <see cref="When"/>, retrieve an
/// <see cref="HttpClient"/> with <see cref="CreateClient"/>, and assert interactions
/// with <see cref="Verify"/>.
/// </summary>
public sealed class HttpMock
{
    private readonly List<HttpMockExpectation> _expectations = new();
    internal ConcurrentBag<CapturedRequest> Captured { get; } = new();
    internal HttpMockMode Mode { get; private set; } = HttpMockMode.Mock;
    internal HttpMessageHandler? PassthroughInner { get; private set; }
    internal List<RecordedExchange> Recorded { get; } = new();

    public HttpRequestBuilder When => new(this);

    internal void Register(HttpMockExpectation expectation) => _expectations.Add(expectation);

    internal IReadOnlyList<HttpMockExpectation> Expectations => _expectations;

    /// <summary>Returns an <see cref="HttpClient"/> whose messages are handled by this mock.</summary>
    public HttpClient CreateClient()
    {
        var handler = new HttpMockDelegatingHandler(this);
        return new HttpClient(handler) { BaseAddress = new Uri("http://mock/") };
    }

    /// <summary>Creates the <see cref="HttpMockDelegatingHandler"/> for use in an existing pipeline.</summary>
    public HttpMockDelegatingHandler AsHandler() => new(this);

    /// <summary>
    /// Switches the mock into record mode. Requests are proxied to <paramref name="inner"/>
    /// and exchanges are captured into <see cref="RecordedExchanges"/>.
    /// </summary>
    public HttpMock RecordAgainst(HttpMessageHandler inner)
    {
        Mode = HttpMockMode.Record;
        PassthroughInner = inner;
        return this;
    }

    /// <summary>Switches to replay mode. Recorded exchanges become expectations.</summary>
    public HttpMock ReplayFrom(IEnumerable<RecordedExchange> recorded)
    {
        Mode = HttpMockMode.Replay;
        Recorded.Clear();
        Recorded.AddRange(recorded);
        foreach (var ex in Recorded)
        {
            When.Method(ex.Method).Path(ex.Path)
                .Responds(ex.ResponseStatus, ex.ResponseBody, ex.ResponseHeaders)
                .And();
        }
        return this;
    }

    public IReadOnlyList<RecordedExchange> RecordedExchanges => Recorded;

    // ─── Matching core ───────────────────────────────────────────────────────

    internal IEnumerable<HttpMockExpectation> MatchAll(HttpRequestMessage request)
    {
        foreach (var expectation in _expectations)
        {
            if (expectation.Matches(request)) yield return expectation;
        }
    }

    // ─── Verification ────────────────────────────────────────────────────────

    public HttpMockVerifier Verify(HttpMethod method, string path) => new(this, method, path);
}

public enum HttpMockMode { Mock, Record, Replay }

internal sealed record CapturedRequest(
    HttpMethod Method,
    string Path,
    IReadOnlyDictionary<string, string> Query,
    IReadOnlyDictionary<string, string> Headers,
    string? Body);

public sealed record RecordedExchange(
    HttpMethod Method,
    string Path,
    HttpStatusCode ResponseStatus,
    string ResponseBody,
    IReadOnlyDictionary<string, string>? ResponseHeaders = null);
