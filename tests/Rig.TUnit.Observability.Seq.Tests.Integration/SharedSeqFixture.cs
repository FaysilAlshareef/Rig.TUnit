using Rig.TUnit.Observability.Seq.Fixtures;
using Rig.TUnit.Observability.Seq.Options;

namespace Rig.TUnit.Observability.Seq.Tests.Integration;

/// <summary>
/// Intentional reuse per A005 audit: container is shared but tests derive per-test
/// names (database / collection / keyspace / key prefix / topic) via IsolationKey or
/// an equivalent primitive, so cross-test isolation is preserved without the cost of
/// a fresh container per test. See planning/post-005-phase-1/SharedFixture-Audit.md.
/// </summary>

/// <summary>
/// Process-wide shared <see cref="SeqFixture"/> — booting Seq takes 20-40s, so we
/// pay the cost once per test assembly. The Testcontainers Ryuk reaper disposes
/// the container when the host exits.
/// </summary>
internal static class SharedSeqFixture
{
    private static readonly Lazy<Task<SeqFixture>> Instance = new(async () =>
    {
        var fx = new SeqFixture(new SeqFixtureOptions
        {
            ImageTag = "latest",
            StartupTimeoutSeconds = 180,
            CaptureDashboardSnapshot = true,
        });
        await fx.InitializeAsync();
        return fx;
    });

    public static Task<SeqFixture> GetAsync() => Instance.Value;
}
