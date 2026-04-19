using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Messaging.Kafka.Options;

public sealed class KafkaFixtureOptions
{
    public const string SectionName = "RigTUnit:Kafka";

    [Required]
    public string ImageTag { get; init; } = "7.6.0";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 300;
}
