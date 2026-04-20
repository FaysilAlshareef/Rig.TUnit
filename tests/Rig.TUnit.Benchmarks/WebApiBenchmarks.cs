using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rig.TUnit.WebAPI.Helpers;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Measures cold-path factory startup + hot-path HttpClient retrieval from
/// <see cref="HttpClientHelper{TProgram}"/>. Complements the existing
/// <see cref="HttpClientHelperBenchmarks"/> which measures the GET round-trip.
/// </summary>
[MemoryDiagnoser]
public class WebApiBenchmarks
{
    public sealed class WebApiBenchmarkProgram;

    private WebApplicationFactory<WebApiBenchmarkProgram>? _factory;

    [IterationSetup]
    public void IterationSetup()
    {
        _factory = new BenchmarkWebFactory();
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _factory?.Dispose();
    }

    [Benchmark]
    public HttpClient ClientRetrieval_FromHelper()
    {
        var helper = new HttpClientHelper<WebApiBenchmarkProgram>(_factory!);
        return helper.Client;
    }

    private sealed class BenchmarkWebFactory : WebApplicationFactory<WebApiBenchmarkProgram>
    {
        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureWebHostDefaults(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services => services.AddRouting());
                    webHost.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(e => e.MapGet("/health", () => Results.Ok()));
                    });
                });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseContentRoot(AppContext.BaseDirectory);
            return base.CreateHost(builder);
        }
    }
}
