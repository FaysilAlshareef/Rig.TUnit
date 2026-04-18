namespace Rig.TUnit.Storage.Assertions;

/// <summary>
/// Provider-agnostic blob descriptor captured by provider-specific assertions
/// (Azure Blob, S3, MinIO, FileSystem) and asserted fluently.
/// </summary>
public sealed record BlobDescriptor(
    string Container,
    string Key,
    long SizeBytes,
    string? ContentType,
    IReadOnlyDictionary<string, string>? Metadata);

public static class BlobAssert
{
    public static BlobAssertion Exists(BlobDescriptor? blob, string container, string key)
    {
        if (blob is null) throw new BlobAssertionException($"Blob {container}/{key} does not exist.");
        if (blob.Container != container || blob.Key != key)
        {
            throw new BlobAssertionException(
                $"Blob descriptor mismatch — expected {container}/{key} but got {blob.Container}/{blob.Key}.");
        }
        return new BlobAssertion(blob);
    }
}

public sealed class BlobAssertion
{
    private readonly BlobDescriptor _blob;
    internal BlobAssertion(BlobDescriptor blob) { _blob = blob; }

    public BlobAssertion WithContentType(string expected)
    {
        if (_blob.ContentType != expected)
        {
            throw new BlobAssertionException(
                $"Blob content type — expected '{expected}' but got '{_blob.ContentType}'.");
        }
        return this;
    }

    public BlobAssertion WithSize(long expected)
    {
        if (_blob.SizeBytes != expected)
        {
            throw new BlobAssertionException(
                $"Blob size — expected {expected} bytes but got {_blob.SizeBytes}.");
        }
        return this;
    }

    public BlobAssertion WithMetadata(string key, string value)
    {
        if (_blob.Metadata is null || !_blob.Metadata.TryGetValue(key, out var v) || v != value)
        {
            var known = _blob.Metadata is null ? "<none>" : string.Join(",", _blob.Metadata.Keys);
            throw new BlobAssertionException(
                $"Blob metadata — missing '{key}=\"{value}\"'. Known: [{known}].");
        }
        return this;
    }
}

/// <summary>Lifecycle-rule assertion — provider-supplied rule set, caller asserts coverage.</summary>
public sealed class LifecycleRule
{
    public required string Name { get; init; }
    public required string Prefix { get; init; }
    public TimeSpan? ExpiresAfter { get; init; }

    public bool AppliesTo(string blobKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobKey);
        return blobKey.StartsWith(Prefix, StringComparison.Ordinal);
    }
}

public sealed class BlobAssertionException : Exception
{
    public BlobAssertionException(string message) : base(message) { }
}

/// <summary>Provider-specific SAS / presigned URL builder base.</summary>
public abstract class SasBuilder
{
    public TimeSpan ExpiresIn { get; protected set; } = TimeSpan.FromMinutes(15);
    public abstract string Build(string container, string key);
}
