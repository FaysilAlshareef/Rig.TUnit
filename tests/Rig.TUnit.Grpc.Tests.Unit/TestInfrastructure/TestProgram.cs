using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

/// <summary>
/// Marker class for type identification in generic constraints.
/// </summary>
public sealed class TestProgram;

/// <summary>
/// Test server factory that creates a properly configured TestServer
/// for gRPC testing without requiring a real application entry point.
/// </summary>
public sealed class TestServerFactory : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly TestServer _testServer;

    public TestServerFactory()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddGrpc();
                    services.AddRouting();
                    services.AddMediatR(cfg =>
                        cfg.RegisterServicesFromAssemblyContaining<TestRequestHandler>());
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGrpcService<TestGrpcService>();
                    });
                });
            })
            .Build();

        _host.Start();
        _testServer = _host.GetTestServer();
    }

    /// <summary>Gets the service provider from the test host.</summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>Creates an HttpClient configured for the test server.</summary>
    public HttpClient CreateDefaultClient()
    {
        var client = _testServer.CreateClient();
        return client;
    }

    /// <summary>Creates a GrpcChannel backed by the test server.</summary>
    public GrpcChannel CreateGrpcChannel()
    {
        var handler = _testServer.CreateHandler();
        var delegatingHandler = new ResponseVersionHandler
        {
            InnerHandler = handler
        };

        var client = new HttpClient(delegatingHandler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        return GrpcChannel.ForAddress(
            client.BaseAddress,
            new GrpcChannelOptions { HttpClient = client });
    }

    public async ValueTask DisposeAsync()
    {
        _testServer.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }

    /// <summary>
    /// Delegating handler that sets the HTTP response version for gRPC compatibility.
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
}
