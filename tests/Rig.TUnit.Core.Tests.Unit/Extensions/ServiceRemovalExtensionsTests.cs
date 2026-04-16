using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Extensions;

namespace Rig.TUnit.Core.Tests.Unit.Extensions;

public class ServiceRemovalExtensionsTests
{
    [Test]
    public async Task RemoveService_WhenServiceExists_RemovesRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDisposable>(_ => null!);

        // Act
        services.RemoveService<IDisposable>();

        // Assert
        await Assert.That(services.Any(d => d.ServiceType == typeof(IDisposable))).IsFalse();
    }

    [Test]
    public async Task RemoveService_WhenServiceMissing_ReturnsUnchanged()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ICloneable>(_ => null!);
        var countBefore = services.Count;

        // Act
        services.RemoveService<IDisposable>();

        // Assert
        await Assert.That(services.Count).IsEqualTo(countBefore);
    }

    [Test]
    public async Task RemoveService_Always_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.RemoveService<IDisposable>();

        // Assert
        await Assert.That(result).IsSameReferenceAs(services);
    }

    [Test]
    public async Task RemoveImplementation_WhenExists_RemovesDescriptor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IFormatProvider, System.Globalization.CultureInfo>();

        // Act
        services.RemoveImplementation<System.Globalization.CultureInfo>();

        // Assert
        await Assert.That(services.Any(d => d.ImplementationType == typeof(System.Globalization.CultureInfo))).IsFalse();
    }

    [Test]
    public async Task RemoveImplementation_WhenMissing_ReturnsUnchanged()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ICloneable>(_ => null!);
        var countBefore = services.Count;

        // Act
        services.RemoveImplementation<System.Globalization.CultureInfo>();

        // Assert
        await Assert.That(services.Count).IsEqualTo(countBefore);
    }

    [Test]
    public async Task RemoveByName_WhenMultipleMatch_RemovesAll()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDisposable>(_ => null!);
        services.AddScoped<IAsyncDisposable>(_ => null!);

        // Act
        services.RemoveByName("Disposable");

        // Assert
        await Assert.That(services.Any(d => d.ServiceType.FullName?.Contains("Disposable") == true)).IsFalse();
    }

    [Test]
    public async Task RemoveByName_WhenNoneMatch_ReturnsUnchanged()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ICloneable>(_ => null!);
        var countBefore = services.Count;

        // Act
        services.RemoveByName("NonExistentService");

        // Assert
        await Assert.That(services.Count).IsEqualTo(countBefore);
    }
}
