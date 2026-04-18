namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-003: every <c>src/Rig.TUnit.{Family}.{Provider}/</c> leaf-provider directory MUST
/// ship a <c>README.md</c> longer than <see cref="MinimumReadmeChars"/> characters. Base
/// packages (<c>Rig.TUnit.Databases.Sql</c>, <c>Rig.TUnit.Messaging</c>, etc.) are out of
/// scope per the spec — the rule targets leaf consumer-facing packages.
///
/// Verified 2026-04-18: 32 leaf providers — 12 already ship a README, 20 are listed in
/// <see cref="SkipUntilFixed"/>. Phase 3/4 writes a README for each skipped provider
/// alongside its other canonical files (per the template in
/// <c>src/Rig.TUnit/Contributing-ProviderTemplate.md</c>); Phase 6 T156 flips the final
/// switch by removing the skip list entirely.
/// </summary>
public sealed class ReadmeCompletenessTests
{
    private const int MinimumReadmeChars = 100;

    /// <summary>Folder-prefix patterns identifying leaf-provider packages under <c>src/</c>.</summary>
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

    /// <summary>Non-family-prefixed leaves (standalone packages that ship as providers).</summary>
    private static readonly string[] StandaloneLeafs =
    [
        "Rig.TUnit.Docker",
    ];

    /// <summary>
    /// Leaf packages deliberately skipped until their phase writes the README. Each entry
    /// names the closing task. Verified 2026-04-18: 20 entries — the exact set documented
    /// in <c>spec.md:19</c> + <c>analysis.md</c> finding #8.
    /// </summary>
    private static readonly (string FolderName, string ClosingTask)[] SkipUntilFixed =
    [
        ("Rig.TUnit.Caching.Fusion",              "T064"),
        ("Rig.TUnit.Caching.Hybrid",              "T060"),
        ("Rig.TUnit.Databases.NoSql.Cassandra",   "T029"),
        ("Rig.TUnit.Databases.NoSql.Dynamo",      "T033"),
        ("Rig.TUnit.Databases.NoSql.ElasticSearch", "T037"),
        ("Rig.TUnit.Databases.NoSql.KurrentDb",   "T041 (renamed from EventStore in T002c)"),
        ("Rig.TUnit.Databases.NoSql.Mongo",       "T024"),
        ("Rig.TUnit.Databases.Sql.Postgresql",    "T176"),
        ("Rig.TUnit.Docker",                      "T137"),
        ("Rig.TUnit.Messaging.Kafka",             "T044"),
        ("Rig.TUnit.Messaging.Nats",              "T051"),
        ("Rig.TUnit.Messaging.RabbitMq",          "T047"),
        ("Rig.TUnit.Messaging.Sqs",               "T055"),
        ("Rig.TUnit.Observability.Metrics",       "T095"),
        ("Rig.TUnit.Security.Mtls",               "T086"),
        ("Rig.TUnit.Security.Policies",           "T090"),
        ("Rig.TUnit.Storage.AzureBlob",           "T067"),
        ("Rig.TUnit.Storage.FileSystem",          "T078"),
        ("Rig.TUnit.Storage.MinIO",               "T074"),
        ("Rig.TUnit.Storage.S3",                  "T070"),
    ];

    [Test]
    public async Task EveryLeafProvider_ShipsReadme()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            return;
        }

        var srcRoot = Path.Combine(repoRoot, "src");
        if (!Directory.Exists(srcRoot))
        {
            return;
        }

        var skipSet = SkipUntilFixed
            .Select(s => s.FolderName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var offenders = new List<string>();

        foreach (var dir in Directory.EnumerateDirectories(srcRoot))
        {
            var folderName = Path.GetFileName(dir);
            if (!IsLeafProvider(folderName))
            {
                continue;
            }

            if (skipSet.Contains(folderName))
            {
                continue;
            }

            var readmePath = Path.Combine(dir, "README.md");
            if (!File.Exists(readmePath))
            {
                offenders.Add($"{folderName}: README.md missing");
                continue;
            }

            var length = new FileInfo(readmePath).Length;
            if (length <= MinimumReadmeChars)
            {
                offenders.Add($"{folderName}: README.md too short ({length} bytes, need > {MinimumReadmeChars})");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                $"Every leaf-provider package MUST ship a README.md > {MinimumReadmeChars} chars. "
                + "Use src/Rig.TUnit/Contributing-ProviderTemplate.md §8 for the canonical shape.");
    }

    [Test]
    public async Task SkipList_OnlyReferencesFoldersThatActuallyExist()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            return;
        }

        var stale = SkipUntilFixed
            .Where(e => !Directory.Exists(Path.Combine(repoRoot, "src", e.FolderName)))
            .Select(e => e.FolderName)
            .ToArray();

        await Assert.That(stale)
            .IsEmpty()
            .Because("SkipUntilFixed entries must reference real src/ folders — remove stale paths");
    }

    private static bool IsLeafProvider(string folderName)
    {
        foreach (var prefix in LeafPrefixes)
        {
            if (folderName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }
        foreach (var standalone in StandaloneLeafs)
        {
            if (string.Equals(folderName, standalone, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
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
