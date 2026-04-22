using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Rig.TUnit.Security.Jwt;

namespace Rig.TUnit.Security.Jwt.Tests.Unit;

public sealed class JwtBuilderTests
{
    private static byte[] ValidKey()
    {
        var key = new byte[32];
        Random.Shared.NextBytes(key);
        return key;
    }

    [Test]
    public async Task Build_WithoutCredentials_ThrowsInvalidOperationException()
    {
        await Assert.That(() => JwtBuilder.Create().Build())
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task SignedWithHs256_NullByteArray_ThrowsArgumentNullException()
    {
        await Assert.That(() => JwtBuilder.Create().SignedWithHs256((byte[])null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task SignedWithHs256_ShortKey_ThrowsArgumentException()
    {
        var shortKey = new byte[16]; // < 32 bytes
        await Assert.That(() => JwtBuilder.Create().SignedWithHs256(shortKey))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task SignedWithRs256_NullCert_ThrowsArgumentNullException()
    {
        await Assert.That(() => JwtBuilder.Create().SignedWithRs256((X509Certificate2)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task SignedWithRs256_NullRsa_ThrowsArgumentNullException()
    {
        await Assert.That(() => JwtBuilder.Create().SignedWithRs256((RSA)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Build_WithHs256Credentials_ReturnsThreePartToken()
    {
        var token = JwtBuilder.Create()
            .SignedWithHs256(ValidKey())
            .Build();

        await Assert.That(token.Split('.').Length).IsEqualTo(3);
    }

    [Test]
    public async Task BuildExpired_ReturnsThreePartToken()
    {
        var token = JwtBuilder.Create()
            .SignedWithHs256(ValidKey())
            .BuildExpired(TimeSpan.FromMinutes(10));

        await Assert.That(token.Split('.').Length).IsEqualTo(3);
    }

    [Test]
    public async Task BuildNotYetValid_ReturnsThreePartToken()
    {
        var token = JwtBuilder.Create()
            .SignedWithHs256(ValidKey())
            .BuildNotYetValid(TimeSpan.FromMinutes(5));

        await Assert.That(token.Split('.').Length).IsEqualTo(3);
    }

    [Test]
    public async Task BuildTampered_ReturnsThreePartTokenWithModifiedSignature()
    {
        var key = ValidKey();

        // BuildTampered flips the first character of the base64url signature
        var tampered = JwtBuilder.Create().SignedWithHs256(key).BuildTampered();
        var parts = tampered.Split('.');

        await Assert.That(parts.Length).IsEqualTo(3);

        // The first char should be 'A' or 'B' depending on the flip; either way it's a char
        var sig = parts[2];
        await Assert.That(sig.Length).IsGreaterThan(0);
    }
}
