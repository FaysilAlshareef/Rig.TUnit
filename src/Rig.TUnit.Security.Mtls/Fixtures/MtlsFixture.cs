using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Rig.TUnit.Security.Fixtures;
using Rig.TUnit.Security.Mtls.Options;

namespace Rig.TUnit.Security.Mtls.Fixtures;

/// <summary>
/// Generates an in-memory CA + client + server certificate triple on Initialize —
/// all private keys live in-process for the fixture lifetime and are disposed
/// deterministically on DisposeAsync. No disk writes, no certificate-store mutation.
/// </summary>
public sealed class MtlsFixture : SecurityFixtureBase
{
    private readonly MtlsFixtureOptions _options;
    private X509Certificate2? _ca;
    private X509Certificate2? _client;
    private X509Certificate2? _server;

    public MtlsFixture() : this(new MtlsFixtureOptions()) { }

    public MtlsFixture(IOptions<MtlsFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public MtlsFixture(MtlsFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public X509Certificate2 Ca => _ca ?? throw new InvalidOperationException("Fixture not initialized.");
    public X509Certificate2 ClientCertificate => _client ?? throw new InvalidOperationException("Fixture not initialized.");
    public X509Certificate2 ServerCertificate => _server ?? throw new InvalidOperationException("Fixture not initialized.");

    public override string ConnectionString => _ca is null
        ? throw new InvalidOperationException("Fixture not initialized.")
        : _ca.Thumbprint;

    public override Task InitializeAsync()
    {
        _ca = MtlsCertificateBuilder.CreateCertificateAuthority(_options.CaSubject);
        _client = MtlsCertificateBuilder.CreateLeaf(_ca, _options.ClientSubject);
        _server = MtlsCertificateBuilder.CreateLeaf(_ca, _options.ServerSubject);
        return Task.CompletedTask;
    }

    public override ValueTask DisposeAsync()
    {
        _server?.Dispose();
        _client?.Dispose();
        _ca?.Dispose();
        _server = null;
        _client = null;
        _ca = null;
        return ValueTask.CompletedTask;
    }
}
