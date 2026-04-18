using Rig.TUnit.Storage.S3.Helpers;

namespace Rig.TUnit.Storage.S3.Tests.Unit;

/// <summary>
/// Pure-function tests for <see cref="S3SasBuilder"/> — builds presigned-URL request
/// parameter records. No network, no AWS signing (LocalStack / prod generates real
/// signatures from the client).
/// </summary>
public sealed class S3SasBuilderTests
{
    [Test]
    public async Task BuildPresignRequest_WithDefaults_ProducesExpectedFields()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 04, 18, 12, 0, 0, TimeSpan.Zero));

        var req = S3SasBuilder.BuildPresignRequest(
            bucket: "demo",
            key: "file.txt",
            verb: "GET",
            expiry: TimeSpan.FromMinutes(15),
            clock: clock);

        await Assert.That(req.BucketName).IsEqualTo("demo");
        await Assert.That(req.Key).IsEqualTo("file.txt");
        await Assert.That(req.Verb).IsEqualTo("GET");
        await Assert.That(req.Expires).IsEqualTo(clock.GetUtcNow().UtcDateTime.Add(TimeSpan.FromMinutes(15)));
    }

    [Test]
    public async Task BuildPresignRequest_NullBucket_ThrowsArgumentException()
    {
        await Assert.That(() => S3SasBuilder.BuildPresignRequest(null!, "k", "GET", TimeSpan.FromMinutes(5), TimeProvider.System))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BuildPresignRequest_NullKey_ThrowsArgumentException()
    {
        await Assert.That(() => S3SasBuilder.BuildPresignRequest("b", null!, "GET", TimeSpan.FromMinutes(5), TimeProvider.System))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BuildPresignRequest_EmptyVerb_ThrowsArgumentException()
    {
        await Assert.That(() => S3SasBuilder.BuildPresignRequest("b", "k", "", TimeSpan.FromMinutes(5), TimeProvider.System))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BuildPresignRequest_ZeroExpiry_ThrowsArgumentOutOfRange()
    {
        await Assert.That(() => S3SasBuilder.BuildPresignRequest("b", "k", "GET", TimeSpan.Zero, TimeProvider.System))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task BuildPresignRequest_NullClock_ThrowsArgumentNullException()
    {
        await Assert.That(() => S3SasBuilder.BuildPresignRequest("b", "k", "GET", TimeSpan.FromMinutes(5), null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
