using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Docker.Builder;

namespace Rig.TUnit.Docker.Tests.Unit;

public sealed class DockerRigBuilderTests
{
    [Test]
    public async Task DockerRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(DockerRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task DockerRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("alpine:3");
        await Assert.That(() => new DockerRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task DockerRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new DockerRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task DockerRigBuilder_Image_PassesThroughFromSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("debian:stable-slim");
        var built = new DockerRigBuilder(captured!, source);
        await Assert.That(built.Image).IsEqualTo("debian:stable-slim");
    }

    [Test]
    public async Task DockerRigBuilder_And_ReturnsRoot()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("alpine:3");
        var built = new DockerRigBuilder(captured!, source);
        await Assert.That(built.And()).IsSameReferenceAs(captured);
    }
}
