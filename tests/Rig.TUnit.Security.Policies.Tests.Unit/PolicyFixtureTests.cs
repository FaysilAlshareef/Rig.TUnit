using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rig.TUnit.Security.Policies.Fixtures;
using Rig.TUnit.Security.Policies.Options;

namespace Rig.TUnit.Security.Policies.Tests.Unit;

public sealed class PolicyFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_UsesDefaultOptions()
    {
        await using var fx = new PolicyFixture();
        await fx.InitializeAsync();
        await Assert.That(fx.ConnectionString).IsEqualTo("Test");
    }

    [Test]
    public async Task Ctor_NullOptions_Throws()
    {
        await Assert.That(() => new PolicyFixture((PolicyFixtureOptions)null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_NullIOptions_Throws()
    {
        await Assert.That(() => new PolicyFixture((IOptions<PolicyFixtureOptions>)null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Services_BeforeInitialize_Throws()
    {
        await using var fx = new PolicyFixture();
        await Assert.That(() => fx.Services).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task InitializeAsync_RegistersAuthorizationService()
    {
        await using var fx = new PolicyFixture();
        await fx.InitializeAsync();
        var auth = fx.Services.GetService<IAuthorizationService>();
        await Assert.That(auth).IsNotNull();
    }

    [Test]
    public async Task InitializeAsync_WithConfigurePolicies_RegistersPolicy()
    {
        await using var fx = new PolicyFixture(
            new PolicyFixtureOptions(),
            policies => policies.AddPolicy("AdminOnly", p => p.RequireRole("admin")));
        await fx.InitializeAsync();

        var admin = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }, "Test"));
        var auth = fx.Services.GetRequiredService<IAuthorizationService>();
        var result = await auth.AuthorizeAsync(admin, resource: null, "AdminOnly");
        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task InitializeAsync_WithConfigurePolicies_DeniesNonMember()
    {
        await using var fx = new PolicyFixture(
            new PolicyFixtureOptions(),
            policies => policies.AddPolicy("AdminOnly", p => p.RequireRole("admin")));
        await fx.InitializeAsync();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "user") }, "Test"));
        var auth = fx.Services.GetRequiredService<IAuthorizationService>();
        var result = await auth.AuthorizeAsync(user, resource: null, "AdminOnly");
        await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    public async Task DisposeAsync_ReleasesProvider_SubsequentAccessThrows()
    {
        var fx = new PolicyFixture();
        await fx.InitializeAsync();
        await fx.DisposeAsync();
        await Assert.That(() => fx.Services).ThrowsExactly<InvalidOperationException>();
    }
}
