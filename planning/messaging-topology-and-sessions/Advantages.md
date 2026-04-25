# Project Advantages — Sessions, Partitions & Topology Builder

**Feature**: 007
**Purpose**: Concrete outcomes this work unlocks, split by audience.

---

## 1. What it unlocks for users of the rig (application teams)

### Realistic integration tests they cannot write today

| Scenario | Blocked today because… | After Feature 007 |
|---|---|---|
| FIFO order processing with Service Bus sessions | Sender doesn't set `SessionId`; listener doesn't use `ServiceBusSessionProcessor` | One test: set `SessionKey`, use `ServiceBusSessionListener`, assert per-session order |
| Multi-partition Kafka ordering per customer ID | Topic hardcoded to 1 partition; `Key` aliased to correlation id | Set `Partitions = 6`, pass `PartitionKey = customerId`, assert ordering per key |
| Topic-exchange fan-out with RabbitMQ routing keys | No exchange support in the fixture | Declare exchange + bindings, send with routing key, assert each queue gets the right subset |
| DLQ tests (Service Bus, SQS, Rabbit) | No DLX / redrive-policy / dead-letter subscription configurable | Topology builder sets all three natively |
| Per-test subscription isolation on Service Bus | JSON seed file fixes subscription names at container boot | Admin client creates `sub-{IsolationKey}` per test |
| Compacted-topic semantics on Kafka | No config plumbing | `.WithConfig("cleanup.policy", "compact")` |
| SQS content-based deduplication | Sender doesn't send `MessageDeduplicationId`; no FIFO queue creation | `.WithFifo(contentBasedDeduplication: true)` |
| NATS durable, ordered consumers surviving reconnects | Only core NATS (fire-and-forget) is exposed | New `NatsJetStreamFixture` with ordered consumer |

### Less boilerplate in test projects

Today, every team that wants sessions must either:
1. Edit the Rig's `service-bus-config.json` (impossible in a library consumer) or
2. Maintain their own fixture and forgo the Rig entirely.

After Feature 007:

```csharp
var rig = ServiceBusRig
    .ForTest()
    .WithTopology(t => t
        .Topic("orders")
        .Subscription("orders", "shipping", s => s.WithRequiresSession())
        .Queue("orders-dlq"))
    .Build();
```

Three lines. No JSON. No `ServiceBusAdministrationClient` ceremony in test code.

### Cross-provider skill transfer

`SendContext.SessionKey` means the same thing whether the test targets Service Bus
(`SessionId`), SQS (`MessageGroupId`), or Kafka (`Message.Key`). A developer who
learned the pattern on one provider writes the same code shape for any other.

---

## 2. What it unlocks for the rig itself (library maintainers)

### Coverage parity across providers

The provider-completeness architecture test currently asserts that every provider has a
Fixture, Options, Sender, Listener, and Builder. After Feature 007 we extend it to
assert every provider has a `TopologyBuilder` and a `SessionKey`-aware sender. Any new
provider added in the future (Feature 008+: Pulsar, MQTT, Google Pub/Sub) must match the
same shape — no silent drift.

### Shared-fixture isolation finally becomes feasible

Post-005 Phase 1 identified shared-fixture bleed between tests as the main source of
Service Bus integration flakes. The fix (per-test subscriptions) was parked because the
JSON seed blocked runtime creation. Feature 007 unblocks the fix:

- `IsolationKey` prefix + admin-client subscription creation → zero-bleed parallel
  Service Bus tests.
- Same pattern works on every provider (topic name, queue name, stream name).

### Fewer "emulator quirks" to document

The `Azure.Messaging.ServiceBus` 7.20.1 release promotes the emulator from
"config-file-only sandbox" to "real broker with admin surface". The rig was written for
the pre-7.20 era; adopting the new surface removes three documented quirks from
`planning/post-004-remediation/CI-Postgres-Flake-RCA.md`-style RCA docs.

### Benchmarks get richer

- Session processor vs non-session processor throughput on the same namespace.
- Kafka partition count vs per-key ordering latency.
- SQS FIFO vs standard throughput gap.

These are the numbers users actually need when sizing their production topology;
they currently cannot be measured in the rig.

---

## 3. What it unlocks as a competitive differentiator

There is no other TUnit-first integration rig in .NET that gives developers one fluent
API for topology across five brokers. The closest alternatives are:

| Alternative | What it gives | What it doesn't |
|---|---|---|
| Testcontainers directly | Container lifecycle per broker | No sender/listener/assertion stack; no topology abstraction |
| Provider SDKs directly | Full API | No fixture glue, no cross-provider assertion model, hand-rolled ordering tests |
| MassTransit TestHarness | Bus-level abstraction | Locks test shape to MassTransit; no raw broker access |
| Aspire | Orchestration | Not a test rig; no assertion or ordering primitives |

Feature 007 is what makes the rig the obvious default for broker-agnostic integration
testing — not just "five fixtures in one box", but a single programming model for the
two hardest problems in production messaging (ordering and topology).

---

## 4. Effort vs value

| Outcome | Effort | Value |
|---|---|---|
| FIFO + partition tests on all 5 providers | 🔴 ~12.5 d serial / ~7 d parallel | 🟢 High — unblocks a category of tests today impossible to write |
| Per-test topology isolation | 🟡 part of Phase 1 | 🟢 High — fixes the #1 Service Bus flake source |
| `SendContext` cross-provider API | 🟢 ~1 d (Phase 0) | 🟢 High — one mental model, every broker |
| Rich per-provider topology (DLX, rules, compaction) | 🔴 per-provider | 🟡 Medium — needed by power users, nice-to-have for most |
| NATS JetStream | 🔴 ~2.5 d | 🟡 Medium — opt-in; doesn't touch core NATS users |

The feature is **net-positive even if NATS JetStream (most expensive phase) is deferred
to a follow-up release.** Phases 0–4 deliver the majority of value in ~8 days of serial
work; Phase 5 can ship in a point release once validated.

---

## 5. Risk-adjusted recommendation

- **Ship Phases 0–1–2–3 as a minor version** — cross-cutting abstractions +
  Service Bus + Kafka + SQS. ~7 days serial. Covers the user's primary ask and the
  three providers where the change is both mechanical and high-leverage.
- **Ship Phase 4 (Rabbit)** in the same release if the exchange-DLX design review lands
  cleanly.
- **Defer Phase 5 (NATS JetStream)** to a follow-up minor release. It's a new fixture
  variant, not a change to existing behaviour, so it ships independently without
  coordination cost.
- **Ship Phase 6 (docs + benchmarks)** pinned to whichever release contains the last
  provider in scope.
