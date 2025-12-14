#!/bin/bash
# CI Agent Push Check Hook
# Prevents CI agents from finishing if there are unpushed commits.
# This ensures all work is pushed before the agent session ends.

# Only enforce in CI/remote agent environments
if [ -z "$CI" ] && [ -z "$CLAUDE_CODE_REMOTE" ]; then
    # Not a CI agent session, allow stop
    exit 0
fi

# Check if we're in a git repository
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    # Not a git repo, allow stop
    exit 0
fi

# Get current branch
current_branch=$(git rev-parse --abbrev-ref HEAD 2>/dev/null)
if [ -z "$current_branch" ] || [ "$current_branch" = "HEAD" ]; then
    # Detached HEAD or no branch, allow stop
    exit 0
fi

# Check for uncommitted changes
if ! git diff --quiet 2>/dev/null || ! git diff --cached --quiet 2>/dev/null; then
    echo "ERROR: You have uncommitted changes."
    echo "Please commit your changes before finishing."
    echo ""
    echo "Uncommitted files:"
    git status --short
    exit 1
fi

# Check for untracked files that might be important
untracked=$(git ls-files --others --exclude-standard 2>/dev/null)
if [ -n "$untracked" ]; then
    # Check if any untracked files are source code (not build artifacts)
    important_untracked=$(echo "$untracked" | grep -E '\.(cs|fs|vb|csproj|fsproj|sln|json|xml|md|sh|ps1|yaml|yml)$' || true)
    if [ -n "$important_untracked" ]; then
        echo "WARNING: You have untracked source files that may need to be committed:"
        echo "$important_untracked"
        echo ""
        echo "If these files should be committed, please add and commit them."
        echo "If they should be ignored, add them to .gitignore."
        # Warning only, don't block for untracked files
    fi
fi

# Check if there's an upstream branch
upstream=$(git rev-parse --abbrev-ref "@{upstream}" 2>/dev/null)

if [ -z "$upstream" ]; then
    # No upstream set - check if we have any commits on this branch
    commits=$(git log --oneline 2>/dev/null | head -1)
    if [ -n "$commits" ]; then
        echo "ERROR: Branch '$current_branch' has no upstream tracking branch."
        echo "You have local commits that haven't been pushed."
        echo ""
        echo "Push your changes with:"
        echo "  git push -u origin $current_branch"
        exit 1
    fi
    # No commits and no upstream, allow stop
    exit 0
fi

# Check for unpushed commits
unpushed=$(git log "$upstream"..HEAD --oneline 2>/dev/null)

if [ -n "$unpushed" ]; then
    commit_count=$(echo "$unpushed" | wc -l)
    echo "ERROR: You have $commit_count unpushed commit(s) on branch '$current_branch'."
    echo ""
    echo "Unpushed commits:"
    echo "$unpushed"
    echo ""
    echo "Push your changes with:"
    echo "  git push origin $current_branch"
    echo ""
    echo "CI agents must push all work before finishing."
    exit 1
fi

# All good - no unpushed changes
exit 0
