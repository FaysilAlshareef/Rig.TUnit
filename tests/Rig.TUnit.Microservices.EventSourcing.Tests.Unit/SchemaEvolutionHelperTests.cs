using Rig.TUnit.Microservices.EventSourcing.Helpers;

namespace Rig.TUnit.Microservices.EventSourcing.Tests.Unit;

public sealed class SchemaEvolutionHelperTests
{
    private sealed record OrderCreatedV1(string OrderId, decimal Amount);
    private sealed record OrderCreatedV2(string OrderId, decimal Amount, string Currency);

    [Test]
    public async Task Analyze_NullJson_Throws()
    {
        await Assert.That(() => SchemaEvolutionHelper.Analyze<OrderCreatedV1>(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Analyze_FieldAdded_ReportsAdded()
    {
        const string legacyJson = """{"orderId":"o1","amount":10.0}""";
        var report = SchemaEvolutionHelper.Analyze<OrderCreatedV2>(legacyJson);
        await Assert.That(report.AddedFields).Contains("Currency");
        await Assert.That(report.DeserializedSuccessfully).IsTrue();
    }

    [Test]
    public async Task Analyze_FieldDropped_ReportsDropped()
    {
        const string legacyJson = """{"orderId":"o1","amount":10.0,"legacyTag":"x"}""";
        var report = SchemaEvolutionHelper.Analyze<OrderCreatedV1>(legacyJson);
        await Assert.That(report.DroppedFields).Contains("legacyTag");
    }

    [Test]
    public async Task Analyze_IdenticalSchema_NoDrift()
    {
        const string legacyJson = """{"orderId":"o1","amount":10.0}""";
        var report = SchemaEvolutionHelper.Analyze<OrderCreatedV1>(legacyJson);
        await Assert.That(report.DroppedFields.Count).IsEqualTo(0);
        await Assert.That(report.AddedFields.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Analyze_InvalidJson_ReportsDeserializationError()
    {
        const string invalid = "not json";
        var report = SchemaEvolutionHelper.Analyze<OrderCreatedV1>(invalid);
        await Assert.That(report.DeserializedSuccessfully).IsFalse();
        await Assert.That(report.DeserializationError).IsNotNullOrEmpty();
    }
}
