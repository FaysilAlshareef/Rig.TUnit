using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Rig.TUnit.Messaging.ServiceBus.Topology;

/// <summary>
/// Idempotent wrapper around <see cref="ServiceBusAdministrationClient"/> that creates
/// topics, subscriptions, queues, and rules only when they do not already exist.
/// </summary>
public sealed class ServiceBusAdministrationHelper
{
    private readonly ServiceBusAdministrationClient _admin;

    public ServiceBusAdministrationHelper(ServiceBusAdministrationClient admin)
    {
        _admin = admin ?? throw new ArgumentNullException(nameof(admin));
    }

    // Check-then-create has a race when multiple test methods run in parallel
    // against the same shared fixture: both probe ExistsAsync at the same time,
    // both get false, both POST, second gets HTTP 409 / MessagingEntityAlreadyExists
    // / SubCode=40900. Catching this on the create side closes the race without
    // serialising callers — same intent as KafkaListener catching
    // CreateTopicsException(TopicAlreadyExists) and NatsTopologyBuilder catching
    // JSStreamNameExistErr (10058). Filtered to the specific error code so
    // genuine conflicts (e.g. different shape on an existing entity) still
    // propagate.

    public async Task CreateTopicIfNotExistsAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (await _admin.TopicExistsAsync(name, ct).ConfigureAwait(false)) return;
        try
        {
            await _admin.CreateTopicAsync(name, ct).ConfigureAwait(false);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
        {
        }
    }

    public async Task CreateTopicIfNotExistsAsync(
        string name,
        CreateTopicOptions options,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);
        if (await _admin.TopicExistsAsync(name, ct).ConfigureAwait(false)) return;
        try
        {
            await _admin.CreateTopicAsync(options, ct).ConfigureAwait(false);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
        {
        }
    }

    public Task CreateSubscriptionIfNotExistsAsync(
        string topic,
        string subscription,
        CancellationToken ct = default)
        => CreateSubscriptionIfNotExistsAsync(topic, subscription, requiresSession: false, ct);

    public async Task CreateSubscriptionIfNotExistsAsync(
        string topic,
        string subscription,
        bool requiresSession,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscription);
        if (await _admin.SubscriptionExistsAsync(topic, subscription, ct).ConfigureAwait(false)) return;
        var options = new CreateSubscriptionOptions(topic, subscription)
        {
            RequiresSession = requiresSession
        };
        try
        {
            await _admin.CreateSubscriptionAsync(options, ct).ConfigureAwait(false);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
        {
        }
    }

    public async Task CreateSubscriptionWithRuleAsync(
        string topic,
        string subscription,
        string ruleName,
        SqlRuleFilter filter,
        bool requiresSession = false,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscription);
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleName);
        ArgumentNullException.ThrowIfNull(filter);

        if (!await _admin.SubscriptionExistsAsync(topic, subscription, ct).ConfigureAwait(false))
        {
            var subOptions = new CreateSubscriptionOptions(topic, subscription)
            {
                RequiresSession = requiresSession
            };
            var ruleOptions = new CreateRuleOptions(ruleName, filter);
            await _admin.CreateSubscriptionAsync(subOptions, ruleOptions, ct).ConfigureAwait(false);
        }
        else
        {
            var existing = _admin.GetRulesAsync(topic, subscription, ct);
            var ruleExists = false;
            await foreach (var r in existing)
            {
                if (r.Name == ruleName) { ruleExists = true; break; }
            }
            if (!ruleExists)
                await _admin.CreateRuleAsync(topic, subscription, new CreateRuleOptions(ruleName, filter), ct).ConfigureAwait(false);
        }
    }

    public async Task CreateQueueIfNotExistsAsync(
        string name,
        CreateQueueOptions options,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);
        if (!await _admin.QueueExistsAsync(name, ct).ConfigureAwait(false))
            await _admin.CreateQueueAsync(options, ct).ConfigureAwait(false);
    }
}
