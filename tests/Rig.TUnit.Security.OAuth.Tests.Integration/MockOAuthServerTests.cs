using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Rig.TUnit.Security.OAuth;
using Rig.TUnit.Security.OAuth.Options;

namespace Rig.TUnit.Security.OAuth.Tests.Integration;

public sealed class MockOAuthServerTests
{
    private const string Issuer = "https://mock-oauth.test";

    private static async Task<MockOAuthServer> StartMockAsync()
    {
        var srv = new MockOAuthServer(new MockOAuthServerOptions { Issuer = Issuer });
        await srv.StartAsync();
        return srv;
    }

    [Test]
    public async Task Discovery_Document_Is_Served()
    {
        await using var mock = await StartMockAsync();
        using var http = new HttpClient();
        var doc = await http.GetStringAsync($"{mock.BaseUrl}/.well-known/openid-configuration");
        await Assert.That(doc).Contains("\"issuer\"");
        await Assert.That(doc).Contains("\"jwks_uri\"");
        await Assert.That(doc).Contains("\"token_endpoint\"");
    }

    [Test]
    public async Task Jwks_Returns_RS256_Key()
    {
        await using var mock = await StartMockAsync();
        using var http = new HttpClient();
        var body = await http.GetStringAsync($"{mock.BaseUrl}/jwks");
        await Assert.That(body).Contains("\"kty\":\"RSA\"");
        await Assert.That(body).Contains("\"alg\":\"RS256\"");
    }

    [Test]
    public async Task ClientCredentials_Flow_Issues_Valid_Token_Accepted_By_JwtBearer()
    {
        await using var mock = await StartMockAsync();
        using var http = new HttpClient();

        var tokenResp = await http.PostAsync($"{mock.BaseUrl}/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "svc-1",
                ["scope"] = "api.read",
            }));
        await Assert.That(tokenResp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var tokenJson = await tokenResp.Content.ReadAsStringAsync();
        var access = JsonDocument.Parse(tokenJson).RootElement.GetProperty("access_token").GetString()!;

        // Start a real JwtBearer-protected server that fetches JWKS from the mock.
        await using var api = await StartProtectedApiAsync(mock.BaseUrl, mock.Issuer);
        var req = new HttpRequestMessage(HttpMethod.Get, "/");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
        var apiResp = await api.Client.SendAsync(req);
        await Assert.That(apiResp.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task AuthorizationCode_Pkce_S256_Flow_Succeeds()
    {
        await using var mock = await StartMockAsync();
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        var verifier = "xyz-verifier-0123456789ABCDEF0123456789";
        var challenge = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));

        var authResp = await http.GetAsync(
            $"{mock.BaseUrl}/authorize?client_id=webapp&redirect_uri=https%3A%2F%2Fapp%2Fcallback&state=s1&code_challenge={challenge}&code_challenge_method=S256");
        await Assert.That(authResp.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        var location = authResp.Headers.Location!.ToString();
        var code = System.Web.HttpUtility.ParseQueryString(new Uri(location).Query)["code"]!;

        var tokenResp = await http.PostAsync($"{mock.BaseUrl}/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = "https://app/callback",
                ["code_verifier"] = verifier,
            }));
        await Assert.That(tokenResp.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Refresh_Flow_Issues_New_Token()
    {
        await using var mock = await StartMockAsync();
        using var http = new HttpClient();

        var first = await http.PostAsync($"{mock.BaseUrl}/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "svc-2",
            }));
        var refresh = JsonDocument.Parse(await first.Content.ReadAsStringAsync()).RootElement.GetProperty("refresh_token").GetString()!;

        var second = await http.PostAsync($"{mock.BaseUrl}/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refresh,
            }));
        await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Wrong_Pkce_Verifier_Is_Rejected()
    {
        await using var mock = await StartMockAsync();
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        var verifier = "ok-verifier-0123456789ABCDEF0123456789";
        var challenge = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));

        var authResp = await http.GetAsync(
            $"{mock.BaseUrl}/authorize?client_id=webapp&redirect_uri=https%3A%2F%2Fapp%2Fcallback&code_challenge={challenge}&code_challenge_method=S256");
        var location = authResp.Headers.Location!.ToString();
        var code = System.Web.HttpUtility.ParseQueryString(new Uri(location).Query)["code"]!;

        var tokenResp = await http.PostAsync($"{mock.BaseUrl}/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = "https://app/callback",
                ["code_verifier"] = "wrong-verifier-XYZXYZXYZXYZXYZXYZ",
            }));
        await Assert.That(tokenResp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private sealed class ProtectedApi : IAsyncDisposable
    {
        private readonly IHost _host;
        public HttpClient Client { get; }
        public ProtectedApi(IHost h, HttpClient c) { _host = h; Client = c; }
        public async ValueTask DisposeAsync() { Client.Dispose(); await _host.StopAsync(); _host.Dispose(); }
    }

    private static async Task<ProtectedApi> StartProtectedApiAsync(string authority, string issuer)
    {
        var host = await Host.CreateDefaultBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(s =>
                {
                    s.AddRouting();
                    s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                     .AddJwtBearer(o =>
                     {
                         o.RequireHttpsMetadata = false;
                         o.Authority = authority;
                         o.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                         {
                             ValidateIssuer = true,
                             ValidIssuer = issuer,
                             ValidateAudience = true,
                             ValidAudience = issuer,
                             ValidateLifetime = true,
                             ValidateIssuerSigningKey = true,
                             ClockSkew = TimeSpan.FromSeconds(5),
                         };
                         o.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                             $"{authority}/.well-known/openid-configuration",
                             new OpenIdConnectConfigurationRetriever(),
                             new HttpDocumentRetriever { RequireHttps = false });
                     });
                    s.AddAuthorization();
                });
                web.Configure(app =>
                {
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.Run(async ctx =>
                    {
                        if (ctx.User.Identity?.IsAuthenticated != true)
                        {
                            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }
                        ctx.Response.StatusCode = 200;
                        await ctx.Response.WriteAsync("ok");
                    });
                });
            }).StartAsync();

        var ts = host.GetTestServer();
        return new ProtectedApi(host, ts.CreateClient());
    }
}
