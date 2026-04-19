using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Rig.TUnit.Security.Policies;

/// <summary>
/// Fluent assertion over a real <see cref="IAuthorizationService"/> — resolves
/// the named policy from DI and evaluates it against a <see cref="ClaimsPrincipal"/>.
/// No test bypass — the production requirement-handlers are exercised.
/// </summary>
public sealed class PolicyAssert
{
    private readonly IAuthorizationService _auth;
    private readonly string _policyName;

    private PolicyAssert(IAuthorizationService auth, string policyName)
    {
        _auth = auth;
        _policyName = policyName;
    }

    public static PolicyAssert Policy(IServiceProvider services, string policyName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        var auth = services.GetRequiredService<IAuthorizationService>();
        return new PolicyAssert(auth, policyName);
    }

    public async Task Allows(ClaimsPrincipal principal, object? resource = null)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var result = await _auth.AuthorizeAsync(principal, resource, _policyName);
        if (!result.Succeeded)
        {
            var failures = string.Join(", ", result.Failure?.FailureReasons?.Select(r => r.Message) ?? Array.Empty<string>());
            throw new PolicyAssertionException(
                $"Expected policy '{_policyName}' to allow principal '{principal.Identity?.Name ?? "<anon>"}' but it was denied. Reasons: [{failures}].");
        }
    }

    public async Task Denies(ClaimsPrincipal principal, object? resource = null)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var result = await _auth.AuthorizeAsync(principal, resource, _policyName);
        if (result.Succeeded)
        {
            throw new PolicyAssertionException(
                $"Expected policy '{_policyName}' to deny principal '{principal.Identity?.Name ?? "<anon>"}' but it was allowed.");
        }
    }
}

public sealed class PolicyAssertionException : Exception
{
    public PolicyAssertionException(string message) : base(message) { }
}
