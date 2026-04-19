namespace Rig.TUnit.Storage.MinIO.Helpers;

public sealed record MinIOPresignRequest(string BucketName, string ObjectName, string Verb, int ExpirySeconds);

/// <summary>
/// Pure-function presigned-URL parameter builder for MinIO. Produces the parameters
/// that <c>IMinioClient.PresignedGetObjectAsync</c> / <c>PresignedPutObjectAsync</c>
/// consume; does NOT sign (the Minio SDK produces the real signed URL).
/// MinIO enforces a maximum expiry of 7 days — same as AWS SigV4.
/// </summary>
public static class MinIOSasBuilder
{
    private static readonly TimeSpan MaxExpiry = TimeSpan.FromDays(7);

    public static MinIOPresignRequest BuildPresignRequest(
        string bucket,
        string objectName,
        string verb,
        TimeSpan expiry,
        TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(verb);
        if (expiry <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiry), "Expiry must be positive.");
        }
        if (expiry > MaxExpiry)
        {
            throw new ArgumentOutOfRangeException(nameof(expiry), $"Expiry must be at most {MaxExpiry.TotalDays} days.");
        }
        ArgumentNullException.ThrowIfNull(clock);

        // Discard the clock reference after validation — MinIO SDK uses its own internal clock.
        _ = clock.GetUtcNow();

        return new MinIOPresignRequest(
            BucketName: bucket,
            ObjectName: objectName,
            Verb: verb.ToUpperInvariant(),
            ExpirySeconds: (int)expiry.TotalSeconds);
    }
}
