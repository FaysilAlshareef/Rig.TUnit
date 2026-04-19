using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Observability.AppInsights.Builder;

public static class AppInsightsRigBuilderExtensions
{
    public static RigBuilder UseAppInsights(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<AppInsightsRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new AppInsightsRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
