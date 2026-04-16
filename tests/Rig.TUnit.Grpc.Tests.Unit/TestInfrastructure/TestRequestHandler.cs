using MediatR;

namespace Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

public sealed class TestRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        => Task.FromResult($"Handled: {request.Value}");
}
