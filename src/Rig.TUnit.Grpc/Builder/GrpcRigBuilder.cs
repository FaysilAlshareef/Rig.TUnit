using Grpc.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Extensions;
using Rig.TUnit.Grpc.Extensions;

namespace Rig.TUnit.Grpc.Builder;

/// <summary>
/// Fluent builder that wires gRPC test infrastructure into an <see cref="IServiceCollection"/>.
/// gRPC clients are stateless and thread-safe, so replacements are registered as Singleton.
/// </summary>
public sealed class GrpcRigBuilder<TProgram> where TProgram : class
{
    private readonly IServiceCollection _services;
    private readonly WebApplicationFactory<TProgram> _factory;

    internal GrpcRigBuilder(IServiceCollection services, WebApplicationFactory<TProgram> factory)
    {
        _services = services;
        _factory = factory;
    }

    /// <summary>
    /// Replaces the <typeparamref name="TClient"/> gRPC client registration with a client connected to
    /// the in-memory test server via <see cref="WebApplicationFactoryExtensions.CreateGrpcChannel{TProgram}"/>.
    /// </summary>
    public GrpcRigBuilder<TProgram> ReplaceClient<TClient>() where TClient : ClientBase<TClient>
    {
        _services.RemoveService<TClient>();

        var channel = _factory.CreateGrpcChannel();
        var client = (TClient)Activator.CreateInstance(typeof(TClient), channel)!;
        _services.AddSingleton(client);

        return this;
    }
}
