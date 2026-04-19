using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Messaging.RabbitMq.Options;

public sealed class RabbitMqFixtureOptions
{
    public const string SectionName = "RigTUnit:RabbitMq";

    [Required]
    public string ImageTag { get; init; } = "3-management";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 180;

    [Required]
    public string Username { get; init; } = "guest";

    [Required]
    public string Password { get; init; } = "guest";
}
