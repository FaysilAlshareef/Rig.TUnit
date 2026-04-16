using Mediator;

namespace Rig.TUnit.Mediator.Tests.Unit.TestInfrastructure;

public sealed class TestNotificationHandler : INotificationHandler<TestNotification>
{
    public bool WasHandled { get; private set; }

    public string? ReceivedMessage { get; private set; }

    public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        WasHandled = true;
        ReceivedMessage = notification.Message;
        return ValueTask.CompletedTask;
    }
}
