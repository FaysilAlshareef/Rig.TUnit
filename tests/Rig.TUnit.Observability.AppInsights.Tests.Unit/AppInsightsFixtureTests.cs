using Microsoft.Extensions.Options;
using Rig.TUnit.Observability.AppInsights.Assertions;
using Rig.TUnit.Observability.AppInsights.Fixtures;
using Rig.TUnit.Observability.AppInsights.Options;

namespace Rig.TUnit.Observability.AppInsights.Tests.Unit;

public sealed class AppInsightsFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_UsesDefaultOptions()
    {
        await using var fx = new AppInsightsFixture();
        await fx.InitializeAsync();
        var name = fx.ServiceName;
        await Assert.That(name).IsEqualTo("rigtunit-tests");
    }

    [Test]
    public async Task Ctor_NullOptions_Throws()
    {
        await Assert.That(() => new AppInsightsFixture((AppInsightsFixtureOptions)null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_NullIOptions_Throws()
    {
        await Assert.That(() => new AppInsightsFixture((IOptions<AppInsightsFixtureOptions>)null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Client_BeforeInitialize_Throws()
    {
        await using var fx = new AppInsightsFixture();
        await Assert.That(() => fx.Client).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task InitializeAsync_TrackEvent_ReachesChannel()
    {
        await using var fx = new AppInsightsFixture();
        await fx.InitializeAsync();
        fx.Client.TrackEvent("test.evt");
        var assertion = AppInsightsAssert.Event(fx.Channel, "test.evt");
        await Assert.That(assertion.Count).IsEqualTo(1);
    }

    [Test]
    public async Task InitializeAsync_TrackException_ReachesChannel()
    {
        await using var fx = new AppInsightsFixture();
        await fx.InitializeAsync();
        fx.Client.TrackException(new InvalidOperationException("boom"));
        var assertion = AppInsightsAssert.Exception<InvalidOperationException>(fx.Channel);
        await Assert.That(assertion.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DisposeAsync_ReleasesClient_SubsequentAccessThrows()
    {
        var fx = new AppInsightsFixture();
        await fx.InitializeAsync();
        await fx.DisposeAsync();
        await Assert.That(() => fx.Client).ThrowsExactly<InvalidOperationException>();
    }
}
