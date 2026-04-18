using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Observability.Tracing.Options;

/// <summary>
/// Options for <c>TracingFixture</c> — bound via the standard
/// <c>AddOptions&lt;T&gt;().BindConfiguration(SectionName).ValidateDataAnnotations().ValidateOnStart()</c>
/// pattern.
/// </summary>
public sealed class TracingFixtureOptions
{
    public const string SectionName = "RigTUnit:Tracing";

    /// <summary>Logical service name emitted as the OTEL <c>service.name</c> resource attribute.</summary>
    [Required]
    public required string ServiceName { get; init; }

    /// <summary>Sampling ratio in [0.0, 1.0]. Default 1.0 (record everything in tests).</summary>
    [Range(0.0, 1.0)]
    public double SampleRatio { get; init; } = 1.0;

    /// <summary>Max spans retained in the in-memory exporter before older spans are dropped.</summary>
    [Range(1, 100000)]
    public int MaxSpansInMemory { get; init; } = 10000;
}
