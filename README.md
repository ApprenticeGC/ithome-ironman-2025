# iThome Ironman 2025

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
