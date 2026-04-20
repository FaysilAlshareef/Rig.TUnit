using Rig.TUnit.Concurrency.Assertions;

namespace Rig.TUnit.Concurrency.Tests.Contract;

/// <summary>
/// Base contract every concurrency-driven rig inherits — asserts the helpers' invariants
/// against stub writers. Provider suites override via [InheritsTests] and plug their own
/// writer implementations (EF DbUpdateConcurrencyException, Mongo Write errors, etc.).
/// </summary>
public abstract class ConcurrencyRigContract
{
    [Test]
    public async Task TwoWriters_Produces_Asserter_ForGenericEntity()
    {
        var asserter = ConcurrencyAssert.TwoWriters(new { Id = 1 });

        await Assert.That(asserter).IsNotNull();
    }

    [Test]
    public async Task SequenceChecker_IsThreadSafe_ForParallelApplies()
    {
        var checker = new SequenceIdempotencyChecker();
        var tasks = Enumerable.Range(1, 100)
            .Select(i => Task.Run(() => checker.TryApply("agg", i)))
            .ToArray();
        await Task.WhenAll(tasks);

        var winningSequence = checker.LastApplied("agg");
        await Assert.That(winningSequence).IsNotNull();
        await Assert.That(winningSequence!.Value).IsLessThanOrEqualTo(100L);
    }
}
