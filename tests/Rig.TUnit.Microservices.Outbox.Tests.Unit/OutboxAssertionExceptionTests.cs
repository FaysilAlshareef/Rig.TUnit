using Rig.TUnit.Microservices.Outbox.Assertions;

namespace Rig.TUnit.Microservices.Outbox.Tests.Unit;

public sealed class OutboxAssertionExceptionTests
{
    [Test]
    public async Task OutboxAssertionException_Message_IsPreserved()
    {
        var ex = new OutboxAssertionException("entry missing");

        await Assert.That(ex.Message).IsEqualTo("entry missing");
    }

    [Test]
    public async Task OutboxAssertionException_IsExceptionSubtype()
    {
        var ex = new OutboxAssertionException("x");

        await Assert.That(ex).IsAssignableTo<Exception>();
    }
}
