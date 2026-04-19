using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Observability.AppInsights.Options;

public sealed class AppInsightsFixtureOptions
{
    public const string SectionName = "RigTUnit:AppInsights";

    /// <summary>
    /// Instrumentation key for the in-process TelemetryClient. Use any non-empty
    /// value — the CapturingTelemetryChannel discards the key since no network
    /// egress happens.
    /// </summary>
    [Required]
    public string InstrumentationKey { get; init; } = "00000000-0000-0000-0000-000000000000";

    /// <summary>Cloud role name attached to every telemetry item.</summary>
    [Required]
    public string RoleName { get; init; } = "rigtunit-tests";
}
