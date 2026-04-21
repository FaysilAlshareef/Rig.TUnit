using Rig.TUnit.Messaging.Contracts;
using Rig.TUnit.Messaging.Fixtures;

namespace Rig.TUnit.Messaging.Tests.Contract;

/// <summary>
/// Concrete contract suite using an in-memory stub — exercises the abstract base
/// class code paths without requiring a real messaging broker.
/// </summary>
[InheritsTests]
public sealed class InMemoryMessagingRigContractTests : MessagingRigContract
{
    protected override ValueTask<IMessagingRig> CreateMessagingRigAsync(CancellationToken ct)
        => ValueTask.FromResult<IMessagingRig>(new InMemoryMessagingRig());

    /// <summary>
    /// Minimal in-memory implementation of <see cref="MessagingFixtureBase"/> that
    /// uses a random topic suffix to guarantee uniqueness across concurrent calls.
    /// </summary>
    private sealed class InMemoryMessagingRig : MessagingFixtureBase
    {
        private readonly string _topic = $"in-memory-{Guid.NewGuid():N}";

        public override string ConnectionString => "in-memory://test";

        public override string TopicName => _topic;

        public override Task InitializeAsync() => Task.CompletedTask;

        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
