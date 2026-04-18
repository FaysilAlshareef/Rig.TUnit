using System.Text.Json;
using System.Text.RegularExpressions;

namespace Rig.TUnit.Http.Matching;

/// <summary>
/// Pre-buffered snapshot of a request — body is read once in the handler and passed
/// to predicates. Avoids the reading-stream-twice problem and any sync-over-async.
/// </summary>
public sealed record HttpRequestSnapshot(
    HttpMethod Method,
    Uri? Uri,
    IReadOnlyDictionary<string, string> Query,
    HttpMockHeaders Headers,
    string? Body);

public sealed class HttpMockHeaders
{
    private readonly IReadOnlyDictionary<string, string[]> _headers;
    public HttpMockHeaders(IReadOnlyDictionary<string, string[]> headers) { _headers = headers; }
    public bool Has(string name, string value) =>
        _headers.TryGetValue(name, out var vals) && vals.Any(v => v == value);
}

/// <summary>Fluent builder for <see cref="HttpMockExpectation"/>s.</summary>
public sealed class HttpRequestBuilder
{
    private readonly HttpMock _owner;
    private HttpMethod? _method;
    private string? _path;
    private Regex? _pathRegex;
    private readonly List<Predicate<HttpRequestSnapshot>> _predicates = new();

    internal HttpRequestBuilder(HttpMock owner) { _owner = owner; }

    public HttpRequestBuilder Method(HttpMethod method) { _method = method; return this; }
    public HttpRequestBuilder Get() => Method(HttpMethod.Get);
    public HttpRequestBuilder Post() => Method(HttpMethod.Post);
    public HttpRequestBuilder Put() => Method(HttpMethod.Put);
    public HttpRequestBuilder Delete() => Method(HttpMethod.Delete);
    public HttpRequestBuilder Patch() => Method(HttpMethod.Patch);

    public HttpRequestBuilder Path(string path) { _path = path; return this; }
    public HttpRequestBuilder PathMatching(string regex) { _pathRegex = new Regex(regex, RegexOptions.Compiled); return this; }

    public HttpRequestBuilder Query(string key, string value)
    {
        _predicates.Add(snap => snap.Query.TryGetValue(key, out var v) && v == value);
        return this;
    }

    public HttpRequestBuilder Header(string key, string value)
    {
        _predicates.Add(snap => snap.Headers.Has(key, value));
        return this;
    }

    public HttpRequestBuilder BodyContains(string fragment)
    {
        _predicates.Add(snap => snap.Body?.Contains(fragment, StringComparison.Ordinal) == true);
        return this;
    }

    public HttpRequestBuilder BodyMatching(string regex)
    {
        var rx = new Regex(regex, RegexOptions.Compiled);
        _predicates.Add(snap => snap.Body is { } b && rx.IsMatch(b));
        return this;
    }

    public HttpRequestBuilder JsonPathEquals(string dotPath, string expected)
    {
        _predicates.Add(snap =>
        {
            if (string.IsNullOrEmpty(snap.Body)) return false;
            try
            {
                using var doc = JsonDocument.Parse(snap.Body);
                var cur = doc.RootElement;
                foreach (var part in dotPath.Split('.'))
                {
                    if (cur.ValueKind != JsonValueKind.Object) return false;
                    if (!cur.TryGetProperty(part, out cur)) return false;
                }
                return cur.ToString() == expected;
            }
            catch (JsonException)
            {
                return false;
            }
        });
        return this;
    }

    internal HttpMockExpectation BuildExpectation() => new(_method, _path, _pathRegex, _predicates);

    public HttpResponseConfigurator Responds() => new(_owner, BuildExpectation());

    public HttpResponseConfigurator Responds(System.Net.HttpStatusCode status, string body, IReadOnlyDictionary<string, string>? headers = null)
    {
        var cfg = Responds().WithStatus(status).WithBody(body);
        if (headers is not null)
        {
            foreach (var kv in headers) cfg = cfg.WithHeader(kv.Key, kv.Value);
        }
        return cfg;
    }
}

internal static class HttpMockPathParser
{
    public static IReadOnlyDictionary<string, string> ParseQuery(string? query)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(query)) return dict;
        var q = query.StartsWith("?") ? query[1..] : query;
        foreach (var pair in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            if (idx < 0) dict[Uri.UnescapeDataString(pair)] = string.Empty;
            else dict[Uri.UnescapeDataString(pair[..idx])] = Uri.UnescapeDataString(pair[(idx + 1)..]);
        }
        return dict;
    }
}
