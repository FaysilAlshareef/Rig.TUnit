# Rig.TUnit.Storage.AzureBlob

Azurite-backed Azure Blob Storage provider (`mcr.microsoft.com/azure-storage/azurite`). Exposes a `BlobServiceClient` + options-driven fixture + fluent `UseAzureBlob` pipeline + pure-function `AzureBlobSasBuilder` for constructing SAS query parameters.

## Install

```
dotnet add package Rig.TUnit.Storage.AzureBlob
```

## Example

```csharp
await using var blob = new AzureBlobFixture();
await blob.InitializeAsync();

var container = blob.Client.GetBlobContainerClient("demo");
await container.CreateIfNotExistsAsync();
await container.GetBlobClient("greeting.txt").UploadAsync(BinaryData.FromString("hello"));
```

### Fluent rig wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseAzureBlob(RigConnect.FromValue("UseDevelopmentStorage=true"), cfg => { }));
```

### SAS query construction

```csharp
var query = AzureBlobSasBuilder.BuildQueryString(
    "demo", "greeting.txt", "r", TimeSpan.FromMinutes(15), TimeProvider.System);
```

## Dependencies

`Rig.TUnit.Storage`, `Testcontainers.Azurite`, `Azure.Storage.Blobs`
