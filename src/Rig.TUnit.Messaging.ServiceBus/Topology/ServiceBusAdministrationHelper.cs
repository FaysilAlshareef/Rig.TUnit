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

    public async Task CreateTopicIfNotExistsAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!await _admin.TopicExistsAsync(name, ct).ConfigureAwait(false))
            await _admin.CreateTopicAsync(name, ct).ConfigureAwait(false);
    }

    public async Task CreateTopicIfNotExistsAsync(
        string name,
        CreateTopicOptions options,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);
        if (!await _admin.TopicExistsAsync(name, ct).ConfigureAwait(false))
            await _admin.CreateTopicAsync(options, ct).ConfigureAwait(false);
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
        if (!await _admin.SubscriptionExistsAsync(topic, subscription, ct).ConfigureAwait(false))
        {
            var options = new CreateSubscriptionOptions(topic, subscription)
            {
                RequiresSession = requiresSession
            };
            await _admin.CreateSubscriptionAsync(options, ct).ConfigureAwait(false);
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
