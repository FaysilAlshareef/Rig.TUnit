using MongoDB.Bson;
using Rig.TUnit.Databases.NoSql.Mongo.Helpers;

namespace Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit;

/// <summary>
/// T022-RED pure-function unit tests for <see cref="BsonDiff"/>. No container required —
/// covers identical docs, value mismatch, missing fields in either direction, nested
/// document dotted paths, and type mismatches. All methods follow the Arrange / Act /
/// Assert layout with blank lines separating sections.
/// </summary>
public sealed class BsonDiffTests
{
    [Test]
    public async Task Differences_IdenticalDocuments_ReturnsEmptyList()
    {
        // Arrange
        var a = new BsonDocument { ["name"] = "alice", ["age"] = 30 };
        var b = new BsonDocument { ["name"] = "alice", ["age"] = 30 };

        // Act
        var differences = BsonDiff.Differences(a, b);

        // Assert
        await Assert.That(differences).IsEmpty();
    }

    [Test]
    public async Task Differences_ValueMismatch_ReportsSinglePath()
    {
        // Arrange
        var a = new BsonDocument { ["name"] = "alice", ["age"] = 30 };
        var b = new BsonDocument { ["name"] = "alice", ["age"] = 31 };

        // Act
        var differences = BsonDiff.Differences(a, b);

        // Assert
        await Assert.That(differences.Count).IsEqualTo(1);
        await Assert.That(differences[0]).Contains("age");
    }

    [Test]
    public async Task Differences_FieldMissingInActual_FlagsMissingInActual()
    {
        // Arrange
        var a = new BsonDocument { ["name"] = "alice", ["age"] = 30 };
        var b = new BsonDocument { ["name"] = "alice" };

        // Act
        var differences = BsonDiff.Differences(a, b);

        // Assert
        await Assert.That(differences.Count).IsEqualTo(1);
        await Assert.That(differences[0]).Contains("age: missing in actual");
    }

    [Test]
    public async Task Differences_FieldMissingInExpected_FlagsMissingInExpected()
    {
        // Arrange
        var a = new BsonDocument { ["name"] = "alice" };
        var b = new BsonDocument { ["name"] = "alice", ["extra"] = true };

        // Act
        var differences = BsonDiff.Differences(a, b);

        // Assert
        await Assert.That(differences.Count).IsEqualTo(1);
        await Assert.That(differences[0]).Contains("extra: missing in expected");
    }

    [Test]
    public async Task Differences_NestedDocumentMismatch_ReportsDottedPath()
    {
        // Arrange
        var a = new BsonDocument { ["addr"] = new BsonDocument { ["city"] = "NYC" } };
        var b = new BsonDocument { ["addr"] = new BsonDocument { ["city"] = "LA" } };

        // Act
        var differences = BsonDiff.Differences(a, b);

        // Assert
        await Assert.That(differences.Count).IsEqualTo(1);
        await Assert.That(differences[0]).Contains("addr.city");
    }

    [Test]
    public async Task Differences_TypeMismatch_FlagsTypesInMessage()
    {
        // Arrange
        var a = new BsonDocument { ["value"] = 42 };
        var b = new BsonDocument { ["value"] = "42" };

        // Act
        var differences = BsonDiff.Differences(a, b);

        // Assert
        await Assert.That(differences.Count).IsEqualTo(1);
        await Assert.That(differences[0]).Contains("type");
    }

    [Test]
    public async Task Differences_NullExpected_Throws()
    {
        // Arrange
        var b = new BsonDocument();

        // Act + Assert
        await Assert.That(() => BsonDiff.Differences(null!, b))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Differences_NullActual_Throws()
    {
        // Arrange
        var a = new BsonDocument();

        // Act + Assert
        await Assert.That(() => BsonDiff.Differences(a, null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
