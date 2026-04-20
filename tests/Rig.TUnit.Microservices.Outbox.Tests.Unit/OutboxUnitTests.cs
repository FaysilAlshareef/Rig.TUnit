namespace Rig.TUnit.Microservices.Outbox.Tests.Unit;

public sealed class OutboxUnitTests
{
    [Test]
    public async Task Placeholder_WithUnimplementedBaseline_ThrowsInvalidOperation()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T063 populates this test.");
    }
}
