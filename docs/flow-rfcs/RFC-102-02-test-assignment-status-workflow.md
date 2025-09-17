# RFC-102-02: Test Assignment Status Workflow (Track: flow)

- Start Date: 2025-09-17
- RFC Author: Copilot
- Status: Draft
- Track: flow

## Summary

Create a comprehensive test for the assignment status workflow to verify that the `update-project-status-on-assignment.yml` workflow correctly updates project status when issues are assigned and unassigned from Copilot.

## Motivation

The repository has an automated assignment status workflow (`update-project-status-on-assignment.yml`) that should:
- Update project status to 'In Progress' when issues are assigned to Copilot
- Update project status to 'Todo' when issues are unassigned from Copilot  
- Add appropriate project status update comments to issues

Without proper testing, we cannot be confident that the assignment status workflow is functioning correctly and providing the intended project tracking automation.

## Detailed Design

### Test Architecture

The test validates the complete assignment status workflow:

1. **Issue Creation**: Create an RFC-102-02 test issue
2. **Assignment**: Assign the issue to Copilot bot
3. **Status Verification**: Verify project status updates to 'In Progress' and comment is added
4. **Unassignment**: Remove Copilot assignment from the issue
5. **Status Verification**: Verify project status reverts to 'Todo' and comment is added
6. **Cleanup**: Provide cleanup mechanism for test artifacts

### Test Components

- **Test Issue**: RFC-102-02 serves as the test case for assignment workflow
- **Assignment Logic**: Uses existing `assign_issue_to_copilot.py` functionality
- **Status Validation**: Monitors issue comments for project status update notifications
- **Workflow Integration**: Tests the actual `update-project-status-on-assignment.yml` workflow
- **Cleanup Process**: Ensures test artifacts can be cleaned up after validation

### Integration Points

- GitHub Issues API (for issue creation, assignment, and comment verification)
- GitHub Actions workflow triggers (assignment and unassignment events)
- Project board automation (status updates via GraphQL API)
- Existing assignment infrastructure in the repository

### Expected Behavior

1. **Issue Created**: Test issue RFC-102-02 is created with proper labels
2. **Issue Assigned**: Copilot bot is assigned to the issue (triggers `assigned` event)
3. **Status Updated**: Project status changes to 'In Progress' with notification comment
4. **Issue Unassigned**: Copilot bot is unassigned from the issue (triggers `unassigned` event)  
5. **Status Reverted**: Project status changes back to 'Todo' with notification comment

## Alternatives Considered

- **Manual Testing**: Rejected as it's not repeatable and prone to human error
- **Unit Tests Only**: Rejected as they wouldn't test the actual workflow integration
- **Mock Testing**: Rejected as it wouldn't validate real GitHub API and project interactions
- **Combined with RFC-102-01**: Rejected to keep test scope focused and clear

## Risks & Mitigations

- **Risk**: Test assignment conflicts with production automation
- **Mitigation**: Use test-specific issue titles and careful timing coordination
- **Risk**: Assignment requires special permissions (AUTO_APPROVE_TOKEN)
- **Mitigation**: Use `copilot` environment with proper token configuration
- **Risk**: Project status updates may be delayed or fail
- **Mitigation**: Include appropriate timeouts and fallback validation methods

## Implementation Plan (Micro Issues)

| Micro | Title | Acceptance Criteria |
|-------|-------|---------------------|
| 02    | Test Assignment Status Workflow | - [x] Create RFC-102-02 test issue<br/>- [x] Implement assignment to Copilot bot using existing infrastructure<br/>- [x] Verify `update-project-status-on-assignment.yml` workflow triggers on assignment<br/>- [x] Confirm project status updates to 'In Progress' with comment<br/>- [x] Implement unassignment from Copilot bot<br/>- [x] Verify workflow triggers on unassignment<br/>- [x] Confirm project status reverts to 'Todo' with comment<br/>- [x] Provide cleanup mechanism for test artifacts<br/>- [x] Create GitHub Actions workflow for automated testing<br/>- [x] Document test procedure and expected results |

## Technical Implementation

### Test Script: `test_assignment_status_workflow.py`

The test script implements:
- Issue creation with RFC-102-02 title pattern
- Assignment using existing `assign_issue_to_copilot.py` logic
- Comment monitoring to detect status update notifications  
- Unassignment with proper cleanup of all assignees
- Result validation and reporting
- Optional cleanup of test artifacts

### GitHub Actions Workflow: `rfc-102-02-assignment-status-test.yml`

The workflow provides:
- Manual trigger via `workflow_dispatch`
- Proper environment setup with `copilot` environment for assignment permissions
- Token management (AUTO_APPROVE_TOKEN preferred, GITHUB_TOKEN fallback)
- Test execution and result reporting
- Summary generation for test results

### Integration with Existing Infrastructure

The test leverages:
- `update-project-status-on-assignment.yml` - The workflow being tested
- `assign_issue_to_copilot.py` - For programmatic Copilot assignment
- GitHub CLI - For issue management and comment retrieval
- Projects v2 API - Via the status update workflow being tested

This implementation provides comprehensive validation of the assignment status workflow while maintaining minimal impact on the existing automation pipeline.