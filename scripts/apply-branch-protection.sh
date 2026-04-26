#!/usr/bin/env bash
# Apply the master-protection ruleset and v* tag protection.
# Run AFTER the CI refactor PR has merged so the required-status-check names
# in this script match the actual job names emitted by ci.yml.
#
#   bash scripts/apply-branch-protection.sh
#
# Verify with:
#   gh api repos/<owner>/<repo>/rulesets

set -euo pipefail

REPO="${RIG_TUNIT_REPO:-FaysilAlshareef/Rig.TUnit}"

echo "==> Master branch protection ruleset"
gh api -X POST "repos/$REPO/rulesets" --input - <<'JSON'
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
    }
  ],
  "bypass_actors": []
}
JSON

echo ""
echo "==> v* tag protection ruleset"
gh api -X POST "repos/$REPO/rulesets" --input - <<'JSON'
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

echo ""
echo "Done. List active rulesets:"
echo "  gh api repos/$REPO/rulesets"
