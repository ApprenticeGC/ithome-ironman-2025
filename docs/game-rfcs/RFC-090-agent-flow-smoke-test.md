# RFC-090: Agent Flow Smoke Test (Track: game)

- Start Date: 2025-09-16
- RFC Author: System
- Status: Draft
- Track: game

## Summary

A minimal smoke test RFC designed to validate the end-to-end autonomous flow system. Creates simple micro-tasks that exercise issue generation, assignment, PR creation, CI, auto-ready, auto-merge, and auto-advance workflows without implementing complex features.

## Motivation

- **Problem**: Need a safe, repeatable way to test the RFC-090 autonomous flow system
- **Goals**: 
  - Validate micro-issue generation from RFCs
  - Test Copilot assignment and PR workflows
  - Exercise CI, auto-ready, and auto-merge processes
  - Verify auto-advance to next micro-issue
- **Non-goals**: Implement actual game features or complex functionality

## Detailed Design

This RFC provides a controlled test environment for the autonomous flow by creating simple file operations that can be safely automated. Each micro-task involves basic file creation/modification that validates workflow steps without side effects.

### Architecture
- Uses existing `docs/game-rfc-test-96/` directory pattern
- Simple placeholder file operations
- Build validation through dotnet build
- Conventional commits for PR tracking

### Failure/Recovery
- Watchdog workflows will reset on CI failures
- Dedupe handles duplicate issue creation
- Stalled PR cleanup handles stuck workflows

## Alternatives Considered

- **Option A**: Create complex game feature tests (rejected - too risky for flow validation)
- **Option B**: Use existing RFC-092/093/094/095 pattern (chosen - proven safe approach)

## Risks & Mitigations

- **Risk**: Test files accumulate in repository
- **Mitigation**: Final cleanup step removes test artifacts and logs completion

## Implementation Plan (Micro Issues)

When creating GitHub issues, break this RFC into micro tasks `RFC-090-YY` with clear acceptance criteria to drive Copilot.

| Micro | Title | Acceptance Criteria |
|-------|-------|---------------------|
| 01    | Create smoke test placeholder | - [ ] Add `docs/game-rfc-test-96/PLACEHOLDER.md` with `ok-96` and RFC reference<br>- [ ] Build passes<br>- [ ] PR title follows Conventional Commits |
| 02    | Update smoke test placeholder | - [ ] Edit `docs/game-rfc-test-96/PLACEHOLDER.md` to include `updated-96`<br>- [ ] Build passes<br>- [ ] PR title follows Conventional Commits |
| 03    | Clean up smoke test | - [ ] Remove `docs/game-rfc-test-96/PLACEHOLDER.md`<br>- [ ] Append `Flow smoke tested: RFC-090` to `AGENTS.md`<br>- [ ] Build passes<br>- [ ] PR follows Conventional Commits |