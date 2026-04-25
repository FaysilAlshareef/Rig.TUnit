# Planning — Consumer-driven contracts (F-041)

**Feature ID**: F-041
**Family**: Microservices
**Status**: planned
**Depends on**: —
**Target release**: v0.15
**Estimated tasks**: ~24 (Phase 0: 7 · 1 package × 12 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.Microservices.Contracts` is currently thin. Real-world contract drift happens when:

- A producer adds / removes / renames a field on a published event.
- A consumer was not redeployed and now silently drops events / throws.
- A provider's API removes a route / changes a parameter type.

[Pact](https://pact.io) and similar consumer-driven contract (CDC) tools are the industry-standard answer. The rig should expose a Pact-shaped surface so contract regressions are caught **in CI**, not in production.

## What we deliver

A `WithContracts(Action<IContractRegistry>)` builder method, plus producer + consumer assertion APIs:

```csharp
public interface IContractRegistry
{
    IContractRegistry RegisterConsumer<T>(string consumerName, T contract);
    IContractRegistry RegisterProducer<T>(string producerName, T schema);
}

public static class ContractAssert
{
    public static ProducerAssertion Producer(string name);
    public static ConsumerAssertion Consumer(string name);
}

public sealed class ProducerAssertion
{
    public ProducerAssertion Schema<T>().BackwardCompatible(T consumerExpected);
    public ProducerAssertion Schema<T>().ForwardCompatible(T consumerExpected);
}

public sealed class ConsumerAssertion
{
    public ConsumerAssertion Expects<T>(T contract).WhichProducer(string name).MustHonour();
}
```

Plus a Pact-broker adapter (publish + verify) gated behind opt-in:

```csharp
public sealed class PactBrokerAdapter
{
    public Task PublishAsync(IContractRegistry registry, Uri brokerUri, CancellationToken ct);
    public Task<VerifyResult> VerifyAsync(string providerName, Uri brokerUri, CancellationToken ct);
}
```

## Gaps closed (from MS-6 in the gap analysis)

- Contract-drift detection.
- Forward / backward schema compatibility.
- Pact-style consumer-driven flow.

## Providers in scope

1: `src/Rig.TUnit.Microservices.Contracts`.

## Exit criteria

- `IContractRegistry`, `ContractAssert.Producer`, `ContractAssert.Consumer` ship with 100 % line coverage.
- ≥ 5 RED-leading scenarios (compatible add, breaking rename, breaking type-change, forward-compat add, downgrade).
- Pact-broker adapter optional and gated by separate package.
- `docs/providers/contracts.md` (new).

## Dependencies on other planned features

- Upstream: none (standalone).
- Downstream: F-040 (event-sourcing schema evolution can plug into ContractAssert).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 041-consumer-driven-contracts

Read first:
- planning/consumer-driven-contracts/README.md
- src/Rig.TUnit.Microservices.Contracts/* (current state)
- Pact spec v3 / v4

Generate a feature spec that:
1. Introduces IContractRegistry + WithContracts on RigBuilder.
2. ContractAssert.Producer / Consumer with backward / forward compat operators.
3. PactBrokerAdapter optional (separate package or extension).
4. ≥ 5 RED-leading scenarios.

Constraints:
- Pact dependency optional; core contract assertions self-contained.
- Schema compatibility handles JSON Schema + Avro + Protobuf via plug-in serializers.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
