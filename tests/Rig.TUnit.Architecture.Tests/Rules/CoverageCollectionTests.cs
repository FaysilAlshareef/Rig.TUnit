using YamlDotNet.RepresentationModel;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-020 / SC-006: every <c>integration-*</c> matrix job in <c>.github/workflows/ci.yml</c>
/// MUST collect code coverage via the MTP-native <c>-- --coverage --coverage-output-format
/// cobertura --coverage-output coverage.cobertura.xml</c> suffix on its <c>dotnet test</c>
/// (or <c>dotnet run</c>) step. The cobertura files are then globbed by the upload-artifact
/// step added in T007 and merged by the <c>coverage-summary</c> job added in T013.
///
/// RED today — no integration matrix job uses the flag. T011 GREEN adds it everywhere.
/// </summary>
public sealed class CoverageCollectionTests
{
    private const string WorkflowRelativePath = ".github/workflows/ci.yml";
    private const string IntegrationJobPrefix = "integration-";

    // The canonical flag set — checking with StartsWith keeps the rule tolerant of minor
    // ordering changes while still enforcing every required argument.
    private static readonly string[] RequiredArguments =
    [
        "--coverage",
        "--coverage-output-format",
        "cobertura",
        "--coverage-output",
        "coverage.cobertura.xml",
    ];

    [Test]
    public async Task EveryIntegrationJob_CollectsCobertura()
    {
        var workflow = LoadWorkflow();
        if (workflow is null)
        {
            return;
        }

        var offenders = new List<string>();
        foreach (var (jobName, jobNode) in EnumerateJobs(workflow))
        {
            if (!jobName.StartsWith(IntegrationJobPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var steps = GetSteps(jobNode);
            var testStep = FindDotnetTestStep(steps);
            if (testStep is null)
            {
                offenders.Add($"{jobName}: no `dotnet test` step found");
                continue;
            }

            var runCommand = GetScalar(testStep, "run") ?? string.Empty;
            foreach (var required in RequiredArguments)
            {
                if (!runCommand.Contains(required, StringComparison.Ordinal))
                {
                    offenders.Add($"{jobName}: `dotnet test` step is missing `{required}`");
                }
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "Every integration-* job MUST collect cobertura via the MTP-native "
                + "`-- --coverage --coverage-output-format cobertura --coverage-output "
                + "coverage.cobertura.xml` suffix on its dotnet test step (FR-020).");
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

    private static IEnumerable<(string Name, YamlMappingNode Node)> EnumerateJobs(YamlMappingNode workflow)
    {
        if (!workflow.Children.TryGetValue(new YamlScalarNode("jobs"), out var node)
            || node is not YamlMappingNode jobs)
        {
            yield break;
        }
        foreach (var child in jobs.Children)
        {
            if (child.Key is YamlScalarNode { Value: { Length: > 0 } name }
                && child.Value is YamlMappingNode jobNode)
            {
                yield return (name, jobNode);
            }
        }
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

    private static YamlMappingNode? FindDotnetTestStep(IReadOnlyList<YamlMappingNode> steps)
    {
        foreach (var step in steps)
        {
            var run = GetScalar(step, "run");
            if (run is null)
            {
                continue;
            }
            // A step is the integration test step if its run block invokes `dotnet test` (MTP)
            // or `dotnet run` (benchmarks) on an Integration project. The build step also uses
            // `dotnet build`; skip it.
            if (run.Contains("dotnet test", StringComparison.Ordinal)
                || run.Contains("dotnet run", StringComparison.Ordinal))
            {
                if (!run.Contains(".Tests.Integration", StringComparison.Ordinal)
                    && !run.Contains("Integration", StringComparison.Ordinal))
                {
                    continue;
                }
                return step;
            }
        }
        return null;
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
