namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-012 / SC-014: pre-004 orphan folders (`src/Rig.TUnit.ServiceBus/`,
/// `tests/Rig.TUnit.ServiceBus.Tests.Integration/`, `tests/Rig.TUnit.SqlServer.Tests.Integration/`)
/// MUST be absent from the repo. They hold no source files — only stale `bin/` / `obj/` — and
/// break the `Provider ↔ TestProject` reflection-time symmetry enforced elsewhere.
///
/// The matching production package is `Rig.TUnit.Messaging.ServiceBus`; the matching test
/// project is `Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration`. Feature 005 Phase 1
/// (T002) deletes the orphans; this test ensures they never come back.
/// </summary>
public sealed class OrphanFolderTests
{
    [Test]
    public async Task SrcRigTUnitServiceBus_MustNotExist()
    {
        var path = ResolveRepoPath("src", "Rig.TUnit.ServiceBus");
        await Assert.That(Directory.Exists(path))
            .IsFalse()
            .Because(
                "`src/Rig.TUnit.ServiceBus/` is a pre-004 orphan (Messaging.ServiceBus is the "
                + "canonical package). Delete the folder — see Feature 005 T002.");
    }

    [Test]
    public async Task TestsRigTUnitServiceBusIntegration_MustNotExist()
    {
        var path = ResolveRepoPath("tests", "Rig.TUnit.ServiceBus.Tests.Integration");
        await Assert.That(Directory.Exists(path))
            .IsFalse()
            .Because(
                "`tests/Rig.TUnit.ServiceBus.Tests.Integration/` is a pre-004 orphan "
                + "(superseded by `Rig.TUnit.Messaging.ServiceBus.Tests.Integration`). "
                + "Delete the folder — see Feature 005 T002.");
    }

    [Test]
    public async Task TestsRigTUnitSqlServerIntegration_MustNotExist()
    {
        var path = ResolveRepoPath("tests", "Rig.TUnit.SqlServer.Tests.Integration");
        await Assert.That(Directory.Exists(path))
            .IsFalse()
            .Because(
                "`tests/Rig.TUnit.SqlServer.Tests.Integration/` is a pre-004 orphan "
                + "(superseded by `Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration`). "
                + "Delete the folder — see Feature 005 T002.");
    }

    private static string ResolveRepoPath(params string[] segments)
    {
        var root = TryFindRepoRoot();
        if (root is null)
        {
            // No repo root found (e.g. running from an installed nupkg). Point the test at a
            // path guaranteed not to exist so the assertion passes harmlessly.
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        }
        return Path.Combine(new[] { root }.Concat(segments).ToArray());
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
