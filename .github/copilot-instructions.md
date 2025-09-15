# Copilot Coding Agent – Repo Instructions

Scope and priorities
- Implement one RFC-scoped task per PR (from `docs/game-rfcs/`). Keep PRs small and self-contained.
- Prefer editing existing files; avoid unrelated refactors.

Build and test
- Always run locally before PR:
  - `dotnet build ./dotnet -warnaserror`
  - `dotnet test ./dotnet --no-build`

PR requirements
- Title uses Conventional Commits, e.g., `feat(ai): implement RFC-007 akka orchestrator provider`.
- Body links the RFC and `docs/playbook/PLAYBOOK.md#pr-checklist`.
- Describe what changed, how it’s tested, and any follow-ups.

Architecture guardrails
- 4-tier services: Tier 1 contracts stable; Tier 2 proxies mechanical; Tier 3 owns behavior; Tier 4 providers optional.
- Do not define new public contracts inside plugins. Use capability facets or message envelopes when extending behavior.
- UI is TUI-first; profiles simulate Unity/Godot behaviors by swapping providers/systems.

Coding standards and scope
- Follow `.editorconfig` and existing directory structure.
- Keep changes scoped: no mass renames/formatting; no unrelated file moves.

Verification checklist (pre-PR)
- [ ] Code compiles with warnings as errors.
- [ ] Tests pass locally (if present).
- [ ] Changes limited to the RFC’s scope.
- [ ] PR title/body follow rules and link to the RFC.
