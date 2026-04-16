using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Core.Tests.Unit.Builder;

public class RigBuilderTests
{
    [Test]
    public async Task AddRigTUnit_InvokesConfigure_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configured = false;

        // Act
        var result = services.AddRigTUnit(rig =>
        {
            configured = true;
        });

        // Assert
        await Assert.That(configured).IsTrue();
        await Assert.That(result).IsSameReferenceAs(services);
    }

    [Test]
    public async Task ForceContainersInCi_SetsFlag()
    {
        // Arrange
        var services = new ServiceCollection();
        RigBuilder? capturedBuilder = null;

        // Act
        services.AddRigTUnit(rig =>
        {
            rig.ForceContainersInCi();
            capturedBuilder = rig;
        });

        // Assert
        await Assert.That(capturedBuilder).IsNotNull();
        await Assert.That(capturedBuilder!.IsForceContainersInCi).IsTrue();
    }

    [Test]
    public async Task Services_ExposesServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        IServiceCollection? capturedServices = null;

        // Act
        services.AddRigTUnit(rig =>
        {
            capturedServices = rig.Services;
        });

        // Assert
        await Assert.That(capturedServices).IsSameReferenceAs(services);
    }
}
