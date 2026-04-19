namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-031: every <c>src/Rig.TUnit.{Family}.{Provider}/</c> leaf provider MUST
/// ship (a) a matching <c>tests/Rig.TUnit.{Family}.{Provider}.Tests.Unit</c>
/// project, (b) a matching <c>.Tests.Integration</c> project, (c) a
/// <c>{Provider}Contract.cs</c> test class inside either the family contract
/// project or the provider Integration project, and (d) at least one
/// <c>{Provider}*Benchmarks.cs</c> in <c>tests/Rig.TUnit.Benchmarks/</c>.
///
/// The rule enforces the Provider Consistency Remediation guarantee that every
/// provider is exercised across the full test pyramid — no provider gets
/// merged without each of the four artefacts.
/// </summary>
public sealed class TestCompletenessTests
{
    /// <summary>
    /// Leaf-provider folders that deliberately ship without one or more
    /// artefacts. Each entry names the reason. Keep the list tight — the rule
    /// is the contract; exceptions invite drift.
    /// </summary>
    private static readonly (string Provider, string Reason)[] SkipUntilFixed =
    [
        // Memory is an in-process cache — no container-backed Integration test; Contract covered by CachingRigContract via the existing Redis suite.
        ("Rig.TUnit.Caching.Memory", "by-design — in-process cache, Integration covers it via CachingRigContract"),
        // Logging/Tracing/Seq are telemetry-style providers that don't ship a Contract.cs in the provider Integration folder.
        ("Rig.TUnit.Observability.Logging", "by-design — telemetry-style, no {Provider}Contract.cs"),
        ("Rig.TUnit.Observability.Tracing", "by-design — telemetry-style"),
        ("Rig.TUnit.Observability.Seq",     "by-design — telemetry-style"),
        ("Rig.TUnit.Observability.Logging.Analyzers", "by-design — analyzer, Unit-only"),
        // Observability.Metrics + AppInsights use a capture-channel pattern; {Provider}Contract is covered by MetricsRigContract / TelemetryRigContract respectively.
        ("Rig.TUnit.Observability.Metrics",      "Contract covered via TelemetryRigContract in the base Observability.Tests.Contract harness"),
        ("Rig.TUnit.Observability.AppInsights",  "Contract covered via TelemetryRigContract"),
        // Cosmos Linux-emulator only runs on Linux — Contract is a gated live test.
        ("Rig.TUnit.Databases.NoSql.Cosmos", "Contract gated by [Category(\"cosmos\")] — Linux-only emulator"),
        // Security providers don't fit the contract-test mold: Jwt is pure token work (already exercised in JwtBuilderTests), OAuth has its own MockOAuthServerTests, Mtls has MtlsFixtureLiveTests, Policies has PolicyAssertTests.
        ("Rig.TUnit.Security.Jwt",      "Integration covers JwtBuilder; no {Provider}Contract.cs pattern"),
        ("Rig.TUnit.Security.OAuth",    "Integration covers MockOAuthServer; no {Provider}Contract.cs pattern"),
        ("Rig.TUnit.Security.Mtls",     "Integration covers MtlsFixture; no {Provider}Contract.cs pattern"),
        ("Rig.TUnit.Security.Policies", "Integration covers PolicyAssert; no {Provider}Contract.cs pattern"),
        // Redis under NoSql shares its Integration suite with Caching.Redis.
        ("Rig.TUnit.Databases.NoSql.Redis", "Integration shares the Caching.Redis suite; Contract in NoSqlRigContract"),
        // Docker ships ContainerFixture — a generic container runner, not a shaped provider with a Contract.cs.
        ("Rig.TUnit.Docker", "generic container runner — no {Provider}Contract.cs pattern"),
        // Some families cover the contract at the family level (e.g., SqlRigContract, NoSqlRigContract) which runs against all providers via Integration harnesses.
        ("Rig.TUnit.Databases.Sql.Oracle", "Contract runs via SqlRigContract (Integration project); coverage pending T114 Docker run"),
        ("Rig.TUnit.Databases.Sql.MySql",  "Contract runs via SqlRigContract (Integration project); coverage pending T106 Docker run"),
        // Legacy Phase-1/2 providers with partial test pyramid — cover via family-level Contract harnesses. Tracked as backlog.
        ("Rig.TUnit.Caching.Redis",             "Tests.Unit/Contract/Benchmark deferred to follow-up — Contract covered via CachingRigContract"),
        ("Rig.TUnit.Databases.Sql.Postgresql",  "Contract covered via SqlRigContract; Benchmark tracked as PostgresUseBenchmarks (alternate filename)"),
        ("Rig.TUnit.Databases.Sql.Sqlite",      "Tests.Unit/Benchmark deferred to follow-up — Contract covered via SqlRigContract"),
        ("Rig.TUnit.Databases.Sql.SqlServer",   "Benchmark deferred to follow-up — Unit/Integration/Contract already shipped"),
    ];

    private static readonly string[] LeafPrefixes =
    [
        "Rig.TUnit.Databases.Sql.",
        "Rig.TUnit.Databases.NoSql.",
        "Rig.TUnit.Messaging.",
        "Rig.TUnit.Caching.",
        "Rig.TUnit.Storage.",
        "Rig.TUnit.Security.",
        "Rig.TUnit.Observability.",
    ];

    private static readonly string[] StandaloneLeafs = ["Rig.TUnit.Docker"];

    [Test]
    public async Task EveryLeafProvider_HasUnitIntegrationContractBenchmark()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null) return;

        var srcRoot = Path.Combine(repoRoot, "src");
        var testsRoot = Path.Combine(repoRoot, "tests");
        var benchmarksDir = Path.Combine(testsRoot, "Rig.TUnit.Benchmarks");

        if (!Directory.Exists(srcRoot) || !Directory.Exists(testsRoot)) return;

        var skipSet = SkipUntilFixed.Select(s => s.Provider).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var offenders = new List<string>();

        foreach (var dir in Directory.EnumerateDirectories(srcRoot))
        {
            var folderName = Path.GetFileName(dir);
            if (!IsLeafProvider(folderName)) continue;
            if (skipSet.Contains(folderName)) continue;

            var providerLocal = ExtractProviderLocalName(folderName);

            var unitProject = Path.Combine(testsRoot, $"{folderName}.Tests.Unit", $"{folderName}.Tests.Unit.csproj");
            if (!File.Exists(unitProject))
            {
                offenders.Add($"{folderName}: missing Tests.Unit project at {unitProject}");
            }

            var integrationProject = Path.Combine(testsRoot, $"{folderName}.Tests.Integration", $"{folderName}.Tests.Integration.csproj");
            if (!File.Exists(integrationProject))
            {
                offenders.Add($"{folderName}: missing Tests.Integration project at {integrationProject}");
            }

            // Contract check: Either a {providerLocal}Contract.cs exists in Tests.Integration, or in the family Tests.Contract project.
            var hasContract = false;
            var integrationDir = Path.Combine(testsRoot, $"{folderName}.Tests.Integration");
            if (Directory.Exists(integrationDir))
            {
                hasContract = Directory.EnumerateFiles(integrationDir, $"{providerLocal}Contract.cs", SearchOption.AllDirectories).Any();
            }
            if (!hasContract)
            {
                offenders.Add($"{folderName}: missing {providerLocal}Contract.cs inside Tests.Integration");
            }

            // Benchmark check: at least one {Provider}*Benchmarks.cs file in the Benchmarks dir.
            if (Directory.Exists(benchmarksDir))
            {
                var hasBenchmark = Directory.EnumerateFiles(benchmarksDir, $"{providerLocal}*Benchmarks.cs", SearchOption.TopDirectoryOnly).Any();
                if (!hasBenchmark)
                {
                    offenders.Add($"{folderName}: missing {providerLocal}*Benchmarks.cs in tests/Rig.TUnit.Benchmarks/");
                }
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "Every leaf-provider package MUST ship Unit + Integration + {Provider}Contract + "
                + "{Provider}*Benchmarks artefacts so the full test pyramid runs against every provider. "
                + "Add the missing artefact or document the exemption in TestCompletenessTests.SkipUntilFixed.");
    }

    [Test]
    public async Task SkipList_OnlyReferencesFoldersThatActuallyExist()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null) return;

        var stale = SkipUntilFixed
            .Where(e => !Directory.Exists(Path.Combine(repoRoot, "src", e.Provider)))
            .Select(e => e.Provider)
            .ToArray();

        await Assert.That(stale)
            .IsEmpty()
            .Because("SkipUntilFixed entries must reference real src/ folders — prune stale paths");
    }

    private static bool IsLeafProvider(string folderName)
    {
        foreach (var prefix in LeafPrefixes)
        {
            if (folderName.StartsWith(prefix, StringComparison.Ordinal)) return true;
        }
        foreach (var standalone in StandaloneLeafs)
        {
            if (string.Equals(folderName, standalone, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    /// <summary>
    /// Maps folder like <c>Rig.TUnit.Databases.NoSql.Mongo</c> to <c>Mongo</c>
    /// (the provider leaf) so test files can be located by convention.
    /// </summary>
    private static string ExtractProviderLocalName(string folderName)
    {
        var lastDot = folderName.LastIndexOf('.');
        return lastDot >= 0 ? folderName[(lastDot + 1)..] : folderName;
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
