using Rig.TUnit.Ci.Enrichers;

namespace Rig.TUnit.Ci.Tests.Unit;

public sealed class CiTests
{
    [Test]
    public async Task FlakyQuarantine_WithBothFailAndSuccess_FlagsAsFlaky()
    {
        var q = new FlakyQuarantine();
        q.RecordFailure("TestA");
        q.RecordSuccess("TestA");
        q.RecordSuccess("TestB");

        var flaky = q.Flaky();
        await Assert.That(flaky).Contains("TestA");
        await Assert.That(flaky).DoesNotContain("TestB");
    }

    [Test]
    public async Task CoverageDelta_WhenCurrentAboveThreshold_IsAcceptable()
    {
        var enforcer = new CoverageDeltaEnforcer(Minimum: 0.02);
        await Assert.That(enforcer.IsAcceptable(0.90, 0.91)).IsTrue();
    }

    [Test]
    public async Task CoverageDelta_WhenCurrentFarBelow_NotAcceptable()
    {
        var enforcer = new CoverageDeltaEnforcer(Minimum: 0.02);
        await Assert.That(enforcer.IsAcceptable(0.80, 0.91)).IsFalse();
    }

    [Test]
    public async Task TrxEnricher_AppendsStdOutToNamedTest()
    {
        var path = Path.GetTempFileName();
        var xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <Results>
            <UnitTestResult testName="OrdersTest_Create" outcome="Passed" />
          </Results>
        </TestRun>
        """;
        await File.WriteAllTextAsync(path, xml);

        var enricher = new TrxEnricher();
        var enriched = enricher.EnrichFile(path, new Dictionary<string, IReadOnlyList<string>>
        {
            ["OrdersTest_Create"] = new[] { "trace:abc123" },
        });

        var contents = await File.ReadAllTextAsync(path);
        await Assert.That(enriched).IsEqualTo(1);
        await Assert.That(contents).Contains("trace:abc123");
        File.Delete(path);
    }
}
