using Mediator;

namespace Rig.TUnit.Mediator.Tests.Unit.TestInfrastructure;

public sealed record TestQuery(string Filter) : IQuery<string>;
