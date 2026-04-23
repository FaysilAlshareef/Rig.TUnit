using Rig.TUnit.Storage.Assertions;

namespace Rig.TUnit.Storage.Tests.Unit;

public sealed class BlobValueObjectTests
{
    [Test]
    public async Task BlobDescriptor_Properties_AreSetCorrectly()
    {
        var meta = new Dictionary<string, string> { ["env"] = "prod" };
        var blob = new BlobDescriptor("container-1", "path/to/file.txt", 2048, "text/plain", meta);

        await Assert.That(blob.Container).IsEqualTo("container-1");
        await Assert.That(blob.Key).IsEqualTo("path/to/file.txt");
        await Assert.That(blob.SizeBytes).IsEqualTo(2048L);
        await Assert.That(blob.ContentType).IsEqualTo("text/plain");
        await Assert.That(blob.Metadata!["env"]).IsEqualTo("prod");
    }

    [Test]
    public async Task BlobAssertionException_Message_IsPreserved()
    {
        var ex = new BlobAssertionException("blob not found");

        await Assert.That(ex.Message).IsEqualTo("blob not found");
    }

    [Test]
    public async Task LifecycleRule_AppliesTo_MatchingPrefix_ReturnsTrue()
    {
        var rule = new LifecycleRule { Name = "archive", Prefix = "logs/", ExpiresAfter = TimeSpan.FromDays(30) };

        await Assert.That(rule.AppliesTo("logs/2026/01.log")).IsTrue();
    }

    [Test]
    public async Task LifecycleRule_AppliesTo_NonMatchingPrefix_ReturnsFalse()
    {
        var rule = new LifecycleRule { Name = "archive", Prefix = "logs/" };

        await Assert.That(rule.AppliesTo("uploads/file.bin")).IsFalse();
    }

    [Test]
    public async Task LifecycleRule_AppliesTo_WhiteSpaceKey_ThrowsArgumentException()
    {
        var rule = new LifecycleRule { Name = "archive", Prefix = "logs/" };

        await Assert.That(() => rule.AppliesTo("  "))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task SasBuilder_ExpiresIn_DefaultIsFifteenMinutes()
    {
        var builder = new StubSasBuilder();

        await Assert.That(builder.ExpiresIn).IsEqualTo(TimeSpan.FromMinutes(15));
    }

    [Test]
    public async Task SasBuilder_Build_ReturnsExpectedUrl()
    {
        var builder = new StubSasBuilder();

        var result = builder.Build("my-container", "my/key");

        await Assert.That(result).IsEqualTo("sas://my-container/my/key");
    }

    private sealed class StubSasBuilder : SasBuilder
    {
        public override string Build(string container, string key) => $"sas://{container}/{key}";
    }
}
