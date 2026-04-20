namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-061 / FR-062 / SC-010: the canonical provider README template and the
/// reviewer rubric MUST ship at their documented paths. The template MUST
/// contain every one of the 14 section headings the rubric grades against.
/// </summary>
public sealed class CanonicalTemplateTests
{
    private const string TemplatePath = "docs/templates/PROVIDER_README_TEMPLATE.md";
    private const string QualityBarPath = "docs/QUALITY-BAR.md";

    /// <summary>
    /// Required literal headings in the template. Order matters — the 14-section
    /// structure is load-bearing for Phase 6c README rewrites.
    /// </summary>
    private static readonly string[] RequiredSectionMarkers =
    [
        "<!-- SECTION 1 -->",
        "<!-- SECTION 2 -->",
        "<!-- SECTION 3 -->",
        "<!-- SECTION 4 -->",
        "<!-- SECTION 5 -->",
        "<!-- SECTION 6 -->",
        "<!-- SECTION 7 -->",
        "<!-- SECTION 8 -->",
        "<!-- SECTION 9 -->",
        "<!-- SECTION 10 -->",
        "<!-- SECTION 11 -->",
        "<!-- SECTION 12 -->",
        "<!-- SECTION 13 -->",
        "<!-- SECTION 14 -->",
    ];

    [Test]
    public async Task CanonicalTemplate_ExistsWith14Sections()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            return;
        }

        var path = Path.Combine(repoRoot, TemplatePath);
        var offenders = new List<string>();

        if (!File.Exists(path))
        {
            offenders.Add($"{TemplatePath}: missing");
        }
        else
        {
            var text = File.ReadAllText(path);
            foreach (var marker in RequiredSectionMarkers)
            {
                if (!text.Contains(marker, StringComparison.Ordinal))
                {
                    offenders.Add($"{TemplatePath}: missing section marker `{marker}`");
                }
            }
        }

        await Assert.That(offenders).IsEmpty().Because(
            $"FR-061: {TemplatePath} must ship with all 14 section markers.");
    }

    [Test]
    public async Task QualityBar_ExistsWithRubricSections()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            return;
        }

        var path = Path.Combine(repoRoot, QualityBarPath);
        var offenders = new List<string>();

        if (!File.Exists(path))
        {
            offenders.Add($"{QualityBarPath}: missing");
        }
        else
        {
            var text = File.ReadAllText(path);
            foreach (var anchor in new[] { "Pass", "Needs work", "Missing", "Per-section rubric" })
            {
                if (!text.Contains(anchor, StringComparison.Ordinal))
                {
                    offenders.Add($"{QualityBarPath}: missing rubric anchor `{anchor}`");
                }
            }
        }

        await Assert.That(offenders).IsEmpty().Because(
            $"FR-062: {QualityBarPath} must ship with Pass / Needs work / Missing rubric.");
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
