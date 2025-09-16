# RFC-096: Autonomous Flow Test - 2025-09-16T01:36:57Z (Track: game)

- Start Date: 2025-09-16
- RFC Author: GitHub Copilot
- Status: Draft
- Track: game

## Summary

A test RFC designed to validate the RFC-090 autonomous flow end-to-end. This RFC creates simple micro issues that can be processed through the complete autonomous workflow: micro-issue generation → Copilot assignment → PR creation → CI → auto-ready → auto-merge → auto-advance.

## Motivation

- **Problem Statement**: Need to validate that the autonomous flow described in RFC-090 works correctly from start to finish
- **Goals**: Create a simple test case that exercises all components of the autonomous flow
- **Non-goals**: This is not intended to implement actual game functionality, just test the flow

## Detailed Design

This RFC defines a minimal test implementation that includes:
- Basic placeholder classes in the .NET solution
- Simple test methods that can pass CI
- Documentation updates to track the test completion
- Minimal changes that validate the autonomous workflow

### Architecture

The test implementation will:
1. Add a simple `AutonomousFlowTest` class to the TestLib project
2. Include basic unit tests to ensure CI passes
3. Update README with a note about the autonomous flow test
4. Each micro issue represents a small, atomic change

### Failure/Recovery

If any micro issue fails during autonomous processing:
- The agent-watchdog will reset the chain
- Manual recovery can be triggered via cleanup-stalled-pr workflow
- Issues can be manually reassigned using assign-copilot-to-issue workflow

## Alternatives Considered

- **Option A**: Create a complex test RFC with multiple components
  - **Pros**: More comprehensive testing
  - **Cons**: Harder to debug if flow fails, more complex to implement
- **Option B**: Use existing placeholder test directories
  - **Pros**: Simpler setup
  - **Cons**: Doesn't follow the game-rfcs pattern expected by workflows

## Risks & Mitigations

- **Risk**: Test micro issues might be too simple to catch real flow issues
  - **Mitigation**: Include variety of change types (code, tests, docs)
- **Risk**: CI failures could break the autonomous flow test
  - **Mitigation**: Keep changes minimal and ensure they pass locally first

## Implementation Plan (Micro Issues)

When creating GitHub issues, break this RFC into micro tasks `RFC-096-YY` with clear acceptance criteria to drive Copilot.

| Micro | Title | Acceptance Criteria |
|-------|-------|---------------------|
| 01    | Create AutonomousFlowTest class | - [ ] Add AutonomousFlowTest.cs to TestLib project<br/>- [ ] Include basic test method that passes<br/>- [ ] Ensure dotnet build succeeds |
| 02    | Add unit test for autonomous flow validation | - [ ] Add test method TestAutonomousFlowTimestamp<br/>- [ ] Test validates timestamp format 2025-09-16T01:36:57Z<br/>- [ ] Ensure dotnet test passes |
| 03    | Update documentation with flow test status | - [ ] Add section to README about autonomous flow test<br/>- [ ] Reference RFC-096 and timestamp<br/>- [ ] Document test completion |
| 04    | Validate flow completion and cleanup | - [ ] Verify all previous micros completed successfully<br/>- [ ] Add final validation test method<br/>- [ ] Ensure autonomous flow worked end-to-end |