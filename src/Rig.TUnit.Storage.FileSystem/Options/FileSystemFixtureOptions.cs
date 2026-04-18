using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Storage.FileSystem.Options;

public sealed class FileSystemFixtureOptions
{
    public const string SectionName = "RigTUnit:FileSystem";

    [Required]
    public string RootPathPrefix { get; init; } = "rigtunit-fs";

    public bool CleanupOnDispose { get; init; } = true;
}
