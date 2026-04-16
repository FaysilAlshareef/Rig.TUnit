using Grpc.Core;
using Rig.TUnit.Grpc.Tests.Unit.Protos;

namespace Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

public sealed class TestGrpcService : TestService.TestServiceBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        => Task.FromResult(new HelloReply { Message = $"Hello {request.Name}" });
}
