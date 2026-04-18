using System.Net;
using System.Text.RegularExpressions;

namespace Rig.TUnit.Http.Matching;

internal sealed class HttpMockExpectation
{
    private readonly HttpMethod? _method;
    private readonly string? _path;
    private readonly Regex? _pathRegex;
    private readonly IReadOnlyList<Predicate<HttpRequestSnapshot>> _predicates;
    private readonly List<HttpMockResponse> _responses = new();
    private int _calls;

    internal HttpMockExpectation(HttpMethod? method, string? path, Regex? pathRegex, IReadOnlyList<Predicate<HttpRequestSnapshot>> predicates)
    {
        _method = method;
        _path = path;
        _pathRegex = pathRegex;
        _predicates = predicates;
    }

    internal void AddResponse(HttpMockResponse response) => _responses.Add(response);

    internal bool Matches(HttpRequestMessage request)
    {
        if (_method is not null && request.Method != _method) return false;
        var reqPath = request.RequestUri?.AbsolutePath ?? string.Empty;
        if (_path is not null && reqPath != _path) return false;
        if (_pathRegex is not null && !_pathRegex.IsMatch(reqPath)) return false;
        return true;
    }

    internal bool MatchesSnapshot(HttpRequestSnapshot snapshot)
    {
        foreach (var p in _predicates)
        {
            if (!p(snapshot)) return false;
        }
        return true;
    }

    internal HttpMockResponse NextResponse()
    {
        if (_responses.Count == 0) return new HttpMockResponse { Status = HttpStatusCode.OK };
        // Scenario/state-machine: return each response in sequence, then stick on the last.
        var idx = Math.Min(_calls, _responses.Count - 1);
        _calls++;
        return _responses[idx];
    }

    internal int CallCount => _calls;
    internal HttpMethod? MatchedMethod => _method;
    internal string? MatchedPath => _path;
}
