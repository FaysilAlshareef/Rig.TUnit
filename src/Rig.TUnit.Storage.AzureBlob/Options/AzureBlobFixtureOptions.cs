using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Storage.AzureBlob.Options;

public sealed class AzureBlobFixtureOptions
{
    public const string SectionName = "RigTUnit:AzureBlob";
    [Required] public string ImageTag { get; init; } = "latest";
    [Range(1, 600)] public int StartupTimeoutSeconds { get; init; } = 120;
}
