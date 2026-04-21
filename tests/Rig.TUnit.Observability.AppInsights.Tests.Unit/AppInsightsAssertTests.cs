using Microsoft.ApplicationInsights.DataContracts;
using Rig.TUnit.Observability.AppInsights.Assertions;
using Rig.TUnit.Observability.AppInsights.Fixtures;

namespace Rig.TUnit.Observability.AppInsights.Tests.Unit;

public sealed class AppInsightsAssertionExceptionTests
{
    [Test]
    public async Task AppInsightsAssertionException_Message_IsPreserved()
    {
        var ex = new AppInsightsAssertionException("no events captured");

        await Assert.That(ex.Message).IsEqualTo("no events captured");
    }

    [Test]
    public async Task AppInsightsAssertionException_IsExceptionSubtype()
    {
        var ex = new AppInsightsAssertionException("x");

        await Assert.That(ex).IsAssignableTo<Exception>();
    }
}

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

public sealed class AppInsightsDependencyAssertTests
{
    [Test]
    public async Task Dependency_NullChannel_ThrowsArgumentNullException()
    {
        await Assert.That(() => AppInsightsAssert.Dependency(null!, "SQL"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Dependency_WhitespaceType_ThrowsArgumentException()
    {
        using var channel = new CapturingTelemetryChannel();

        await Assert.That(() => AppInsightsAssert.Dependency(channel, " "))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public Task Dependency_AtLeast_MatchingCount_DoesNotThrow()
    {
        using var channel = new CapturingTelemetryChannel();
        channel.Send(new DependencyTelemetry { Type = "SQL" });
        channel.Send(new DependencyTelemetry { Type = "SQL" });

        AppInsightsAssert.Dependency(channel, "SQL").AtLeast(1);
        return Task.CompletedTask;
    }

    [Test]
    public async Task Dependency_AtLeast_TooFew_ThrowsAppInsightsAssertionException()
    {
        using var channel = new CapturingTelemetryChannel();

        await Assert.That(() => AppInsightsAssert.Dependency(channel, "SQL").AtLeast(1))
            .ThrowsExactly<AppInsightsAssertionException>();
    }
}

public sealed class AppInsightsExceptionAssertTests
{
    [Test]
    public async Task Exception_NullChannel_ThrowsArgumentNullException()
    {
        await Assert.That(() => AppInsightsAssert.Exception<InvalidOperationException>(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public Task Exception_Exactly_MatchingCount_DoesNotThrow()
    {
        using var channel = new CapturingTelemetryChannel();
        channel.Send(new ExceptionTelemetry(new InvalidOperationException("boom")));

        AppInsightsAssert.Exception<InvalidOperationException>(channel).Exactly(1);
        return Task.CompletedTask;
    }

    [Test]
    public async Task Exception_Exactly_Mismatch_ThrowsAppInsightsAssertionException()
    {
        using var channel = new CapturingTelemetryChannel();

        await Assert.That(() => AppInsightsAssert.Exception<InvalidOperationException>(channel).Exactly(1))
            .ThrowsExactly<AppInsightsAssertionException>();
    }
}
