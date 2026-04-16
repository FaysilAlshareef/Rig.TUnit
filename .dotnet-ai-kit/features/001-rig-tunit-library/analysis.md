# Analysis Report: Rig.TUnit Testing Infrastructure Library

**Feature**: 001-rig-tunit-library | **Mode**: Generic
**Date**: 2026-04-16 | **Findings**: 8

## Summary
- CRITICAL: 0
- HIGH: 2
- MEDIUM: 4
- LOW: 2

## Findings

### [HIGH] Naming Consistency: US-2 AC-2 references removed method `HandleEvent<T>()`

**Location**: `spec.md` line 23 — User Story 2, Acceptance Scenario 2
**Details**: US-2 AC-2 says "When I use `HandlerHelper.HandleEvent<T>()`" but this method was explicitly removed per FR-003 and plan override #4. The clarified spec (Key Entities, line 138) correctly says "only `Send<TResult>(IRequest<TResult>)` exists", and the plan confirms "Remove — only Send<TResult> exists (FR-003)". The user story still references the old API.
**Suggested Fix**: Update US-2 AC-2 to: "**When** I use `HandlerHelper.Send<TResult>()`, **Then** the request is dispatched via MediatR within a scoped lifetime." This is a spec-only change — does not affect tasks or implementation.

---

### [HIGH] Concurrency: ListenerHelper `_messages` list is not thread-safe

**Location**: `plan.md` Phase 5 / design doc `ListenerHelper` class
**Details**: The design doc shows `private readonly List<ServiceBusReceivedMessage> _messages = [];` with `MaxConcurrentSessions = 100`. With 100 concurrent sessions, the `OnMessage` handler can be called from multiple threads simultaneously, each adding to the same `List<>`. `List<T>.Add()` is not thread-safe — this causes potential data corruption or `InvalidOperationException` under concurrent load.
**Suggested Fix**: Replace `List<ServiceBusReceivedMessage>` with `ConcurrentBag<ServiceBusReceivedMessage>` (from `System.Collections.Concurrent`), or use a `lock` around the add. Update `Messages` property to return `IReadOnlyCollection<ServiceBusReceivedMessage>`. Update `_messages.Count` checks in `WaitForMessagesAsync` to be thread-safe. This affects T032 implementation.

---

### [MEDIUM] Naming Consistency: US-2 AC-1 uses `GrpcClientHelper<TClient>` (missing TProgram)

**Location**: `spec.md` line 22 — User Story 2, Acceptance Scenario 1
**Details**: US-2 AC-1 says `GrpcClientHelper<TClient>` but C-002 clarified this must be `GrpcClientHelper<TClient, TProgram>`. The Key Entities section (line 137) is correct. Only the user story text is stale.
**Suggested Fix**: Update US-2 AC-1 to reference `GrpcClientHelper<TClient, TProgram>`. Spec-only change.

---

### [MEDIUM] Coverage Gap: FR-020 (ClassDataSource fixture sharing) missing from task notes

**Location**: `tasks.md` — T019, T020, T021, T024, T025, T028-T031 (all integration test tasks)
**Details**: FR-020 requires integration tests to use `[ClassDataSource<TFixture>(Shared = SharedType.PerTestSession)]` for container fixture sharing. None of the integration test tasks mention this TUnit attribute pattern. Without it, each test class would create its own container instance (expensive and slow).
**Suggested Fix**: Add a note to T018, T023, T027 (integration test csproj tasks) or T019/T024/T028 (first integration test tasks per package): "Integration test classes MUST use `[ClassDataSource<TFixture>(Shared = SharedType.PerTestSession)]` to share the container fixture across all tests in the session (FR-020)."

---

### [MEDIUM] Coverage Gap: ServiceBusContainerExtensions behavior not documented

**Location**: `spec.md` — Key Entities section, `tasks.md` T031
**Details**: `ServiceBusContainerExtensions` is listed as a file in the source project table and has a task (T031) with a single test (`UseServiceBusContainer_ReplacesConnectionString`), but its exact behavior is not described in the spec, design doc, or handoff doc. What DI registrations does it replace? Does it replace `ServiceBusClient`? A connection string in `IOptions<T>`? Without this, the implementer must guess.
**Suggested Fix**: Add a description to the Key Entities section or a clarification entry: "ServiceBusContainerExtensions replaces the Service Bus connection string in the service collection's options (e.g., replaces `ServiceBusOptions.ConnectionString` or re-registers `ServiceBusClient` with the fixture's connection string)." Determine the exact pattern by checking what the consuming services register.

---

### [MEDIUM] Coverage Gap: Plan mentions `.editorconfig` but T001 omits it

**Location**: `plan.md` Phase 0 vs `tasks.md` T001
**Details**: The plan Phase 0 lists `.editorconfig` as a file to create ("C# conventions: file-scoped namespaces, var preferences"), but T001 only lists `global.json`, `Directory.Build.props`, `.gitignore`, and `Rig.TUnit.slnx`. The `.editorconfig` was dropped from the task.
**Suggested Fix**: Add `.editorconfig` to T001's file list, or drop it from the plan if not needed (the Directory.Build.props already enforces LangVersion and other settings).

---

### [LOW] Naming Consistency: FR-010 wording misleading about Task.Delay

**Location**: `spec.md` line 116 — FR-010
**Details**: FR-010 says "MUST poll every 250ms — no `Task.Delay()` for arbitrary waits." The implementation in the design doc actually uses `await Task.Delay(250, ct)` as the polling mechanism. The intent is "no arbitrary long Task.Delay (like 10-20 seconds)" not "never use Task.Delay at all." The wording could confuse an implementer who reads it literally.
**Suggested Fix**: Reword FR-010 to: "MUST poll every 250ms using `Task.Delay(250, ct)` — no arbitrary long waits like `Task.Delay(10000)` or `Task.Delay(20000)`."

---

### [LOW] Coverage Gap: T031 ServiceBusContainerExtensionsTests has only 1 test

**Location**: `tasks.md` T031
**Details**: T031 specifies only 1 test (`UseServiceBusContainer_ReplacesConnectionString`). Other extension test tasks have 2-3 tests covering replacement + functional verification. A single test may not be sufficient if the extension has multiple behaviors (e.g., replacing multiple registrations, handling missing registrations).
**Suggested Fix**: Consider adding at least one more test: `UseServiceBusContainer_CanCreateClientFromReplacedConnection` to verify the replaced connection is functional end-to-end.
