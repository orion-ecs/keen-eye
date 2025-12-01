# Installs git hooks by configuring git to use the .githooks directory
# Usage: .\scripts\install-hooks.ps1

$ErrorActionPreference = "Stop"

Write-Host "Installing git hooks..."

# Configure git to use .githooks directory
git config core.hooksPath .githooks

Write-Host "Git hooks installed successfully."
Write-Host ""
Write-Host "Hooks enabled:"
Write-Host "  - pre-commit: Validates code formatting"
Write-Host "  - pre-push:   Runs build and tests"
Write-Host ""
Write-Host "To skip hooks temporarily, use:"
Write-Host "  git commit --no-verify"
Write-Host "  git push --no-verify"
