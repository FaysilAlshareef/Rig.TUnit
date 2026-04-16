using Grpc.Core;
using Rig.TUnit.Grpc.Helpers;

namespace Rig.TUnit.Grpc.Tests.Unit.Helpers;

public class MetadataHelperTests
{
    [Test]
    public async Task Build_WithClaimsDictionary_ReturnsMetadataWithAccessClaimsBinKey()
    {
        // Arrange
        var claims = new Dictionary<string, string> { ["userId"] = "123", ["role"] = "admin" };

        // Act
        var metadata = MetadataHelper.Build(claims);

        // Assert
        var entry = metadata.FirstOrDefault(e => e.Key == "access-claims-bin");
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.IsBinary).IsTrue();
    }

    [Test]
    public async Task Build_WithEmptyDictionary_ReturnsValidMetadataWithBinaryEntry()
    {
        // Arrange
        var claims = new Dictionary<string, string>();

        // Act
        var metadata = MetadataHelper.Build(claims);

        // Assert
        await Assert.That(metadata).IsNotNull();
        var entry = metadata.FirstOrDefault(e => e.Key == "access-claims-bin");
        await Assert.That(entry).IsNotNull();
    }

    [Test]
    public async Task Build_WithClaims_ReturnsNonEmptyProtobufBinary()
    {
        // Arrange
        var claims = new Dictionary<string, string> { ["tenant"] = "acme" };

        // Act
        var metadata = MetadataHelper.Build(claims);

        // Assert
        var entry = metadata.First(e => e.Key == "access-claims-bin");
        await Assert.That(entry.ValueBytes.Length).IsGreaterThan(0);
    }
}
