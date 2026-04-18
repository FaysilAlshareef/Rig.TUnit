using Microsoft.Extensions.DependencyInjection;
using Minio.DataModel.Args;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.MinIO.Builder;
using Rig.TUnit.Storage.MinIO.Helpers;

namespace Rig.TUnit.Storage.MinIO.Tests.Integration;

public sealed class UseMinIOFluentTests
{
    [Test]
    public async Task UseMinIO_RegistersBuilder_WithoutException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("http://localhost:9000");

        MinIORigBuilder? configured = null;
        captured!.UseMinIO(source, b => configured = b);

        await Assert.That(configured).IsNotNull();
    }

    [Test]
    public async Task Fixture_Initialize_ThenPutAndGetObject_Succeeds()
    {
        var fx = await SharedMinIOFixture.GetAsync();
        var bucket = $"integ-{Guid.NewGuid():N}";
        await fx.Client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
        try
        {
            var body = "hello";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));
            await fx.Client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucket).WithObject("test.txt")
                .WithStreamData(stream).WithObjectSize(stream.Length));

            string? received = null;
            await fx.Client.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucket).WithObject("test.txt")
                .WithCallbackStream(s =>
                {
                    using var reader = new StreamReader(s);
                    received = reader.ReadToEnd();
                }));

            await Assert.That(received).IsEqualTo("hello");
        }
        finally
        {
            await fx.Client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(bucket).WithObject("test.txt"));
            await fx.Client.RemoveBucketAsync(new RemoveBucketArgs().WithBucket(bucket));
        }
    }

    [Test]
    public async Task SasBuilder_BuildPresignRequest_ProducesExpectedShape()
    {
        var req = MinIOSasBuilder.BuildPresignRequest(
            "bucket", "key", "GET", TimeSpan.FromMinutes(5), TimeProvider.System);

        await Assert.That(req.BucketName).IsEqualTo("bucket");
        await Assert.That(req.ObjectName).IsEqualTo("key");
    }
}
