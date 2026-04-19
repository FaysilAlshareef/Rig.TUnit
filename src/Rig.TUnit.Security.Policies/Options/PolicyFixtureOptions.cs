using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Security.Policies.Options;

public sealed class PolicyFixtureOptions
{
    public const string SectionName = "RigTUnit:Policies";

    [Required]
    public string DefaultScheme { get; init; } = "Test";

    public IReadOnlyList<string> RequiredClaims { get; init; } = Array.Empty<string>();
}
