using Amazon.S3;
using Amazon.S3.Model;

namespace Rig.TUnit.Storage.S3.Tests.Integration;

/// <summary>
/// S3 quirks tested against LocalStack (Testcontainer) — not real AWS. Verifies
/// bucket creation, object PUT/GET roundtrip, and presigned URL generation.
/// </summary>
public sealed class S3QuirkTests
{
    [Test]
    public async Task PutBucket_ThenListBuckets_ContainsNewBucket()
    {
        var fx = await SharedS3Fixture.GetAsync();
        var name = "b-" + Guid.NewGuid().ToString("N");
        await fx.Client.PutBucketAsync(new PutBucketRequest { BucketName = name });
        var list = await fx.Client.ListBucketsAsync();
        await Assert.That(list.Buckets!.Any(b => b.BucketName == name)).IsTrue();
    }

    [Test]
    public async Task PutObject_ThenGetObject_RoundtripsContent()
    {
        var fx = await SharedS3Fixture.GetAsync();
        var name = "b-" + Guid.NewGuid().ToString("N");
        await fx.Client.PutBucketAsync(new PutBucketRequest { BucketName = name });

        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hello"));
        await fx.Client.PutObjectAsync(new PutObjectRequest { BucketName = name, Key = "hello.txt", InputStream = ms });
        var get = await fx.Client.GetObjectAsync(new GetObjectRequest { BucketName = name, Key = "hello.txt" });
        using var reader = new StreamReader(get.ResponseStream);
        var content = await reader.ReadToEndAsync();

        await Assert.That(content).IsEqualTo("hello");
    }

    [Test]
    public async Task GetPreSignedUrl_ReturnsSignedLink()
    {
        var fx = await SharedS3Fixture.GetAsync();
        var name = "b-" + Guid.NewGuid().ToString("N");
        await fx.Client.PutBucketAsync(new PutBucketRequest { BucketName = name });

        var url = await fx.Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = name,
            Key = "any.txt",
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(5),
        });

        await Assert.That(url).StartsWith("http");
        await Assert.That(url).Contains("Signature=");
    }
}
