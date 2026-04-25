using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Messaging.ServiceBus.Options;

public sealed class ServiceBusFixtureOptions : IValidatableObject
{
    public const string SectionName = "RigTUnit:ServiceBus";

    /// <summary>
    /// Image tag for <c>mcr.microsoft.com/azure-messaging/servicebus-emulator</c>.
    /// 2.0.0 is the first release with Administration-Client (CreateTopic /
    /// CreateSubscription / CreateRule) HTTP routes — 1.1.2 returns HTTP 404 on
    /// every PUT to <c>/{namespace}/{topic}</c>. The admin route is hosted on
    /// the management port (5300) exposed via
    /// <see cref="Fixtures.ServiceBusFixture.AdminConnectionString"/>.
    /// </summary>
    [Required]
    public string ImageTag { get; init; } = "2.0.0";

    [Required]
    public string SqlEdgeImageTag { get; init; } = "1.0.7";

    [Required]
    public string ConfigFilePath { get; init; } = "TestInfrastructure/service-bus-config.json";

    /// <summary>When false, ValidateOnStart fails (EULA must be accepted for the emulator to run).</summary>
    public bool AcceptEula { get; init; } = true;

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 120;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AcceptEula)
        {
            yield return new ValidationResult(
                "ServiceBus emulator requires AcceptEula=true (EULA acceptance).",
                [nameof(AcceptEula)]);
        }
    }
}
