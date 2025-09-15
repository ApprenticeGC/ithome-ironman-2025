# GitHub Copilot Coding Agent Instructions (ithome-ironman-2025)

Project overview
- Generic C# game + editor (TUI-first) with a 4-tier service architecture and engine simulation via profiles (Unity/Godot behaviors simulated, no engine UI).
- Contracts are stable in Tier 1; Tier 3 owns real behavior; Tier 4 providers are optional for pluggable backends. See `docs/RFC/`.

What you work on
- Implement narrowly scoped tasks tied to a single RFC (or a small part of one) under `docs/RFC/`.
- Prefer editing existing files; avoid unrelated refactors in the same PR.

Branch and PR rules
- Branch: `copilot/rfc-XXX-short-slug`.
- PR title: Conventional Commits, e.g., `feat(audio): implement RFC-014 ECS adapter sample`.
- PR body: link the RFC and `docs/playbook/PLAYBOOK.md#pr-checklist`; include a checklist of acceptance criteria.

Build and test
- Run locally before opening PRs:
  - `dotnet build ./dotnet -warnaserror`
  - `dotnet test ./dotnet --no-build`

Architecture guardrails
- Do NOT make core contracts a plugin. Tier 1 contracts live in shared assemblies; keep them stable.
- Tier 3 is the real service (policy/state/orchestration). Use providers (Tier 4) only when backend variability is required.
- Prefer capability facets (probeable optional features) or message envelopes over fragmenting contracts.
- UI remains TUI; profiles simulate engine behaviors by swapping providers/systems.

Coding standards
- Follow `.editorconfig` at repo root and category configs.
- Keep changes scoped; no mass renames; no unrelated formatting.

Definition of done
- All acceptance criteria in the RFC are met.
- `dotnet build` and `dotnet test` pass.
- PR follows Conventional Commits; links the RFC and PR checklist.

Where to look
- Flow docs: `docs/flow-rfcs/` (agent automation/process docs)
- Game RFCs (single source of truth): `docs/game-rfcs/`
- Multi-modal + profiles: `docs/game-rfcs/RFC-010-multi-modal-ui.md`, `docs/game-rfcs/RFC-011-ui-mode-profiles.md`
- ECS behavior: `docs/game-rfcs/RFC-014-ecs-behavior-composition.md`

Automation flow
- Create micro RFC issues with `.github/ISSUE_TEMPLATE/rfc.yml` (use IDs like `RFC-014-01`).
- Assign Copilot using the script or manually:
  - `python .github/scripts/create_issue_assign_copilot.py --owner <org> --repo <name> --title "RFC-014-01: ..." --body "..."`
- CI: `.github/workflows/ci.yml` runs on PRs.
- Auto-advance on merge: `.github/workflows/auto-advance-micro.yml` selects the next micro issue in sequence and assigns Copilot.
- Watchdog on failure: `.github/workflows/agent-watchdog.yml` closes the failed PR/branch, closes the issue, recreates the issue, and re-assigns Copilot.

Notes
- Do not source control “micro RFC” files. Micro RFCs exist only as GitHub issues derived from a Game RFC.
- Author new RFCs using `docs/rfc-templates/RFC-TEMPLATE.md` and keep them under `docs/RFC/`.

Flow smoke tested: RFC-090
