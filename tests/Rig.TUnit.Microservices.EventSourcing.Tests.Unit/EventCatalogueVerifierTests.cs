using Rig.TUnit.Microservices.EventSourcing.Helpers;

namespace Rig.TUnit.Microservices.EventSourcing.Tests.Unit;

public sealed class EventCatalogueVerifierTests
{
    [Test]
    public async Task FindMissingFactories_NullCatalogue_Throws()
    {
        await Assert.That(() => EventCatalogueVerifier.FindMissingFactories(null!, typeof(object).Assembly))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task FindMissingFactories_NullFactory_Throws()
    {
        await Assert.That(() => EventCatalogueVerifier.FindMissingFactories(typeof(object).Assembly, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task FindMissingFactories_CustomFilter_IsApplied()
    {
        // When we pass a filter that excludes every type, the result is always empty.
        var missing = EventCatalogueVerifier.FindMissingFactories(
            typeof(object).Assembly,
            typeof(object).Assembly,
            _ => false);
        var count = missing.Count;
        await Assert.That(count).IsEqualTo(0);
    }
}
