using Microsoft.ApplicationInsights.DataContracts;
using Rig.TUnit.Observability.AppInsights.Fixtures;

namespace Rig.TUnit.Observability.AppInsights.Tests.Unit;

public sealed class CapturingTelemetryChannelTests
{
    [Test]
    public async Task Send_EnqueuesItem()
    {
        using var channel = new CapturingTelemetryChannel();
        channel.Send(new EventTelemetry("evt"));
        var count = channel.Captured.Count;
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task Send_NullItem_Noop()
    {
        using var channel = new CapturingTelemetryChannel();
        channel.Send(null!);
        var count = channel.Captured.Count;
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_EmptiesQueue()
    {
        using var channel = new CapturingTelemetryChannel();
        channel.Send(new EventTelemetry("evt"));
        channel.Send(new EventTelemetry("evt"));
        channel.Clear();
        var count = channel.Captured.Count;
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Send_ConcurrentFromMultipleThreads_AllCaptured()
    {
        using var channel = new CapturingTelemetryChannel();
        const int perThread = 100;
        const int threads = 8;

        await Task.WhenAll(Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < perThread; i++)
            {
                channel.Send(new EventTelemetry($"evt-{i}"));
            }
        })));

        var count = channel.Captured.Count;
        await Assert.That(count).IsEqualTo(threads * perThread);
    }

    [Test]
    public async Task DeveloperMode_DefaultsTrue()
    {
        using var channel = new CapturingTelemetryChannel();
        var devMode = channel.DeveloperMode;
        await Assert.That(devMode).IsTrue();
    }
}
