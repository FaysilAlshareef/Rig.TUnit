using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rig.TUnit.WebAPI.Helpers;

namespace Rig.TUnit.WebAPI.Tests.Integration;

/// <summary>
/// End-to-end exercises of <see cref="HttpClientHelper{TProgram}"/> against a real
/// WebApplicationFactory standing up minimal routing + an in-memory endpoint.
/// </summary>
public sealed class WebApiIntegrationTests
{
    [Test]
    public async Task HttpClientHelper_GetAsync_DeserializesJsonResponse()
    {
        await using var factory = new TestFactory();
        var helper = new HttpClientHelper<TestProgram>(factory);

        var result = await helper.GetAsync<PingResponse>("/ping", CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Ok).IsTrue();
    }

    [Test]
    public async Task HttpClientHelper_Client_IsStableAcrossCalls()
    {
        await using var factory = new TestFactory();
        var helper = new HttpClientHelper<TestProgram>(factory);

        var first = helper.Client;
        var second = helper.Client;

        await Assert.That(first).IsSameReferenceAs(second);
    }

    [Test]
    public async Task HttpClientHelper_CreateClient_AlwaysReturnsNew()
    {
        await using var factory = new TestFactory();
        var helper = new HttpClientHelper<TestProgram>(factory);

        var a = helper.CreateClient();
        var b = helper.CreateClient();

        await Assert.That(a).IsNotSameReferenceAs(b);
    }

    [Test]
    public async Task HttpClientHelper_WithBearerToken_SetsAuthorizationHeader()
    {
        await using var factory = new TestFactory();
        var helper = new HttpClientHelper<TestProgram>(factory);

        helper.WithBearerToken("abc.def");

        await Assert.That(helper.Client.DefaultRequestHeaders.Authorization).IsNotNull();
        await Assert.That(helper.Client.DefaultRequestHeaders.Authorization!.Scheme).IsEqualTo("Bearer");
        await Assert.That(helper.Client.DefaultRequestHeaders.Authorization.Parameter).IsEqualTo("abc.def");
    }

    [Test]
    public async Task HttpClientHelper_WithHeader_OverridesExistingValue()
    {
        await using var factory = new TestFactory();
        var helper = new HttpClientHelper<TestProgram>(factory);

        helper.WithHeader("X-Tenant", "acme");
        helper.WithHeader("X-Tenant", "zenith");

        await Assert.That(helper.Client.DefaultRequestHeaders.GetValues("X-Tenant")).Contains("zenith");
    }

    public sealed record PingResponse(bool Ok);

    public sealed class TestProgram;

    private sealed class TestFactory : WebApplicationFactory<TestProgram>
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
}
