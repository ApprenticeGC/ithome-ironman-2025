# RFC-100: GitHub Projects Integration Test (Track: game)

- Start Date: 2025-09-17
- RFC Author: System
- Status: Draft
- Track: game

## Summary

A test RFC designed to validate the GitHub Projects v2 integration with the automation pipeline. Creates simple micro-tasks that exercise project board workflow integration, ensuring RFC issues are properly tracked through the kanban board workflow via the `update-project-board.yml` workflow.

## Motivation

- **Problem**: Need to verify GitHub Projects v2 integration works correctly with automation pipeline
- **Goals**: 
  - Validate that RFC issues trigger project board tracking workflows
  - Test project tracking comments are added to new RFC issues
  - Verify PR events are properly logged for project board integration
  - Ensure completion tracking works when PRs are merged
- **Non-goals**: Implement actual GitHub Projects v2 API integration or complex project features

## Detailed Design

This RFC provides a controlled test environment for the GitHub Projects integration by creating simple file operations that can be safely automated. Each micro-task exercises different aspects of the project board workflow integration without side effects.

### Architecture
- Uses `docs/game-rfc-test-100/` directory pattern for safety
- Simple placeholder file operations
- Build validation through dotnet build
- Conventional commits for PR tracking
- Tests existing `update-project-board.yml` workflow

### Project Integration Points
- Issue creation triggers project board detection
- PR creation/updates logged for project tracking
- Merge completion tracked for project board updates
- Workflow events captured for project board automation

### Failure/Recovery
- Watchdog workflows will reset on CI failures
- Dedupe handles duplicate issue creation
- Stalled PR cleanup handles stuck workflows
- Project board workflow logging provides debugging info

## Alternatives Considered

- **Option A**: Implement full GitHub Projects v2 API integration (rejected - too complex for test validation)
- **Option B**: Use existing RFC-090/RFC-092 test pattern (chosen - proven safe approach)

## Risks & Mitigations

- **Risk**: Test files accumulate in repository
- **Mitigation**: Final cleanup step removes test artifacts and logs completion

## Implementation Plan (Micro Issues)

When creating GitHub issues, break this RFC into micro tasks `RFC-100-YY` with clear acceptance criteria to drive Copilot.

| Micro | Title | Acceptance Criteria |
|-------|-------|---------------------|
| 01    | Test project board issue detection | - [ ] Add `docs/game-rfc-test-100/PROJECT_TEST.md` with `rfc-100-detected` content<br>- [ ] Verify project tracking comment added to RFC-100-01 issue<br>- [ ] Build passes<br>- [ ] PR title follows Conventional Commits |
| 02    | Test project board PR tracking | - [ ] Update `docs/game-rfc-test-100/PROJECT_TEST.md` to include `pr-tracked-100`<br>- [ ] Verify update-project-board workflow logs PR events<br>- [ ] Build passes<br>- [ ] PR title follows Conventional Commits |
| 03    | Test project board completion tracking | - [ ] Add completion marker to `docs/game-rfc-test-100/PROJECT_TEST.md` with `completed-100`<br>- [ ] Remove `docs/game-rfc-test-100/PROJECT_TEST.md` after validation<br>- [ ] Append `Projects integration tested: RFC-100` to `AGENTS.md`<br>- [ ] Build passes<br>- [ ] PR follows Conventional Commits |