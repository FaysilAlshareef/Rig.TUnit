using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Security.Policies;
using Rig.TUnit.Security.Policies.Fixtures;
using Rig.TUnit.Security.Policies.Options;

namespace Rig.TUnit.Security.Policies.Tests.Unit;

public sealed class PolicyAssertTests
{
    private static async Task<PolicyFixture> CreateAdminPolicyFixtureAsync()
    {
        var fx = new PolicyFixture(
            new PolicyFixtureOptions(),
            policies => policies.AddPolicy("AdminOnly", p => p.RequireRole("admin")));
        await fx.InitializeAsync();
        return fx;
    }

    // ─── PolicyAssert.Allows ─────────────────────────────────────────────────

    [Test]
    public async Task Allows_NullPrincipal_ThrowsArgumentNullException()
    {
        await using var fx = await CreateAdminPolicyFixtureAsync();
        var assert = PolicyAssert.Policy(fx.Services, "AdminOnly");

        await Assert.That(async () => await assert.Allows(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Allows_WithAllowedPrincipal_DoesNotThrow()
    {
        await using var fx = await CreateAdminPolicyFixtureAsync();
        var admin = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "admin")], "Test"));

        var assert = PolicyAssert.Policy(fx.Services, "AdminOnly");
        await assert.Allows(admin);
    }

    [Test]
    public async Task Allows_WithDeniedPrincipal_ThrowsPolicyAssertionException()
    {
        await using var fx = await CreateAdminPolicyFixtureAsync();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "user")], "Test"));

        var assert = PolicyAssert.Policy(fx.Services, "AdminOnly");

        await Assert.That(async () => await assert.Allows(user))
            .ThrowsExactly<PolicyAssertionException>();
    }

    // ─── PolicyAssert.Denies ─────────────────────────────────────────────────

    [Test]
    public async Task Denies_NullPrincipal_ThrowsArgumentNullException()
    {
        await using var fx = await CreateAdminPolicyFixtureAsync();
        var assert = PolicyAssert.Policy(fx.Services, "AdminOnly");

        await Assert.That(async () => await assert.Denies(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Denies_WithDeniedPrincipal_DoesNotThrow()
    {
        await using var fx = await CreateAdminPolicyFixtureAsync();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "user")], "Test"));

        var assert = PolicyAssert.Policy(fx.Services, "AdminOnly");
        await assert.Denies(user);
    }

    [Test]
    public async Task Denies_WithAllowedPrincipal_ThrowsPolicyAssertionException()
    {
        await using var fx = await CreateAdminPolicyFixtureAsync();
        var admin = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "admin")], "Test"));

        var assert = PolicyAssert.Policy(fx.Services, "AdminOnly");

        await Assert.That(async () => await assert.Denies(admin))
            .ThrowsExactly<PolicyAssertionException>();
    }
}
