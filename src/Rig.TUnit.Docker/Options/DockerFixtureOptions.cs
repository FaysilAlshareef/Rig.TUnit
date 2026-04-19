using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Docker.Options;

public sealed class DockerFixtureOptions
{
    public const string SectionName = "RigTUnit:Docker";

    /// <summary>Container image for ContainerFixture defaults (e.g., <c>alpine:3</c>).</summary>
    [Required]
    public string DefaultImage { get; init; } = "alpine:3";

    /// <summary>When true, each fixture runs in its own Docker network for isolation.</summary>
    public bool IsolatePerTestNetwork { get; init; } = true;

    /// <summary>When true, reuse pulled-image cache across fixtures to cut startup cost.</summary>
    public bool ReuseImageCache { get; init; } = true;

    [Range(10, 900)]
    public int DefaultStartupTimeoutSeconds { get; init; } = 300;
}
