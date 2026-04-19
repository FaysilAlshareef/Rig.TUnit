using Microsoft.Extensions.Options;
using Rig.TUnit.Security.Mtls.Fixtures;
using Rig.TUnit.Security.Mtls.Options;

namespace Rig.TUnit.Security.Mtls.Tests.Unit;

public sealed class MtlsFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_UsesDefaultOptions()
    {
        await using var fx = new MtlsFixture();
        await fx.InitializeAsync();
        await Assert.That(fx.Ca.Subject).Contains("rigtunit-test-ca");
        await Assert.That(fx.ClientCertificate.Subject).Contains("rigtunit-client");
        await Assert.That(fx.ServerCertificate.Subject).Contains("rigtunit-server");
    }

    [Test]
    public async Task Ctor_IOptions_UsesOptions()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new MtlsFixtureOptions
        {
            CaSubject = "CN=unit-ca",
            ClientSubject = "CN=unit-client",
            ServerSubject = "CN=unit-server",
        });
        await using var fx = new MtlsFixture(opts);
        await fx.InitializeAsync();
        await Assert.That(fx.Ca.Subject).Contains("unit-ca");
        await Assert.That(fx.ClientCertificate.Subject).Contains("unit-client");
        await Assert.That(fx.ServerCertificate.Subject).Contains("unit-server");
    }

    [Test]
    public async Task Ctor_NullOptions_Throws()
    {
        await Assert.That(() => new MtlsFixture((MtlsFixtureOptions)null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_NullIOptions_Throws()
    {
        await Assert.That(() => new MtlsFixture((IOptions<MtlsFixtureOptions>)null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ca_BeforeInitialize_Throws()
    {
        await using var fx = new MtlsFixture();
        await Assert.That(() => fx.Ca).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task ConnectionString_AfterInitialize_ReturnsThumbprint()
    {
        await using var fx = new MtlsFixture();
        await fx.InitializeAsync();
        await Assert.That(fx.ConnectionString).IsEqualTo(fx.Ca.Thumbprint);
    }

    [Test]
    public async Task DisposeAsync_ReleasesCertificates_SubsequentAccessThrows()
    {
        var fx = new MtlsFixture();
        await fx.InitializeAsync();
        await fx.DisposeAsync();
        await Assert.That(() => fx.Ca).ThrowsExactly<InvalidOperationException>();
    }
}
