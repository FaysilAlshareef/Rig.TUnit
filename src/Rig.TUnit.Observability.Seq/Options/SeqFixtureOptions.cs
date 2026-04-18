using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Observability.Seq.Options;

public sealed class SeqFixtureOptions
{
    public const string SectionName = "RigTUnit:Seq";

    [Required]
    public string ImageTag { get; init; } = "latest";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 60;

    /// <summary>When true, <c>SeqFixture.CaptureDashboardSnapshot()</c> writes a snapshot artifact.</summary>
    public bool CaptureDashboardSnapshot { get; init; } = true;

    public string SnapshotDirectory { get; init; } = "TestResults/seq-dashboards";
}
