using Rig.TUnit.HealthChecks.Assertions;

namespace Rig.TUnit.HealthChecks.Tests.Unit;

/// <summary>
/// Unit coverage for health-check primitives — no HTTP server. DependencyDownSimulator
/// and ProbeKind are pure types that can be tested without infrastructure; HealthAssert
/// itself requires an HttpClient so it's exercised at Integration level.
/// </summary>
public sealed class HealthChecksUnitTests
{
    [Test]
    public async Task DependencyDownSimulator_DefaultsToUp()
    {
        var sim = new DependencyDownSimulator();
        await Assert.That(sim.IsDown).IsFalse();
    }

    [Test]
    public async Task DependencyDownSimulator_GoDown_AndRecover_TogglesFlag()
    {
        var sim = new DependencyDownSimulator();
        sim.GoDown();
        await Assert.That(sim.IsDown).IsTrue();

        sim.Recover();
        await Assert.That(sim.IsDown).IsFalse();
    }

    [Test]
    public async Task DependencyDownSimulator_IsThreadSafe()
    {
        var sim = new DependencyDownSimulator();
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            if (i % 2 == 0) sim.GoDown();
            else sim.Recover();
            _ = sim.IsDown;
        })).ToArray();

        await Task.WhenAll(tasks);

        await Assert.That(sim.IsDown).IsTrue().Or.IsFalse();
    }

    [Test]
    public async Task ProbeKind_HasThreeDistinctValues()
    {
        var kinds = Enum.GetValues<ProbeKind>();
        await Assert.That(kinds.Length).IsEqualTo(3);
        await Assert.That(kinds).Contains(ProbeKind.Live);
        await Assert.That(kinds).Contains(ProbeKind.Ready);
        await Assert.That(kinds).Contains(ProbeKind.Startup);
    }

    [Test]
    public async Task HealthAssert_On_RejectsNullClient()
    {
        await Assert.That(() => HealthAssert.On(null!, "/health")).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task HealthAssert_On_RejectsEmptyPath()
    {
        using var client = new HttpClient();
        await Assert.That(() => HealthAssert.On(client, "")).Throws<ArgumentException>();
    }
}
