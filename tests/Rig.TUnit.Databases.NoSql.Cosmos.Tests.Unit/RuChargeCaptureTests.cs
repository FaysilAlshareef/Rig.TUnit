using Rig.TUnit.Databases.NoSql.Cosmos.Helpers;

namespace Rig.TUnit.Databases.NoSql.Cosmos.Tests.Unit;

public sealed class RuChargeCaptureTests
{
    [Test]
    public async Task Record_SingleOp_TotalMatches()
    {
        var capture = new RuChargeCapture();
        capture.Record("read", 2.5);
        var total = capture.TotalRu;
        await Assert.That(total).IsEqualTo(2.5);
    }

    [Test]
    public async Task Record_MultipleOps_TotalSums()
    {
        var capture = new RuChargeCapture();
        capture.Record("read", 2.5);
        capture.Record("write", 7.0);
        capture.Record("read", 2.5);
        var total = capture.TotalRu;
        var count = capture.Samples.Count;
        await Assert.That(total).IsEqualTo(12.0);
        await Assert.That(count).IsEqualTo(3);
    }

    [Test]
    public async Task Record_NullOperation_Throws()
    {
        var capture = new RuChargeCapture();
        await Assert.That(() => capture.Record(null!, 1.0)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Record_NegativeCharge_Throws()
    {
        var capture = new RuChargeCapture();
        await Assert.That(() => capture.Record("x", -1.0)).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Clear_EmptiesSamples()
    {
        var capture = new RuChargeCapture();
        capture.Record("read", 2.5);
        capture.Clear();
        var count = capture.Samples.Count;
        var total = capture.TotalRu;
        await Assert.That(count).IsEqualTo(0);
        await Assert.That(total).IsEqualTo(0.0);
    }
}
