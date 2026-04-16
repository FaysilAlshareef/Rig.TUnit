using System.Security.Claims;
using Rig.TUnit.WebAPI.Authentication;

namespace Rig.TUnit.WebAPI.Tests.Unit.Authentication;

public sealed class TestAuthenticationOptionsTests
{
    [Test]
    public async Task DefaultUserName_WhenConstructed_IsTestUser()
    {
        // Arrange / Act
        var options = new TestAuthenticationOptions();

        // Assert
        await Assert.That(options.DefaultUserName).IsEqualTo("test-user");
    }

    [Test]
    public async Task Claims_WhenConstructed_IsEmpty()
    {
        // Arrange / Act
        var options = new TestAuthenticationOptions();

        // Assert
        await Assert.That(options.Claims).IsNotNull();
        await Assert.That(options.Claims.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Claims_CanBeMutated_AfterConstruction()
    {
        // Arrange
        var options = new TestAuthenticationOptions();

        // Act
        options.Claims.Add(new Claim(ClaimTypes.Name, "alice"));
        options.Claims.Add(new Claim(ClaimTypes.Role, "admin"));

        // Assert
        await Assert.That(options.Claims.Count).IsEqualTo(2);
        await Assert.That(options.Claims[0].Type).IsEqualTo(ClaimTypes.Name);
        await Assert.That(options.Claims[0].Value).IsEqualTo("alice");
        await Assert.That(options.Claims[1].Type).IsEqualTo(ClaimTypes.Role);
        await Assert.That(options.Claims[1].Value).IsEqualTo("admin");
    }

    [Test]
    public async Task DefaultUserName_CanBeOverridden()
    {
        // Arrange
        var options = new TestAuthenticationOptions
        {
            DefaultUserName = "custom-user"
        };

        // Assert
        await Assert.That(options.DefaultUserName).IsEqualTo("custom-user");
    }
}
