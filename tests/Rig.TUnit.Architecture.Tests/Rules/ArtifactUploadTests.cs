using YamlDotNet.RepresentationModel;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-013 / SC-018: every job in <c>.github/workflows/ci.yml</c> MUST ship a step
/// using <c>actions/upload-artifact@v4</c> with <c>if: always()</c> and
/// <c>retention-days: 14</c>, so triage after a failed run can fetch TUnit HTML reports,
/// TRX files, and cobertura coverage XML without replaying the pipeline.
///
/// RED today — no job uploads artefacts. T007 GREEN adds the step to every job.
/// </summary>
public sealed class ArtifactUploadTests
{
    private const string WorkflowRelativePath = ".github/workflows/ci.yml";
    private const string RequiredActionPrefix = "actions/upload-artifact@v4";
    private const string RequiredIfExpression = "always()";
    private const int RequiredRetentionDays = 14;

    /// <summary>
    /// Jobs that are genuinely artefact-free (no `dotnet test`, no build output) and therefore
    /// exempt from the upload requirement. Keep the list empty when the workflow is well-formed;
    /// any future bookkeeping / meta-only job gets added here with a rationale comment.
    /// </summary>
    private static readonly HashSet<string> ExemptJobs = new(StringComparer.Ordinal)
    {
        // Phase 1 T008 meta-job — walks git log, no test output to upload.
        "commit-discipline-gate",
        // Phase 2 T013 summary job — uploads its own `coverage-report` artefact with a
        // longer 30-day retention per FR-021. CoverageSummaryJobTests enforces that shape
        // separately; this rule would otherwise reject the intentional retention-days: 30.
        "coverage-summary",
        // Phase 7 T164 — runs the arch tests; architecture tests don't produce
        // meaningful TRX/HTML artefacts beyond the existing build-unit-arch upload.
        "architecture-tests",
        // Phase 7 T166/T167 — benchmark-regression uploads its own `benchmark-results`
        // artefact (not test-results).
        "benchmark-regression",
        // Phase 6d T162 — snippet-extraction runs dotnet build, emits compiler output
        // to the action log; no artefact.
        "snippet-extraction",
    };

    [Test]
    public async Task EveryCiJob_UploadsTestArtifacts()
    {
        var workflow = LoadWorkflow();
        if (workflow is null)
        {
            // Running from a test binary that isn't inside the repo (e.g. packaged nupkg) —
            // nothing to assert.
            return;
        }

        var offenders = new List<string>();
        foreach (var (jobName, jobNode) in EnumerateJobs(workflow))
        {
            if (ExemptJobs.Contains(jobName))
            {
                continue;
            }

            var steps = GetSteps(jobNode);
            var uploadStep = FindUploadStep(steps);
            if (uploadStep is null)
            {
                offenders.Add($"{jobName}: no `actions/upload-artifact@v4` step found");
                continue;
            }

            var ifExpr = GetScalar(uploadStep, "if");
            if (!string.Equals(ifExpr, RequiredIfExpression, StringComparison.Ordinal)
                && !string.Equals(ifExpr, "${{ always() }}", StringComparison.Ordinal))
            {
                offenders.Add($"{jobName}: upload-artifact step has `if: {ifExpr ?? "<missing>"}` (expected `always()`)");
            }

            var with = GetMapping(uploadStep, "with");
            if (with is null)
            {
                offenders.Add($"{jobName}: upload-artifact step is missing its `with:` block");
                continue;
            }

            var retention = GetScalar(with, "retention-days");
            if (!int.TryParse(retention, out var days) || days != RequiredRetentionDays)
            {
                offenders.Add($"{jobName}: upload-artifact `retention-days: {retention ?? "<missing>"}` (expected {RequiredRetentionDays})");
            }

            if (GetScalar(with, "name") is not { Length: > 0 })
            {
                offenders.Add($"{jobName}: upload-artifact `with.name` is missing or blank");
            }

            if (GetScalar(with, "path") is not { Length: > 0 })
            {
                offenders.Add($"{jobName}: upload-artifact `with.path` is missing or blank");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                $"Every CI job MUST upload its TestResults artefacts (FR-013) via `{RequiredActionPrefix}` "
                + $"with `if: {RequiredIfExpression}` and `retention-days: {RequiredRetentionDays}`. "
                + "See planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md.");
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
        if (stream.Documents.Count == 0)
        {
            return null;
        }

        return stream.Documents[0].RootNode as YamlMappingNode;
    }

    private static IEnumerable<(string Name, YamlMappingNode Node)> EnumerateJobs(YamlMappingNode workflow)
    {
        var jobs = GetMapping(workflow, "jobs");
        if (jobs is null)
        {
            yield break;
        }

        foreach (var child in jobs.Children)
        {
            if (child.Key is YamlScalarNode key && key.Value is { Length: > 0 } name
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

    private static YamlMappingNode? FindUploadStep(IReadOnlyList<YamlMappingNode> steps)
    {
        foreach (var step in steps)
        {
            var uses = GetScalar(step, "uses");
            if (uses is not null && uses.StartsWith(RequiredActionPrefix, StringComparison.Ordinal))
            {
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
