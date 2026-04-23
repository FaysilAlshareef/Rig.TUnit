using Azure.Messaging.ServiceBus.Administration;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;
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
        var admin = new ServiceBusAdministrationClient(Source.ConnectionString);
        var helper = new ServiceBusAdministrationHelper(admin);
        var builder = new ServiceBusTopologyBuilder(helper);
        _topologyConfig(builder);
        await builder.ApplyAsync(ct).ConfigureAwait(false);
    }
}
