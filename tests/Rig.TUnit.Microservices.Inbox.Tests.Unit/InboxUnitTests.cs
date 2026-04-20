namespace Rig.TUnit.Microservices.Inbox.Tests.Unit;

public sealed class InboxUnitTests
{
    [Test]
    public async Task Placeholder_WithUnimplementedBaseline_ThrowsInvalidOperation()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T061 populates this test.");
    }
}
