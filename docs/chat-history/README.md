# Chat History

Create timestamped session records in UTC+8.

## Quick usage

- One-off (no commit/push):
  - Run in repo root:
    - PowerShell: `./scripts/new-chat-history.ps1`
- Auto-commit and push:
  - PowerShell: `./scripts/new-chat-history.ps1 -Commit -Push`

## VS Code Task (local)

A local task is provided at `.vscode/tasks.json` (ignored by git).
- Open the Command Palette and run: "Tasks: Run Task" -> "New Chat History (UTC+8)".
- This will generate a file, commit it with a conventional message, and push to `main`.

## Output location

Files are created in `docs/chat-history/` named like:
- `YYYY-MM-DD_HH-mm-ss-UTC+8.md`

## Notes

- The script uses your current git config and remote.
- If push fails (e.g. auth), rerun without `-Push` and push manually.
