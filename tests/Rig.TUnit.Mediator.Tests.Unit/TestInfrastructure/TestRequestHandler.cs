using Mediator;

namespace Rig.TUnit.Mediator.Tests.Unit.TestInfrastructure;

public sealed class TestRequestHandler : IRequestHandler<TestRequest, string>
{
    public ValueTask<string> Handle(TestRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult($"Handled: {request.Value}");
}
