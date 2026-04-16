using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rig.TUnit.WebAPI.Tests.Unit.TestInfrastructure;

/// <summary>
/// Custom factory that hosts the test endpoints without requiring a real entry-point
/// <c>Main</c> method. TUnit test projects cannot declare their own <c>Main</c>, so we override
/// <see cref="WebApplicationFactory{TEntryPoint}.CreateHostBuilder"/> to bypass the default
/// assembly entry-point discovery.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<TestProgram>
{
    protected override IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureWebHostDefaults(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAuthentication();
                    services.AddAuthorization();
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => TestEndpoints.MapEndpoints(endpoints));
                });
            });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseContentRoot(AppContext.BaseDirectory);
        return base.CreateHost(builder);
    }
}
