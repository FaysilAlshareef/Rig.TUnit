using System.Net;

namespace Rig.TUnit.Http.Matching;

internal sealed record HttpMockResponse(
    HttpStatusCode Status = HttpStatusCode.OK,
    string? Body = null,
    byte[]? BinaryBody = null,
    string ContentType = "application/json",
    IReadOnlyDictionary<string, string>? Headers = null,
    TimeSpan Delay = default,
    TimeSpan Jitter = default,
    int? IntermittentFailureEvery = null,
    string? SseBody = null);

public sealed class HttpResponseConfigurator
{
    private readonly HttpMock _owner;
    private readonly HttpMockExpectation _expectation;
    private HttpMockResponse _response = new();
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    internal HttpResponseConfigurator(HttpMock owner, HttpMockExpectation expectation)
    {
        _owner = owner;
        _expectation = expectation;
    }

    public HttpResponseConfigurator WithStatus(HttpStatusCode status)
        => Update(_response with { Status = status });

    public HttpResponseConfigurator WithBody(string body)
        => Update(_response with { Body = body });

    public HttpResponseConfigurator WithJson(string json)
        => Update(_response with { Body = json, ContentType = "application/json" });

    public HttpResponseConfigurator WithBinary(byte[] bytes, string contentType = "application/octet-stream")
        => Update(_response with { BinaryBody = bytes, ContentType = contentType });

    public HttpResponseConfigurator WithHeader(string key, string value)
    {
        _headers[key] = value;
        return Update(_response with { Headers = new Dictionary<string, string>(_headers) });
    }

    public HttpResponseConfigurator WithDelay(TimeSpan delay)
        => Update(_response with { Delay = delay });

    public HttpResponseConfigurator WithJitter(TimeSpan jitter)
        => Update(_response with { Jitter = jitter });

    public HttpResponseConfigurator IntermittentFailure(int every)
        => Update(_response with { IntermittentFailureEvery = every });

    /// <summary>Configures the response as a Server-Sent Events stream.</summary>
    public HttpResponseConfigurator WithSse(string sseBody)
        => Update(_response with { SseBody = sseBody, ContentType = "text/event-stream" });

    /// <summary>Adds a next response in a scenario state machine.</summary>
    public HttpResponseConfigurator Then()
    {
        _expectation.AddResponse(_response);
        return new HttpResponseConfigurator(_owner, _expectation);
    }

    /// <summary>Finalises the expectation and registers it on the mock.</summary>
    public HttpMock And()
    {
        _expectation.AddResponse(_response);
        _owner.Register(_expectation);
        return _owner;
    }

    private HttpResponseConfigurator Update(HttpMockResponse next) { _response = next; return this; }
}
