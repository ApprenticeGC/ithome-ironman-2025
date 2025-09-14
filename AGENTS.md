# GitHub Coding Agent Rules

<!-- include: docs/playbook/PLAYBOOK.md#process -->
<!-- /include -->

<!-- include: docs/playbook/PLAYBOOK.md#coding-standards -->
<!-- /include -->

<!-- include: docs/playbook/PLAYBOOK.md#pr-checklist -->
<!-- /include -->

**GitHub-specific deltas**
- Run `dotnet build` and `dotnet test` before opening a PR.
- PR titles must follow Conventional Commits.
- Keep changes scoped: avoid unrelated refactors in the same PR.
- Reference files by relative path (wrap with backticks) and prefer editing existing files.
- Link to relevant playbook sections when citing rules, e.g. `docs/playbook/PLAYBOOK.md#pr-checklist`.
