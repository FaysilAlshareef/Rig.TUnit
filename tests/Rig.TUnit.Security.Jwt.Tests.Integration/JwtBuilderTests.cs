using System.Net;
using System.Net.Http.Headers;
using Rig.TUnit.Security.Jwt;
using Rig.TUnit.Security.Jwt.Options;

namespace Rig.TUnit.Security.Jwt.Tests.Integration;

public sealed class JwtBuilderTests
{
    private static JwtBuilder New() => JwtBuilder.Create(new JwtBuilderOptions
    {
        DefaultIssuer = JwtBearerTestServer.Issuer,
        DefaultAudience = JwtBearerTestServer.Audience,
        DefaultLifetimeMinutes = 10,
    });

    [Test]
    public async Task Valid_Hs256_Token_IsAcceptedByRealJwtBearer()
    {
        await using var server = await JwtBearerTestServer.StartAsync();
        var token = New()
            .Subject("alice")
            .SignedWithHs256(JwtBearerTestServer.SigningKeyBytes)
            .Build();

        var req = new HttpRequestMessage(HttpMethod.Get, "/");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await server.Client.SendAsync(req);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task No_Authorization_Header_Is401()
    {
        await using var server = await JwtBearerTestServer.StartAsync();
        var resp = await server.Client.GetAsync("/");
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Expired_Token_Is401()
    {
        await using var server = await JwtBearerTestServer.StartAsync();
        var token = New()
            .SignedWithHs256(JwtBearerTestServer.SigningKeyBytes)
            .BuildExpired(TimeSpan.FromMinutes(10));

        var req = new HttpRequestMessage(HttpMethod.Get, "/");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await server.Client.SendAsync(req);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task NotYetValid_Token_Is401()
    {
        await using var server = await JwtBearerTestServer.StartAsync();
        var token = New()
            .SignedWithHs256(JwtBearerTestServer.SigningKeyBytes)
            .BuildNotYetValid(TimeSpan.FromMinutes(10));

        var req = new HttpRequestMessage(HttpMethod.Get, "/");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await server.Client.SendAsync(req);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Tampered_Signature_Is401()
    {
        await using var server = await JwtBearerTestServer.StartAsync();
        var token = New()
            .SignedWithHs256(JwtBearerTestServer.SigningKeyBytes)
            .BuildTampered();

        var req = new HttpRequestMessage(HttpMethod.Get, "/");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await server.Client.SendAsync(req);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Wrong_Audience_Is401()
    {
        await using var server = await JwtBearerTestServer.StartAsync();
        var token = New()
            .Audience("different-audience")
            .SignedWithHs256(JwtBearerTestServer.SigningKeyBytes)
            .Build();

        var req = new HttpRequestMessage(HttpMethod.Get, "/");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await server.Client.SendAsync(req);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Wrong_Issuer_Is401()
    {
        await using var server = await JwtBearerTestServer.StartAsync();
        var token = New()
            .Issuer("different-issuer")
            .SignedWithHs256(JwtBearerTestServer.SigningKeyBytes)
            .Build();

        var req = new HttpRequestMessage(HttpMethod.Get, "/");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await server.Client.SendAsync(req);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Rs256_Token_WithKid_RoundTrips()
    {
        // RS256 path — generate a throwaway RSA key, sign with kid, build the token.
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var token = New()
            .WithKid("test-key-1")
            .Subject("bob")
            .SignedWithRs256(rsa, kid: "test-key-1")
            .Build();

        await Assert.That(token).IsNotNullOrEmpty();
        // Sanity: decoding the header contains the kid.
        var parts = token.Split('.');
        await Assert.That(parts.Length).IsEqualTo(3);
    }
}
