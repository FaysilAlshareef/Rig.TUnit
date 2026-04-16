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

public sealed record PingResponse(bool Ok);

public sealed class BenchmarkProgram;

[MemoryDiagnoser]
public class HttpClientHelperBenchmarks
{
    private sealed class BenchmarkFactory : WebApplicationFactory<BenchmarkProgram>
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
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/ping", () => Results.Ok(new PingResponse(true)));
                        });
                    });
                });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseContentRoot(AppContext.BaseDirectory);
            return base.CreateHost(builder);
        }
    }

    private BenchmarkFactory _factory = null!;
    private HttpClientHelper<BenchmarkProgram> _helper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new BenchmarkFactory();
        _helper = new HttpClientHelper<BenchmarkProgram>(_factory);
        _ = _helper.Client; // warm up the host
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _helper.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _factory.Dispose();
    }

    [Benchmark]
    public async Task<PingResponse?> GetAsync_Ping()
    {
        return await _helper.GetAsync<PingResponse>("/ping");
    }
}
