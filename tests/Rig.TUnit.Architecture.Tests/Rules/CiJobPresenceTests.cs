using YamlDotNet.RepresentationModel;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// Phase 7 FR-070/FR-071/FR-072: <c>.github/workflows/ci.yml</c> MUST declare
/// <c>architecture-tests</c>, <c>benchmark-regression</c>, <c>commit-discipline-gate</c>,
/// <c>red-commit-verification</c>, <c>markdown-link-check</c>, <c>snippet-extraction</c>,
/// and <c>coverage-summary</c> jobs.
/// </summary>
public sealed class CiJobPresenceTests
{
    private const string WorkflowRelativePath = ".github/workflows/ci.yml";

    private static readonly string[] RequiredJobs =
    [
        "architecture-tests",
        "benchmark-regression",
        "commit-discipline-gate",
        "red-commit-verification",
        "markdown-link-check",
        "snippet-extraction",
        "coverage-summary",
    ];

    [Test]
    public async Task CiWorkflow_DeclaresEveryPhase7Job()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            return;
        }

        var path = Path.Combine(repoRoot, WorkflowRelativePath);
        var offendersEarly = new List<string>();
        if (!File.Exists(path))
        {
            offendersEarly.Add($"{WorkflowRelativePath}: missing");
            await Assert.That(offendersEarly).IsEmpty().Because("ci.yml must exist");
            return;
        }

        using var reader = new StreamReader(path);
        var stream = new YamlStream();
        stream.Load(reader);
        var root = stream.Documents[0].RootNode as YamlMappingNode;

        var offenders = new List<string>();
        var jobs = root is null ? null : GetMapping(root, "jobs");
        if (jobs is null)
        {
            offenders.Add("ci.yml: `jobs:` key missing");
        }
        else
        {
            var presentJobs = jobs.Children.Keys
                .OfType<YamlScalarNode>()
                .Where(k => k.Value is { Length: > 0 })
                .Select(k => k.Value!)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var required in RequiredJobs)
            {
                if (!presentJobs.Contains(required))
                {
                    offenders.Add($"ci.yml: required job `{required}` is missing");
                }
            }
        }

        await Assert.That(offenders).IsEmpty().Because(
            "FR-070 / FR-071 / FR-072 / SC-016 / SC-017 / SC-019: every Phase 7 CI job must "
            + "be declared in .github/workflows/ci.yml.");
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
