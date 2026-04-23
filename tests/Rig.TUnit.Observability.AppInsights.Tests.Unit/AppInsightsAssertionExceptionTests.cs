using Rig.TUnit.Observability.AppInsights.Assertions;

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
