#!/usr/bin/env bash
# Fetch the canonical Contributor Covenant 2.1 text and write it to CODE_OF_CONDUCT.md
# at the repo root, substituting the project contact.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
TARGET="$REPO_ROOT/CODE_OF_CONDUCT.md"
CONTACT="${RIG_TUNIT_COC_CONTACT:-faysilalshareef@gmail.com}"
UPSTREAM="https://www.contributor-covenant.org/version/2/1/code_of_conduct/code_of_conduct.md"

echo "Fetching Contributor Covenant 2.1 from $UPSTREAM"
tmp="$(mktemp)"
trap 'rm -f "$tmp"' EXIT
curl -fsSL "$UPSTREAM" -o "$tmp"

# The upstream template contains a [INSERT CONTACT METHOD] placeholder we replace.
sed "s|\\[INSERT CONTACT METHOD\\]|$CONTACT|g" "$tmp" > "$TARGET"

echo "Wrote $TARGET (contact: $CONTACT)"
