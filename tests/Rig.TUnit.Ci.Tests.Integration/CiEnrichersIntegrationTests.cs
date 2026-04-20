using System.Xml.Linq;
using Rig.TUnit.Ci.Enrichers;

namespace Rig.TUnit.Ci.Tests.Integration;

/// <summary>
/// End-to-end exercises of <see cref="TrxEnricher"/>, <see cref="FlakyQuarantine"/>, and
/// <see cref="CoverageDeltaEnforcer"/>. TrxEnricher round-trips an actual TRX document via
/// <see cref="Path.GetTempFileName"/> — no mocks, real I/O.
/// </summary>
public sealed class CiEnrichersIntegrationTests
{
    [Test]
    public async Task TrxEnricher_EnrichFile_AppendsAnnotationsToMatchingTests()
    {
        var path = Path.Combine(Path.GetTempPath(), $"trx-{Guid.NewGuid():N}.trx");
        var trx = new XElement("TestRun",
            new XElement("Results",
                new XElement("UnitTestResult",
                    new XAttribute("testName", "SampleTest.Passes"))));
        new XDocument(trx).Save(path);

        try
        {
            var enricher = new TrxEnricher();
            var enrichments = new Dictionary<string, IReadOnlyList<string>>
            {
                ["SampleTest.Passes"] = new[] { "span-id=abc", "screenshot=/tmp/x.png" },
            };

            var count = enricher.EnrichFile(path, enrichments);

            await Assert.That(count).IsEqualTo(1);
            var saved = XDocument.Load(path);
            var stdout = saved.Descendants("StdOut").Single();
            await Assert.That(stdout.Value).Contains("span-id=abc");
            await Assert.That(stdout.Value).Contains("screenshot=/tmp/x.png");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task TrxEnricher_EnrichFile_IgnoresUnmatchedTests()
    {
        var path = Path.Combine(Path.GetTempPath(), $"trx-{Guid.NewGuid():N}.trx");
        var trx = new XElement("TestRun",
            new XElement("Results",
                new XElement("UnitTestResult", new XAttribute("testName", "Other.Test"))));
        new XDocument(trx).Save(path);

        try
        {
            var count = new TrxEnricher().EnrichFile(
                path,
                new Dictionary<string, IReadOnlyList<string>>
                {
                    ["Unmatched.Test"] = new[] { "x" },
                });

            await Assert.That(count).IsEqualTo(0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task FlakyQuarantine_RecordsFlaky_OnMixedRuns()
    {
        var quarantine = new FlakyQuarantine();
        quarantine.RecordFailure("Flaky.Test");
        quarantine.RecordSuccess("Flaky.Test");
        quarantine.RecordFailure("Always.Fails");
        quarantine.RecordSuccess("Always.Passes");

        var flaky = quarantine.Flaky();

        await Assert.That(flaky).Contains("Flaky.Test");
        await Assert.That(flaky).DoesNotContain("Always.Fails");
        await Assert.That(flaky).DoesNotContain("Always.Passes");
    }

    [Test]
    public async Task CoverageDeltaEnforcer_Acceptable_WhenCurrentMeetsBaselineMinusTolerance()
    {
        var enforcer = new CoverageDeltaEnforcer(Minimum: 0.02);

        await Assert.That(enforcer.IsAcceptable(current: 0.88, baseline: 0.90)).IsTrue();
        await Assert.That(enforcer.IsAcceptable(current: 0.85, baseline: 0.90)).IsFalse();
    }
}
