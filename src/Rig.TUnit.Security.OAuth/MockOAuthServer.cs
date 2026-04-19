using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Rig.TUnit.Security.Jwt;
using Rig.TUnit.Security.Jwt.Options;
using Rig.TUnit.Security.OAuth.Options;

namespace Rig.TUnit.Security.OAuth;

/// <summary>
/// In-process mock OAuth 2.0 / OpenID Connect server. Exposes:
/// <c>/authorize</c>, <c>/token</c>, <c>/jwks</c>, <c>/.well-known/openid-configuration</c>.
/// Supports client-credentials, authorization-code + PKCE, refresh-token flows.
/// Signs tokens with RS256 using an ephemeral key pair generated at <see cref="StartAsync"/>.
/// </summary>
public sealed class MockOAuthServer : IAsyncDisposable
{
    private readonly MockOAuthServerOptions _options;
    private readonly RSA _rsa;
    private readonly string _kid;
    private readonly ConcurrentDictionary<string, AuthCode> _codes = new();
    private readonly ConcurrentDictionary<string, string> _refreshTokens = new();
    private IHost? _host;

    public MockOAuthServer(MockOAuthServerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _rsa = RSA.Create(2048);
        _kid = Guid.NewGuid().ToString("N");
    }

    public string BaseUrl { get; private set; } = string.Empty;
    public string Issuer => _options.Issuer;

    public async Task StartAsync()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseKestrel();
                web.UseUrls(_options.Port == 0 ? "http://127.0.0.1:0" : $"http://127.0.0.1:{_options.Port}");
                web.ConfigureServices(s => s.AddRouting());
                web.Configure(ConfigurePipeline);
            });
        _host = builder.Build();
        await _host.StartAsync();

        // Resolve the actual listening URL (Kestrel with port 0).
        var server = _host.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
        var addresses = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
        BaseUrl = addresses?.Addresses.FirstOrDefault() ?? $"http://127.0.0.1:{_options.Port}";
    }

    private void ConfigurePipeline(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(ep =>
        {
            ep.MapGet("/.well-known/openid-configuration", DiscoveryAsync);
            ep.MapGet("/jwks", JwksAsync);
            ep.MapGet("/authorize", AuthorizeAsync);
            ep.MapPost("/token", TokenAsync);
        });
    }

    // ─── Endpoints ───────────────────────────────────────────────────────────

    private async Task DiscoveryAsync(HttpContext ctx)
    {
        var doc = new
        {
            issuer = Issuer,
            authorization_endpoint = $"{BaseUrl}/authorize",
            token_endpoint = $"{BaseUrl}/token",
            jwks_uri = $"{BaseUrl}/jwks",
            response_types_supported = new[] { "code" },
            grant_types_supported = _options.SupportedFlows.Select(f => f switch
            {
                "ClientCredentials" => "client_credentials",
                "AuthorizationCode" => "authorization_code",
                "Refresh" => "refresh_token",
                _ => f,
            }).ToArray(),
            id_token_signing_alg_values_supported = new[] { "RS256" },
        };
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(doc));
    }

    private async Task JwksAsync(HttpContext ctx)
    {
        var parameters = _rsa.ExportParameters(false);
        var jwk = new
        {
            keys = new object[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    alg = "RS256",
                    kid = _kid,
                    n = Base64UrlEncoder.Encode(parameters.Modulus!),
                    e = Base64UrlEncoder.Encode(parameters.Exponent!),
                }
            }
        };
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(jwk));
    }

    private async Task AuthorizeAsync(HttpContext ctx)
    {
        var q = ctx.Request.Query;
        var clientId = q["client_id"].ToString();
        var redirectUri = q["redirect_uri"].ToString();
        var state = q["state"].ToString();
        var challenge = q["code_challenge"].ToString();
        var challengeMethod = q["code_challenge_method"].ToString();

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsync("missing client_id or redirect_uri");
            return;
        }

        var code = Guid.NewGuid().ToString("N");
        _codes[code] = new AuthCode(clientId, redirectUri, challenge, challengeMethod, DateTimeOffset.UtcNow.AddMinutes(2));

        var location = $"{redirectUri}?code={code}" + (string.IsNullOrEmpty(state) ? "" : $"&state={Uri.EscapeDataString(state)}");
        ctx.Response.Redirect(location);
    }

    private async Task TokenAsync(HttpContext ctx)
    {
        var form = await ctx.Request.ReadFormAsync();
        var grant = form["grant_type"].ToString();
        switch (grant)
        {
            case "client_credentials":
                await IssueAsync(ctx, subject: form["client_id"].ToString(), scope: form["scope"].ToString());
                break;

            case "authorization_code":
                var code = form["code"].ToString();
                if (!_codes.TryRemove(code, out var stored) || stored.Expires < DateTimeOffset.UtcNow)
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.WriteAsync("invalid_grant");
                    return;
                }
                if (!string.IsNullOrEmpty(stored.CodeChallenge))
                {
                    var verifier = form["code_verifier"].ToString();
                    if (!VerifyPkce(verifier, stored.CodeChallenge, stored.CodeChallengeMethod))
                    {
                        ctx.Response.StatusCode = 400;
                        await ctx.Response.WriteAsync("invalid_grant");
                        return;
                    }
                }
                await IssueAsync(ctx, subject: stored.ClientId, scope: "openid profile");
                break;

            case "refresh_token":
                var rt = form["refresh_token"].ToString();
                if (!_refreshTokens.TryGetValue(rt, out var subject))
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.WriteAsync("invalid_grant");
                    return;
                }
                await IssueAsync(ctx, subject: subject, scope: form["scope"].ToString());
                break;

            default:
                ctx.Response.StatusCode = 400;
                await ctx.Response.WriteAsync("unsupported_grant_type");
                break;
        }
    }

    private async Task IssueAsync(HttpContext ctx, string subject, string scope)
    {
        var token = JwtBuilder.Create(new JwtBuilderOptions { DefaultIssuer = Issuer, DefaultAudience = Issuer, DefaultLifetimeMinutes = _options.TokenLifetimeSeconds / 60 })
            .Issuer(Issuer).Audience(Issuer)
            .Subject(string.IsNullOrEmpty(subject) ? "anonymous" : subject)
            .Claim("scope", string.IsNullOrEmpty(scope) ? "" : scope)
            .WithKid(_kid)
            .ExpiresIn(TimeSpan.FromSeconds(_options.TokenLifetimeSeconds))
            .SignedWithRs256(_rsa, kid: _kid)
            .Build();

        var refresh = Guid.NewGuid().ToString("N");
        _refreshTokens[refresh] = subject;

        var body = new
        {
            access_token = token,
            token_type = "Bearer",
            expires_in = _options.TokenLifetimeSeconds,
            refresh_token = refresh,
            scope,
        };
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    private static bool VerifyPkce(string verifier, string challenge, string? method)
    {
        if (string.IsNullOrEmpty(verifier)) return false;
        method ??= string.Empty;
        if (string.Equals(method, "plain", StringComparison.OrdinalIgnoreCase))
        {
            return verifier == challenge;
        }
        // Default / S256:
        var hash = SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier));
        var computed = Base64UrlEncoder.Encode(hash);
        return computed == challenge;
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        _rsa.Dispose();
    }

    private sealed record AuthCode(string ClientId, string RedirectUri, string? CodeChallenge, string? CodeChallengeMethod, DateTimeOffset Expires);
}
