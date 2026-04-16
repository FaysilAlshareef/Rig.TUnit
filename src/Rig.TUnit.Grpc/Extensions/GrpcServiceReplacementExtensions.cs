using Grpc.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Extensions;

namespace Rig.TUnit.Grpc.Extensions;

/// <summary>
/// Extension methods for replacing gRPC client registrations to route through the test server.
/// gRPC clients are stateless and thread-safe, making Singleton appropriate for test scenarios.
/// </summary>
public static class GrpcServiceReplacementExtensions
{
    /// <summary>
    /// Replaces a gRPC client registration to route through the test server.
    /// Registers the replacement client as Singleton since gRPC clients are thread-safe.
    /// </summary>
    public static IServiceCollection ReplaceGrpcClient<TClient, TProgram>(
        this IServiceCollection services,
        WebApplicationFactory<TProgram> factory)
        where TClient : ClientBase<TClient>
        where TProgram : class
    {
        services.RemoveService<TClient>();

        var channel = factory.CreateGrpcChannel();
        var client = (TClient)Activator.CreateInstance(typeof(TClient), channel)!;
        services.AddSingleton(client);

        return services;
    }
}
