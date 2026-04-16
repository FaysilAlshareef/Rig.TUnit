using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that hosts the gRPC <see cref="TestGrpcService"/>
/// without requiring a real entry-point <c>Main</c>. TUnit test projects cannot declare their own
/// <c>Main</c>, so we override <see cref="CreateHostBuilder"/> to bypass assembly entry-point discovery.
/// </summary>
public class TestGrpcWebApplicationFactory : WebApplicationFactory<TestProgram>
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
                    services.AddGrpc();
                    services.AddRouting();
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGrpcService<TestGrpcService>();
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
