# Rig.TUnit.Storage.S3

LocalStack-backed Amazon S3 provider (`localstack/localstack:3`). Exposes an `IAmazonS3` client + options-driven fixture + fluent `UseS3` pipeline + pure-function `S3SasBuilder` for constructing presigned-URL request parameters.

## Install

```
dotnet add package Rig.TUnit.Storage.S3
```

## Example

```csharp
await using var s3 = new S3Fixture();
await s3.InitializeAsync();

await s3.Client.PutBucketAsync("demo");
await s3.Client.PutObjectAsync(new PutObjectRequest
{
    BucketName = "demo",
    Key = "greeting.txt",
    ContentBody = "hello",
});
```

### Fluent rig wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseS3(RigConnect.FromValue("http://localhost:4566"), cfg => { }));
```

### Presign request construction

```csharp
var req = S3SasBuilder.BuildPresignRequest(
    "demo", "greeting.txt", "GET", TimeSpan.FromMinutes(15), TimeProvider.System);
var url = await s3.Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
{
    BucketName = req.BucketName,
    Key = req.Key,
    Verb = HttpVerb.GET,
    Expires = req.Expires,
});
```

## Dependencies

`Rig.TUnit.Storage`, `Testcontainers.LocalStack`, `AWSSDK.S3`
