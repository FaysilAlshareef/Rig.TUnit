# Branch Protection — `master` ruleset & `v*` tag protection

Applied via `gh api` once Phase E (CI refactor) is merged so the required-status-check names
match the refactored workflow.

---

## 1 · Repository ruleset for `refs/heads/master`

```bash
gh api -X POST repos/FaysilAlshareef/Rig.TUnit/rulesets \
  --input - <<'JSON'
{
  "name": "master-protection",
  "target": "branch",
  "enforcement": "active",
  "conditions": {
    "ref_name": {
      "include": ["refs/heads/master"],
      "exclude": []
    }
  },
  "rules": [
    { "type": "deletion" },
    { "type": "non_fast_forward" },
    { "type": "required_linear_history" },
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 1,
        "dismiss_stale_reviews_on_push": true,
        "require_code_owner_review": true,
        "require_last_push_approval": true,
        "required_review_thread_resolution": true,
        "automatic_copilot_code_review_enabled": false,
        "allowed_merge_methods": ["squash", "rebase"]
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": true,
        "required_status_checks": [
          { "context": "Build + Unit + Arch" },
          { "context": "Coverage summary" },
          { "context": "Architecture tests" },
          { "context": "Pack & validate" },
          { "context": "Commit discipline gate (Phase 1 minimal)" },
          { "context": "Link checker" },
          { "context": "lint" }
        ]
      }
    },
    {
      "type": "commit_message_pattern",
      "parameters": {
        "operator": "regex",
        "pattern": "^(test|feat|refactor|fix|chore|docs|style|perf|build|ci|revert|red|green|release)(\\(.+\\))?:"
      }
    }
  ],
  "bypass_actors": []
}
JSON
```

**Notes**:
- `bypass_actors: []` — even the owner cannot bypass; if a recovery override is needed, add an
  entry with `actor_type: "RepositoryRole"`, `actor_id: 5` (admin), `bypass_mode: "always"` and
  remove it once the operation is complete.
- The required-status-check `context` values must match **exactly** the GitHub job names emitted
  by `ci.yml` and `release.yml`. Confirm with `gh run view <run-id> --json jobs` after the CI
  refactor merges and adjust this ruleset before applying.
- `lint` is the job name from `commit-msg-lint.yml`. Rename either side if you rename one.
- `allowed_merge_methods` excludes `merge`; squash/rebase only keeps `master` linear and the
  `commit_message_pattern` enforceable.

---

## 2 · Tag protection for `refs/tags/v*`

```bash
gh api -X POST repos/FaysilAlshareef/Rig.TUnit/rulesets \
  --input - <<'JSON'
{
  "name": "release-tag-protection",
  "target": "tag",
  "enforcement": "active",
  "conditions": {
    "ref_name": {
      "include": ["refs/tags/v*"],
      "exclude": []
    }
  },
  "rules": [
    { "type": "creation" },
    { "type": "deletion" },
    { "type": "update" }
  ],
  "bypass_actors": [
    {
      "actor_type": "RepositoryRole",
      "actor_id": 5,
      "bypass_mode": "always"
    }
  ]
}
JSON
```

**Notes**:
- `actor_id: 5` is GitHub's stable repository-role ID for **admin**. Only admins (the owner) can
  create/update/delete tags matching `v*`.
- The "rules" being enforced means *only bypass actors can perform these actions* — admins
  effectively become the only tag pushers.
- This blocks accidental tag pushes from CI tokens or future contributor PATs.

---

## 3 · Verifying the rulesets

```bash
# List active rulesets
gh api repos/FaysilAlshareef/Rig.TUnit/rulesets

# Inspect a specific ruleset (replace ID)
gh api repos/FaysilAlshareef/Rig.TUnit/rulesets/<id>
```

End-to-end smoke test:

```bash
# 1. Open a draft PR with a failing required check.
#    Try to merge via the API:
gh pr merge <num> --squash
# Expected: 405 Method Not Allowed — required status checks not met.

# 2. Try to push directly to master from a non-admin token.
git push origin some-branch:master
# Expected: ! [remote rejected] some-branch -> master (protected branch hook declined)

# 3. Try to push a v-tag from a non-admin token.
git tag v0.0.0-rejected && git push origin v0.0.0-rejected
# Expected: ! [remote rejected] v0.0.0-rejected (tag protection rule)
```

---

## 4 · Order of application

1. **Merge Phase E (CI refactor) to `master`** — this stabilises required-check names.
2. **Run a sample PR** — note the exact job names that show up under "Checks". Compare
   against the `required_status_checks` array above; update the JSON if any differ.
3. **Apply the master ruleset** (Section 1).
4. **Apply the tag-protection ruleset** (Section 2).
5. **Open a probe PR** that intentionally fails one required check — verify it cannot be
   merged. Close it.
6. **Document** the required-check list in `CONTRIBUTING.md` (Phase B-4).

---

## 5 · Rollback

If a ruleset blocks a legitimate operation (e.g. the very PR that introduces the rules):

```bash
gh api repos/FaysilAlshareef/Rig.TUnit/rulesets/<id> -X PATCH \
  -f enforcement=disabled
```

Re-enable with `-f enforcement=active`. Never delete the ruleset; `disabled` preserves the
configuration for re-enable.
