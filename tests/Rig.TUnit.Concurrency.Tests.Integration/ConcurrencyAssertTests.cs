using System.Net;
using Rig.TUnit.Concurrency.Assertions;

namespace Rig.TUnit.Concurrency.Tests.Integration;

public sealed class ConcurrencyAssertTests
{
    /// <summary>Simulated optimistic-concurrency entity with an in-memory row-version check.</summary>
    private sealed class OcEntity
    {
        private int _rowVersion;
        public int RowVersion => _rowVersion;
        public int Value { get; private set; }

        public Task TryUpdateAsync(int expected, int newValue)
        {
            // Mimic DbUpdateConcurrencyException — use CAS on row version.
            var original = Interlocked.CompareExchange(ref _rowVersion, expected + 1, expected);
            if (original != expected)
            {
                throw new FakeConcurrencyException($"Expected {expected} but was {original}.");
            }
            Value = newValue;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeConcurrencyException(string msg) : Exception(msg);

    [Test]
    public async Task TwoWriters_ExactlyOneWins()
    {
        var entity = new OcEntity();
        await ConcurrencyAssert.TwoWriters(entity).OneWinsWith<FakeConcurrencyException>(
            async e => await e.TryUpdateAsync(0, 1),
            async e => await e.TryUpdateAsync(0, 2));
        await Assert.That(entity.RowVersion).IsEqualTo(1);
    }

    [Test]
    public async Task TwoWriters_BothSucceed_Throws()
    {
        var counter = 0;
        Func<int, Task> writer = async _ => { Interlocked.Increment(ref counter); await Task.Yield(); };

        var threw = false;
        try
        {
            await ConcurrencyAssert.TwoWriters(0).OneWinsWith<FakeConcurrencyException>(writer, writer);
        }
        catch (ConcurrencyAssertionException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task Precondition_IfMatchFails_ExpectsHttp412()
    {
        var resp = new HttpResponseMessage(HttpStatusCode.PreconditionFailed) { Content = new StringContent("") };
        await Precondition.IfMatchFails(resp);
    }

    [Test]
    public async Task Precondition_IfMatchFails_WrongStatus_Throws()
    {
        var resp = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") };
        var threw = false;
        try { await Precondition.IfMatchFails(resp); }
        catch (ConcurrencyAssertionException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task Precondition_NotModified_ExpectsHttp304()
    {
        var resp = new HttpResponseMessage(HttpStatusCode.NotModified);
        await Precondition.NotModified(resp);
    }

    [Test]
    public async Task Sequence_Idempotency_FirstApplyAccepted()
    {
        var checker = new SequenceIdempotencyChecker();
        await Assert.That(checker.TryApply("agg-1", 1)).IsTrue();
        await Assert.That(checker.TryApply("agg-1", 2)).IsTrue();
    }

    [Test]
    public async Task Sequence_Idempotency_DuplicateRejected()
    {
        var checker = new SequenceIdempotencyChecker();
        checker.TryApply("agg-1", 1);
        checker.TryApply("agg-1", 2);
        await Assert.That(checker.TryApply("agg-1", 2)).IsFalse();
        await Assert.That(checker.TryApply("agg-1", 1)).IsFalse();
    }

    [Test]
    public async Task Sequence_Idempotency_PerAggregate()
    {
        var checker = new SequenceIdempotencyChecker();
        checker.TryApply("agg-1", 5);
        await Assert.That(checker.TryApply("agg-2", 1)).IsTrue();
        await Assert.That(checker.LastApplied("agg-1")).IsEqualTo(5L);
        await Assert.That(checker.LastApplied("agg-2")).IsEqualTo(1L);
    }
}
