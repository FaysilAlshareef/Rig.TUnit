using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Messaging.Sqs.Options;

public sealed class SqsFixtureOptions
{
    public const string SectionName = "RigTUnit:Sqs";

    [Required]
    public string ImageTag { get; init; } = "3";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 240;

    [Required]
    public string Region { get; init; } = "us-east-1";

    [Required]
    public string AccessKeyId { get; init; } = "test";

    [Required]
    public string SecretAccessKey { get; init; } = "test";
}
