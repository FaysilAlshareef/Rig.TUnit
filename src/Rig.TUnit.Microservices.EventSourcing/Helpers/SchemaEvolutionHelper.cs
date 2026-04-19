using System.Text.Json;

namespace Rig.TUnit.Microservices.EventSourcing.Helpers;

/// <summary>
/// Schema-evolution helper for event-sourced stores. Given a legacy JSON payload
/// (captured from production) and a current event type, deserializes and reports
/// fields that were dropped, added-with-default, or renamed — so a schema change
/// doesn't silently corrupt the stream on replay.
/// </summary>
public static class SchemaEvolutionHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static SchemaEvolutionReport Analyze<TEvent>(string legacyJson)
        where TEvent : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legacyJson);

        HashSet<string> legacyProps;
        try
        {
            var legacyDoc = JsonDocument.Parse(legacyJson);
            legacyProps = legacyDoc.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();
        }
        catch (JsonException ex)
        {
            return new SchemaEvolutionReport(
                DroppedFields: Array.Empty<string>(),
                AddedFields: Array.Empty<string>(),
                DeserializedSuccessfully: false,
                DeserializationError: ex.Message);
        }

        var currentProps = typeof(TEvent).GetProperties()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        var droppedFields = legacyProps
            .Where(lp => !currentProps.Contains(lp) && !currentProps.Any(cp => string.Equals(cp, lp, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var addedFields = currentProps
            .Where(cp => !legacyProps.Contains(cp) && !legacyProps.Any(lp => string.Equals(lp, cp, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        TEvent? hydrated = null;
        string? deserializationError = null;
        try
        {
            hydrated = JsonSerializer.Deserialize<TEvent>(legacyJson, Options);
        }
        catch (JsonException ex)
        {
            deserializationError = ex.Message;
        }

        return new SchemaEvolutionReport(
            DroppedFields: droppedFields,
            AddedFields: addedFields,
            DeserializedSuccessfully: hydrated is not null,
            DeserializationError: deserializationError);
    }
}

public sealed record SchemaEvolutionReport(
    IReadOnlyList<string> DroppedFields,
    IReadOnlyList<string> AddedFields,
    bool DeserializedSuccessfully,
    string? DeserializationError);
