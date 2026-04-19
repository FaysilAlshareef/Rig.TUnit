using Rig.TUnit.Databases.NoSql.Cosmos.Helpers;

namespace Rig.TUnit.Databases.NoSql.Cosmos.Tests.Unit;

public sealed class PartitionKeyDistributionCheckerTests
{
    [Test]
    public async Task MaxShare_Empty_ReturnsZero()
    {
        var share = PartitionKeyDistributionChecker.MaxShare(new Dictionary<string, int>());
        await Assert.That(share).IsEqualTo(0.0);
    }

    [Test]
    public async Task MaxShare_EvenDistribution_ReturnsFraction()
    {
        var counts = new Dictionary<string, int> { ["a"] = 10, ["b"] = 10, ["c"] = 10 };
        var share = PartitionKeyDistributionChecker.MaxShare(counts);
        await Assert.That(share).IsEqualTo(10.0 / 30.0);
    }

    [Test]
    public async Task MaxShare_HotPartition_ReturnsHighFraction()
    {
        var counts = new Dictionary<string, int> { ["hot"] = 90, ["a"] = 5, ["b"] = 5 };
        var share = PartitionKeyDistributionChecker.MaxShare(counts);
        await Assert.That(share).IsEqualTo(0.9);
    }

    [Test]
    public async Task NormalisedEntropy_PureHot_ReturnsLow()
    {
        var counts = new Dictionary<string, int> { ["hot"] = 100, ["cold"] = 1 };
        var entropy = PartitionKeyDistributionChecker.NormalisedEntropy(counts);
        await Assert.That(entropy).IsLessThan(0.15);
    }

    [Test]
    public async Task NormalisedEntropy_Even_ReturnsOne()
    {
        var counts = new Dictionary<string, int> { ["a"] = 10, ["b"] = 10, ["c"] = 10, ["d"] = 10 };
        var entropy = PartitionKeyDistributionChecker.NormalisedEntropy(counts);
        await Assert.That(entropy).IsEqualTo(1.0);
    }

    [Test]
    public async Task IsHealthy_WithinThreshold_ReturnsTrue()
    {
        var counts = new Dictionary<string, int> { ["a"] = 3, ["b"] = 3, ["c"] = 4 };
        var healthy = PartitionKeyDistributionChecker.IsHealthy(counts, threshold: 0.5);
        await Assert.That(healthy).IsTrue();
    }

    [Test]
    public async Task IsHealthy_ExceedsThreshold_ReturnsFalse()
    {
        var counts = new Dictionary<string, int> { ["hot"] = 90, ["cold"] = 10 };
        var healthy = PartitionKeyDistributionChecker.IsHealthy(counts, threshold: 0.5);
        await Assert.That(healthy).IsFalse();
    }

    [Test]
    public async Task IsHealthy_InvalidThreshold_Throws()
    {
        var counts = new Dictionary<string, int> { ["a"] = 1 };
        await Assert.That(() => PartitionKeyDistributionChecker.IsHealthy(counts, threshold: 0))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task MaxShare_NullInput_Throws()
    {
        await Assert.That(() => PartitionKeyDistributionChecker.MaxShare(null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
