using System.Text.RegularExpressions;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-011 / FR-076 / SC-013: every <c>tests/**/Shared*Fixture.cs</c> file MUST carry an
/// `Intentional reuse` rationale comment explaining why sharing the fixture across tests
/// is safe (e.g., per-test helper handles isolation, append-only semantics, etc.).
///
/// The A005 audit classified 20 shared fixtures; the (a)-safe entries keep their shared
/// pattern + rationale, while (b)/(c) entries are being migrated per-family in Phase 3
/// T066/T067 follow-ups.
/// </summary>
public sealed class SharedFixtureGuardTests
{
    private static readonly Regex RationalePattern = new(
        @"(Intentional reuse|per-test isolation|append-only|IsolationKey)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [Test]
    public async Task SharedFixtures_MustCarryRationaleComment()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            return;
        }

        var testsRoot = Path.Combine(repoRoot, "tests");
        if (!Directory.Exists(testsRoot))
        {
            return;
        }

        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(testsRoot, "Shared*Fixture.cs", SearchOption.AllDirectories))
        {
            // Skip obj/bin/
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            var rel = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');

            if (!RationalePattern.IsMatch(text))
            {
                offenders.Add(
                    $"{rel}: missing 'Intentional reuse …' rationale comment "
                    + "(or equivalent: 'per-test isolation', 'append-only', 'IsolationKey')");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "FR-011 / SC-013: every Shared*Fixture.cs must document why sharing is safe. "
                + "See planning/post-005-phase-1/SharedFixture-Audit.md for the approved rationale strings.");
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
