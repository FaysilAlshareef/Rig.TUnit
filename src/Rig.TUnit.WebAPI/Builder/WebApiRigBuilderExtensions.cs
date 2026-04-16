using Microsoft.AspNetCore.Mvc.Testing;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.WebAPI.Builder;

/// <summary>
/// Entry-point extensions for wiring WebAPI testing support into a <see cref="RigBuilder"/>.
/// </summary>
public static class WebApiRigBuilderExtensions
{
    /// <summary>
    /// Registers WebAPI test helpers scoped to the supplied <see cref="WebApplicationFactory{TEntryPoint}"/>.
    /// </summary>
    public static RigBuilder UseWebApi<TProgram>(
        this RigBuilder rigBuilder,
        WebApplicationFactory<TProgram> factory,
        Action<WebApiRigBuilder<TProgram>> configure) where TProgram : class
    {
        ArgumentNullException.ThrowIfNull(rigBuilder);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(configure);

        var webApiBuilder = new WebApiRigBuilder<TProgram>(rigBuilder.Services, factory);
        configure(webApiBuilder);
        return rigBuilder;
    }
}
