using System.Net;
using BenchmarkDotNet.Attributes;
using Rig.TUnit.Http;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Measures per-request overhead of an in-memory <see cref="HttpMock"/> match +
/// canned-response pipeline.
/// </summary>
[MemoryDiagnoser]
public class HttpMockBenchmarks
{
    private HttpMock _mock = null!;
    private HttpClient _client = null!;

    [GlobalSetup]
    public void Setup()
    {
        _mock = new HttpMock();
        _mock.When.Get().Path("/ping").Responds().WithStatus(HttpStatusCode.OK).And();
        _client = _mock.CreateClient();
    }

    [GlobalCleanup]
    public void Cleanup() => _client.Dispose();

    [Benchmark]
    public async Task<HttpResponseMessage> MatchedGet_Ping()
        => await _client.GetAsync("/ping");
}
