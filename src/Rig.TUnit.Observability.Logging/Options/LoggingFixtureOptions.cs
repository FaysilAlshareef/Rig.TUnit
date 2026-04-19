using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Rig.TUnit.Observability.Logging.Options;

/// <summary>
/// Options for <c>LoggingFixture</c> — bound via
/// <c>AddOptions&lt;T&gt;().BindConfiguration(SectionName).ValidateDataAnnotations().ValidateOnStart()</c>.
/// </summary>
public sealed class LoggingFixtureOptions
{
    public const string SectionName = "RigTUnit:Logging";

    /// <summary>Minimum level captured by the in-memory provider. Default <see cref="LogLevel.Debug"/>.</summary>
    public LogLevel MinLevel { get; init; } = LogLevel.Debug;

    /// <summary>Max entries retained before older entries are dropped. Default 50,000.</summary>
    [Range(1, 1000000)]
    public int MaxEntriesInMemory { get; init; } = 50_000;
}
