using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Rig.TUnit.Security.Jwt.Tests.Integration;

/// <summary>
/// Minimal ASP.NET Core test host wired with <c>AddJwtBearer</c> validation.
/// Tokens are validated by the real <see cref="JwtBearerHandler"/> — no test-only
/// authentication bypass.
/// </summary>
internal sealed class JwtBearerTestServer : IAsyncDisposable
{
    public const string Issuer = "rigtunit-jwt-tests";
    public const string Audience = "rigtunit-jwt-tests";
    public static readonly byte[] SigningKeyBytes = Encoding.UTF8.GetBytes("super-secret-hs256-test-key-32bytes++");

    private readonly IHost _host;
    public HttpClient Client { get; }

    private JwtBearerTestServer(IHost host, HttpClient client)
    {
        _host = host;
        Client = client;
    }

    public static async Task<JwtBearerTestServer> StartAsync()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(s =>
                {
                    s.AddRouting();
                    s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                     .AddJwtBearer(o =>
                     {
                         o.TokenValidationParameters = new TokenValidationParameters
                         {
                             ValidateIssuer = true,
                             ValidIssuer = Issuer,
                             ValidateAudience = true,
                             ValidAudience = Audience,
                             ValidateIssuerSigningKey = true,
                             IssuerSigningKey = new SymmetricSecurityKey(SigningKeyBytes),
                             ValidateLifetime = true,
                             ClockSkew = TimeSpan.Zero,
                         };
                     });
                    s.AddAuthorization();
                });
                web.Configure(app =>
                {
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.Run(async ctx =>
                    {
                        if (!ctx.User.Identity?.IsAuthenticated ?? true)
                        {
                            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }
                        ctx.Response.StatusCode = StatusCodes.Status200OK;
                        await ctx.Response.WriteAsync(ctx.User.Identity!.Name ?? "anon");
                    });
                });
            });

        var host = await builder.StartAsync();
        var testServer = host.GetTestServer();
        var client = testServer.CreateClient();
        return new JwtBearerTestServer(host, client);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }
}
