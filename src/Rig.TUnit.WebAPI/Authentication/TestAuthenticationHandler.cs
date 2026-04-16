using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Rig.TUnit.WebAPI.Authentication;

/// <summary>
/// Authentication handler that authenticates every request unconditionally using a configurable set of claims.
/// Incoming <c>Authorization</c> headers are ignored — authentication succeeds even when no header is present.
/// Use in tests to bypass the real authentication pipeline without changing production code paths.
/// </summary>
public sealed class TestAuthenticationHandler(
    IOptionsMonitor<TestAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthenticationOptions>(options, logger, encoder)
{
    /// <summary>The default scheme name used by <see cref="TestAuthenticationHandler"/>.</summary>
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var opts = Options;
        var claims = opts.Claims.Count > 0
            ? opts.Claims
            : new List<Claim> { new(ClaimTypes.Name, opts.DefaultUserName) };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
