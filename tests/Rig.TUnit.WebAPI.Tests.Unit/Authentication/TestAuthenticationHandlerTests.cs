using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Rig.TUnit.WebAPI.Authentication;

namespace Rig.TUnit.WebAPI.Tests.Unit.Authentication;

public sealed class TestAuthenticationHandlerTests
{
    [Test]
    public async Task SchemeName_IsTest()
    {
        var name = TestAuthenticationHandler.SchemeName;
        await Assert.That(name).IsEqualTo("Test");
    }

    [Test]
    public async Task AuthenticateAsync_NoConfiguredClaims_UsesDefaultUserNameClaim()
    {
        // Arrange
        var handler = await CreateHandlerAsync(new TestAuthenticationOptions());

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Principal).IsNotNull();
        await Assert.That(result.Principal!.Identity!.IsAuthenticated).IsTrue();
        await Assert.That(result.Principal.Identity.Name).IsEqualTo("test-user");
        await Assert.That(result.Principal.Identity.AuthenticationType).IsEqualTo("Test");
    }

    [Test]
    public async Task AuthenticateAsync_CustomDefaultUserName_IsAppliedWhenClaimsEmpty()
    {
        // Arrange
        var options = new TestAuthenticationOptions { DefaultUserName = "bob" };
        var handler = await CreateHandlerAsync(options);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Principal!.Identity!.Name).IsEqualTo("bob");
    }

    [Test]
    public async Task AuthenticateAsync_WithConfiguredClaims_AppliesThem()
    {
        // Arrange
        var options = new TestAuthenticationOptions();
        options.Claims.Add(new Claim(ClaimTypes.Name, "alice"));
        options.Claims.Add(new Claim(ClaimTypes.Role, "admin"));
        options.Claims.Add(new Claim("tenant", "acme"));
        var handler = await CreateHandlerAsync(options);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        await Assert.That(result.Succeeded).IsTrue();
        var principal = result.Principal!;
        await Assert.That(principal.Identity!.Name).IsEqualTo("alice");
        await Assert.That(principal.IsInRole("admin")).IsTrue();
        await Assert.That(principal.FindFirst("tenant")?.Value).IsEqualTo("acme");
    }

    [Test]
    public async Task AuthenticateAsync_WithConfiguredClaims_DoesNotInjectDefaultName()
    {
        // Arrange — claims supplied but no Name claim.
        var options = new TestAuthenticationOptions();
        options.Claims.Add(new Claim(ClaimTypes.Role, "reader"));
        var handler = await CreateHandlerAsync(options);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Principal!.Identity!.Name).IsNull();
    }

    private static async Task<TestAuthenticationHandler> CreateHandlerAsync(TestAuthenticationOptions options)
    {
        var optionsMonitor = new StaticOptionsMonitor(options);
        var handler = new TestAuthenticationHandler(optionsMonitor, NullLoggerFactory.Instance, UrlEncoder.Default);
        var scheme = new AuthenticationScheme(
            TestAuthenticationHandler.SchemeName,
            displayName: null,
            handlerType: typeof(TestAuthenticationHandler));
        await handler.InitializeAsync(scheme, new DefaultHttpContext());
        return handler;
    }

    private sealed class StaticOptionsMonitor(TestAuthenticationOptions options)
        : IOptionsMonitor<TestAuthenticationOptions>
    {
        public TestAuthenticationOptions CurrentValue { get; } = options;

        public TestAuthenticationOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<TestAuthenticationOptions, string?> listener) => null;
    }
}
