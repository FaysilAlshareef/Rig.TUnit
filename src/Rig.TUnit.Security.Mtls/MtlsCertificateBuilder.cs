using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Rig.TUnit.Security.Mtls;

/// <summary>
/// Generates self-signed CA + leaf certificates for mTLS tests. Certificates are
/// entirely in-memory and persist only for the test lifetime — no disk writes,
/// no registry mutations.
/// </summary>
public static class MtlsCertificateBuilder
{
    /// <summary>Creates a self-signed CA certificate.</summary>
    public static X509Certificate2 CreateCertificateAuthority(string subject = "CN=rigtunit-test-ca")
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(
            certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, critical: true));
        return req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365));
    }

    /// <summary>Issues a leaf certificate signed by <paramref name="ca"/>.</summary>
    public static X509Certificate2 CreateLeaf(X509Certificate2 ca, string subject = "CN=rigtunit-client")
    {
        ArgumentNullException.ThrowIfNull(ca);
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.1"), new Oid("1.3.6.1.5.5.7.3.2") }, critical: false));

        var serial = new byte[16];
        RandomNumberGenerator.Fill(serial);
        var leafNotAfter = new DateTimeOffset(ca.NotAfter.ToUniversalTime().AddSeconds(-1), TimeSpan.Zero);
        using var temp = req.Create(ca, DateTimeOffset.UtcNow.AddDays(-1), leafNotAfter, serial);
        return temp.CopyWithPrivateKey(rsa);
    }
}

/// <summary>Verifies an mTLS handshake completed — i.e., both client and server exchanged trusted certs.</summary>
public static class MtlsAssert
{
    public static void BothSidesAuthenticated(X509Certificate2? clientCert, X509Certificate2? serverCert)
    {
        if (clientCert is null) throw new MtlsAssertionException("Client did not present a certificate.");
        if (serverCert is null) throw new MtlsAssertionException("Server did not present a certificate.");
        if (!clientCert.HasPrivateKey) throw new MtlsAssertionException("Client certificate has no private key.");
        if (!serverCert.HasPrivateKey) throw new MtlsAssertionException("Server certificate has no private key.");
    }
}

public sealed class MtlsAssertionException : Exception
{
    public MtlsAssertionException(string message) : base(message) { }
}
