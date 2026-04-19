using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Observability.Metrics.Options;

public sealed class MetricsFixtureOptions
{
    public const string SectionName = "RigTUnit:Metrics";

    [Required]
    public string MeterName { get; init; } = "Rig.TUnit.Metrics";

    [Range(1, 10_000)]
    public int MaxTagCardinality { get; init; } = 100;
}
