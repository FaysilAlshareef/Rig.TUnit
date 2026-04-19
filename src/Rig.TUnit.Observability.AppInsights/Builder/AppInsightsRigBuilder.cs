using Rig.TUnit.Core.Builder;
using Rig.TUnit.Observability.Builder;

namespace Rig.TUnit.Observability.AppInsights.Builder;

public sealed class AppInsightsRigBuilder : TelemetryRigBuilder<AppInsightsRigBuilder>
{
    public AppInsightsRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source) { }

    public string InstrumentationKey => Source.ConnectionString;
}
