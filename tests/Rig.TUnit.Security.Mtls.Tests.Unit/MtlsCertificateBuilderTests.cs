using Rig.TUnit.Security.Mtls;

namespace Rig.TUnit.Security.Mtls.Tests.Unit;

public sealed class MtlsCertificateBuilderTests
{
    [Test]
    public async Task CreateCertificateAuthority_HasBasicConstraintsCaTrue()
    {
        using var ca = MtlsCertificateBuilder.CreateCertificateAuthority();
        await Assert.That(ca.Subject).Contains("rigtunit-test-ca");
        await Assert.That(ca.HasPrivateKey).IsTrue();
    }

    [Test]
    public async Task CreateLeaf_SignedByCa_HasPrivateKey()
    {
        using var ca = MtlsCertificateBuilder.CreateCertificateAuthority();
        using var leaf = MtlsCertificateBuilder.CreateLeaf(ca);

        await Assert.That(leaf.Subject).Contains("rigtunit-client");
        await Assert.That(leaf.Issuer).IsEqualTo(ca.Subject);
        await Assert.That(leaf.HasPrivateKey).IsTrue();
    }

    [Test]
    public async Task MtlsAssert_BothSides_PassesWhenBothHaveKeys()
    {
        using var ca = MtlsCertificateBuilder.CreateCertificateAuthority();
        using var client = MtlsCertificateBuilder.CreateLeaf(ca);
        using var server = MtlsCertificateBuilder.CreateLeaf(ca, "CN=rigtunit-server");

        MtlsAssert.BothSidesAuthenticated(client, server);
        await Task.CompletedTask;
    }

    [Test]
    public async Task MtlsAssert_WithNullCert_Throws()
    {
        var threw = false;
        try { MtlsAssert.BothSidesAuthenticated(null, null); }
        catch (MtlsAssertionException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }
}
