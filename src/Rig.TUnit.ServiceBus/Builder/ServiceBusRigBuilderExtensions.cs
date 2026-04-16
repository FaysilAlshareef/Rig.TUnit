using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.ServiceBus.Builder;

/// <summary>
/// Entry-point extensions that wire Azure Service Bus testing support into a <see cref="RigBuilder"/>.
/// </summary>
public static class ServiceBusRigBuilderExtensions
{
    /// <summary>
    /// Registers Service Bus test infrastructure scoped to the supplied connection source.
    /// </summary>
    public static RigBuilder UseServiceBus(
        this RigBuilder rigBuilder,
        IRigConnectionSource source,
        Action<ServiceBusRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rigBuilder);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ServiceBusRigBuilder(rigBuilder.Services, source);
        configure(builder);
        return rigBuilder;
    }
}
