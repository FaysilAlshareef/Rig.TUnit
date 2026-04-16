using Microsoft.AspNetCore.Mvc.Testing;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Grpc.Builder;

/// <summary>
/// Entry-point extensions that wire gRPC testing support into a <see cref="RigBuilder"/>.
/// </summary>
public static class GrpcRigBuilderExtensions
{
    /// <summary>
    /// Registers gRPC test infrastructure scoped to the supplied <see cref="WebApplicationFactory{TEntryPoint}"/>.
    /// </summary>
    public static RigBuilder UseGrpc<TProgram>(
        this RigBuilder rigBuilder,
        WebApplicationFactory<TProgram> factory,
        Action<GrpcRigBuilder<TProgram>> configure) where TProgram : class
    {
        ArgumentNullException.ThrowIfNull(rigBuilder);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new GrpcRigBuilder<TProgram>(rigBuilder.Services, factory);
        configure(builder);
        return rigBuilder;
    }
}
