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

    [Test]
    public async Task TrxEnricher_NullPath_ThrowsArgumentNullException()
    {
        var enricher = new TrxEnricher();

        await Assert.That(() => enricher.EnrichFile(null!, new Dictionary<string, IReadOnlyList<string>>()))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task TrxEnricher_WhiteSpacePath_ThrowsArgumentException()
    {
        var enricher = new TrxEnricher();

        await Assert.That(() => enricher.EnrichFile("   ", new Dictionary<string, IReadOnlyList<string>>()))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task TrxEnricher_NullEnrichments_ThrowsArgumentNullException()
    {
        var enricher = new TrxEnricher();

        await Assert.That(() => enricher.EnrichFile("somepath.trx", null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task TrxEnricher_TestNameNotInEnrichments_EnrichedCountIsZero()
    {
        var path = Path.GetTempFileName();
        var xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <Results>
            <UnitTestResult testName="SomeTest" outcome="Passed" />
          </Results>
        </TestRun>
        """;
        await File.WriteAllTextAsync(path, xml);

        var enricher = new TrxEnricher();
        var enriched = enricher.EnrichFile(path, new Dictionary<string, IReadOnlyList<string>>
        {
            ["AnotherTest"] = new[] { "item" },
        });

        await Assert.That(enriched).IsEqualTo(0);
        File.Delete(path);
    }

    [Test]
    public async Task TrxEnricher_EmptyEnrichmentsList_IsSkipped()
    {
        var path = Path.GetTempFileName();
        var xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <Results>
            <UnitTestResult testName="SomeTest" outcome="Passed" />
          </Results>
        </TestRun>
        """;
        await File.WriteAllTextAsync(path, xml);

        var enricher = new TrxEnricher();
        var enriched = enricher.EnrichFile(path, new Dictionary<string, IReadOnlyList<string>>
        {
            ["SomeTest"] = Array.Empty<string>(), // empty list — should not enrich
        });

        await Assert.That(enriched).IsEqualTo(0);
        File.Delete(path);
    }

    [Test]
    public async Task FlakyQuarantine_RecordFailure_NullOrWhiteSpace_ThrowsArgumentException()
    {
        var q = new FlakyQuarantine();

        await Assert.That(() => q.RecordFailure("   ")).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task FlakyQuarantine_RecordSuccess_NullOrWhiteSpace_ThrowsArgumentException()
    {
        var q = new FlakyQuarantine();

        await Assert.That(() => q.RecordSuccess("   ")).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task FlakyQuarantine_OnlyFailures_NotFlaky()
    {
        var q = new FlakyQuarantine();
        q.RecordFailure("TestX");
        q.RecordFailure("TestX");

        var flaky = q.Flaky();

        await Assert.That(flaky).DoesNotContain("TestX");
    }

    [Test]
    public async Task FlakyQuarantine_OnlySuccesses_NotFlaky()
    {
        var q = new FlakyQuarantine();
        q.RecordSuccess("TestY");
        q.RecordSuccess("TestY");

        var flaky = q.Flaky();

        await Assert.That(flaky).DoesNotContain("TestY");
    }

    [Test]
    public async Task FlakyQuarantine_EmptyHistory_ReturnsEmptyList()
    {
        var q = new FlakyQuarantine();

        var flaky = q.Flaky();

        await Assert.That(flaky.Count).IsEqualTo(0);
    }
}
