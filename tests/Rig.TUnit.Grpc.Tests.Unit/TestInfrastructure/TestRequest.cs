using MediatR;

namespace Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

public sealed record TestRequest(string Value) : IRequest<string>;
