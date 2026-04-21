# Quick Start — 006-coverage-quality-uplift

Get Phase 1 (the blocking CI foundation) merged in under 30 minutes.

---

## Prerequisites

- Branch `feat/006-coverage-quality-uplift` checked out from `master`
- `git status` is clean

---

## Step 1: Open the CI workflow

```
.github/workflows/ci.yml
```

---

## Step 2: Extend the integration-core matrix (T001)

Find line ~294:

```yaml
        area: [Concurrency, Docker, HealthChecks, Parallelism, Resilience]
```

Replace with:

```yaml
        area: [Concurrency, Docker, HealthChecks, Parallelism, Resilience, Core, Ci, Grpc, Http, WebAPI, Mediator]
```

---

## Step 3: Annotate the coverage gate (T002)

Find line ~360–363:

```yaml
      - name: Enforce coverage threshold (line-rate ≥ 0.90, branch-rate ≥ 0.85)
        continue-on-error: true
```

Add the annotation comment:

```yaml
      - name: Enforce coverage threshold (line-rate ≥ 0.90, branch-rate ≥ 0.85)
        # Disabled 2026-04-20; re-enabled by feat/006 T090
        continue-on-error: true
```

---

## Step 4: Commit

```bash
git add .github/workflows/ci.yml
git commit -m "$(cat <<'EOF'
green(T001): extend integration-core matrix with 6 missing projects

Adds Core, Ci, Grpc, Http, WebAPI, Mediator to the integration-core
CI matrix. The ${{ matrix.area }} parameterisation already handles
both build and test paths — no step body changes needed.

green(T002): annotate coverage gate with re-enable reference

Adds inline comment to continue-on-error: true at ci.yml:363 per
feat/006 spec. Gate will be hardened in T090 (Phase 7) after all
packages reach ≥ 90 % line / ≥ 85 % branch.

CI change — no production code affected.
EOF
)"
```

---

## Step 5: Push and open PR

```bash
git push -u origin feat/006-coverage-quality-uplift
gh pr create \
  --title "feat(006) Phase 1: CI foundation — integration matrix + gate annotation" \
  --body "Extends integration-core matrix and annotates coverage gate. Phase 1 of feature 006."
```

---

## Step 6: Verify CI (T003)

Watch the `Integration — Core` job matrix in the Actions tab. Confirm:
- ✅ Core
- ✅ Ci
- ✅ Grpc (if flaky: add `continue-on-error: true` per-entry, create T001a)
- ✅ Http (same as above)
- ✅ WebAPI
- ✅ Mediator

Record the run ID from the Actions URL and paste it into the PR description.

Merge once all 6 are GREEN.

---

## Next: Phases 2, 3, 4 (parallel)

After Phase 1 merges, open parallel sub-branches for each pattern:

```bash
# Pattern A (builders)
git checkout -b feat/006-phase2-builders

# Pattern B (assertions)
git checkout -b feat/006-phase3-assertions

# Pattern C (helpers)
git checkout -b feat/006-phase4-helpers
```

Reference builder test: `tests/Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit/UsePostgresRigBuilderExtensionsTests.cs`

First task in Phase 2: `T010` — copy the Postgres extension test file to `SqlServerBuilderTests.cs`, substitute `SqlServer` for `Postgres`, run to confirm RED, then check that the source already exists (single `green` commit).
