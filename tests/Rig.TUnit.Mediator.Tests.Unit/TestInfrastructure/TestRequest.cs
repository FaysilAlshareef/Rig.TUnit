using Mediator;

namespace Rig.TUnit.Mediator.Tests.Unit.TestInfrastructure;

public sealed record TestRequest(string Value) : IRequest<string>;
