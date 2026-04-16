using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Rig.TUnit.WebAPI.Authentication;

/// <summary>
/// Options that control how <see cref="TestAuthenticationHandler"/> builds the authenticated principal.
/// </summary>
public sealed class TestAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Name applied when <see cref="Claims"/> is empty.</summary>
    public string DefaultUserName { get; set; } = "test-user";

    /// <summary>Claims assigned to every authenticated request. If empty, only a <see cref="ClaimTypes.Name"/> claim is added.</summary>
    public IList<Claim> Claims { get; } = [];
}
