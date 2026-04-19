using MongoDB.Bson;

namespace Rig.TUnit.Databases.NoSql.Mongo.Helpers;

/// <summary>
/// Structural diff for BSON documents — returns the set of field paths that differ.
/// Use in assertions where exact-value equality is too brittle (e.g. server-assigned
/// <c>_id</c>, timestamps, version stamps).
/// </summary>
public static class BsonDiff
{
    public static IReadOnlyList<string> Differences(BsonDocument expected, BsonDocument actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        var differences = new List<string>();
        Compare(expected, actual, path: string.Empty, differences);
        return differences;
    }

    private static void Compare(BsonValue expected, BsonValue actual, string path, List<string> differences)
    {
        if (expected.BsonType != actual.BsonType)
        {
            differences.Add($"{path}: type {expected.BsonType} != {actual.BsonType}");
            return;
        }

        if (expected is BsonDocument expectedDoc && actual is BsonDocument actualDoc)
        {
            foreach (var key in expectedDoc.Names.Concat(actualDoc.Names).Distinct(StringComparer.Ordinal))
            {
                var child = string.IsNullOrEmpty(path) ? key : $"{path}.{key}";
                if (!expectedDoc.Contains(key))
                {
                    differences.Add($"{child}: missing in expected");
                }
                else if (!actualDoc.Contains(key))
                {
                    differences.Add($"{child}: missing in actual");
                }
                else
                {
                    Compare(expectedDoc[key], actualDoc[key], child, differences);
                }
            }
            return;
        }

        if (!expected.Equals(actual))
        {
            differences.Add($"{path}: {expected} != {actual}");
        }
    }
}
