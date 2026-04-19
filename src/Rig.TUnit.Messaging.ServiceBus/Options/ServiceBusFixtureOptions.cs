using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Messaging.ServiceBus.Options;

public sealed class ServiceBusFixtureOptions : IValidatableObject
{
    public const string SectionName = "RigTUnit:ServiceBus";

    [Required]
    public string ImageTag { get; init; } = "1.1.2";

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
