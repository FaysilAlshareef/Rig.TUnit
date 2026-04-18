using Rig.TUnit.Storage.AzureBlob.Helpers;

namespace Rig.TUnit.Storage.AzureBlob.Tests.Unit;

/// <summary>
/// Pure-function tests for <see cref="AzureBlobSasBuilder"/> — builds SAS parameter
/// strings given container + blob + permissions + expiry. No network, no real SAS
/// token signing (Azurite validates its own).
/// </summary>
public sealed class AzureBlobSasBuilderTests
{
    [Test]
    public async Task BuildQueryString_WithDefaults_IncludesExpiryAndPermissions()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 04, 18, 12, 0, 0, TimeSpan.Zero));

        var query = AzureBlobSasBuilder.BuildQueryString(
            container: "demo",
            blob: "file.txt",
            permissions: "r",
            expiry: TimeSpan.FromMinutes(15),
            clock: clock);

        await Assert.That(query).Contains("sp=r");
        await Assert.That(query).Contains("se=");
        await Assert.That(query).Contains("sr=b");
    }

    [Test]
    public async Task BuildQueryString_NullContainer_ThrowsArgumentException()
    {
        await Assert.That(() => AzureBlobSasBuilder.BuildQueryString(null!, "f", "r", TimeSpan.FromMinutes(5), TimeProvider.System))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BuildQueryString_NullBlob_ThrowsArgumentException()
    {
        await Assert.That(() => AzureBlobSasBuilder.BuildQueryString("c", null!, "r", TimeSpan.FromMinutes(5), TimeProvider.System))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BuildQueryString_EmptyPermissions_ThrowsArgumentException()
    {
        await Assert.That(() => AzureBlobSasBuilder.BuildQueryString("c", "f", "", TimeSpan.FromMinutes(5), TimeProvider.System))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BuildQueryString_ZeroExpiry_ThrowsArgumentOutOfRange()
    {
        await Assert.That(() => AzureBlobSasBuilder.BuildQueryString("c", "f", "r", TimeSpan.Zero, TimeProvider.System))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task BuildQueryString_NullClock_ThrowsArgumentNullException()
    {
        await Assert.That(() => AzureBlobSasBuilder.BuildQueryString("c", "f", "r", TimeSpan.FromMinutes(5), null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
