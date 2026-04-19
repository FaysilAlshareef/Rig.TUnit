using Amazon.S3.Model;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.S3.Builder;
using Rig.TUnit.Storage.S3.Helpers;

namespace Rig.TUnit.Storage.S3.Tests.Integration;

public sealed class UseS3FluentTests
{
    [Test]
    public async Task UseS3_RegistersBuilder_WithoutException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("http://localhost:4566");

        S3RigBuilder? configured = null;
        captured!.UseS3(source, b => configured = b);

        await Assert.That(configured).IsNotNull();
    }

    [Test]
    public async Task Fixture_Initialize_ThenPutAndGetObject_Succeeds()
    {
        var fx = await SharedS3Fixture.GetAsync();
        var bucket = $"integ-{Guid.NewGuid():N}";
        await fx.Client.PutBucketAsync(bucket);
        try
        {
            await fx.Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = "test.txt",
                ContentBody = "hello",
            });
            var response = await fx.Client.GetObjectAsync(bucket, "test.txt");
            using var reader = new StreamReader(response.ResponseStream);
            var body = await reader.ReadToEndAsync();
            await Assert.That(body).IsEqualTo("hello");
        }
        finally
        {
            await fx.Client.DeleteObjectAsync(bucket, "test.txt");
            await fx.Client.DeleteBucketAsync(bucket);
        }
    }

    [Test]
    public async Task SasBuilder_BuildPresignRequest_ProducesExpectedShape()
    {
        var req = S3SasBuilder.BuildPresignRequest(
            "bucket", "key", "GET", TimeSpan.FromMinutes(5), TimeProvider.System);

        await Assert.That(req.BucketName).IsEqualTo("bucket");
        await Assert.That(req.Key).IsEqualTo("key");
    }
}
