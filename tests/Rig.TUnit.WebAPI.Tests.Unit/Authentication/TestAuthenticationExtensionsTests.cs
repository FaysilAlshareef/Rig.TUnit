using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;
using Rig.TUnit.WebAPI.Authentication;
using Rig.TUnit.WebAPI.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.WebAPI.Tests.Unit.Authentication;

public sealed class TestAuthenticationExtensionsTests
{
    [Test]
    public async Task WithTestAuthentication_NullFactory_Throws()
    {
        // Arrange
        WebApplicationFactory<TestProgram>? factory = null;

        // Act / Assert
        await Assert.That(() => factory!.WithTestAuthentication())
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task WithPermissiveAuthorization_NullFactory_Throws()
    {
        // Arrange
        WebApplicationFactory<TestProgram>? factory = null;

        // Act / Assert
        await Assert.That(() => factory!.WithPermissiveAuthorization())
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task WithTestAuthentication_NoConfig_AuthenticatesWithDefaultUserName()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory()
            .WithTestAuthentication()
            .WithPermissiveAuthorization();

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/secure/me");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TestEndpoints.EchoResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Message).IsEqualTo("test-user");
    }

    [Test]
    public async Task WithTestAuthentication_CustomClaims_AreAppliedToPrincipal()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory()
            .WithTestAuthentication(options =>
            {
                options.Claims.Add(new Claim(ClaimTypes.Name, "alice"));
                options.Claims.Add(new Claim(ClaimTypes.Role, "admin"));
            })
            .WithPermissiveAuthorization();

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/secure/me");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TestEndpoints.EchoResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Message).IsEqualTo("alice");
    }

    [Test]
    public async Task WithTestAuthentication_AnonymousEndpoint_RemainsAccessible()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory()
            .WithTestAuthentication();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/echo/anon");

        // Assert — anonymous endpoint is not gated by auth.
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
