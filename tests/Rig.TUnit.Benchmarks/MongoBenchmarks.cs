using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using Rig.TUnit.Databases.NoSql.Mongo.Helpers;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// T022-RED benchmark for <see cref="BsonDiff"/>. Pure-function benchmark — no container.
/// Measures allocations on identical, shallow-diff, and deeply-nested diff scenarios.
/// </summary>
[MemoryDiagnoser]
public class MongoBenchmarks
{
    private BsonDocument _identicalA = null!;
    private BsonDocument _identicalB = null!;
    private BsonDocument _shallowDiffA = null!;
    private BsonDocument _shallowDiffB = null!;
    private BsonDocument _nestedA = null!;
    private BsonDocument _nestedB = null!;

    [GlobalSetup]
    public void Setup()
    {
        _identicalA = new BsonDocument { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
        _identicalB = new BsonDocument { ["a"] = 1, ["b"] = 2, ["c"] = 3 };

        _shallowDiffA = new BsonDocument { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
        _shallowDiffB = new BsonDocument { ["a"] = 1, ["b"] = 99, ["c"] = 3 };

        _nestedA = new BsonDocument
        {
            ["user"] = new BsonDocument { ["name"] = "alice", ["addr"] = new BsonDocument { ["city"] = "NYC" } },
            ["orders"] = new BsonArray { 1, 2, 3 },
        };
        _nestedB = new BsonDocument
        {
            ["user"] = new BsonDocument { ["name"] = "alice", ["addr"] = new BsonDocument { ["city"] = "LA" } },
            ["orders"] = new BsonArray { 1, 2, 3 },
        };
    }

    [Benchmark]
    public IReadOnlyList<string> BsonDiff_Identical() => BsonDiff.Differences(_identicalA, _identicalB);

    [Benchmark]
    public IReadOnlyList<string> BsonDiff_ShallowDiff() => BsonDiff.Differences(_shallowDiffA, _shallowDiffB);

    [Benchmark]
    public IReadOnlyList<string> BsonDiff_NestedDiff() => BsonDiff.Differences(_nestedA, _nestedB);
}
