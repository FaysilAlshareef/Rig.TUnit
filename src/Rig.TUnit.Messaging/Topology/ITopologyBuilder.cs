namespace Rig.TUnit.Messaging.Topology;

/// <summary>
/// Marker / application hook implemented by every provider-specific topology builder.
/// Per C-003 the base contract carries no fluent methods — those live on the
/// provider-specific sub-interface (<c>IServiceBusTopologyBuilder</c>,
/// <c>IKafkaTopologyBuilder</c>, <c>ISqsTopologyBuilder</c>,
/// <c>IRabbitMqTopologyBuilder</c>, <c>INatsTopologyBuilder</c>).
/// </summary>
/// <remarks>
/// The regression guard for this contract is
/// <c>ITopologyBuilderContractTests.ITopologyBuilder_DeclaresOnlyApplyAsync</c>. If a
/// generic fluent verb is added here, every provider gains a runtime-failing method it
/// cannot meaningfully implement.
/// </remarks>
public interface ITopologyBuilder
{
    /// <summary>
    /// Applies every declaration recorded on the provider-specific builder to the broker,
    /// idempotently (create-if-not-exists). Called by the rig's <c>BuildAsync</c> pipeline.
    /// </summary>
    /// <param name="ct">Cancellation token for the broker admin calls.</param>
    Task ApplyAsync(CancellationToken ct);
}
