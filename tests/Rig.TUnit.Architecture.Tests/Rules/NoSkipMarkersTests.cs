using System.Text.RegularExpressions;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-004 / FR-075 / SC-012: no test outside the 4 legitimate architecture-rule files may
/// introduce a <c>[Category("SkipUntilFixed")]</c>, <c>[Skip]</c>, or
/// <c>[NotInParallel]</c> marker. Phase 3 retired every legacy marker; this rule guards
/// against regression.
/// </summary>
public sealed class NoSkipMarkersTests
{
    /// <summary>
    /// Rule files that legitimately reference the forbidden markers as literal strings
    /// for their own skip-list plumbing. Matched by filename, case-sensitive.
    /// </summary>
    private static readonly HashSet<string> ExemptFiles = new(StringComparer.Ordinal)
    {
        "TestCompletenessTests.cs",
        "ProviderCompletenessTests.cs",
        "TestFileOrganizationTests.cs",
        "ReadmeCompletenessTests.cs",
        "NoSkipMarkersTests.cs",
        // Environment-variable mutation tests — must serialise process-global state.
        // Pre-005 [NotInParallel] is grandfathered; per-test env-var sandboxing is a
        // Phase-3-follow-up (see planning/post-005-phase-1/SharedFixture-Audit.md).
        "EnvironmentDetectionTests.cs",
        "ConnectionSourceTests.cs",
    };

    private static readonly Regex SkipCategoryPattern = new(
        @"\[\s*Category\s*\(\s*""SkipUntilFixed""\s*\)\s*\]",
        RegexOptions.Compiled);

    private static readonly Regex SkipAttributePattern = new(
        @"\[\s*Skip(?![a-zA-Z_])",
        RegexOptions.Compiled);

    private static readonly Regex NotInParallelPattern = new(
        @"\[\s*NotInParallel\s*(?:\(|\])",
        RegexOptions.Compiled);

    [Test]
    public async Task NoSkipMarkers_AnywhereInTestTree_MustNotExist()
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
        foreach (var file in Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);
            if (ExemptFiles.Contains(fileName))
            {
                continue;
            }

            // Skip obj/bin/
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            var rel = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');

            if (SkipCategoryPattern.IsMatch(text))
            {
                offenders.Add($"{rel}: contains [Category(\"SkipUntilFixed\")] — forbidden per FR-004");
            }

            if (SkipAttributePattern.IsMatch(text))
            {
                offenders.Add($"{rel}: contains [Skip...] attribute — forbidden per FR-004");
            }

            if (NotInParallelPattern.IsMatch(text))
            {
                offenders.Add($"{rel}: contains [NotInParallel] — forbidden per FR-004 (use per-test isolation instead)");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "FR-004 forbids introducing new SkipUntilFixed / Skip / NotInParallel markers. "
                + "Use per-test isolation helpers (PerTestHelper, ephemeral DBs) instead.");
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
