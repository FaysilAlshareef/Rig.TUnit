using YamlDotNet.RepresentationModel;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// Phase 7 FR-070/FR-071/FR-072: the .github/workflows/ tree MUST declare every
/// required job. <c>benchmark-regression</c> lives in <c>benchmark.yml</c>
/// (post-merge-only, see feat/006 wrap-up); all other Phase 7 jobs live in
/// <c>ci.yml</c>.
/// </summary>
public sealed class CiJobPresenceTests
{
    private static readonly (string Workflow, string Job)[] RequiredJobs =
    [
        (".github/workflows/ci.yml",        "architecture-tests"),
        (".github/workflows/benchmark.yml", "benchmark-regression"),
        (".github/workflows/ci.yml",        "commit-discipline-gate"),
        (".github/workflows/ci.yml",        "red-commit-verification"),
        (".github/workflows/ci.yml",        "markdown-link-check"),
        (".github/workflows/ci.yml",        "snippet-extraction"),
        (".github/workflows/ci.yml",        "coverage-summary"),
    ];

    [Test]
    public async Task CiWorkflow_DeclaresEveryPhase7Job()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            return;
        }

        var offenders = new List<string>();
        var jobsByWorkflow = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var workflow in RequiredJobs.Select(r => r.Workflow).Distinct())
        {
            var path = Path.Combine(repoRoot, workflow);
            if (!File.Exists(path))
            {
                offenders.Add($"{workflow}: missing");
                continue;
            }

            using var reader = new StreamReader(path);
            var stream = new YamlStream();
            stream.Load(reader);
            var root = stream.Documents[0].RootNode as YamlMappingNode;
            var jobs = root is null ? null : GetMapping(root, "jobs");
            if (jobs is null)
            {
                offenders.Add($"{workflow}: `jobs:` key missing");
                jobsByWorkflow[workflow] = new HashSet<string>(StringComparer.Ordinal);
                continue;
            }

            jobsByWorkflow[workflow] = jobs.Children.Keys
                .OfType<YamlScalarNode>()
                .Where(k => k.Value is { Length: > 0 })
                .Select(k => k.Value!)
                .ToHashSet(StringComparer.Ordinal);
        }

        foreach (var (workflow, job) in RequiredJobs)
        {
            if (jobsByWorkflow.TryGetValue(workflow, out var present) && !present.Contains(job))
            {
                offenders.Add($"{workflow}: required job `{job}` is missing");
            }
        }

        await Assert.That(offenders).IsEmpty().Because(
            "FR-070 / FR-071 / FR-072 / SC-016 / SC-017 / SC-019: every Phase 7 CI job must "
            + "be declared in its designated .github/workflows/ file.");
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
