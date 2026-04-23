# Planning — Messaging Topology & Sessions

**Scope**: Feature 007 — add first-class support for ordered-delivery semantics
(sessions / partition keys / message groups) and a unified topology-builder abstraction
(create topics, subscriptions, exchanges, streams, queues at runtime from code) across
every messaging provider in the ecosystem.

Proposed branch: `feat/007-messaging-topology-sessions`

---

## Why this feature exists

Two real gaps block a class of production-grade messaging tests today:

1. **No ordered-delivery primitives.** The base library ships `OrderingAssert.PerKeyMonotonic`
   (see [OrderingAssert.cs](../../src/Rig.TUnit.Messaging/Assertions/OrderingAssert.cs)) but the
   concrete providers do not expose the knobs required to actually produce per-key ordered
   streams:
   - ServiceBus sender never sets `SessionId` or `PartitionKey`; the listener uses
     `ServiceBusProcessor`, not `ServiceBusSessionProcessor`.
   - Kafka conflates `correlationId` and `Message.Key` ([KafkaEventSender.cs:34](../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs:34))
     and hardcodes a single partition, so cross-partition ordering can never be tested.
   - SQS fixture targets standard queues only — no `MessageGroupId`, no FIFO creation.
   - NATS uses core pub/sub, not JetStream — no durable, ordered consumer exists.

2. **Topology is either declarative-only or too thin.**
   - ServiceBus depends on a static `service-bus-config.json` seed file; topology cannot
     change at runtime.
   - RabbitMQ fixture only calls `QueueDeclareAsync` — no exchange, no bindings, no DLX.
   - SQS / NATS have no topology creation helpers at all.
   - Kafka creates topics with `NumPartitions = 1` and no configs (retention, compaction).

   The SDK-level creation APIs exist for every provider. We are not using them.

The `Azure.Messaging.ServiceBus` v7.20.1 release (Sep 2025) added
`ServiceBusAdministrationClient` support against the emulator, so runtime topology is finally
possible on the one provider where it was previously blocked.

---

## What we deliver

- A `PartitionKey` / `SessionKey` concept on `EventSenderBase`, propagated to each provider's
  native equivalent (Service Bus `SessionId`, Kafka `Message.Key`, SQS `MessageGroupId`,
  RabbitMQ `routing-key`, NATS JetStream `Subject`).
- A session-aware listener variant per provider that supports it natively.
- A common `TopologyBuilder` abstraction on every `{Provider}RigBuilder` — one fluent API that
  maps to each SDK's creation surface (topics, subscriptions, exchanges, bindings, streams,
  FIFO queues, DLQs).
- Integration tests that exercise per-key ordering end-to-end via `OrderingAssert.PerKeyMonotonic`.

---

## File index

This folder holds **design inputs** for Feature 007 only. The authoritative feature specification — FRs, NFRs, task list, coverage plan, clarifications, open questions — lives in the SDD feature folder:

- [Feature 007 spec (authoritative)](../../.dotnet-ai-kit/features/007-messaging-topology-sessions/spec.md)
- [Requirements checklist](../../.dotnet-ai-kit/features/007-messaging-topology-sessions/checklists/requirements.md)

| File | Purpose |
|------|---------|
| [README.md](README.md) | This index |
| [Feature-007-Roadmap.md](Feature-007-Roadmap.md) | Phased delivery plan — tasks, FR refs, effort table, exit gates |
| [Sessions-And-Partitions-Design.md](Sessions-And-Partitions-Design.md) | Technical design: sender/listener extensions for ordered delivery across all 5 providers |
| [Topology-Builder-Design.md](Topology-Builder-Design.md) | Technical design: unified `TopologyBuilder` API and per-provider mappings |
| [Provider-Enhancement-Matrix.md](Provider-Enhancement-Matrix.md) | Gap table + per-provider change list + effort per cell |
| [Advantages.md](Advantages.md) | What the feature unlocks — for library users, for the library itself, and as a competitive differentiator |

---

## Order of execution

1. **Sessions-And-Partitions-Design.md** — agree the shape of the cross-provider
   `SessionKey`/`PartitionKey` abstraction before any provider work starts.
2. **Topology-Builder-Design.md** — agree the fluent API and the per-provider mapping table.
3. **Feature-007-Roadmap.md** — execute per phase; each phase is one provider end-to-end.
4. **Provider-Enhancement-Matrix.md** — ground-truth backlog kept in sync as phases complete.

---

## Dependencies / prerequisites

- Feature 006 coverage gates must be green before any provider in scope is modified — we do not
  want to regress a package below the ≥ 90 % line / ≥ 85 % branch gate while adding surface.
- `Azure.Messaging.ServiceBus` ≥ 7.20.1 on the ServiceBus package (current: check
  [Directory.Packages.props](../../Directory.Packages.props)).
- New dependency: `NATS.Client.JetStream` (only for the NATS provider when that phase starts).

---

## Related planning folders

- `planning/fluent-builder-expansion/` — the `{Provider}RigBuilder` pattern we extend here.
- `planning/ecosystem-expansion/` — multi-provider strategy feature 003; context on provider
  parity.
- `planning/post-005-phase-1/` — shared-fixture isolation; topology-per-test builds on this.
