using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Rig.TUnit.Grpc.Extensions;

/// <summary>
/// Extension methods for WebApplicationFactory to support gRPC testing.
/// </summary>
public static class WebApplicationFactoryExtensions
{
    /// <summary>
    /// Configures WebApplicationFactory with optional service configuration and endpoint mapping.
    /// </summary>
    public static WebApplicationFactory<TProgram> WithTestConfiguration<TProgram>(
        this WebApplicationFactory<TProgram> factory,
        Action<IServiceCollection>? configureServices = null,
        Action<IEndpointRouteBuilder>? mapEndpoints = null) where TProgram : class
    {
        return factory.WithWebHostBuilder(builder =>
        {
            if (configureServices is not null)
            {
                builder.ConfigureTestServices(configureServices);
            }

            if (mapEndpoints is not null)
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IStartupFilter>(
                        new EndpointMappingStartupFilter(mapEndpoints));
                });
            }
        });
    }

    /// <summary>
    /// Creates a GrpcChannel backed by the in-memory test server.
    /// </summary>
    public static GrpcChannel CreateGrpcChannel<TProgram>(
        this WebApplicationFactory<TProgram> factory) where TProgram : class
    {
        var client = factory.CreateDefaultClient(new ResponseVersionHandler());
        return GrpcChannel.ForAddress(
            client.BaseAddress ?? throw new InvalidOperationException("BaseAddress is null"),
            new GrpcChannelOptions { HttpClient = client });
    }

    /// <summary>
    /// Delegating handler that sets the HTTP response version to 2.0 for gRPC compatibility.
    /// </summary>
    private sealed class ResponseVersionHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            response.Version = request.Version;
            return response;
        }
    }

    /// <summary>
    /// Startup filter that adds endpoint mappings to the application pipeline.
    /// </summary>
    private sealed class EndpointMappingStartupFilter(Action<IEndpointRouteBuilder> mapEndpoints) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);
                app.UseEndpoints(mapEndpoints);
            };
        }
    }
}
