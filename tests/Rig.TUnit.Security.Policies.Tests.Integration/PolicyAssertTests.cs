using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Security.Policies;

namespace Rig.TUnit.Security.Policies.Tests.Integration;

public sealed class PolicyAssertTests
{
    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(opts =>
        {
            opts.AddPolicy("AdminOnly", p => p.RequireRole("admin"));
            opts.AddPolicy("HasEmail", p => p.RequireClaim(ClaimTypes.Email));
        });
        return services.BuildServiceProvider();
    }

    [Test]
    public async Task Policy_WithAdminRole_IsAllowed()
    {
        var sp = BuildServices();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }, "test"));
        await PolicyAssert.Policy(sp, "AdminOnly").Allows(user);
    }

    [Test]
    public async Task Policy_WithoutAdminRole_IsDenied()
    {
        var sp = BuildServices();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "user") }, "test"));
        await PolicyAssert.Policy(sp, "AdminOnly").Denies(user);
    }

    [Test]
    public async Task Policy_WithEmailClaim_IsAllowed()
    {
        var sp = BuildServices();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "a@b.co") }, "test"));
        await PolicyAssert.Policy(sp, "HasEmail").Allows(user);
    }
}
