#!/usr/bin/env bash
# Start a new Tiny Orbit build run on its own branch off main.
# Usage: ./scripts/new-run.sh <model>-<date>   e.g. ./scripts/new-run.sh opus-4.8-2026-06-20
set -euo pipefail

name="${1:-}"
if [ -z "$name" ]; then
  echo "usage: $0 <run-name>   e.g. $0 opus-4.8-2026-06-20" >&2
  exit 1
fi

branch="run/$name"
git checkout main
git checkout -b "$branch"

echo
echo "On branch $branch (off main baseline)."
echo "Next: open this folder in Unity 6000.4.11f1, then paste prompts/START-HERE.md"
echo "as the first message to the build session."
