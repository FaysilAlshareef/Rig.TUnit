# Analysis Report — 007-messaging-topology-sessions

**Feature**: 007-messaging-topology-sessions · **Mode**: Generic (single-repo)
**Date**: 2026-04-23 · **Findings**: 8 (0 CRITICAL · 1 HIGH · 4 MEDIUM · 3 LOW)
**Status**: **All 8 findings addressed on 2026-04-23.** See "Resolution log" at the bottom of this file for the exact edits applied per finding.

Read-only consistency check over `spec.md`, `plan.md`, `tasks.md`, `data-model.md`, `research.md`, `quickstart.md`, `contracts/*`, `checklists/requirements.md`, and relevant source files under `src/Rig.TUnit.Messaging.*`.

---

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 0 |
| HIGH | 1 |
| MEDIUM | 4 |
| LOW | 3 |

No blocking issues. The HIGH finding is a scope-widening risk for Phase 0 that is easy to address before `/dotnet-ai-kit:implement` begins. The MEDIUM findings are residual drift from the C-003 restructuring pass — small textual fixes. LOW findings are documentation polish.

---

## Findings

### [HIGH] Pass 1 — Phase 0 T000-GREEN widens scope into every provider listener

**Location**: [spec.md §Task List T000](spec.md) + [plan.md §Phase 0](plan.md) + `tasks.md` T000-GREEN + `data-model.md §CapturedMessage<TMessage> (modified)`

**Details**: T000-GREEN narrows `CapturedMessage<TMessage>.Body` from `string?` to `string` (C-001, ships clean per C-000). Today 5 provider listeners construct `CapturedMessage<T>` directly:

- [ServiceBusListener.cs:52](src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusListener.cs:52) — passes `args.Message.Body.ToString()` (non-null, **safe**)
- [KafkaListener.cs:147](src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs:147) — passes `result.Message.Value` (nullable in `Confluent.Kafka.Message<TKey,TValue>`, **unsafe**)
- [RabbitMqListener.cs:47](src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqListener.cs:47) — passes `Encoding.UTF8.GetString(ea.Body.Span)` (non-null, **safe**)
- [NatsListener.cs:84](src/Rig.TUnit.Messaging.Nats/Helpers/NatsListener.cs:84) — passes `msg.Data` (nullable, **unsafe**)
- [SqsListener.cs:91](src/Rig.TUnit.Messaging.Sqs/Helpers/SqsListener.cs:91) — passes `msg.Body` from AWS SDK (nullable, **unsafe**)

At least 3 provider listeners produce a compile error the moment T000-GREEN lands, because the narrowed `string Body` parameter won't accept `string?`. Phase 0's plan ("One PR: `#007-p0`. Merge before any provider phase starts.") cannot compile the solution without rippling into those provider packages. Not flagged in `tasks.md`.

**Suggested Fix**: add a sub-step to T000-GREEN (or a new T000b task) that updates the 5 listener call sites to coerce `null → ""`. Concretely:

| File | Change |
|------|--------|
| `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs` | `result.Message.Value` → `result.Message.Value ?? string.Empty` |
| `src/Rig.TUnit.Messaging.Nats/Helpers/NatsListener.cs` | `msg.Data` → `msg.Data ?? string.Empty` |
| `src/Rig.TUnit.Messaging.Sqs/Helpers/SqsListener.cs` | `msg.Body` → `msg.Body ?? string.Empty` |
| ServiceBus & RabbitMQ | already non-null — no change |

Update `tasks.md` T000-GREEN's file list to include these three listener files. Update `plan.md §Phase 0` to mention the ripple. Keep the PR scope honest — Phase 0 now touches 8 files (base library + 3 provider listeners), still small.

---

### [MEDIUM] Pass 2 — `spec.md:294` Coverage Plan row lists obsolete unified config interfaces

**Location**: [spec.md:294](spec.md) (Coverage Plan table, row for `Rig.TUnit.Messaging` base package)

**Details**: The "new public types" column still reads:

> `SendContext`, `ITopologyBuilder`, `IQueueConfig`, `ITopicConfig`, `ISubscriptionConfig`, `IExchangeConfig`, `IStreamConfig`, `CapturedMessage<TMessage>.SessionKey`, `EventSenderBase.BuildHeaders(SendContext,…)`, `MessagingRigBuilder<TSelf>.WithTopology(...)`

Per C-003, the unified `IQueueConfig` / `ITopicConfig` / `ISubscriptionConfig` / `IExchangeConfig` / `IStreamConfig` interfaces **do not exist** — each provider owns its own (`IServiceBusQueueConfig`, `IKafkaTopicConfig`, etc.), and those are correctly listed in the per-provider rows below. The `MessagingRigBuilder<TSelf>.WithTopology(...)` reference is also wrong per C-003 — that method is declared on each provider's `RigBuilder`, not the base class.

This is a drafting miss from the C-003 pass — the Key Entities section was updated, but the Coverage Plan row wasn't.

**Suggested Fix**: edit [spec.md:294](spec.md) to change the base-package "new public types" cell to:

> `SendContext`, `ITopologyBuilder` (marker), `CapturedMessage<TMessage>` tightened `Body` + new `SessionKey`, `EventSenderBase.BuildHeaders(SendContext, …)` overload

Remove the unified config interfaces and the base-class `WithTopology` reference.

---

### [MEDIUM] Pass 3 — `checklists/requirements.md:28` still checks FR-007-08 as covered

**Location**: [checklists/requirements.md:28](checklists/requirements.md)

**Details**: Line reads `- [x] FR-007-08 — additive-only public API — validated via dotnet api-diff per shipped package`.

FR-007-08 is **superseded by C-000** in the spec ([spec.md:101](spec.md), [spec.md:377](spec.md)). The checklist should reflect that — leaving it as `[x]` implies the feature ships with an additive-only guarantee, which it doesn't.

**Suggested Fix**: change line 28 to `- [~~x~~] ~~FR-007-08 — additive-only public API~~ — **superseded by C-000** (packages pre-release; breaking changes allowed when they yield a cleaner surface).`

---

### [MEDIUM] Pass 2 — T002-RED test is green from day one (no RED state)

**Location**: [tasks.md §T002-RED](tasks.md) + [plan.md §Phase 0 T002](plan.md)

**Details**: T002-RED's test `MessagingRigBuilder_DoesNotDeclareWithTopology` asserts the base class has no `WithTopology` method. The base class (see [MessagingRigBuilder.cs:5](src/Rig.TUnit.Messaging/Builder/MessagingRigBuilder.cs:5)) **already has no such method** — the test passes the instant it compiles. There is no RED state; the commit labelled `test(007): RED T002` goes green immediately.

This violates the RED→GREEN discipline stated in [spec.md §Clarifications C-002](spec.md) ("test must fail the build or test"). It's also a semantic oddity — RED commits are supposed to drive the design, but T002-RED is a regression guard against a future mistake.

**Suggested Fix**: convert T002 to a **single-GREEN** task (following the same pattern as T014, T016, T050) with the rationale "regression guard — no new production code; asserts the C-003 shape of the base class is preserved." Update [tasks.md §T002](tasks.md) to collapse into one task row:

```
- [ ] **T002** [depends: T001-GREEN] Regression guard — MessagingRigBuilder base class must not declare WithTopology (per C-003).
      Files:
      - tests/Rig.TUnit.Messaging.Tests.Unit/Builder/MessagingRigBuilderNoGenericWithTopologyTests.cs (new)
      - src/Rig.TUnit.Messaging/Builder/MessagingRigBuilder.cs (XML comment pointing at C-003)
      Commit: feat(007): GREEN T002 — regression guard against base-class WithTopology (no red — structural assertion)
```

Also update [spec.md §4 Phase 0](spec.md) T002 row and [plan.md §Phase 0](plan.md) work-sequence steps 5-6 to match.

---

### [MEDIUM] Pass 1 — `.parity-coverage.txt` runtime access not captured in T003

**Location**: [tasks.md §T003-GREEN](tasks.md) + [data-model.md §.parity-coverage.txt](data-model.md)

**Details**: T003's `ProviderCompletenessTests` extension is to read `tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt` at runtime (C-005). When `dotnet test` executes, the working directory is the test project's `bin/Debug/net10.0/` output — the `.parity-coverage.txt` file at the project root is not automatically copied there. Without a `<None Include=".parity-coverage.txt" CopyToOutputDirectory="PreserveNewest" />` entry in `Rig.TUnit.Architecture.Tests.csproj`, the test will fail with "file not found" even when it's present in source.

Not captured in T003-GREEN's file list. Also not captured as an expected test helper — the test needs a resolver that looks up the file relative to the test project directory (e.g. via `AppContext.BaseDirectory` + relative walk, or `[CallerFilePath]` trick, or embedded resource).

**Suggested Fix**: in [tasks.md §T003-GREEN](tasks.md), add:

```
- tests/Rig.TUnit.Architecture.Tests/Rig.TUnit.Architecture.Tests.csproj
  <ItemGroup>
    <None Include=".parity-coverage.txt">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

And note in T003-GREEN's file list: "copy-to-output config in .csproj so the file is present at test runtime." Mirror in [plan.md §Phase 0 step 7](plan.md).

---

### [LOW] Pass 2 — `spec.md §Key Entities` uses ambiguous "internal or sealed public"

**Location**: [spec.md §Key Entities](spec.md) — Per-provider topology-builder implementations bullet.

**Details**: Text reads: "Per-provider topology-builder implementations — `ServiceBusTopologyBuilder`, `KafkaTopologyBuilder`, `RabbitMqTopologyBuilder`, `NatsTopologyBuilder`, `SqsTopologyBuilder` (**internal or sealed public**; implement their provider-specific interface)."

Ambiguity — if `internal`, the `WithTopology(Action<IXxxTopologyBuilder>)` lambda on each `RigBuilder` couldn't reference the concrete type outside the assembly (it'd only see the interface, which is fine functionally but removes the option of construction helpers). If `sealed public`, consumers can instantiate directly, which is fine but wider surface. Contracts snapshots consistently show **sealed public** impls (e.g. [contracts/base.cs](contracts/base.cs)).

**Suggested Fix**: change "internal or sealed public" → "sealed public" in [spec.md §Key Entities](spec.md). Data-model (`data-model.md`) and contracts already assume sealed public — align spec with them.

---

### [LOW] Pass 3 — Plan §Coverage baseline quotes pre-F006-uplift percentages

**Location**: [plan.md §Technical Context / research.md §Coverage baseline](research.md)

**Details**: The `Rig.TUnit.Messaging` base package row quotes "30.9 % (T024 target in F006)" — that's the **pre-uplift** number from the F006 spec. Feature 007 depends on F006 exit gates being green, so by the time F007 starts, that package is ≥ 90 %. Phrasing implies these numbers are current; they're actually the baseline the F006 work **closed**.

**Suggested Fix**: change the row header from "Pre-uplift line % (Feature 006 input)" to "F006 input → F006 exit gate (≥ 90 / ≥ 85 when F007 starts)" to remove the implicit claim. Same one-line edit in [research.md §Coverage baseline](research.md).

---

### [LOW] Pass 4 — No explicit idempotency test for concurrent `WithTopology` calls on non-ServiceBus providers

**Location**: [tasks.md §T023](tasks.md), [tasks.md §T031](tasks.md), [tasks.md §T042](tasks.md), [tasks.md §T054](tasks.md)

**Details**: T013 (ServiceBus) includes a `WithTopology_CalledTwice_IsIdempotent` test — confirms idempotent re-apply. No equivalent test in T023 (Kafka), T031 (SQS), T042 (Rabbit), T054 (NATS). The design doc claims every `Create*Async` is idempotent, but if the underlying SDK surface differs (e.g. RabbitMQ's `QueueDeclareAsync` on a pre-existing queue with conflicting args throws), idempotency can silently break per-provider.

Not a blocker — the existing scenario tests in each phase will catch gross breakage via the shared-container re-run path — but a targeted idempotency test per provider would be low-cost insurance.

**Suggested Fix**: append to each of T023-RED / T031-RED / T042-RED / T054-RED a single additional test `WithTopology_CalledTwice_IsIdempotent` that runs the full declaration twice and asserts no exception + same entity count. One-line additions.

---

## Cross-artefact traceability summary

| Requirement | Spec §4 task | Plan phase | tasks.md row | Data-model type | Contract snapshot |
|-------------|--------------|------------|--------------|-----------------|-------------------|
| FR-007-01 (SendContext sender) | T010, T020, T030, T040, T052 | Phases 1–5 | T010/T020/T030/T040/T052 RED+GREEN | `SendContext` + per-provider sender overloads | `base.cs` + per-provider `.cs` |
| FR-007-02 (WithTopology hook) | T013, T023, T031, T042, T054 | Phases 1–5 | T013/T023/T031/T042/T054 RED+GREEN | Per-provider `I{Provider}TopologyBuilder` | `base.cs` marker + per-provider |
| FR-007-03 (PerKeyMonotonic E2E) | T015a, T025a, T033a, T044a, T055a | Phases 1–5 (scenarios) | *a scenarios | `OrderingAssert.PerKeyMonotonic` (unchanged) | n/a |
| FR-007-04 (no regression) | implicit — every phase | every phase | implicit | n/a | n/a |
| FR-007-05 (coverage ≥ 90/≥ 85) | `spec.md §5 Coverage Plan` | reviewer rule in plan | enforced by CI | 25 new types + 12 new methods | n/a |
| FR-007-06 (emulator admin works) | T014 | Phase 1 | T014 single GREEN | n/a | n/a |
| FR-007-07 (JetStream isolated) | T050 | Phase 5 | T050 single GREEN | `DependencyDirectionTests` extension | n/a |
| ~~FR-007-08~~ | superseded by C-000 | — | — | — | — |

No orphan tasks (every task in `tasks.md` maps back to an FR, NFR, or clarification). No orphan FRs (every FR has at least one task — except FR-007-08, which is superseded).

---

## Recommendation

Ready to proceed to `/dotnet-ai-kit:implement` once the HIGH finding and the four MEDIUM findings are addressed (estimated ~15 minutes of spec/plan/tasks edits). The LOW findings are polish and can be absorbed during implementation PRs.

Specifically, before Phase 0 starts:

1. **HIGH** — update T000-GREEN task file list + plan Phase 0 step to include Kafka/NATS/SQS listener null-coercion.
2. **MEDIUM** — fix `spec.md:294` Coverage Plan row (remove obsolete unified config interfaces).
3. **MEDIUM** — strike FR-007-08 check on `checklists/requirements.md:28`.
4. **MEDIUM** — collapse T002 to single-GREEN regression guard.
5. **MEDIUM** — add `<None Include=".parity-coverage.txt" CopyToOutputDirectory="..." />` instruction to T003-GREEN.

After those edits, run `/dotnet-ai-kit:analyze` once more to confirm zero HIGH findings before kicking off `/dotnet-ai-kit:implement`.

---

## Resolution log — 2026-04-23

All 8 findings applied in the feature artefacts.

| Finding | Resolution |
|---------|------------|
| **HIGH — Phase 0 T000-GREEN listener ripple** | [tasks.md T000-GREEN](tasks.md) file list now includes the 3 listener files (Kafka, NATS, SQS) with the `?? string.Empty` coercion line numbers. [plan.md §Phase 0 step 2](plan.md) rewritten to document the ripple + PR-scope widening. Commit message updated to `feat(007): GREEN T000 — SendContext + BuildHeaders overload + CapturedMessage extension + listener null-coercion`. |
| **MEDIUM — `spec.md:294` obsolete unified config interfaces** | [spec.md §5 Coverage Plan](spec.md) base-library row rewritten to list only `SendContext`, `ITopologyBuilder` (marker), `CapturedMessage<TMessage>` changes, and the `BuildHeaders(SendContext, …)` overload. Removed references to `IQueueConfig` / `ITopicConfig` / `ISubscriptionConfig` / `IExchangeConfig` / `IStreamConfig` and the base-class `WithTopology`. Test-name for T002 updated to `MessagingRigBuilderNoGenericWithTopologyTests`. |
| **MEDIUM — `checklists/requirements.md:28` FR-007-08 drift** | [checklists/requirements.md:28](checklists/requirements.md) line struck through with pointer to C-000 supersession; `dotnet api-diff` gate removed. |
| **MEDIUM — T002-RED passes from day one** | Collapsed to single-GREEN in [spec.md §4 Phase 0 T002](spec.md), [plan.md §Phase 0 step 5](plan.md), [tasks.md §T002](tasks.md). Commit discipline note updated: structural assertion has no RED state. `tasks.md` total task count 73 → 72. Phase 0 task count 8 → 7. |
| **MEDIUM — `.parity-coverage.txt` runtime access** | [tasks.md T003-GREEN](tasks.md) file list adds `tests/Rig.TUnit.Architecture.Tests/Rig.TUnit.Architecture.Tests.csproj` with the `<None Include="…"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>` snippet. Mirrored in [plan.md §Phase 0 step 7](plan.md). |
| **LOW — "internal or sealed public" ambiguity** | [spec.md §Key Entities](spec.md) "Per-provider topology-builder implementations" bullet tightened to **sealed public**. |
| **LOW — Plan coverage baseline phrasing** | [research.md §Coverage baseline](research.md) rewritten: makes explicit that the quoted percentages are F006 pre-uplift, closed by F006's exit gate before F007 starts. No change to [plan.md §Per-package coverage plan](plan.md) — already correct because it references the spec table. |
| **LOW — Per-provider idempotency smoke test** | Added `WithTopology_CalledTwice_IsIdempotent` to T023-RED (Kafka), T031-RED (SQS), T042-RED (RabbitMQ, with explicit `PRECONDITION_FAILED` guard), T054-RED (NATS). Commit messages updated to include `+ idempotency`. T013-RED already had this test. |

Artefacts touched by this resolution pass:

- `.dotnet-ai-kit/features/007-messaging-topology-sessions/tasks.md`
- `.dotnet-ai-kit/features/007-messaging-topology-sessions/spec.md`
- `.dotnet-ai-kit/features/007-messaging-topology-sessions/plan.md`
- `.dotnet-ai-kit/features/007-messaging-topology-sessions/research.md`
- `.dotnet-ai-kit/features/007-messaging-topology-sessions/checklists/requirements.md`
- `.dotnet-ai-kit/features/007-messaging-topology-sessions/analysis.md` (this file — status marker + resolution log)

Zero source code was modified during this pass (read-only posture preserved). The implementation changes the findings reference will land during `/dotnet-ai-kit:implement`.

**Post-resolution state**: 0 CRITICAL · 0 HIGH · 0 MEDIUM · 0 LOW outstanding. Ready for `/dotnet-ai-kit:implement`.
