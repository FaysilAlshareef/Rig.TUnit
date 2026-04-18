using System.Globalization;
using System.Text;

namespace Rig.TUnit.Storage.AzureBlob.Helpers;

/// <summary>
/// Pure-function SAS-query-string builder for Azure Blob Storage. Produces the query
/// portion of a Shared Access Signature URL given container + blob + permissions +
/// expiry. Does NOT sign — tests that need real signatures should use
/// <c>Azure.Storage.Sas.BlobSasBuilder</c> via the fixture's BlobServiceClient.
/// This helper is for validating parameter shape + time-bounds + permissions.
/// </summary>
public static class AzureBlobSasBuilder
{
    public static string BuildQueryString(
        string container,
        string blob,
        string permissions,
        TimeSpan expiry,
        TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(container);
        ArgumentException.ThrowIfNullOrWhiteSpace(blob);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissions);
        if (expiry <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiry), "Expiry must be positive.");
        }
        ArgumentNullException.ThrowIfNull(clock);

        var expiresAt = clock.GetUtcNow().Add(expiry).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.Append("sv=2024-11-04")
          .Append("&sr=b")
          .Append("&sp=").Append(Uri.EscapeDataString(permissions))
          .Append("&se=").Append(Uri.EscapeDataString(expiresAt))
          .Append("&spr=https")
          .Append("&rscd=inline");
        return sb.ToString();
    }
}
