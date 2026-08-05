---
description: Commit all changes and create a GitHub pull request
argument-hint: "[additional context]"
---
Create a GitHub pull request containing all changes in the current repository.

1. Inspect the repository status, diffs (including staged changes), current branch, and remote. Include every tracked and untracked worktree change in the pull request, but do not include ignored files.
2. Treat `${ARGUMENTS:-no additional context provided}` as optional additional context describing what changed. Use it together with the current conversation and task context when choosing the branch name, commit message, pull request title, and pull request body.
3. Determine whether that context contains an unambiguous GitHub issue number. Accept forms such as `123`, `#123`, or a GitHub issue URL; do not infer an issue number merely from unrelated numbers in source code or command output.
4. Create a descriptive non-default branch if currently on the repository's default branch. When creating the branch, use a lowercase kebab-case name in the form `<prefix>/<short-description>`. Choose the most appropriate prefix from this list:
   - `feature/` - new functionality or user-facing capabilities
   - `bugfix/` - defect corrections
   - `hotfix/` - urgent production fixes
   - `chore/` - maintenance, configuration, or dependency updates
   - `docs/` - documentation-only changes
   - `refactor/` - internal restructuring without behavior changes
   - `test/` - test additions or corrections
   - `ci/` - continuous integration workflow changes
   - `build/` - build system or packaging changes
   - `perf/` - performance improvements
   - `security/` - security hardening or vulnerability fixes
   - `release/` - release preparation
   - `revert/` - reverting an earlier change

   Commit all changes with a concise commit message, including any changes that were already staged. Do not rewrite existing commits.
5. Push the branch and create a non-draft pull request with `gh`. Choose a concise title and write a concise body that provides an overview of the completed work and explains what the pull request does.
   - If an issue number was identified, include `Closes #<issue-number>` in the pull request body.
6. Return the pull request URL.

Do not claim success unless the push and pull request creation both complete successfully.
