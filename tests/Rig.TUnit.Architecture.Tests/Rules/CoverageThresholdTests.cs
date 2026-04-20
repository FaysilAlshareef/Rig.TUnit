using YamlDotNet.RepresentationModel;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-022 / SC-006: the <c>coverage-summary</c> job MUST contain a threshold step that
/// fails when any package node in the merged cobertura XML reports
/// <c>line-rate &lt; 0.90</c> OR <c>branch-rate &lt; 0.85</c>.
///
/// The step starts with <c>continue-on-error: true</c> in Phase 2 (warnings only;
/// baseline captured in T016). T069b flips it to blocking at Phase 3 close once every
/// provider reaches the bar. This test asserts the step is PRESENT and shape-correct —
/// it does NOT pin <c>continue-on-error</c>, so the Phase 3 flip doesn't require a
/// matching rule-file edit.
///
/// RED today — no threshold step exists.
/// </summary>
public sealed class CoverageThresholdTests
{
    private const string WorkflowRelativePath = ".github/workflows/ci.yml";
    private const string JobName = "coverage-summary";
    private const double RequiredLineRate = 0.90;
    private const double RequiredBranchRate = 0.85;

    [Test]
    public async Task CoverageSummary_ContainsThresholdStep()
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
            offenders.Add($"`{JobName}` job missing — cannot enforce threshold (FR-021 + FR-022)");
            await Assert.That(offenders).IsEmpty().Because("FR-022 requires a threshold step inside coverage-summary.");
            return;
        }

        var steps = GetSteps(summaryJob);
        var thresholdStep = FindThresholdStep(steps);
        if (thresholdStep is null)
        {
            offenders.Add(
                $"`{JobName}` has no threshold step. Required: parses merged cobertura and fails "
                + $"on line-rate < {RequiredLineRate:F2} or branch-rate < {RequiredBranchRate:F2}.");
        }
        else
        {
            var run = GetScalar(thresholdStep, "run") ?? string.Empty;
            if (!run.Contains(RequiredLineRate.ToString("F2"), StringComparison.Ordinal)
                && !run.Contains("line-rate", StringComparison.Ordinal))
            {
                offenders.Add($"threshold step must reference line-rate ≥ {RequiredLineRate:F2}");
            }
            if (!run.Contains(RequiredBranchRate.ToString("F2"), StringComparison.Ordinal)
                && !run.Contains("branch-rate", StringComparison.Ordinal))
            {
                offenders.Add($"threshold step must reference branch-rate ≥ {RequiredBranchRate:F2}");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "FR-022: the coverage-summary job MUST contain a threshold step that enforces "
                + $"per-package `line-rate ≥ {RequiredLineRate:F2}` and `branch-rate ≥ {RequiredBranchRate:F2}`. "
                + "Phase 2 ships it with continue-on-error: true; T069b flips it blocking.");
    }

    private static YamlMappingNode? FindThresholdStep(IReadOnlyList<YamlMappingNode> steps)
    {
        foreach (var step in steps)
        {
            var name = GetScalar(step, "name") ?? string.Empty;
            var run = GetScalar(step, "run") ?? string.Empty;
            var lowered = (name + " " + run).ToLowerInvariant();
            if (lowered.Contains("threshold")
                || (lowered.Contains("line-rate") && lowered.Contains("branch-rate")))
            {
                return step;
            }
        }
        return null;
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
