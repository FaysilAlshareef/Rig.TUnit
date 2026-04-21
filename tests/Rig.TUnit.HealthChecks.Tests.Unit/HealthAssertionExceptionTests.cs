using Rig.TUnit.HealthChecks.Assertions;

namespace Rig.TUnit.HealthChecks.Tests.Unit;

public sealed class HealthAssertionExceptionTests
{
    [Test]
    public async Task HealthAssertionException_Message_IsPreserved()
    {
        var ex = new HealthAssertionException("check failed");

        await Assert.That(ex.Message).IsEqualTo("check failed");
    }

    [Test]
    public async Task HealthAssertionException_IsExceptionSubtype()
    {
        var ex = new HealthAssertionException("x");

        await Assert.That(ex).IsAssignableTo<Exception>();
    }
}
