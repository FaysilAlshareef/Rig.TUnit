using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Rig.TUnit.Grpc.Extensions;

namespace Rig.TUnit.Grpc.Helpers;

/// <summary>
/// Creates typed gRPC clients that route through the in-memory test server.
/// </summary>
public sealed class GrpcClientHelper<TClient, TProgram>
    where TClient : ClientBase<TClient>
    where TProgram : class
{
    private readonly GrpcChannel _channel;

    public GrpcClientHelper(WebApplicationFactory<TProgram> factory)
    {
        _channel = factory.CreateGrpcChannel();
    }

    /// <summary>Sends a synchronous gRPC request.</summary>
    public TResult Send<TResult>(Func<TClient, TResult> action)
    {
        var client = CreateClient();
        return action(client);
    }

    /// <summary>Sends an async unary gRPC request.</summary>
    public async Task<TResult> SendAsync<TResult>(Func<TClient, AsyncUnaryCall<TResult>> action)
    {
        var client = CreateClient();
        return await action(client);
    }

    private TClient CreateClient() =>
        (TClient)Activator.CreateInstance(typeof(TClient), _channel)!;
}
