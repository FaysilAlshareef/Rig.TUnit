namespace Rig.TUnit.Storage.S3.Helpers;

public sealed record S3PresignRequest(string BucketName, string Key, string Verb, DateTime Expires);

/// <summary>
/// Pure-function presigned-URL parameter builder for S3. Produces the parameters
/// that <c>GetPreSignedUrlAsync</c> consumes; does NOT sign (tests that need real
/// signatures should use the fixture's <c>IAmazonS3</c> client).
/// </summary>
public static class S3SasBuilder
{
    public static S3PresignRequest BuildPresignRequest(
        string bucket,
        string key,
        string verb,
        TimeSpan expiry,
        TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(verb);
        if (expiry <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiry), "Expiry must be positive.");
        }
        ArgumentNullException.ThrowIfNull(clock);

        return new S3PresignRequest(
            BucketName: bucket,
            Key: key,
            Verb: verb.ToUpperInvariant(),
            Expires: clock.GetUtcNow().Add(expiry).UtcDateTime);
    }
}
