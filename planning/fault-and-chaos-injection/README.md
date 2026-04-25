# Planning — Fault & chaos injection (F-009)

**Feature ID**: F-009
**Family**: Cross-cutting
**Status**: planned
**Depends on**: F-008 (deterministic clock — for time-driven faults)
**Target release**: v0.9
**Estimated tasks**: ~108 (Phase 0: 7 · 12 providers × 8 wiring tasks · 5 docs)

---

## Why this feature exists

Production failures are dominated by **partial** faults — latency spikes, half-open sockets, DNS NXDOMAIN, partial reads, broker disconnects mid-stream, replication lag, intermittent 503s. The rig today can spin a container up or take it down. There is no surface in between.

Concrete real-world bugs the rig cannot reproduce today:

- HTTP client timing out at p99 because the upstream pool is saturated (`Rig.TUnit.Http` mock returns or doesn't — no latency knob).
- gRPC deadline exceeded mid-stream (`Rig.TUnit.Grpc` has no chaos handler).
- Kafka consumer rebalance during a poll (`Rig.TUnit.Messaging.Kafka` cannot simulate).
- ServiceBus session lock expiring under load.
- SQL connection killed by network partition; `OptimisticConcurrencyException` retry loop.
- Cosmos `429 Too Many Requests` under burst write.
- S3 `503 SlowDown` during a multipart upload.
- Redis `LOADING` reply during failover.

## What we deliver

A `WithFault(Action<IFaultBuilder>)` builder method on every multi-network RigBuilder. The fault builder declares latency distributions, failure rates, partition events, slow-socket modes. Implementation strategy splits by transport:

- **Container-bound providers** (Postgres, MSSQL, Mongo, Cassandra, Cosmos emulator, ServiceBus emulator, RabbitMQ, Kafka, Redis, MinIO, Elasticsearch) → [Toxiproxy](https://github.com/Shopify/toxiproxy) sidecar in front of the container.
- **In-process clients** (HTTP, gRPC) → `DelegatingHandler` / `Interceptor` chain.
- **Service-Bus / SQS / Cosmos SDK emulators** → mix: SDK pipeline policies for retryable codes, Toxiproxy for raw socket faults.

## Public API surface (sketch)

```csharp
public interface IFaultBuilder
{
    IFaultBuilder Latency(TimeSpan p50, TimeSpan p99, TimeSpan jitter);
    IFaultBuilder FailureRate(double fraction, FaultMode mode);
    IFaultBuilder PartitionAfter(int messages);
    IFaultBuilder DropConnectionsAt(double probability);
    IFaultBuilder SlowSocket(int bytesPerSecond);
}

public enum FaultMode { ConnectionReset, Timeout, Status503, DnsFailure }
```

## Gaps closed (from CC-2 in the gap analysis)

- HTTP latency / timeout / pool-saturation tests.
- gRPC deadline propagation, mid-stream cancel.
- Messaging broker disconnect / session-lock loss / consumer rebalance.
- SQL connection-killer / replication-lag / deadlock-victim simulation.
- NoSQL throttling (`429`), eventual-consistency window, retry storm.
- Storage `5xx SlowDown`, partial multipart upload, presigned URL expiry mid-flight.
- Cache `LOADING` reply, Redis cluster failover, ServiceBus emulator restart.

## Providers in scope (wiring)

| Package | Strategy |
|---------|----------|
| `src/Rig.TUnit.Http` | DelegatingHandler chain |
| `src/Rig.TUnit.Grpc` | client + server `Interceptor` |
| `src/Rig.TUnit.Messaging.Kafka|RabbitMq|Nats|ServiceBus|Sqs` | Toxiproxy sidecar (5) |
| `src/Rig.TUnit.Databases.Sql.SqlServer|Postgresql|MySql|Oracle` | Toxiproxy sidecar (4) |
| `src/Rig.TUnit.Databases.NoSql.Mongo|Cassandra|Cosmos|ElasticSearch|Redis|KurrentDb|Dynamo` | Toxiproxy where TCP, SDK pipeline where HTTP (7) |
| `src/Rig.TUnit.Caching.Redis|Hybrid|Fusion` | StackExchange.Redis profile + Toxiproxy |
| `src/Rig.TUnit.Storage.S3|MinIO|AzureBlob` | Toxiproxy + SDK retry-policy override (3) |

## Exit criteria

- `IFaultBuilder` and `WithFault` ship in `Rig.TUnit` base library, 100 % line coverage in introducing PR.
- Toxiproxy sidecar fixture lifecycle integrated with the existing `RigBuilder<TSelf>` start/stop.
- Each provider package has at least 3 fault scenarios as RED-leading integration tests (latency, failure-rate, partition).
- `ProviderCompletenessTests` extended with `Providers_Declare_WithFault` rule (parity coverage file).
- ≥ 90 % line / ≥ 85 % branch on every touched package.
- ADR-011 (planned) — "Toxiproxy as the rig's network-fault primitive".

## Dependencies on other planned features

- Upstream: **F-008** — fault scenarios that depend on time (`PartitionAfter(seconds)`, `LatencyJitter`) need the fake clock.
- Downstream: F-042 (HTTP streaming), F-044 (gRPC deadlines), F-046 (HealthCheck degraded paths), F-047 (resilience policies under chaos).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 009-fault-and-chaos-injection

Read first:
- planning/fault-and-chaos-injection/README.md
- planning/deterministic-clock/README.md (F-008 must be shipped first)
- https://github.com/Shopify/toxiproxy README + .NET client docs
- src/Rig.TUnit.Http/* (sample DelegatingHandler chain)
- planning/messaging-topology-and-sessions/Provider-Enhancement-Matrix.md (parity matrix style)

Generate a feature spec that:
1. Introduces IFaultBuilder + WithFault on every multi-network RigBuilder.
2. Decides per-provider strategy: Toxiproxy sidecar vs in-process handler. Justify each choice in research.md.
3. Phase 0 lands the contract + ProviderCompletenessTests parity rule (empty .fault-coverage.txt).
4. Each provider phase delivers ≥ 3 RED-leading scenarios (latency, failure-rate, partition).
5. Phase 6 ships ADR-011 documenting the Toxiproxy choice.

Constraints:
- Honour F-008's IFakeClock for any time-driven fault.
- Toxiproxy lifecycle starts/stops with the parent fixture, never leaks containers.
- Faults are opt-in per-test; default fixtures are fault-free.
- File-scoped namespaces, sealed concrete types, TUnit AAA, no real Task.Delay in tests.

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
