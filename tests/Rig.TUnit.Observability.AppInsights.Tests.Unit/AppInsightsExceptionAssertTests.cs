using Microsoft.ApplicationInsights.DataContracts;
using Rig.TUnit.Observability.AppInsights.Assertions;
using Rig.TUnit.Observability.AppInsights.Fixtures;

namespace Rig.TUnit.Observability.AppInsights.Tests.Unit;

public sealed class AppInsightsExceptionAssertTests
{
    [Test]
    public async Task Exception_WhenChannelIsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => AppInsightsAssert.Exception<InvalidOperationException>(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public Task Exception_Exactly_WhenCountMatches_DoesNotThrow()
    {
        using var channel = new CapturingTelemetryChannel();
        channel.Send(new ExceptionTelemetry(new InvalidOperationException("boom")));

        AppInsightsAssert.Exception<InvalidOperationException>(channel).Exactly(1);
        return Task.CompletedTask;
    }

    [Test]
    public async Task Exception_Exactly_WhenMismatch_ThrowsAppInsightsAssertionException()
    {
        using var channel = new CapturingTelemetryChannel();

        await Assert.That(() => AppInsightsAssert.Exception<InvalidOperationException>(channel).Exactly(1))
            .ThrowsExactly<AppInsightsAssertionException>();
    }
}
