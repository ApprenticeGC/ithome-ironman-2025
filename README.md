# iThome Ironman 2025

[![CI](https://github.com/ApprenticeGC/ithome-ironman-2025/actions/workflows/ci.yml/badge.svg)](https://github.com/ApprenticeGC/ithome-ironman-2025/actions/workflows/ci.yml)
[![LOC](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/ApprenticeGC/ithome-ironman-2025/main/.github/badges/dotnet_game_loc.json)](./.github/badges/dotnet_game_loc.json)
[![CI minutes (month)](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/ApprenticeGC/ithome-ironman-2025/main/.github/badges/runner-usage.json)](https://github.com/ApprenticeGC/ithome-ironman-2025/actions/workflows/runner-usage-badge.yml)

Concise repo for notes, demos, and artifacts related to the 2025 iThome Ironman challenge. It uses a lightweight docs-first workflow with small, focused commits and simple automation.

## Repo layout

- `docs/` – Playbook, RFCs, and supporting docs
- `dotnet/` – .NET projects (libs, samples, tests)
- `build/` – Generated artifacts (ignored in development)
- `AGENTS.md` – Coding agent rules (GitHub Copilot, etc.)
- `CLAUDE.md` – Claude-specific agent rules

## Getting started

Prerequisites depend on what you want to run. For .NET examples:

- Install .NET SDK 8.0+
- Optionally: a recent Node/Python runtime if examples appear later

Common actions:

- Build .NET: `dotnet build`
- Test .NET: `dotnet test`

## Contributing

- Follow the Playbook in `docs/playbook/PLAYBOOK.md`
- Use Conventional Commits for messages (e.g., `feat:`, `fix:`, `docs:`, `chore:`)
- Prefer small PRs with clear intent

## Notes

- Main may be fast-moving initially; branch protection can be added later.
- Keep edits minimal and focused; avoid formatting-only diffs unless needed.

### Autonomous Flow Test

This repository includes RFC-096, an autonomous flow test designed to validate the end-to-end Copilot workflow described in RFC-090. The test creates simple micro issues that exercise the complete autonomous flow: micro-issue generation → Copilot assignment → PR creation → CI → auto-ready → auto-merge → auto-advance.

- **Test RFC**: `docs/game-rfcs/RFC-096-autonomous-flow-test-20250916.md`
- **Test Timestamp**: 2025-09-16T01:36:57Z  
- **Test Class**: `dotnet/TestLib/AutonomousFlowTest.cs`

## Badges

[![CI](https://github.com/ApprenticeGC/ithome-ironman-2025/actions/workflows/ci.yml/badge.svg)](https://github.com/ApprenticeGC/ithome-ironman-2025/actions/workflows/ci.yml)
[![LOC](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/ApprenticeGC/ithome-ironman-2025/main/.github/badges/dotnet_game_loc.json)](./.github/badges/dotnet_game_loc.json)
[![CI minutes (month)](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/ApprenticeGC/ithome-ironman-2025/main/.github/badges/runner-usage.json)](https://github.com/ApprenticeGC/ithome-ironman-2025/actions/workflows/runner-usage-badge.yml)
