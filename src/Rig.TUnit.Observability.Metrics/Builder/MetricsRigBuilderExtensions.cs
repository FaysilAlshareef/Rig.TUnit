using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Observability.Metrics.Builder;

public static class MetricsRigBuilderExtensions
{
    public static RigBuilder UseMetricsCapture(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<MetricsRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new MetricsRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
