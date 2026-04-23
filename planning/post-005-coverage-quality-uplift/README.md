# Planning — Post-005 Coverage & Quality Uplift

**Scope**: Feature 006 — raise overall coverage from 80.4 % line / 66.4 % branch to the stated
gates (≥ 90 % line, ≥ 85 % branch per package), repair the CI pipeline gaps found during the
2026-04-21 coverage scan, fix benchmark infrastructure, and rewrite the root README.

---

## Why this feature exists

The 2026-04-21 CI scan (branch `ci/coverage-scan`, run id `24712477011`) revealed that:

- **29 of 40 source packages** fail the ≥ 90 % line-coverage gate — the overall rate is **80.4 %**.
- **6 integration-test projects** are never executed in production CI (`Core`, `Ci`, `Grpc`, `Http`,
  `WebAPI`, `Mediator`).
- The **coverage gate** in `ci.yml` is `continue-on-error: true` with no scheduled re-enable date.
- The **benchmark baseline** (`benchmarks/baseline-005.json`) is empty (`"benchmarks": {}`), making
  regression detection non-functional.
- `InProcessEmitBenchmarkConfig` targets **.NET 8**, not .NET 10 — benchmark numbers are wrong for
  22 of 55 benchmark files.
- The **root README** does not describe the solution accurately.

---

## File index

| File | Purpose |
|------|---------|
| [README.md](README.md) | This index |
| [Real-Coverage-Gap-Matrix.md](Real-Coverage-Gap-Matrix.md) | Per-package line/branch rates from the scan; class-level zeros; root-cause patterns |
| [Feature-006-Roadmap.md](Feature-006-Roadmap.md) | Phased delivery plan — all tasks, FR refs, success criteria, effort table, risks |
| [CI-Pipeline-Gap-Audit.md](CI-Pipeline-Gap-Audit.md) | Six missing integration-test projects, disabled coverage gate — location and fix |
| [Benchmark-Remediation-Plan.md](Benchmark-Remediation-Plan.md) | Runtime-version fix, baseline population, visualisation strategy |
| [README-Rewrite-Plan.md](README-Rewrite-Plan.md) | Fourteen-section README template with content guidance per section |

---

## Order of execution

1. **CI-Pipeline-Gap-Audit.md** — fix gate and add missing test projects first; everything else
   should be measured against a working gate.
2. **Real-Coverage-Gap-Matrix.md** — use as the ground-truth backlog for coverage work; do not
   start writing tests before reading this document.
3. **Feature-006-Roadmap.md** — follow phase order strictly; Phase 1 unblocks Phase 2.
4. **Benchmark-Remediation-Plan.md** — can run in parallel with Phases 2–3 of the roadmap.
5. **README-Rewrite-Plan.md** — written last, after phases are complete so docs reflect reality.

---

## Branch

`feat/006-coverage-quality-uplift`

---

## Related planning folders

- `planning/post-004-remediation/` — Feature 005 origin; coverage gap matrix v1 (predicted, not
  measured)
- `planning/post-005-phase-1/SharedFixture-Audit.md` — Phase 3 T066 fixture isolation work that
  feeds coverage for SQL / messaging providers
- `coverage-scan-results/` — raw CI artefacts: `summary.csv`, `merged.cobertura.xml`,
  `summary.md` (branch `ci/coverage-scan`)
