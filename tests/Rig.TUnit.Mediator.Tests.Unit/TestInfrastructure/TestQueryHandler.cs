using Mediator;

namespace Rig.TUnit.Mediator.Tests.Unit.TestInfrastructure;

public sealed class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public ValueTask<string> Handle(TestQuery query, CancellationToken cancellationToken)
        => ValueTask.FromResult($"Result: {query.Filter}");
}
