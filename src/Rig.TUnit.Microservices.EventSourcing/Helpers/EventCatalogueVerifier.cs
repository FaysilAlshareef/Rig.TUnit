using System.Reflection;

namespace Rig.TUnit.Microservices.EventSourcing.Helpers;

/// <summary>
/// Verifies an event catalogue is producible: given a set of event factory
/// methods (e.g., static <c>OrderCreated.Of(...)</c> helpers), asserts that
/// every declared event type in the catalogue has a corresponding factory so
/// unknown-event drift between the catalogue and the factories surfaces at
/// test time instead of deployment.
/// </summary>
public static class EventCatalogueVerifier
{
    /// <summary>
    /// Returns event types declared in <paramref name="catalogueAssembly"/> that
    /// lack a matching factory producer in <paramref name="factoryAssembly"/>.
    /// An event type matches a factory when a public static method exists whose
    /// return type is assignable to the event type.
    /// </summary>
    public static IReadOnlyCollection<Type> FindMissingFactories(
        Assembly catalogueAssembly,
        Assembly factoryAssembly,
        Func<Type, bool>? eventTypeFilter = null)
    {
        ArgumentNullException.ThrowIfNull(catalogueAssembly);
        ArgumentNullException.ThrowIfNull(factoryAssembly);

        var filter = eventTypeFilter ?? IsLikelyEventType;

        var eventTypes = catalogueAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(filter)
            .ToArray();

        var factoryReturnTypes = factoryAssembly.GetTypes()
            .Where(t => t.IsClass)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Select(m => m.ReturnType)
            .ToHashSet();

        return eventTypes
            .Where(evt => !factoryReturnTypes.Any(rt => evt.IsAssignableFrom(rt)))
            .ToArray();
    }

    private static bool IsLikelyEventType(Type t)
        => t.Name.EndsWith("Event", StringComparison.Ordinal)
           || t.Name.EndsWith("ed", StringComparison.Ordinal);
}
