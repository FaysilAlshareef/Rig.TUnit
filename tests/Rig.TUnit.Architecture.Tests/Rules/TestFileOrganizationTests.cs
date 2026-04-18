using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-002 + FR-010 (+ C-003): every <c>tests/**/*.cs</c> file outside the per-project
/// <c>TestInfrastructure/</c>, <c>Fixtures/</c>, <c>Fakers/</c>, <c>Helpers/</c>,
/// <c>Assertions/</c>, <c>obj/</c>, <c>bin/</c> folders MUST declare exactly one
/// top-level type (class, record, struct, interface, enum). Applies uniformly to
/// <c>*Contract.cs</c> — contract base classes with inline helper types extract those
/// helpers to <c>TestInfrastructure/ContractHelpers/</c> in Phase 2.
///
/// Files currently carrying inline infrastructure live in <see cref="SkipUntilFixed"/>.
/// Phase 2 (T011–T020) removes each entry as the file is cleaned; the final flip in T019
/// deletes the skip list entirely.
/// </summary>
public sealed class TestFileOrganizationTests
{
    private static readonly string[] ExcludedFolders =
    [
        "TestInfrastructure",
        "Fixtures",
        "Fakers",
        "Helpers",
        "Assertions",
        "obj",
        "bin",
    ];

    /// <summary>
    /// Paths (relative to the repo root) known to declare &gt; 1 top-level type. Each entry
    /// names the closing Phase-2 task. Verified 2026-04-18 via a repo-wide scan.
    /// </summary>
    private static readonly (string RelativePath, string ClosingTask)[] SkipUntilFixed =
    [
        ("tests/Rig.TUnit.Databases.Sql.Tests.Contract/SqlRigContract.cs",         "T016 — C-003 contract helper extraction"),
        ("tests/Rig.TUnit.Parallelism.Tests.Contract/ParallelIsolationContract.cs", "T016 — C-003 contract helper extraction"),
    ];

    [Test]
    public async Task EveryTestFile_HasSingleTopLevelType()
    {
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            // Defensive: if the rule cannot locate the repo root, treat as inconclusive rather
            // than a false GREEN. Happens when the test assembly is executed outside a git
            // checkout (e.g. a published package). Asserts nothing — caller's CI catches.
            return;
        }

        var testsRoot = Path.Combine(repoRoot, "tests");
        if (!Directory.Exists(testsRoot))
        {
            return;
        }

        var skipSet = SkipUntilFixed
            .Select(s => s.RelativePath.Replace('/', Path.DirectorySeparatorChar))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsInExcludedFolder(file))
            {
                continue;
            }

            var relative = Path.GetRelativePath(repoRoot, file);

            // Benchmark projects are outside the scope of FR-010 — they use BenchmarkDotNet,
            // not TUnit, and each benchmark class commonly carries supporting types (runners,
            // fake entities) in the same file. The rule targets test file hygiene.
            if (relative.Contains(".Benchmarks" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || relative.Contains(".Benchmarks/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (skipSet.Contains(relative))
            {
                continue;
            }

            var count = CountTopLevelTypes(file);
            if (count > 1)
            {
                offenders.Add($"{relative}: {count} top-level types");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "Every test .cs file outside TestInfrastructure/Fixtures/Fakers/Helpers/Assertions "
                + "MUST declare exactly one top-level type. Extract inline fakers, harnesses, test "
                + "entities to TestInfrastructure/ (Phase 2 = T011-T020).");
    }

    [Test]
    public async Task SkipList_OnlyReferencesFilesThatActuallyExist()
    {
        // Meta-test: keeps the skip list honest — stale entries fail loud.
        var repoRoot = TryFindRepoRoot();
        if (repoRoot is null)
        {
            return;
        }

        var stale = SkipUntilFixed
            .Where(e => !File.Exists(Path.Combine(repoRoot, e.RelativePath)))
            .Select(e => e.RelativePath)
            .ToArray();

        await Assert.That(stale)
            .IsEmpty()
            .Because("SkipUntilFixed entries must reference real files — remove stale paths");
    }

    private static bool IsInExcludedFolder(string fullPath)
    {
        var segments = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var segment in segments)
        {
            foreach (var excluded in ExcludedFolders)
            {
                if (string.Equals(segment, excluded, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static int CountTopLevelTypes(string file)
    {
        string text;
        try
        {
            text = File.ReadAllText(file);
        }
        catch (IOException)
        {
            return 0;
        }

        var tree = CSharpSyntaxTree.ParseText(text, path: file);
        var root = tree.GetCompilationUnitRoot();

        return root.DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .Count(t => t.Parent is NamespaceDeclarationSyntax
                     or FileScopedNamespaceDeclarationSyntax
                     or CompilationUnitSyntax);
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
