using Mediator;

namespace Rig.TUnit.Mediator.Tests.Unit.TestInfrastructure;

public sealed class TestCommandHandler : ICommandHandler<TestCommand, int>
{
    public ValueTask<int> Handle(TestCommand command, CancellationToken cancellationToken)
        => ValueTask.FromResult(command.Value * 2);
}
