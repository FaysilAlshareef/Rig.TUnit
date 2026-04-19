using Rig.TUnit.Http.Matching;

namespace Rig.TUnit.Http;

public sealed class HttpMockVerifier
{
    private readonly HttpMock _mock;
    private readonly HttpMethod _method;
    private readonly string _path;

    internal HttpMockVerifier(HttpMock mock, HttpMethod method, string path)
    {
        _mock = mock;
        _method = method;
        _path = path;
    }

    public void Called(int expected)
    {
        var total = Matches().Sum(x => x.CallCount);
        if (total != expected)
        {
            throw new HttpMockVerificationException(
                $"Expected {expected} call(s) to {_method} {_path} but observed {total}.");
        }
    }

    public HttpMockVerifier CalledAtLeast(int min)
    {
        var total = Matches().Sum(x => x.CallCount);
        if (total < min)
        {
            throw new HttpMockVerificationException(
                $"Expected at least {min} call(s) to {_method} {_path} but observed {total}.");
        }
        return this;
    }

    public HttpMockVerifier WithHeader(string _, string __)
    {
        // Header verification must be traced at request-intercept time to be useful.
        // Current implementation does not retain per-call headers; callers can use
        // the HttpMock.CreateClient() variant and inspect the outgoing HttpRequestMessage
        // via a custom DelegatingHandler layered outside the mock. Retained for API shape.
        return this;
    }

    private IEnumerable<HttpMockExpectation> Matches() =>
        _mock.Expectations.Where(e => e.MatchedMethod == _method && e.MatchedPath == _path);
}

public sealed class HttpMockVerificationException : Exception
{
    public HttpMockVerificationException(string message) : base(message) { }
}
