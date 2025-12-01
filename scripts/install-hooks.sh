#!/usr/bin/env bash
# Installs git hooks by configuring git to use the .githooks directory
# Usage: ./scripts/install-hooks.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
HOOKS_DIR="$REPO_ROOT/.githooks"

echo "Installing git hooks..."

# Configure git to use .githooks directory
git config core.hooksPath .githooks

# Make hooks executable
chmod +x "$HOOKS_DIR/pre-commit"
chmod +x "$HOOKS_DIR/pre-push"

echo "Git hooks installed successfully."
echo ""
echo "Hooks enabled:"
echo "  - pre-commit: Validates code formatting"
echo "  - pre-push:   Runs build and tests"
echo ""
echo "To skip hooks temporarily, use:"
echo "  git commit --no-verify"
echo "  git push --no-verify"
