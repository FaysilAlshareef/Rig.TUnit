using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Messaging.Nats.Options;

public sealed class NatsFixtureOptions
{
    public const string SectionName = "RigTUnit:Nats";

    [Required]
    public string ImageTag { get; init; } = "2.10-alpine";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 180;
}
