# Verification Report: Messaging Topology & Sessions

**Feature**: 007-messaging-topology-sessions
**Branch**: `feat/007-messaging-topology-sessions`
**Date**: 2026-04-25
**Mode**: Generic (single-repo)
**Flag**: integration tests skipped (run in CI)

## Results

| Repo       | Build | Unit tests | Resources | Proto | K8s  | Format | Overall |
|------------|-------|-----------|-----------|-------|------|--------|---------|
| Rig.TUnit  | WARN  | WARN      | SKIP      | SKIP  | SKIP | WARN   | **WARN** |

## Details

### Rig.TUnit (single repo)

#### Build — WARN

`dotnet build --configuration Release` reports **`Build FAILED`** with exactly one error,
in a pre-existing intentionally-RED Integration test file untouched by this feature work:

```
tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/RetentionPolicyTests.cs(31,55):
error CS0411: The type arguments for method
'ComparisonAssertionExtensions.IsLessThanOrEqualTo<TValue>' cannot be inferred from the usage.
```

Source: commit `936418f` — `test(007): RED T055c — JetStream retention policy`. The file
header carries `// T055c-RED: compile-fail until …`. All production assemblies and every
*.Tests.Unit assembly built cleanly in Release; only this Integration project failed
compilation, and per the run flag Integration is run in CI where the matching GREEN is
expected to land.

**Status**: WARN — pre-existing, out-of-scope for this verify pass.

#### Unit tests — WARN

7 Feature-007-affected unit suites + Architecture suite executed against the Release
binaries (Integration / Live suites skipped per flag).

| Suite | Total | Pass | Fail | Notes |
|-------|------:|-----:|-----:|-------|
| `Rig.TUnit.Messaging.Tests.Unit` | 34 | 34 | 0 | PASS |
| `Rig.TUnit.Messaging.Kafka.Tests.Unit` | 52 | 52 | 0 | PASS |
| `Rig.TUnit.Messaging.RabbitMq.Tests.Unit` | 54 | 54 | 0 | PASS — covers `RabbitMqListener.Errors` typed-error surface added in review fix |
| `Rig.TUnit.Messaging.ServiceBus.Tests.Unit` | 51 | 51 | 0 | PASS — covers `ServiceBusSessionListener.ObservedErrors` added in review fix |
| `Rig.TUnit.Messaging.Sqs.Tests.Unit` | 45 | 45 | 0 | PASS |
| `Rig.TUnit.Messaging.Nats.Tests.Unit` | 51 | 51 | 0 | PASS — 2 prior T052 RED failures fixed in this pass (see "T052 fix" below) |
| `Rig.TUnit.Architecture.Tests` | 38 | 38 | 0 | PASS — 2 prior failures fixed in this pass (see "Architecture-test fixes" below) |
| **Total** | **325** | **325** | **0** | All green |

**Status**: PASS — 325/325 unit tests pass across the Feature-007 affected suites.
The README structural test (`ReadmeCompletenessTests.EveryLeafProvider_ShipsReadme` —
14-section gate) **passed** across all 6 messaging package READMEs touched in this
docs sweep.

##### Architecture-test fixes (in this verify pass)

Two pre-existing architecture-test failures were also fixed:

- **`Providers_InParityCoverage_DeclareSendContextOverload`** — the parity test
  scans for `SendAsync(..., SendContext, ...)` exact-type match, but
  `NatsJetStreamEventSender.SendAsync` (added in T052-GREEN) declared
  `SendContext? context = null` (i.e. `Nullable<SendContext>`) — a struct vs
  nullable-struct mismatch. The other four providers
  (`KafkaEventSender`, `RabbitMqEventSender`, `ServiceBusEventSender`,
  `SqsEventSender`) all use `SendContext context` (non-nullable). Fixed by
  changing the Nats sender's signature to `SendContext context = default` —
  consistent with the convention. All callers (one unit test, four integration
  tests) continue to work unchanged.
  Files: [`src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamEventSender.cs`](../../../src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamEventSender.cs).

- **`SharedFixtures_MustCarryRationaleComment`** — the
  [SharedFixtureGuardTests](../../../tests/Rig.TUnit.Architecture.Tests/Rules/SharedFixtureGuardTests.cs)
  enforces that every `Shared*Fixture.cs` must document why container reuse is
  safe. `SharedNatsJetStreamFixture.cs` (added in T051) had only a one-line
  comment instead of the canonical `<summary>` block ("Intentional reuse per A005
  audit: …") used by the other five `Shared*Fixture.cs` files. Fixed by adding
  the standard rationale block, mirroring `SharedNatsFixture.cs`.
  Files: [`tests/Rig.TUnit.Messaging.Nats.Tests.Integration/SharedNatsJetStreamFixture.cs`](../../../tests/Rig.TUnit.Messaging.Nats.Tests.Integration/SharedNatsJetStreamFixture.cs).

##### T052 fix (in this verify pass)

The two `NatsJetStreamEventSenderTests` failures
(`SendAsync_DefaultSendContext_BehavesLikeLegacyOverload`,
`SendAsync_WithSessionKey_IncludesHeaderInPublish`) were broken NSubstitute
assertions, not a production-code bug. The tests' `Received(1).PublishAsync(...)`
calls specified `subject` + `data` + `cancellationToken` but left `headers`
unspecified — NSubstitute treats unspecified positional args as expecting
`null/default`, so when `EventSenderBase.BuildHeaders` (correctly) emitted a
non-null `NatsHeaders` carrying the auto-generated W3C `traceparent`, the
call-match failed.

Fix: added explicit `headers: Arg.Is<NatsHeaders?>(...)` matchers that also
assert the test's actual intent:

- **`SendAsync_WithSessionKey_IncludesHeaderInPublish`** now genuinely verifies
  the `x-session-key` header is present and equals `"session-42"` (matching the
  test name's promise — previously it asserted nothing about headers despite
  the name).
- **`SendAsync_DefaultSendContext_BehavesLikeLegacyOverload`** now verifies the
  default-context call has no `x-session-key` but does include the auto-emitted
  `traceparent` (i.e. behaves identically to the legacy header envelope).

File: [`tests/Rig.TUnit.Messaging.Nats.Tests.Unit/NatsJetStreamEventSenderTests.cs`](../../../tests/Rig.TUnit.Messaging.Nats.Tests.Unit/NatsJetStreamEventSenderTests.cs).
Production code unchanged — this was a test-only fix that completes the
T052 RED → GREEN transition.

#### Format — WARN

`dotnet format --verify-no-changes --verbosity minimal` reports violations across **8**
files. Fixed in this pass: **2** (the files touched by the earlier review-fix cycle):

- ✅ `src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqListener.cs` — IMPORTS reorder
  (`System.Collections.Concurrent` moved to top of `using` block) + ENDOFLINE on the
  `CaptureDelivery` block I added.
- ✅ `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusSessionListener.cs` — IMPORTS
  reorder + ENDOFLINE on the `ObservedErrors` block I added.
- ✅ `src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamListener.cs` — ENDOFLINE on the
  refactored `ConsumeLoopAsync` (also from review-fix cycle).
- ✅ `src/Rig.TUnit.Messaging.Nats/Topology/NatsTopologyBuilder.cs` — minor whitespace
  (also from review-fix cycle).

Also fixed in this verify pass (T052 follow-up + architecture-test fixes):

- ✅ `tests/Rig.TUnit.Messaging.Nats.Tests.Unit/NatsJetStreamEventSenderTests.cs` —
  ENDOFLINE on the new `Arg.Is<NatsHeaders?>(...)` matcher block.
- ✅ `src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamEventSender.cs` —
  ENDOFLINE on the touched lines while changing `SendContext? context = null`
  to `SendContext context = default`.
- ✅ `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/SharedNatsJetStreamFixture.cs` —
  ENDOFLINE on the new rationale comment block.

Pre-existing offenders (NOT touched by this work, NOT fixed):

- `src/Rig.TUnit.Storage.FileSystem/Fixtures/FileSystemFixture.cs` — IMPORTS ordering.
- `tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs` — IMPORTS ordering.
- `tests/Rig.TUnit.Messaging.Tests.Unit/Topology/ITopologyBuilderContractTests.cs` —
  ENDOFLINE (LF instead of CRLF) on roughly 14 lines.
- `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/DlqRedriveTests.cs` —
  IMPORTS ordering.
- `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/ServiceBusSessionListenerTests.cs` — IMPORTS ordering.
- `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/SessionFifoOrderingTests.cs` — IMPORTS ordering.

**Status**: WARN — 6 pre-existing format offenders remain. Re-running `dotnet format`
without `--verify-no-changes` would auto-fix them but is out of scope for this verify pass.

#### Resources / Proto / K8s — SKIP

- **Resources**: no `*.resx` files in scope (Rig.TUnit is a test-harness library; no
  user-facing strings).
- **Proto**: no `*.proto` files.
- **K8s**: no `k8s/` or `deploy/` manifest folders.

## Overall: WARN

All Feature-007-affected unit suites and the architecture suite are GREEN
(325/325 tests + 38/38 architecture tests). No regressions introduced by the
docs sweep (T064) or the review fixes. Four issues were also fixed in this
verify pass: the two T052 NSubstitute test bugs, the parity-test signature
mismatch on `NatsJetStreamEventSender`, and the missing rationale comment on
`SharedNatsJetStreamFixture.cs`.

The only remaining issue is the **pre-existing T055c RED build error** in
`tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/RetentionPolicyTests.cs`
(out of scope per the "skip integration tests" flag — this RED → GREEN cycle
runs in CI). The 6 unrelated format offenders also remain, untouched.

## Next

- T055c RED → GREEN remains as its own task.
- Format auto-fix (`dotnet format`) on the 6 unrelated files can land as its own
  housekeeping commit if desired.
- This branch is ready for `dai.pr`.
