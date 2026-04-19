using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Rig.TUnit.HealthChecks.Assertions;

namespace Rig.TUnit.HealthChecks.Tests.Integration;

public sealed class HealthAssertTests
{
    private sealed class DepCheck : IHealthCheck
    {
        private readonly DependencyDownSimulator _sim;
        public DepCheck(DependencyDownSimulator sim) { _sim = sim; }
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
            => Task.FromResult(_sim.IsDown ? HealthCheckResult.Unhealthy("dep down") : HealthCheckResult.Healthy("ok"));
    }

    private sealed class TestHarness : IAsyncDisposable
    {
        private readonly IHost _host;
        public HttpClient Client { get; }
        public DependencyDownSimulator Simulator { get; }

        private TestHarness(IHost host, HttpClient client, DependencyDownSimulator sim)
        {
            _host = host; Client = client; Simulator = sim;
        }

        public static async Task<TestHarness> StartAsync()
        {
            var sim = new DependencyDownSimulator();
            var host = await Host.CreateDefaultBuilder()
                .ConfigureWebHost(web =>
                {
                    web.UseTestServer();
                    web.ConfigureServices(s =>
                    {
                        s.AddRouting();
                        s.AddSingleton(sim);
                        s.AddHealthChecks()
                          .AddCheck<DepCheck>("database")
                          .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });
                    });
                    web.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(ep =>
                        {
                            ep.MapHealthChecks("/health/live", new()
                            {
                                Predicate = r => r.Tags.Contains("live"),
                                ResponseWriter = WriteJson,
                            });
                            ep.MapHealthChecks("/health/ready", new()
                            {
                                ResponseWriter = WriteJson,
                            });
                        });
                    });
                }).StartAsync();

            return new TestHarness(host, host.GetTestServer().CreateClient(), sim);
        }

        private static Task WriteJson(HttpContext ctx, HealthReport report)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
            return ctx.Response.WriteAsJsonAsync(new
            {
                status = report.Status.ToString(),
                entries = report.Entries.ToDictionary(kv => kv.Key, kv => new { status = kv.Value.Status.ToString() }),
            });
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    [Test]
    public async Task Ready_IsHealthy_WhenDependencyUp()
    {
        await using var h = await TestHarness.StartAsync();
        await HealthAssert.On(h.Client, "/health/ready").IsHealthy();
    }

    [Test]
    public async Task Ready_FlipsToUnhealthy_WhenDependencyDown()
    {
        await using var h = await TestHarness.StartAsync();
        h.Simulator.GoDown();
        await HealthAssert.On(h.Client, "/health/ready").IsUnhealthy();
    }

    [Test]
    public async Task Live_StaysHealthy_WhenDependencyDown()
    {
        await using var h = await TestHarness.StartAsync();
        h.Simulator.GoDown();
        await HealthAssert.On(h.Client, "/health/live").IsHealthy();
    }

    [Test]
    public async Task Ready_Contains_NamedDependency()
    {
        await using var h = await TestHarness.StartAsync();
        await HealthAssert.On(h.Client, "/health/ready").Contains("database");
    }

    [Test]
    public async Task Ready_Probe_InTime_Budget()
    {
        await using var h = await TestHarness.StartAsync();
        await HealthAssert.On(h.Client, "/health/ready").InTime(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task Probe_Kinds_Distinguished()
    {
        await using var h = await TestHarness.StartAsync();
        h.Simulator.GoDown();
        // Live == Healthy (only `live`-tagged checks), Ready == Unhealthy (includes db).
        await HealthAssert.On(h.Client, "/health/live").IsHealthy();
        await HealthAssert.On(h.Client, "/health/ready").IsUnhealthy();
        // ProbeKind distinguisher — compiled-in ordering.
        var live = (int)ProbeKind.Live;
        var ready = (int)ProbeKind.Ready;
        var startup = (int)ProbeKind.Startup;
        await Assert.That(live).IsEqualTo(0);
        await Assert.That(ready).IsEqualTo(1);
        await Assert.That(startup).IsEqualTo(2);
    }
}
