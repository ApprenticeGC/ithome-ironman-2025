# RFC-090 Smoke Test Usage

This file documents how to use the RFC-090 Agent Flow Smoke Test to validate the autonomous flow system.

## Purpose

The `RFC-090-agent-flow-smoke-test.md` provides a minimal, safe way to test the complete autonomous flow:
1. Issue generation from RFCs
2. Copilot assignment and PR creation  
3. CI, auto-ready, auto-merge workflows
4. Auto-advance to next micro-issue

## How to Run

### Method 1: GitHub Actions UI
1. Go to Actions → `generate-micro-issues`
2. Click "Run workflow" 
3. Set inputs:
   - `rfc_path`: `docs/game-rfcs/RFC-090-agent-flow-smoke-test.md`
   - `assign_mode`: `bot` (to assign to Copilot)
4. Run and watch the automation

### Method 2: GitHub CLI
```bash
gh workflow run generate-micro-issues \
  --field rfc_path="docs/game-rfcs/RFC-090-agent-flow-smoke-test.md" \
  --field assign_mode="bot"
```

## Expected Flow

1. **Issue Generation**: Creates 3 micro-issues (RFC-090-01, RFC-090-02, RFC-090-03)
2. **Assignment**: First issue auto-assigned to Copilot
3. **PR Creation**: Copilot creates draft PR for first micro-issue
4. **CI**: Runs build/test on PR
5. **Auto-Ready**: Flips draft to ready on CI success  
6. **Auto-Merge**: Approves and merges PR
7. **Auto-Advance**: Assigns next micro-issue to Copilot
8. **Repeat**: Process continues through all micro-issues

## Safety

- Uses simple placeholder file operations
- Follows proven RFC-092/093/094/095 patterns
- Final cleanup step removes test artifacts
- Cannot break existing functionality

## Troubleshooting

See `docs/flow-rfcs/RFC-090-agent-flow-ops.md` for operational guidance and troubleshooting steps.