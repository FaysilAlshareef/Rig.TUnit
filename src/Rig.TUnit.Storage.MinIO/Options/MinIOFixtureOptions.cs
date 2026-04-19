using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Storage.MinIO.Options;

public sealed class MinIOFixtureOptions
{
    public const string SectionName = "RigTUnit:MinIO";

    [Required]
    public string ImageTag { get; init; } = "latest";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 180;

    [Required]
    public string Username { get; init; } = "minioadmin";

    [Required]
    public string Password { get; init; } = "minioadmin";
}
