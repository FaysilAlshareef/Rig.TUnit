using YamlDotNet.RepresentationModel;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-021 / SC-006 / SC-018: <c>.github/workflows/ci.yml</c> MUST declare a
/// <c>coverage-summary</c> job that fan-ins every <c>integration-*</c> + <c>build-unit-arch</c>
/// artefact, merges cobertura into an HTML + Markdown summary via
/// <a href="https://github.com/danielpalme/ReportGenerator">ReportGenerator</a>, publishes
/// the summary to <c>$GITHUB_STEP_SUMMARY</c>, and uploads the rendered report as a
/// 30-day artefact named <c>coverage-report</c>.
///
/// RED today — no such job exists. T013 GREEN authors it.
/// </summary>
public sealed class CoverageSummaryJobTests
{
    private const string WorkflowRelativePath = ".github/workflows/ci.yml";
    private const string JobName = "coverage-summary";

    /// <summary>
    /// The `needs:` set: build-unit-arch + every integration-* job. Any new integration job
    /// added later MUST be added here so the coverage merge fires only after all cobertura
    /// artefacts land.
    /// </summary>
    private static readonly string[] RequiredNeeds =
    [
        "build-unit-arch",
        "integration-sql",
        "integration-nosql",
        "integration-caching",
        "integration-messaging",
        "integration-microservices",
        "integration-security",
        "integration-observability",
        "integration-storage",
        "integration-core",
    ];

    [Test]
    public async Task CoverageSummaryJob_IsDeclaredAndWiredCorrectly()
    {
        var workflow = LoadWorkflow();
        if (workflow is null)
        {
            return;
        }

        var offenders = new List<string>();
        var jobs = GetMapping(workflow, "jobs");
        var summaryJob = jobs is null ? null : GetMapping(jobs, JobName);
        if (summaryJob is null)
        {
            offenders.Add($"Job `{JobName}` is missing from .github/workflows/ci.yml");
            await Assert.That(offenders)
                .IsEmpty()
                .Because($"FR-021 requires a `{JobName}` job to orchestrate cobertura merge + report upload.");
            return;
        }

        // if: always() — coverage summary must publish even if a matrix job fails so triage
        // has the partial data.
        var ifExpr = GetScalar(summaryJob, "if");
        if (!string.Equals(ifExpr, "always()", StringComparison.Ordinal)
            && !string.Equals(ifExpr, "${{ always() }}", StringComparison.Ordinal))
        {
            offenders.Add($"`{JobName}.if` must be `always()` (found `{ifExpr ?? "<missing>"}`)");
        }

        // needs: [ ...every matrix job... ]
        var actualNeeds = GetNeeds(summaryJob);
        foreach (var required in RequiredNeeds)
        {
            if (!actualNeeds.Contains(required))
            {
                offenders.Add($"`{JobName}.needs` missing `{required}`");
            }
        }

        var steps = GetSteps(summaryJob);
        if (!AnyStepUses(steps, "actions/download-artifact"))
        {
            offenders.Add($"`{JobName}` must use `actions/download-artifact` with `pattern: test-results-*`");
        }
        else if (!AnyStepUsesDownloadPattern(steps, "test-results-*"))
        {
            offenders.Add($"`{JobName}` download-artifact step is missing `with.pattern: test-results-*`");
        }

        if (!AnyStepRunContains(steps, "reportgenerator", StringComparison.OrdinalIgnoreCase)
            && !AnyStepUses(steps, "danielpalme/ReportGenerator"))
        {
            offenders.Add($"`{JobName}` must run ReportGenerator to merge cobertura → Html + MarkdownSummaryGithub");
        }

        if (!AnyStepRunContains(steps, "Html", StringComparison.Ordinal)
            || !AnyStepRunContains(steps, "Cobertura", StringComparison.Ordinal)
            || !AnyStepRunContains(steps, "MarkdownSummaryGithub", StringComparison.Ordinal))
        {
            offenders.Add($"`{JobName}` ReportGenerator reporttypes must include `Html;Cobertura;MarkdownSummaryGithub`");
        }

        if (!AnyStepRunContains(steps, "GITHUB_STEP_SUMMARY", StringComparison.Ordinal))
        {
            offenders.Add($"`{JobName}` must publish the markdown summary to `$GITHUB_STEP_SUMMARY`");
        }

        var uploadStep = FindUploadStep(steps);
        if (uploadStep is null)
        {
            offenders.Add($"`{JobName}` must upload its rendered report via `actions/upload-artifact@v7`");
        }
        else
        {
            var with = GetMapping(uploadStep, "with");
            var name = with is null ? null : GetScalar(with, "name");
            var retention = with is null ? null : GetScalar(with, "retention-days");
            if (!string.Equals(name, "coverage-report", StringComparison.Ordinal))
            {
                offenders.Add($"`{JobName}` upload-artifact `with.name` must be `coverage-report` (found `{name ?? "<missing>"}`)");
            }
            if (!int.TryParse(retention, out var days) || days != 30)
            {
                offenders.Add($"`{JobName}` upload-artifact `with.retention-days` must be `30` (found `{retention ?? "<missing>"}`)");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "FR-021: the `coverage-summary` job orchestrates cobertura → HTML + Markdown "
                + "reports, publishes to $GITHUB_STEP_SUMMARY, and uploads a 30-day archive. "
                + "See planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md §New summary job.");
    }

    private static HashSet<string> GetNeeds(YamlMappingNode job)
    {
        if (!job.Children.TryGetValue(new YamlScalarNode("needs"), out var node))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
        var result = new HashSet<string>(StringComparer.Ordinal);
        switch (node)
        {
            case YamlScalarNode scalar when scalar.Value is { Length: > 0 }:
                result.Add(scalar.Value);
                break;
            case YamlSequenceNode seq:
                foreach (var child in seq.Children.OfType<YamlScalarNode>())
                {
                    if (child.Value is { Length: > 0 } v)
                    {
                        result.Add(v);
                    }
                }
                break;
        }
        return result;
    }

    private static IReadOnlyList<YamlMappingNode> GetSteps(YamlMappingNode job)
    {
        if (!job.Children.TryGetValue(new YamlScalarNode("steps"), out var node)
            || node is not YamlSequenceNode seq)
        {
            return Array.Empty<YamlMappingNode>();
        }
        return seq.Children.OfType<YamlMappingNode>().ToArray();
    }

    private static bool AnyStepUses(IReadOnlyList<YamlMappingNode> steps, string prefix)
    {
        foreach (var s in steps)
        {
            if (GetScalar(s, "uses") is { } uses && uses.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static bool AnyStepUsesDownloadPattern(IReadOnlyList<YamlMappingNode> steps, string pattern)
    {
        foreach (var s in steps)
        {
            if (GetScalar(s, "uses") is { } uses
                && uses.StartsWith("actions/download-artifact", StringComparison.Ordinal))
            {
                var with = GetMapping(s, "with");
                var actualPattern = with is null ? null : GetScalar(with, "pattern");
                if (string.Equals(actualPattern, pattern, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool AnyStepRunContains(
        IReadOnlyList<YamlMappingNode> steps, string needle, StringComparison comparison)
    {
        foreach (var s in steps)
        {
            if (GetScalar(s, "run") is { } run && run.Contains(needle, comparison))
            {
                return true;
            }
        }
        return false;
    }

    private static YamlMappingNode? FindUploadStep(IReadOnlyList<YamlMappingNode> steps)
    {
        foreach (var s in steps)
        {
            if (GetScalar(s, "uses") is { } uses
                && uses.StartsWith("actions/upload-artifact@v7", StringComparison.Ordinal))
            {
                return s;
            }
        }
        return null;
    }

    private static YamlMappingNode? LoadWorkflow()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            return null;
        }
        var path = Path.Combine(repoRoot, WorkflowRelativePath);
        if (!File.Exists(path))
        {
            return null;
        }
        using var reader = new StreamReader(path);
        var stream = new YamlStream();
        stream.Load(reader);
        return stream.Documents.Count == 0 ? null : stream.Documents[0].RootNode as YamlMappingNode;
    }

    private static string? GetScalar(YamlMappingNode mapping, string key)
    {
        if (mapping.Children.TryGetValue(new YamlScalarNode(key), out var value)
            && value is YamlScalarNode scalar)
        {
            return scalar.Value;
        }
        return null;
    }

    private static YamlMappingNode? GetMapping(YamlMappingNode mapping, string key)
    {
        if (mapping.Children.TryGetValue(new YamlScalarNode(key), out var value)
            && value is YamlMappingNode child)
        {
            return child;
        }
        return null;
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
