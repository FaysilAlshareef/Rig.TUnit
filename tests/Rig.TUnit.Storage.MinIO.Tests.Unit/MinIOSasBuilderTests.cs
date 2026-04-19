using Rig.TUnit.Storage.MinIO.Helpers;

namespace Rig.TUnit.Storage.MinIO.Tests.Unit;

/// <summary>
/// Pure-function tests for <see cref="MinIOSasBuilder"/>. No network — the Minio SDK
/// signs the real presigned URL; here we validate parameter shape + guard semantics.
/// </summary>
public sealed class MinIOSasBuilderTests
{
    [Test]
    public async Task BuildPresignRequest_WithDefaults_ProducesExpectedFields()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 04, 18, 12, 0, 0, TimeSpan.Zero));

        var req = MinIOSasBuilder.BuildPresignRequest(
            bucket: "demo",
            objectName: "file.txt",
            verb: "GET",
            expiry: TimeSpan.FromMinutes(15),
            clock: clock);

        await Assert.That(req.BucketName).IsEqualTo("demo");
        await Assert.That(req.ObjectName).IsEqualTo("file.txt");
        await Assert.That(req.Verb).IsEqualTo("GET");
        await Assert.That(req.ExpirySeconds).IsEqualTo(900);
    }

    [Test]
    public async Task BuildPresignRequest_NullBucket_ThrowsArgumentException()
    {
        await Assert.That(() => MinIOSasBuilder.BuildPresignRequest(null!, "k", "GET", TimeSpan.FromMinutes(5), TimeProvider.System))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BuildPresignRequest_NullObject_ThrowsArgumentException()
    {
        await Assert.That(() => MinIOSasBuilder.BuildPresignRequest("b", null!, "GET", TimeSpan.FromMinutes(5), TimeProvider.System))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BuildPresignRequest_EmptyVerb_ThrowsArgumentException()
    {
        await Assert.That(() => MinIOSasBuilder.BuildPresignRequest("b", "o", "", TimeSpan.FromMinutes(5), TimeProvider.System))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BuildPresignRequest_ZeroExpiry_ThrowsArgumentOutOfRange()
    {
        await Assert.That(() => MinIOSasBuilder.BuildPresignRequest("b", "o", "GET", TimeSpan.Zero, TimeProvider.System))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task BuildPresignRequest_ExpiryOverOneWeek_ThrowsArgumentOutOfRange()
    {
        await Assert.That(() => MinIOSasBuilder.BuildPresignRequest("b", "o", "GET", TimeSpan.FromDays(8), TimeProvider.System))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task BuildPresignRequest_NullClock_ThrowsArgumentNullException()
    {
        await Assert.That(() => MinIOSasBuilder.BuildPresignRequest("b", "o", "GET", TimeSpan.FromMinutes(5), null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
