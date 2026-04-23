using Microsoft.ApplicationInsights.DataContracts;
using Rig.TUnit.Observability.AppInsights.Assertions;
using Rig.TUnit.Observability.AppInsights.Fixtures;

namespace Rig.TUnit.Observability.AppInsights.Tests.Unit;

public sealed class AppInsightsEventAssertTests
{
    [Test]
    public async Task Event_NullChannel_ThrowsArgumentNullException()
    {
        await Assert.That(() => AppInsightsAssert.Event(null!, "MyEvent"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Event_WhitespaceName_ThrowsArgumentException()
    {
        using var channel = new CapturingTelemetryChannel();

        await Assert.That(() => AppInsightsAssert.Event(channel, "  "))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public Task Event_Exactly_MatchingCount_DoesNotThrow()
    {
        using var channel = new CapturingTelemetryChannel();
        channel.Send(new EventTelemetry("OrderPlaced"));

        AppInsightsAssert.Event(channel, "OrderPlaced").Exactly(1);
        return Task.CompletedTask;
    }

    [Test]
    public async Task Event_Exactly_Mismatch_ThrowsAppInsightsAssertionException()
    {
        using var channel = new CapturingTelemetryChannel();

        await Assert.That(() => AppInsightsAssert.Event(channel, "OrderPlaced").Exactly(1))
            .ThrowsExactly<AppInsightsAssertionException>();
    }
}
