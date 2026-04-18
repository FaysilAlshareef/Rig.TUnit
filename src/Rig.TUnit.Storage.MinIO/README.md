# Rig.TUnit.Storage.MinIO

Testcontainers-backed MinIO provider (`minio/minio`). Exposes an `IMinioClient` + options-driven fixture + fluent `UseMinIO` pipeline + pure-function `MinIOSasBuilder` for constructing presigned-URL parameters.

## Install

```
dotnet add package Rig.TUnit.Storage.MinIO
```

## Example

```csharp
await using var mio = new MinIOFixture();
await mio.InitializeAsync();

await mio.Client.MakeBucketAsync(new MakeBucketArgs().WithBucket("demo"));

using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hello"));
await mio.Client.PutObjectAsync(new PutObjectArgs()
    .WithBucket("demo").WithObject("greeting.txt")
    .WithStreamData(stream).WithObjectSize(stream.Length));
```

### Fluent rig wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseMinIO(RigConnect.FromValue("http://localhost:9000"), cfg => { }));
```

### Presign request construction

```csharp
var req = MinIOSasBuilder.BuildPresignRequest(
    "demo", "greeting.txt", "GET", TimeSpan.FromMinutes(15), TimeProvider.System);
var url = await mio.Client.PresignedGetObjectAsync(new PresignedGetObjectArgs()
    .WithBucket(req.BucketName).WithObject(req.ObjectName)
    .WithExpiry(req.ExpirySeconds));
```

## Dependencies

`Rig.TUnit.Storage`, `Testcontainers.Minio`, `Minio`
