# Review Report: 007 — Messaging Topology & Sessions

**Date**: 2026-04-24 | **Mode**: Generic (single-repo) | **Branch**: `feat/007-messaging-topology-sessions`
**Scope**: 69 commits · 143 files · +10,101 / −34 lines vs `master`

---

## Rig.TUnit (repo) — NEEDS FIXES (minor)

### Standards Review

#### Check 1 · Naming conventions — PASS
- Topology builders: `{Provider}TopologyBuilder` / `I{Provider}TopologyBuilder` — consistent.
- Session listeners: `ServiceBusSessionListener`, `NatsJetStreamListener` — match `{Provider}{Capability}Listener`.
- Record types use PascalCase; file names match declared types.
- Interfaces prefixed `I`; enums (`ExchangeType`, `NatsRetentionPolicy`) PascalCase singular.

#### Check 2 · Architecture boundary — PASS
- New `Topology/` folders sit inside each provider package; no cross-provider leaks.
- `NATS.Client.JetStream` package confined to `Rig.TUnit.Messaging.Nats` — guarded by `DependencyDirectionTests.NatsJetStream_ReferencedOnlyByNatsProvider` ([Rules/DependencyDirectionTests.cs](tests/Rig.TUnit.Architecture.Tests/Rules/DependencyDirectionTests.cs)).
- `ITopologyBuilder` marker kept in core `Rig.TUnit.Messaging` — no fluent methods on base (per C-003), enforced by [MessagingRigBuilderNoGenericWithTopologyTests.cs](tests/Rig.TUnit.Messaging.Tests.Unit/Builder/MessagingRigBuilderNoGenericWithTopologyTests.cs).
- `ProviderCompletenessTests` parity gate is active and extensible via `.parity-coverage.txt`.

#### Check 3 · Localization — N/A
- Rig.TUnit has no `.resx` / `IStringLocalizer` usage. Rule: "skip if project does not use localization."

#### Check 4 · Error handling — 3 findings

- **[HIGH]** [ServiceBusSessionListener.cs:87](src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusSessionListener.cs#L87) — `HandleErrorAsync(ProcessErrorEventArgs args) => Task.CompletedTask` silently discards every broker error surfaced by `ServiceBusSessionProcessor`. When a session lock is lost, auth fails, or the subscription is missing, the test-observable symptom is "no messages received" instead of the real cause. Mitigation: expose the last error via a public property (`LastError`), or push onto a `ConcurrentQueue<Exception>` surfaced by `ObservedErrors`, so the test-rig consumer can assert on it.

- **[MEDIUM]** [NatsJetStreamListener.cs:27-32](src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamListener.cs#L27-L32) — `StartAsync` fires `Task.Run(ConsumeLoopAsync)` then `await Task.Yield()`. `Task.Yield` only cedes one turn; it does not wait for `CreateOrderedConsumerAsync` to complete. A fast publisher that sends immediately after `await StartAsync` can race the consumer creation and the test only notices via timeout. Compare with [KafkaListener.cs:86-97](src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs#L86-L97) which correctly waits on `_partitionsAssigned` before returning. Mitigation: introduce a `TaskCompletionSource _consumerReady` signalled after `CreateOrderedConsumerAsync` succeeds, and `await _consumerReady.Task` at the end of `StartAsync`.

- **[MEDIUM]** [RabbitMqListener.cs:62-88](src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqListener.cs#L62-L88) — `ReceivedAsync` lambda runs with `autoAck: true` and has no try-around the header decoder / `UTF8.GetString(ea.Body.Span)`. A malformed byte-array header or non-UTF8 body throws inside the consumer, message is already acked, and `Record(…)` is never called — tests see silence. Mitigation: wrap the capture in `try { … } catch (Exception ex) { /* stash into Errors, still return completed */ }` so decode failures become assertable instead of invisible.

Positive: `CancellationToken` propagation is clean across all new async paths; no `.Result` / `.Wait()` / `async void` introduced; no `Thread.Sleep` in tests or src.

#### Check 5 · Testing — PASS
RED-GREEN commit discipline verified. All 26 new public types have matching test references (cross-referenced `ITopologyBuilder`, `SendContext`, the five `*TopologyBuilder`s, `ServiceBusSessionListener`, `ServiceBusAdministrationHelper`, `ServiceBusDeadLetterProbe`, `NatsJetStreamFixture`, `NatsJetStreamEventSender`, `NatsJetStreamListener`, `NatsRetentionPolicy`, `ExchangeType`, and per-provider config interfaces).

Coverage gate `line-rate ≥ 0.90 / branch-rate ≥ 0.85` inherited from feature 005 remains enforceable.

#### Check 6 · Security — PASS
- No hardcoded secrets / SAS tokens / broker credentials in `src/` (verified across all 49 touched src files).
- Connection strings flow through `IRigConnectionSource` and fixture options — no literal endpoints baked into production code.
- SQL filter construction in `ServiceBusTopologyBuilder` uses the Azure SDK `CreateRuleOptions` record, not string concatenation — parameter injection not possible.
- Check "authorization on endpoints" — N/A (Rig.TUnit is a library, no endpoints).

#### Check 7 · Event structure — N/A (rationale)
This is test-harness code, not production event publishers. `SendContext` is a routing-hint record, not a domain event. The check's intent (aggregate IDs, timestamps, idempotency) doesn't apply; the analogous concern — idempotent topology apply — IS covered:
- [ServiceBusAdministrationHelper.cs](src/Rig.TUnit.Messaging.ServiceBus/Topology/ServiceBusAdministrationHelper.cs) uses create-or-update semantics.
- [NatsTopologyBuilder.cs:30-37](src/Rig.TUnit.Messaging.Nats/Topology/NatsTopologyBuilder.cs#L30-L37) catches `NatsJSApiException` code 400 and falls back to `UpdateStreamAsync`.
- `KafkaListener.EnsureTopicExistsAsync` catches `CreateTopicsException` where all results are `TopicAlreadyExists`.

Minor sub-finding — **[LOW]** NATS idempotency catches *any* code-400 (generic "bad request"), not a stream-name-in-use-specific code. If the NATS server returns 400 for a validation error unrelated to existence (e.g. invalid retention policy), the catch silently swallows it and re-tries `UpdateStreamAsync`, which may succeed for a different reason or mask the real config error. Worth tightening to `ex.Error.ErrCode == 10058` (stream name in use) once the client exposes it.

#### Check 8 · Performance — N/A (rationale)
No EF Core, no N+1 surface, no list endpoints. `AsNoTracking` / pagination rules don't apply. CancellationToken propagation is already checked under Check 4.

#### Check 9 · Brief compliance — N/A
Mode is generic (single-repo, messaging sub-tree per `plan.md`). No secondary-repo briefs exist under `.dotnet-ai-kit/briefs/` for feature 007.

### CodeRabbit
- CLI detected at `coderabbit 0.4.1`.
- **Not executed** in this pass — a 10K-line / 69-commit review would dominate the report and duplicate the focused findings above. Re-run with `/dotnet-ai.review --skip-coderabbit=false` or invoke `coderabbit review` directly on this branch when a second opinion is wanted.

### Auto-Fixed
- None applied. `--auto-fix` was not set; no purely-safe auto-fix candidates surfaced (unused usings / missing `sealed` were not detected in scope).

---

## CHANGELOG framing
Intentional — `CHANGELOG.md` explicitly scopes each "Added" section to a planned release window ("release N+1", "N+2", "N+3"), reflecting the phased rollout sequence from `plan.md`. Not a finding.

---

## Summary

| Severity  | Count |
|-----------|------:|
| CRITICAL  | 0 |
| HIGH      | 1 |
| MEDIUM    | 2 |
| LOW       | 1 |
| **Total** | **4** |

- Auto-fixed: 0
- Remaining: 0
- Manually fixed: 4 (2026-04-25)

### Actions
1. ✅ **HIGH** — `ServiceBusSessionListener.HandleErrorAsync` now enqueues every `ProcessErrorEventArgs.Exception` into a `ConcurrentQueue<Exception>`, surfaced via `ObservedErrors` (full snapshot) and `LastError` (most recent). Tests can assert on broker errors instead of only observing "no messages received" ([ServiceBusSessionListener.cs](src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusSessionListener.cs)).
2. ✅ **MEDIUM** — `NatsJetStreamListener.StartAsync` now calls `CreateOrderedConsumerAsync` synchronously (before spawning the consume loop) and passes the resulting `INatsJSConsumer` into `ConsumeLoopAsync`. Broker errors propagate naturally to the caller; the publish-before-subscribe race is gone without needing a TCS gate ([NatsJetStreamListener.cs:27-43](src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamListener.cs#L27-L43)).
3. ✅ **MEDIUM** — `RabbitMqListener` extracted the `ReceivedAsync` body into `CaptureDelivery`, wrapped in a try/catch for `DecoderFallbackException` and `ArgumentException`. Decode failures are returned as a typed error via the new `Errors` collection so they're assertable instead of silently dropped under `autoAck: true` ([RabbitMqListener.cs](src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqListener.cs)).
4. ✅ **LOW** — `NatsTopologyBuilder.ApplyAsync` exception filter tightened from `ex.Error.Code == 400` (generic bad-request) to `ex.Error.ErrCode == 10058` (`JSStreamNameExistErr` — stream name already in use). Validation errors with code 400 (e.g. invalid retention policy) now surface instead of being masked by an `UpdateStreamAsync` fallback ([NatsTopologyBuilder.cs:34](src/Rig.TUnit.Messaging.Nats/Topology/NatsTopologyBuilder.cs#L34)).
