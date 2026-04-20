namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-060 / SC-008: the repo root MUST ship every OSS-standard governance file:
/// <c>LICENSE</c>, <c>CONTRIBUTING.md</c>, <c>SECURITY.md</c>, <c>CHANGELOG.md</c>, and
/// <c>README.md</c>. Each file must be non-empty to reject forgotten placeholders.
/// </summary>
public sealed class GovernanceFilesTests
{
    private static readonly string[] RequiredFiles =
    [
        "LICENSE",
        "CONTRIBUTING.md",
        "SECURITY.md",
        "CHANGELOG.md",
        "README.md",
    ];

    [Test]
    public async Task RepositoryRoot_ShipsEveryGovernanceFile()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            return;
        }

        var offenders = new List<string>();
        foreach (var file in RequiredFiles)
        {
            var path = Path.Combine(repoRoot, file);
            if (!File.Exists(path))
            {
                offenders.Add($"{file}: missing from repository root");
                continue;
            }
            if (new FileInfo(path).Length < 100)
            {
                offenders.Add($"{file}: present but shorter than 100 bytes — likely placeholder");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "Every OSS-standard governance file must be present at the repo root "
                + "with non-placeholder content (FR-060, SC-008).");
    }

    private static string? TryFindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Rig.TUnit.slnx")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        return null;
    }
}
