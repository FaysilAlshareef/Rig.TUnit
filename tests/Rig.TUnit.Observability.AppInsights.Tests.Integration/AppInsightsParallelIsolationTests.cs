using Rig.TUnit.Observability.AppInsights.Fixtures;

namespace Rig.TUnit.Observability.AppInsights.Tests.Integration;

/// <summary>
/// 20 parallel in-process AppInsightsFixture instances each emit 10 events to
/// their own channel — asserts zero cross-channel contamination. Runs without
/// Docker — purely in-memory.
/// </summary>
public sealed class AppInsightsParallelIsolationTests
{
    [Test]
    public async Task Parallel_Fixtures_DoNotShareChannels()
    {
        const int fixtures = 20;
        const int events = 10;

        var tasks = Enumerable.Range(0, fixtures).Select(async i =>
        {
            await using var fx = new AppInsightsFixture();
            await fx.InitializeAsync();
            for (var n = 0; n < events; n++)
            {
                fx.Client.TrackEvent($"evt-{i}-{n}");
            }
            return fx.Channel.Captured.Count;
        });

        var counts = await Task.WhenAll(tasks);
        foreach (var count in counts)
        {
            await Assert.That(count).IsEqualTo(events);
        }
    }
}
