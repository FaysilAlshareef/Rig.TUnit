using Rig.TUnit.Core.Builder;
using Rig.TUnit.Observability.Builder;

namespace Rig.TUnit.Observability.Metrics.Builder;

public sealed class MetricsRigBuilder : TelemetryRigBuilder<MetricsRigBuilder>
{
    public MetricsRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string MeterName => Source.ConnectionString;
}
