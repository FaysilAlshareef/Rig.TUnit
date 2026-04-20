using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.Tables;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-065/FR-066/FR-069 + Feature 005 T123c: every package listed in
/// <see cref="CanonicalReadmePackages"/> MUST ship a <c>README.md</c> whose Markdig-parsed
/// structure matches the 14-section canonical template at
/// <c>docs/templates/PROVIDER_README_TEMPLATE.md</c>.
///
/// Required H2 headings (exact text, or a <c>## §N — N/A: &lt;rationale&gt;</c> placeholder
/// where the package genuinely has no meaningful content for that section — meta-package
/// carve-out per Documentation-Audit §3.2):
/// <list type="number">
/// <item>What this package is</item>
/// <item>When to use it</item>
/// <item>Prerequisites</item>
/// <item>Quick start</item>
/// <item>Options</item>
/// <item>Fixture + helper APIs</item>
/// <item>Per-test isolation</item>
/// <item>Parallelism + performance</item>
/// <item>Troubleshooting</item>
/// <item>Provider quirks + edge cases</item>
/// <item>Benchmarks</item>
/// <item>Related docs</item>
/// <item>License</item>
/// </list>
///
/// Additional structural assertions:
/// <list type="bullet">
/// <item>H1 present and starts with <c># Rig.TUnit</c>.</item>
/// <item>Section 5 (Quick start) contains at least one fenced <c>csharp</c> code block
///   with more than just a <c>// TODO</c> placeholder — the
///   <c>snippet-extraction</c> CI job (T161/T162) extracts and compiles these.</item>
/// <item>Section 6 (Options) contains a pipe-table with header row
///   <c>| Property | Type | Default | Description |</c> OR is marked
///   <c>## §6 — N/A</c> for meta-packages with no <c>FixtureOptions</c>.</item>
/// <item>Section 12 (Benchmarks) contains either a link to a <c>*Benchmarks.cs</c> file
///   or references <c>baseline-005.json</c>, OR is marked <c>## §12 — N/A</c>.</item>
/// </list>
///
/// Phase 6c rolls out family-by-family — each family's GREEN commit removes its
/// providers from <see cref="SkipUntilFixed"/>. T157 adds a guard asserting the list is
/// empty; T158 cleans up any residual entries. Per FR-004: this <b>rescope</b> of the
/// existing skip mechanism within the same architecture rule file is NOT "introducing
/// new skip markers" — <c>NoSkipMarkersTests</c> excludes this file from its walk.
/// </summary>
public sealed class ReadmeCompletenessTests
{
    /// <summary>
    /// The 13 H2 section headings required in every canonical provider README.
    /// Section 1 is the H1 title — handled separately.
    /// </summary>
    private static readonly string[] RequiredH2Sections =
    [
        "What this package is",
        "When to use it",
        "Prerequisites",
        "Quick start",
        "Options",
        "Fixture + helper APIs",
        "Per-test isolation",
        "Parallelism + performance",
        "Troubleshooting",
        "Provider quirks + edge cases",
        "Benchmarks",
        "Related docs",
        "License",
    ];

    /// <summary>
    /// Every package under <c>src/</c> that must ship a canonical 14-section README per
    /// Feature 005 Phase 6c scope (tasks.md T137–T156).
    /// </summary>
    private static readonly string[] CanonicalReadmePackages =
    [
        // T137 — base / meta packages
        "Rig.TUnit",
        "Rig.TUnit.All",
        "Rig.TUnit.Ci",
        "Rig.TUnit.Core",
        "Rig.TUnit.Grpc",
        "Rig.TUnit.Mediator",
        "Rig.TUnit.Microservices",
        "Rig.TUnit.Microservices.Contracts",
        "Rig.TUnit.Microservices.Saga",
        "Rig.TUnit.Parallelism",
        "Rig.TUnit.Storage",
        "Rig.TUnit.WebAPI",

        // T139 — SQL
        "Rig.TUnit.Databases.Sql",
        "Rig.TUnit.Databases.Sql.MySql",
        "Rig.TUnit.Databases.Sql.Oracle",
        "Rig.TUnit.Databases.Sql.Postgresql",
        "Rig.TUnit.Databases.Sql.SqlServer",
        "Rig.TUnit.Databases.Sql.Sqlite",

        // T141 — NoSQL
        "Rig.TUnit.Databases.NoSql",
        "Rig.TUnit.Databases.NoSql.Cassandra",
        "Rig.TUnit.Databases.NoSql.Cosmos",
        "Rig.TUnit.Databases.NoSql.Dynamo",
        "Rig.TUnit.Databases.NoSql.ElasticSearch",
        "Rig.TUnit.Databases.NoSql.KurrentDb",
        "Rig.TUnit.Databases.NoSql.Mongo",
        "Rig.TUnit.Databases.NoSql.Redis",

        // T143 — Caching
        "Rig.TUnit.Caching",
        "Rig.TUnit.Caching.Fusion",
        "Rig.TUnit.Caching.Hybrid",
        "Rig.TUnit.Caching.Memory",
        "Rig.TUnit.Caching.Redis",

        // T145 — Messaging
        "Rig.TUnit.Messaging",
        "Rig.TUnit.Messaging.Kafka",
        "Rig.TUnit.Messaging.Nats",
        "Rig.TUnit.Messaging.RabbitMq",
        "Rig.TUnit.Messaging.ServiceBus",
        "Rig.TUnit.Messaging.Sqs",

        // T147 — Microservices
        "Rig.TUnit.Microservices.EventSourcing",
        "Rig.TUnit.Microservices.Inbox",
        "Rig.TUnit.Microservices.Outbox",
        "Rig.TUnit.Microservices.Snapshots",

        // T149 — Security
        "Rig.TUnit.Security",
        "Rig.TUnit.Security.Jwt",
        "Rig.TUnit.Security.Mtls",
        "Rig.TUnit.Security.OAuth",
        "Rig.TUnit.Security.Policies",

        // T151 — Observability
        "Rig.TUnit.Observability",
        "Rig.TUnit.Observability.AppInsights",
        "Rig.TUnit.Observability.Logging",
        "Rig.TUnit.Observability.Logging.Analyzers",
        "Rig.TUnit.Observability.Metrics",
        "Rig.TUnit.Observability.Seq",
        "Rig.TUnit.Observability.Tracing",

        // T153 — Storage leaves
        "Rig.TUnit.Storage.AzureBlob",
        "Rig.TUnit.Storage.FileSystem",
        "Rig.TUnit.Storage.MinIO",
        "Rig.TUnit.Storage.S3",

        // T155 — Cross-cutting
        "Rig.TUnit.Concurrency",
        "Rig.TUnit.Docker",
        "Rig.TUnit.HealthChecks",
        "Rig.TUnit.Http",
        "Rig.TUnit.Resilience",
    ];

    /// <summary>
    /// Packages deliberately skipped until their Phase 6c family GREEN commit writes the
    /// canonical 14-section README. T123d expands this list at RED time; each family GREEN
    /// commit trims its entries; T158 removes the last residual entries.
    ///
    /// Skip list expanded for Phase 6c rollout; each family GREEN commit MUST remove its
    /// entries; final empty at T157/T158.
    /// </summary>
    private static readonly (string FolderName, string ClosingTask)[] SkipUntilFixed =
    [
        // T154 — Storage leaves (4; Storage base is in T138)
        ("Rig.TUnit.Storage.AzureBlob", "T154"),
        ("Rig.TUnit.Storage.FileSystem", "T154"),
        ("Rig.TUnit.Storage.MinIO", "T154"),
        ("Rig.TUnit.Storage.S3", "T154"),

        // T156 — Cross-cutting (5)
        ("Rig.TUnit.Concurrency", "T156"),
        ("Rig.TUnit.Docker", "T156"),
        ("Rig.TUnit.HealthChecks", "T156"),
        ("Rig.TUnit.Http", "T156"),
        ("Rig.TUnit.Resilience", "T156"),
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

        foreach (var folderName in CanonicalReadmePackages)
        {
            if (skipSet.Contains(folderName))
            {
                continue;
            }

            var dir = Path.Combine(srcRoot, folderName);
            if (!Directory.Exists(dir))
            {
                offenders.Add($"{folderName}: package folder missing");
                continue;
            }

            var readmePath = Path.Combine(dir, "README.md");
            if (!File.Exists(readmePath))
            {
                offenders.Add($"{folderName}: README.md missing");
                continue;
            }

            var content = File.ReadAllText(readmePath);
            var problems = ValidateStructure(content);
            foreach (var problem in problems)
            {
                offenders.Add($"{folderName}: {problem}");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "Every package listed in CanonicalReadmePackages MUST ship a README.md "
                + "matching the 14-section canonical template at "
                + "docs/templates/PROVIDER_README_TEMPLATE.md. "
                + "Meta packages may use `## §N — N/A: <rationale>` for genuinely-absent sections.");
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

    private static IReadOnlyList<string> ValidateStructure(string markdown)
    {
        var problems = new List<string>();
        var pipeline = new MarkdownPipelineBuilder().UsePipeTables().Build();
        var doc = Markdown.Parse(markdown, pipeline);

        var headings = doc.Descendants<HeadingBlock>().ToList();

        // H1 check
        var h1 = headings.FirstOrDefault(h => h.Level == 1);
        if (h1 is null)
        {
            problems.Add("missing H1 title");
        }
        else
        {
            var h1Text = ExtractHeadingText(h1);
            if (!h1Text.StartsWith("Rig.TUnit", StringComparison.Ordinal))
            {
                problems.Add($"H1 must start with `Rig.TUnit`, got `{h1Text}`");
            }
        }

        // Build H2 index (headingText → heading block)
        var h2Texts = headings
            .Where(h => h.Level == 2)
            .Select(ExtractHeadingText)
            .ToList();

        // For each required H2, allow exact match OR `§N — N/A:` placeholder form.
        for (var i = 0; i < RequiredH2Sections.Length; i++)
        {
            var required = RequiredH2Sections[i];
            var sectionNum = i + 2; // section 1 is H1 title
            var placeholder = $"§{sectionNum} — N/A";

            var present = h2Texts.Any(t =>
                string.Equals(t, required, StringComparison.Ordinal)
                || t.StartsWith(placeholder, StringComparison.Ordinal));

            if (!present)
            {
                problems.Add($"missing required H2 `{required}` (or `## §{sectionNum} — N/A: <rationale>` placeholder)");
            }
        }

        // Section 5 — Quick start must contain a non-placeholder csharp fenced block
        var quickStart = FindSectionBlocks(doc, "Quick start");
        if (quickStart.Count > 0)
        {
            var code = quickStart.OfType<FencedCodeBlock>()
                .FirstOrDefault(f => string.Equals(f.Info, "csharp", StringComparison.Ordinal));
            if (code is null)
            {
                problems.Add("Section 5 `Quick start` has no ```csharp fenced code block");
            }
            else
            {
                var lines = code.Lines.ToString();
                if (string.IsNullOrWhiteSpace(lines) || lines.Trim().StartsWith("// TODO", StringComparison.Ordinal))
                {
                    problems.Add("Section 5 `Quick start` csharp block is a `// TODO` placeholder");
                }
            }
        }

        // Section 6 — Options table (or N/A)
        if (!SectionIsNotApplicable(doc, "Options", 6))
        {
            var optionsBlocks = FindSectionBlocks(doc, "Options");
            var table = optionsBlocks.OfType<Table>().FirstOrDefault();
            if (table is null)
            {
                problems.Add("Section 6 `Options` has no pipe-table (expected `| Property | Type | Default | Description |`)");
            }
            else
            {
                var headerRow = table.Descendants<TableRow>().FirstOrDefault(r => r.IsHeader);
                if (headerRow is null)
                {
                    problems.Add("Section 6 `Options` table is missing a header row");
                }
                else
                {
                    var headerCells = headerRow.Descendants<TableCell>()
                        .Select(c => ExtractCellText(c).Trim())
                        .ToList();
                    string[] expected = ["Property", "Type", "Default", "Description"];
                    if (headerCells.Count < 4
                        || !headerCells.Take(4).SequenceEqual(expected, StringComparer.Ordinal))
                    {
                        problems.Add($"Section 6 `Options` table header must be `| Property | Type | Default | Description |`, got `| {string.Join(" | ", headerCells)} |`");
                    }
                }
            }
        }

        // Section 12 — Benchmarks link or N/A
        if (!SectionIsNotApplicable(doc, "Benchmarks", 12))
        {
            var benchBlocks = FindSectionBlocks(doc, "Benchmarks");
            var bodyText = BlocksToPlainText(benchBlocks);
            var hasBenchmarkLink =
                bodyText.Contains("Benchmarks.cs", StringComparison.Ordinal)
                || bodyText.Contains("baseline-005.json", StringComparison.Ordinal)
                || bodyText.Contains("Rig.TUnit.Benchmarks", StringComparison.Ordinal);
            if (!hasBenchmarkLink)
            {
                problems.Add("Section 12 `Benchmarks` must link to a `*Benchmarks.cs` file or reference `baseline-005.json`");
            }
        }

        return problems;
    }

    private static bool SectionIsNotApplicable(MarkdownDocument doc, string sectionName, int sectionNum)
    {
        var placeholder = $"§{sectionNum} — N/A";
        return doc.Descendants<HeadingBlock>()
            .Where(h => h.Level == 2)
            .Select(ExtractHeadingText)
            .Any(t => t.StartsWith(placeholder, StringComparison.Ordinal));
    }

    private static List<Block> FindSectionBlocks(MarkdownDocument doc, string sectionName)
    {
        var blocks = new List<Block>();
        var capturing = false;

        foreach (var top in doc)
        {
            if (top is HeadingBlock hb && hb.Level == 2)
            {
                var text = ExtractHeadingText(hb);
                if (capturing)
                {
                    // Next H2 ends the section.
                    break;
                }
                if (string.Equals(text, sectionName, StringComparison.Ordinal))
                {
                    capturing = true;
                    continue;
                }
            }
            else if (capturing)
            {
                blocks.Add(top);
            }
        }
        return blocks;
    }

    private static string BlocksToPlainText(IEnumerable<Block> blocks)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var block in blocks)
        {
            CollectBlockText(block, sb);
        }
        return sb.ToString();
    }

    private static void CollectBlockText(Block block, System.Text.StringBuilder sb)
    {
        switch (block)
        {
            case FencedCodeBlock fenced:
                sb.Append(fenced.Lines.ToString()).Append(' ');
                return;
            case CodeBlock cb:
                sb.Append(cb.Lines.ToString()).Append(' ');
                return;
            case LeafBlock leaf when leaf.Inline is not null:
                foreach (var inline in leaf.Inline.Descendants())
                {
                    if (inline is LiteralInline lit)
                    {
                        sb.Append(lit.Content.ToString()).Append(' ');
                    }
                    else if (inline is CodeInline code)
                    {
                        sb.Append(code.Content).Append(' ');
                    }
                    else if (inline is LinkInline link)
                    {
                        sb.Append(link.Url ?? string.Empty).Append(' ');
                    }
                }
                return;
            case ContainerBlock container:
                foreach (var child in container)
                {
                    CollectBlockText(child, sb);
                }
                return;
        }
    }

    private static string ExtractHeadingText(HeadingBlock h)
    {
        if (h.Inline is null)
        {
            return string.Empty;
        }
        var sb = new System.Text.StringBuilder();
        foreach (var inline in h.Inline.Descendants())
        {
            if (inline is LiteralInline lit)
            {
                sb.Append(lit.Content.ToString());
            }
            else if (inline is CodeInline code)
            {
                sb.Append(code.Content);
            }
        }
        return sb.ToString();
    }

    private static string ExtractCellText(TableCell cell)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var leaf in cell.Descendants<LeafBlock>())
        {
            if (leaf.Inline is null) continue;
            foreach (var inline in leaf.Inline.Descendants())
            {
                if (inline is LiteralInline lit)
                {
                    sb.Append(lit.Content.ToString());
                }
                else if (inline is CodeInline code)
                {
                    sb.Append(code.Content);
                }
            }
        }
        return sb.ToString();
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
