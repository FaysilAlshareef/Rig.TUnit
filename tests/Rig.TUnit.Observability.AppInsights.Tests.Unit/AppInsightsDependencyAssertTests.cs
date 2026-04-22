using Microsoft.ApplicationInsights.DataContracts;
using Rig.TUnit.Observability.AppInsights.Assertions;
using Rig.TUnit.Observability.AppInsights.Fixtures;

namespace Rig.TUnit.Observability.AppInsights.Tests.Unit;

public sealed class AppInsightsDependencyAssertTests
{
    [Test]
    public async Task Dependency_WhenChannelIsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => AppInsightsAssert.Dependency(null!, "SQL"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Dependency_WhenTypeIsWhitespace_ThrowsArgumentException()
    {
        using var channel = new CapturingTelemetryChannel();

        await Assert.That(() => AppInsightsAssert.Dependency(channel, " "))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public Task Dependency_AtLeast_WhenCountMatches_DoesNotThrow()
    {
        using var channel = new CapturingTelemetryChannel();
        channel.Send(new DependencyTelemetry { Type = "SQL" });
        channel.Send(new DependencyTelemetry { Type = "SQL" });

        AppInsightsAssert.Dependency(channel, "SQL").AtLeast(1);
        return Task.CompletedTask;
    }

    [Test]
    public async Task Dependency_AtLeast_WhenTooFew_ThrowsAppInsightsAssertionException()
    {
        using var channel = new CapturingTelemetryChannel();

        await Assert.That(() => AppInsightsAssert.Dependency(channel, "SQL").AtLeast(1))
            .ThrowsExactly<AppInsightsAssertionException>();
    }
}
