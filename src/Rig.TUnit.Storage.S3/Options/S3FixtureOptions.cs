using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Storage.S3.Options;

public sealed class S3FixtureOptions
{
    public const string SectionName = "RigTUnit:S3";
    [Required] public string ImageTag { get; init; } = "3";
    [Range(1, 600)] public int StartupTimeoutSeconds { get; init; } = 180;
}
