using Rig.TUnit.Security.OAuth;
using Rig.TUnit.Security.OAuth.Options;

namespace Rig.TUnit.Security.OAuth.Tests.Unit;

public sealed class MockOAuthServerTests
{
    private static MockOAuthServerOptions DefaultOptions() =>
        new() { Issuer = "https://test-issuer/" };

    [Test]
    public async Task Ctor_NullOptions_ThrowsArgumentNullException()
    {
        await Assert.That(() => new MockOAuthServer(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task DisposeAsync_WithoutStartAsync_CompletesWithoutError()
    {
        // Host is null when StartAsync was never called — should not throw
        var server = new MockOAuthServer(DefaultOptions());
        await server.DisposeAsync();
    }

    [Test]
    public async Task BaseUrl_BeforeStart_IsEmptyString()
    {
        var server = new MockOAuthServer(DefaultOptions());
        await Assert.That(server.BaseUrl).IsEqualTo(string.Empty);
        await server.DisposeAsync();
    }

    [Test]
    public async Task Issuer_ReflectsOptionsValue()
    {
        var server = new MockOAuthServer(new MockOAuthServerOptions { Issuer = "https://my-issuer.example/" });
        await Assert.That(server.Issuer).IsEqualTo("https://my-issuer.example/");
        await server.DisposeAsync();
    }
}
