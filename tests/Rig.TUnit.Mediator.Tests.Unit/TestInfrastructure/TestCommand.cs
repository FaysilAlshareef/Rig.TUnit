using Mediator;

namespace Rig.TUnit.Mediator.Tests.Unit.TestInfrastructure;

public sealed record TestCommand(int Value) : ICommand<int>;
