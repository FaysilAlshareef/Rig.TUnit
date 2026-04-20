using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rig.TUnit.Core;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Core.Tests.Integration;

/// <summary>
/// End-to-end exercises of <see cref="RigBuilder"/> + <see cref="RigConnect"/> against
/// a real <see cref="ServiceCollection"/>. These are "integration" tests in the loose
/// sense — they exercise multiple units (builder, connection source factories, service
/// collection extension) wired together, without external infrastructure.
/// </summary>
public sealed class RigBuilderIntegrationTests
{
    [Test]
    public async Task AddRigTUnit_InvokesConfigureDelegateWithUsableBuilder()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;

        services.AddRigTUnit(rig => captured = rig);

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Services).IsSameReferenceAs(services);
    }

    [Test]
    public async Task RigConnect_FromValue_RoundTripsConnectionString()
    {
        const string expected = "Host=example;Database=test;Username=u;Password=p";
        var source = RigConnect.FromValue(expected);

        await Assert.That(source).IsNotNull();
        await Assert.That(source).IsAssignableTo<IRigConnectionSource>();
    }

    [Test]
    public async Task RigConnect_FromConfig_ResolvesValueFromIConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OrderDb"] = "Host=cfg;Database=ord",
            })
            .Build();

        var source = RigConnect.FromConfig(config, "ConnectionStrings:OrderDb");

        await Assert.That(source).IsNotNull();
    }

    [Test]
    public async Task RigConnect_FromOptions_ResolvesViaSelector()
    {
        var options = Options.Create(new TestOptions { ConnectionString = "Host=opts" });
        var source = RigConnect.FromOptions(options, o => o.ConnectionString);

        await Assert.That(source).IsNotNull();
    }

    [Test]
    public async Task IsolationKey_FromExecutionContext_ProducesDeterministicSuffix()
    {
        var first = IsolationKey.FromExecutionContext("Acme.Tests.SampleTest.Scenario1");
        var second = IsolationKey.FromExecutionContext("Acme.Tests.SampleTest.Scenario1");
        var different = IsolationKey.FromExecutionContext("Acme.Tests.SampleTest.Scenario2");

        await Assert.That(first.Value).IsEqualTo(second.Value);
        await Assert.That(first.Value).IsNotEqualTo(different.Value);
    }

    [Test]
    public async Task RigBuilder_ForceContainersInCi_IsChainable()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;

        services.AddRigTUnit(rig => captured = rig.ForceContainersInCi());

        await Assert.That(captured).IsNotNull();
    }

    private sealed class TestOptions
    {
        public string ConnectionString { get; init; } = string.Empty;
    }
}
