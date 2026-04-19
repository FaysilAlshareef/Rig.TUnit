using Rig.TUnit.Security.Mtls;
using Rig.TUnit.Security.Mtls.Fixtures;

namespace Rig.TUnit.Security.Mtls.Tests.Integration;

/// <summary>
/// End-to-end: MtlsFixture generates a CA + client + server cert triple; both sides
/// authenticate via the MtlsAssert helper — exercises the real X.509 verification stack.
/// </summary>
public sealed class MtlsFixtureLiveTests
{
    [Test]
    public async Task InitializeAsync_ProducesMatchedCaClientServerTriple()
    {
        await using var fx = new MtlsFixture();
        await fx.InitializeAsync();

        await Assert.That(fx.ClientCertificate.Issuer).IsEqualTo(fx.Ca.Subject);
        await Assert.That(fx.ServerCertificate.Issuer).IsEqualTo(fx.Ca.Subject);
        await Assert.That(fx.ClientCertificate.HasPrivateKey).IsTrue();
        await Assert.That(fx.ServerCertificate.HasPrivateKey).IsTrue();

        MtlsAssert.BothSidesAuthenticated(fx.ClientCertificate, fx.ServerCertificate);
    }

    [Test]
    public async Task InitializeAsync_ClientServer_AreDistinctCertificates()
    {
        await using var fx = new MtlsFixture();
        await fx.InitializeAsync();
        await Assert.That(fx.ClientCertificate.Thumbprint).IsNotEqualTo(fx.ServerCertificate.Thumbprint);
    }

    [Test]
    public async Task InitializeAsync_CaCertificate_IsSelfSigned()
    {
        await using var fx = new MtlsFixture();
        await fx.InitializeAsync();
        await Assert.That(fx.Ca.Issuer).IsEqualTo(fx.Ca.Subject);
    }
}
