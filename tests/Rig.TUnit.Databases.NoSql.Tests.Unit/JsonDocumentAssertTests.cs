using Rig.TUnit.Databases.NoSql.Assertions;

namespace Rig.TUnit.Databases.NoSql.Tests.Unit;

public sealed class JsonDocumentAssertTests
{
    [Test]
    public async Task DeepEquals_NullOrWhiteSpaceExpected_ThrowsArgumentException()
    {
        await Assert.That(() => JsonDocumentAssert.DeepEquals("  ", "{\"a\":1}"))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task DeepEquals_NullOrWhiteSpaceActual_ThrowsArgumentException()
    {
        await Assert.That(() => JsonDocumentAssert.DeepEquals("{\"a\":1}", "  "))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public Task DeepEquals_IdenticalJson_DoesNotThrow()
    {
        JsonDocumentAssert.DeepEquals("{\"name\":\"Alice\"}", "{\"name\":\"Alice\"}");
        return Task.CompletedTask;
    }

    [Test]
    public async Task DeepEquals_DifferentValues_ThrowsInvalidOperationException()
    {
        await Assert.That(() => JsonDocumentAssert.DeepEquals("{\"name\":\"Alice\"}", "{\"name\":\"Bob\"}"))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public Task DeepEquals_SystemFieldsScrubbedByDefault_DoesNotThrow()
    {
        var expected = "{\"name\":\"Alice\"}";
        var actual = "{\"name\":\"Alice\",\"_etag\":\"xyz\",\"_ts\":12345,\"_rid\":\"abc\",\"_self\":\"s\",\"_attachments\":\"a\",\"__v\":1,\"_id\":\"id1\"}";

        JsonDocumentAssert.DeepEquals(expected, actual);
        return Task.CompletedTask;
    }

    [Test]
    public async Task DeepEquals_ScrubFalse_ExtraSystemField_ThrowsInvalidOperationException()
    {
        var expected = "{\"name\":\"Alice\"}";
        var actual = "{\"name\":\"Alice\",\"_etag\":\"xyz\"}";

        await Assert.That(() => JsonDocumentAssert.DeepEquals(expected, actual, scrubSystemFields: false))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public Task DeepEquals_NestedSystemFields_AreScrubbedRecursively()
    {
        var expected = "{\"data\":{\"value\":1}}";
        var actual = "{\"data\":{\"value\":1,\"_etag\":\"xyz\"}}";

        JsonDocumentAssert.DeepEquals(expected, actual);
        return Task.CompletedTask;
    }

    [Test]
    public Task DeepEquals_SystemFieldsInArray_AreScrubbedRecursively()
    {
        var expected = "{\"items\":[{\"id\":1}]}";
        var actual = "{\"items\":[{\"id\":1,\"_ts\":999}]}";

        JsonDocumentAssert.DeepEquals(expected, actual);
        return Task.CompletedTask;
    }

    [Test]
    public async Task DeepEquals_ExtraNonSystemField_ThrowsInvalidOperationException()
    {
        var expected = "{\"name\":\"Alice\"}";
        var actual = "{\"name\":\"Alice\",\"extra\":\"value\"}";

        await Assert.That(() => JsonDocumentAssert.DeepEquals(expected, actual))
            .ThrowsExactly<InvalidOperationException>();
    }
}
