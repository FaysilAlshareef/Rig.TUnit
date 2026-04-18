using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Security.Jwt.Options;

public sealed class JwtBuilderOptions
{
    public const string SectionName = "RigTUnit:Jwt";

    [Required]
    public required string DefaultIssuer { get; init; }

    [Required]
    public required string DefaultAudience { get; init; }

    [Range(1, 1440)]
    public int DefaultLifetimeMinutes { get; init; } = 60;
}
