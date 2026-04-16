using Microsoft.Extensions.DependencyInjection;

namespace Rig.TUnit.Core.Builder;

public sealed class RigBuilder
{
    private readonly IServiceCollection _services;
    private bool _forceContainersInCi;

    internal RigBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// When CI is detected, signals that infrastructure should use container fixtures.
    /// This is a metadata flag — consumers can read it within the configure delegate.
    /// </summary>
    public RigBuilder ForceContainersInCi()
    {
        _forceContainersInCi = true;
        return this;
    }

    public IServiceCollection Services => _services;

    internal bool IsForceContainersInCi => _forceContainersInCi;
}
