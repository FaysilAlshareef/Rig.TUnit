using Rig.TUnit.Core.Fakers;
using Rig.TUnit.Core.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.Core.Tests.Unit.Fakers;

public class CustomConstructorFakerTests
{
    [Test]
    public async Task Create_WithPrivateConstructor_CreatesInstance()
    {
        // Arrange
        var faker = new CustomConstructorFaker<TestEntity>();

        // Act
        var entity = faker.Generate();

        // Assert
        await Assert.That(entity).IsNotNull();
        await Assert.That(entity).IsTypeOf<TestEntity>();
    }

    [Test]
    public async Task Generate_WithPropertyRules_AppliesRules()
    {
        // Arrange
        var faker = new CustomConstructorFaker<TestEntity>();
        faker.RuleFor(e => e.Id, f => 42);
        faker.RuleFor(e => e.Name, f => "TestName");

        // Act
        var entity = faker.Generate();

        // Assert
        await Assert.That(entity.Id).IsEqualTo(42);
        await Assert.That(entity.Name).IsEqualTo("TestName");
    }

    [Test]
    public async Task Generate_CalledTwice_ProducesDistinctInstances()
    {
        // Arrange
        var faker = new CustomConstructorFaker<TestEntity>();

        // Act
        var entity1 = faker.Generate();
        var entity2 = faker.Generate();

        // Assert
        await Assert.That(entity1).IsNotNull();
        await Assert.That(entity2).IsNotNull();
        await Assert.That(entity1).IsNotSameReferenceAs(entity2);
    }
}
