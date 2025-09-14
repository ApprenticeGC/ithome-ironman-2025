# Chat History

Create timestamped session records in UTC+8.

## Quick usage

- One-off (no commit/push):
  - Run in repo root:
    - PowerShell: `./scripts/new-chat-history.ps1`
- Auto-commit and push:
  - PowerShell: `./scripts/new-chat-history.ps1 -Commit -Push`

## Include discussion details

- Inline summary:
  - `./scripts/new-chat-history.ps1 -Summary "What was discussed, decisions made, next steps."`
- Title and summary:
  - `./scripts/new-chat-history.ps1 -Title "Sprint planning" -Summary "Prioritized tasks and set milestones."`
- External notes file:
  - `./scripts/new-chat-history.ps1 -NotesPath ./notes/today.md`
- Full transcript from clipboard:
  - Copy the chat text, then run: `./scripts/new-chat-history.ps1 -FromClipboard`
- Full transcript from a file:
  - `./scripts/new-chat-history.ps1 -DiscussionPath ./exports/chat.txt`
- Configure number of recent commits (default 10):
  - `./scripts/new-chat-history.ps1 -Commits 25`

## VS Code Task (local)

A local task is provided at `.vscode/tasks.json` (ignored by git).
- Open the Command Palette and run: "Tasks: Run Task" -> "New Chat History (UTC+8)".
- This will generate a file, commit it with a conventional message, and push to `main`.
- You can add arguments to the task or run the script directly for summaries/notes.

## Output location

Files are created in `docs/chat-history/` named like:
- `YYYY-MM-DD_HH-mm-ss-UTC+8.md`

## Notes

- The script uses your current git config and remote.
- If push fails (e.g. auth), rerun without `-Push` and push manually.
- Before auto-commit, the script runs `gitleaks` on `docs/chat-history/` if available. If leaks are found, it aborts commit/push. Use `-NoLeakScan` to bypass (not recommended).
