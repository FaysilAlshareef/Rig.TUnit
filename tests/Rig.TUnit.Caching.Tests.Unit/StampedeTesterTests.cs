using Rig.TUnit.Caching.Helpers;

namespace Rig.TUnit.Caching.Tests.Unit;

public sealed class StampedeTesterTests
{
    [Test]
    public async Task Ctor_NullProducer_ThrowsArgumentNullException()
    {
        await Assert.That(() => new StampedeTester(null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Invocations_InitiallyZero()
    {
        var tester = new StampedeTester((_, _) => Task.FromResult("v"));

        await Assert.That(tester.Invocations).IsEqualTo(0);
    }

    [Test]
    public async Task RaceAsync_ZeroConcurrentCallers_ThrowsArgumentOutOfRangeException()
    {
        var tester = new StampedeTester((_, _) => Task.FromResult("v"));

        await Assert.That(async () => await tester.RaceAsync("key", 0)).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task RaceAsync_NegativeConcurrentCallers_ThrowsArgumentOutOfRangeException()
    {
        var tester = new StampedeTester((_, _) => Task.FromResult("v"));

        await Assert.That(async () => await tester.RaceAsync("key", -1)).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task RaceAsync_SingleCaller_ProducerInvokedOnce()
    {
        var tester = new StampedeTester((_, _) => Task.FromResult("result"));

        var results = await tester.RaceAsync("key", 1);

        await Assert.That(tester.Invocations).IsEqualTo(1);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0]).IsEqualTo("result");
    }

    [Test]
    public async Task RaceAsync_MultipleCallers_AllReceiveResults()
    {
        var tester = new StampedeTester((key, _) => Task.FromResult($"value-for-{key}"));

        var results = await tester.RaceAsync("k", 5);

        await Assert.That(results.Count).IsEqualTo(5);
        await Assert.That(results).Contains("value-for-k");
    }

    [Test]
    public async Task RaceAsync_MultipleCallers_InvocationsCountMatchesConcurrentCallers()
    {
        var tester = new StampedeTester((_, _) => Task.FromResult("v"));

        await tester.RaceAsync("key", 4);

        await Assert.That(tester.Invocations).IsEqualTo(4);
    }

    [Test]
    public async Task RaceAsync_CalledTwice_InvocationsAreAdditive()
    {
        var tester = new StampedeTester((_, _) => Task.FromResult("v"));

        await tester.RaceAsync("key", 3);
        await tester.RaceAsync("key", 2);

        await Assert.That(tester.Invocations).IsEqualTo(5);
    }

    [Test]
    public async Task RaceAsync_ProducerPassesKeyThrough()
    {
        string? capturedKey = null;
        var tester = new StampedeTester((key, _) =>
        {
            capturedKey = key;
            return Task.FromResult("v");
        });

        await tester.RaceAsync("my-key", 1);

        await Assert.That(capturedKey).IsEqualTo("my-key");
    }
}
