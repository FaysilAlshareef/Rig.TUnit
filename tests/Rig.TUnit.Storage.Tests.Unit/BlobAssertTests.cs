using Rig.TUnit.Storage.Assertions;

namespace Rig.TUnit.Storage.Tests.Unit;

public sealed class BlobAssertTests
{
    private static BlobDescriptor MakeBlob(string container, string key) =>
        new(container, key, 1024, "application/octet-stream", new Dictionary<string, string> { ["env"] = "test" });

    [Test]
    public async Task Exists_NullBlob_ThrowsBlobAssertionException()
    {
        await Assert.That(() => BlobAssert.Exists(null, "container", "key"))
            .ThrowsExactly<BlobAssertionException>();
    }

    [Test]
    public Task Exists_MatchingBlob_ReturnsBlobAssertion()
    {
        var blob = MakeBlob("my-container", "my/key");

        var assertion = BlobAssert.Exists(blob, "my-container", "my/key");

        _ = assertion;
        return Task.CompletedTask;
    }

    [Test]
    public async Task Exists_WrongContainer_ThrowsBlobAssertionException()
    {
        var blob = MakeBlob("actual", "key");

        await Assert.That(() => BlobAssert.Exists(blob, "expected", "key"))
            .ThrowsExactly<BlobAssertionException>();
    }

    [Test]
    public async Task Exists_WrongKey_ThrowsBlobAssertionException()
    {
        var blob = MakeBlob("container", "actual-key");

        await Assert.That(() => BlobAssert.Exists(blob, "container", "expected-key"))
            .ThrowsExactly<BlobAssertionException>();
    }

    [Test]
    public Task WithContentType_MatchingType_ReturnsSelf()
    {
        var blob = new BlobDescriptor("c", "k", 100, "image/png", null);
        var assertion = BlobAssert.Exists(blob, "c", "k");

        var result = assertion.WithContentType("image/png");

        _ = result;
        return Task.CompletedTask;
    }

    [Test]
    public async Task WithContentType_WrongType_ThrowsBlobAssertionException()
    {
        var blob = new BlobDescriptor("c", "k", 100, "image/png", null);
        var assertion = BlobAssert.Exists(blob, "c", "k");

        await Assert.That(() => assertion.WithContentType("application/json"))
            .ThrowsExactly<BlobAssertionException>();
    }

    [Test]
    public Task WithSize_MatchingSize_ReturnsSelf()
    {
        var blob = MakeBlob("c", "k");
        var assertion = BlobAssert.Exists(blob, "c", "k");

        var result = assertion.WithSize(1024);

        _ = result;
        return Task.CompletedTask;
    }

    [Test]
    public async Task WithSize_WrongSize_ThrowsBlobAssertionException()
    {
        var blob = MakeBlob("c", "k");
        var assertion = BlobAssert.Exists(blob, "c", "k");

        await Assert.That(() => assertion.WithSize(999))
            .ThrowsExactly<BlobAssertionException>();
    }

    [Test]
    public Task WithMetadata_MatchingKeyValue_ReturnsSelf()
    {
        var blob = MakeBlob("c", "k");
        var assertion = BlobAssert.Exists(blob, "c", "k");

        var result = assertion.WithMetadata("env", "test");

        _ = result;
        return Task.CompletedTask;
    }

    [Test]
    public async Task WithMetadata_WrongValue_ThrowsBlobAssertionException()
    {
        var blob = MakeBlob("c", "k");
        var assertion = BlobAssert.Exists(blob, "c", "k");

        await Assert.That(() => assertion.WithMetadata("env", "production"))
            .ThrowsExactly<BlobAssertionException>();
    }

    [Test]
    public async Task WithMetadata_NullMetadata_ThrowsBlobAssertionException()
    {
        var blob = new BlobDescriptor("c", "k", 100, null, null);
        var assertion = BlobAssert.Exists(blob, "c", "k");

        await Assert.That(() => assertion.WithMetadata("any", "value"))
            .ThrowsExactly<BlobAssertionException>();
    }
}
