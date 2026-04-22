using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

/// <summary>
/// Minimal test factory with routing only — no gRPC service mapped.
/// Used to test <see cref="Rig.TUnit.Grpc.Extensions.WebApplicationFactoryExtensions.WithTestConfiguration"/>
/// and <c>EndpointMappingStartupFilter</c> in isolation without competing UseEndpoints calls.
/// </summary>
internal sealed class MinimalTestFactory : WebApplicationFactory<TestProgram>
{
    protected override IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureWebHostDefaults(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services => services.AddRouting());
                webHost.Configure(app => app.UseRouting());
            });

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseContentRoot(AppContext.BaseDirectory);
        return base.CreateHost(builder);
    }
}
