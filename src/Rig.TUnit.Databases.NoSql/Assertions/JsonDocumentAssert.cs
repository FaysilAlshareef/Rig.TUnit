using System.Text.Json;
using System.Text.Json.Nodes;

namespace Rig.TUnit.Databases.NoSql.Assertions;

/// <summary>
/// Deep equality over JSON documents, ignoring system fields inserted by the backing
/// store (<c>_etag</c>, <c>_ts</c>, <c>_rid</c>, <c>__v</c>).
/// </summary>
public sealed class JsonDocumentAssert
{
    private JsonDocumentAssert() { }

    private static readonly string[] SystemFields =
        ["_etag", "_ts", "_rid", "_self", "_attachments", "__v", "_id"];

    public static void DeepEquals(string expectedJson, string actualJson, bool scrubSystemFields = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(actualJson);

        var expected = JsonNode.Parse(expectedJson);
        var actual = JsonNode.Parse(actualJson);

        if (scrubSystemFields)
        {
            Scrub(expected);
            Scrub(actual);
        }

        var expectedNormalized = expected?.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        var actualNormalized = actual?.ToJsonString(new JsonSerializerOptions { WriteIndented = false });

        if (!string.Equals(expectedNormalized, actualNormalized, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"JsonDocumentAssert.DeepEquals failed.\nExpected: {expectedNormalized}\nActual: {actualNormalized}");
        }
    }

    private static void Scrub(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var field in SystemFields)
            {
                obj.Remove(field);
            }
            foreach (var kv in obj)
            {
                Scrub(kv.Value);
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                Scrub(item);
            }
        }
    }
}
