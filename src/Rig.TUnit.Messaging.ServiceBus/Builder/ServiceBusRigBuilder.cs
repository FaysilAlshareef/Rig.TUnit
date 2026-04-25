using Azure.Messaging.ServiceBus.Administration;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;
using Rig.TUnit.Messaging.ServiceBus.Fixtures;
using Rig.TUnit.Messaging.ServiceBus.Topology;

namespace Rig.TUnit.Messaging.ServiceBus.Builder;

public sealed class ServiceBusRigBuilder : MessagingRigBuilder<ServiceBusRigBuilder>
{
    private Action<IServiceBusTopologyBuilder>? _topologyConfig;

    public ServiceBusRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;

    public ServiceBusRigBuilder WithTopology(Action<IServiceBusTopologyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _topologyConfig = configure;
        return this;
    }

    public async Task ApplyTopologyAsync(CancellationToken ct = default)
    {
        if (_topologyConfig is null) return;
        // The emulator exposes the management API on a separate HTTP port (5300),
        // distinct from the AMQP port carried by Source.ConnectionString. When the
        // source is a ServiceBusFixture we use its dedicated AdminConnectionString;
        // for real Azure (or any other source) the SAS connection string carries
        // both endpoints.
        var adminConnStr = Source is ServiceBusFixture sbFixture
            ? sbFixture.AdminConnectionString
            : Source.ConnectionString;
        var admin = new ServiceBusAdministrationClient(adminConnStr);
        var helper = new ServiceBusAdministrationHelper(admin);
        var builder = new ServiceBusTopologyBuilder(helper);
        _topologyConfig(builder);
        await builder.ApplyAsync(ct).ConfigureAwait(false);
    }
}
