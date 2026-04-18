# Quality Checklist — 004-provider-consistency-remediation

Verify the spec at `../spec.md` before running `/dotnet-ai-kit:plan`.

## Spec completeness

- [x] Feature ID + created date + status present
- [x] User stories have priorities (P1, P2)
- [x] Each user story has at least one acceptance scenario in Given/When/Then form
- [x] Each user story is independently testable
- [x] Functional requirements numbered FR-NNN
- [x] Key entities identified
- [x] Edge cases documented
- [x] Success criteria measurable (numbered SC-NNN)
- [x] Architecture Scope section (generic mode) present
- [x] `[NEEDS CLARIFICATION]` markers ≤ 3 (currently 3)
- [x] Defaults stated for each clarification marker so planning can proceed if unanswered

## Requirement quality

- [x] Every FR-NNN is verifiable by an automated test (architecture test, contract test, or integration test)
- [x] No FR relies on subjective language ("cleanly", "nicely") — each ties to a file path, type name, or measurable invariant
- [x] TDD discipline (FR-024, FR-025, FR-026) matches 003 R1 — no relaxation
- [x] Merge-gate thresholds identical to 003 (90% line / 85% branch / contract suite 100%)
- [x] No pre-existing test is forecast to regress (FR-026 + SC-008)

## User-story coverage

- [x] US1 TDD discipline — carries 003 R1 forward
- [x] US2 Phase 1 enforcement scaffolding
- [x] US3 Phase 2 test-file hygiene
- [x] US4 Phase 3 close gaps in existing providers (NoSql, Messaging, Caching, Storage, Security, Observability)
- [x] US5 Phase 4 create 4 missing packages + complete Docker
- [x] US6 Phase 5 Microservices depth (EventSourcing, Saga, Contracts)
- [x] US7 Phase 6 README polish + meta-package sync
- [x] US8 .NET 10 compatibility preserved (Pomelo 9 pin, Oracle-Free, Cosmos Linux emulator, in-process AppInsights)
- [x] US9 CI matrix extension

## Reference integrity

- [x] Library design doc referenced
- [x] Build prompt referenced
- [x] Session handoff referenced (acceptance criteria SC-009 ties to its checkboxes)
- [x] 003 baseline design referenced
- [x] `.claude/rules/*` conventions referenced

## Risks flagged in spec

- [x] Testcontainers version drift (4.6 vs 4.11) called out in `[NEEDS CLARIFICATION] #1`
- [x] Pact broker stub fidelity called out in `[NEEDS CLARIFICATION] #2`
- [x] TestFileOrganizationTests treatment of `*Contract.cs` called out in `[NEEDS CLARIFICATION] #3`
- [x] Pomelo MySql .NET 10 upgrade path documented (Edge Cases + US8)
- [x] Oracle container aspire#12036 mitigation documented (Edge Cases + US8)
- [x] Cosmos emulator on Windows runners documented (Edge Cases + FR-023 + US9)

## Scope boundaries (per library design §10)

- [x] No renames of existing public APIs (explicit in FR-008)
- [x] No test-file splits by method-under-test (explicit in FR-012)
- [x] No new infrastructure families (Messaging/Caching/Storage frozen — explicit in Overview)
- [x] No NuGet publication in this feature (out of scope)
- [x] No feature flags (explicit in Overview)

## Observed-state alignment

- [x] `Rig.TUnit.Security` base already exists — spec notes planning gap matrix is stale on this row (Overview + Edge Cases)
- [x] `Rig.TUnit.Docker` has fixture already — spec treats as "complete the template", not "create" (FR-017, US5)
- [x] Pinned package versions reconciled with `Directory.Packages.props` (Overview "Observed deltas" block)
