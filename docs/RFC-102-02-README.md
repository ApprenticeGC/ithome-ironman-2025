# RFC-102-02: Test Assignment Status Workflow

## Overview

This directory contains the implementation for RFC-102-02, which provides a comprehensive test for the assignment status workflow to ensure that the `update-project-status-on-assignment.yml` workflow correctly updates project status when issues are assigned and unassigned.

## Components

### 1. RFC Document
- **File**: `docs/flow-rfcs/RFC-102-02-test-assignment-status-workflow.md`
- **Purpose**: Defines the requirements and specifications for testing the assignment status workflow

### 2. Assignment Status Test Script
- **File**: `scripts/python/production/test_assignment_status_workflow.py`
- **Purpose**: Automated test script that creates a test RFC issue, assigns/unassigns it, and validates the status workflow

### 3. GitHub Actions Workflow
- **File**: `.github/workflows/rfc-102-02-assignment-status-test.yml`
- **Purpose**: Provides a workflow to run the assignment status test from GitHub Actions

## What the Test Validates

The assignment status test performs the following validations:

1. **Issue Creation**: Creates an RFC-102-02 test issue with proper formatting and labels
2. **Copilot Assignment**: Uses existing assignment infrastructure to assign the issue to Copilot
3. **Assignment Workflow Trigger**: Verifies that the `update-project-status-on-assignment.yml` workflow triggers on assignment
4. **Status Update to 'In Progress'**: Validates that project status updates correctly
5. **Status Update Comment**: Confirms a project status update comment is added to the issue
6. **Copilot Unassignment**: Removes Copilot assignment from the issue
7. **Unassignment Workflow Trigger**: Verifies the workflow triggers on unassignment
8. **Status Revert to 'Todo'**: Validates that project status reverts correctly
9. **Revert Comment**: Confirms an appropriate status revert comment is added

### Expected Results

#### Success Case
When the test passes, you should see:
```
✅ Assignment status workflow test PASSED
   - Issue assignment: ✅
   - 'In Progress' status update: ✅
   - Issue unassignment: ✅
   - 'Todo' status update: ✅
```

#### Failure Cases
The test may fail if:
- The `update-project-status-on-assignment.yml` workflow is not working
- Assignment to Copilot fails (usually permissions/token issues)
- Project status updates are not working correctly
- The workflow doesn't detect assignment/unassignment events correctly
- Project status update comments are missing or incorrectly formatted
- The workflow takes longer than expected to complete

## Running the Test

### Manual Execution
```bash
# Set required environment variables
export REPO="ApprenticeGC/ithome-ironman-2025"
export GITHUB_TOKEN="your_github_token_here"

# Run the test
python3 scripts/python/production/test_assignment_status_workflow.py
```

### GitHub Actions Execution
1. Go to the Actions tab in the repository
2. Select "RFC-102-02 Assignment Status Workflow Test"
3. Click "Run workflow"
4. Choose whether to automatically cleanup the test issue after completion

## Requirements

### Permissions
- The test requires assignment permissions, preferably with `AUTO_APPROVE_TOKEN` 
- Uses the `copilot` environment for proper token access
- Needs `issues: write` permissions for issue creation and assignment

### Dependencies
- `scripts/python/production/assign_issue_to_copilot.py` - For Copilot assignment logic
- `update-project-status-on-assignment.yml` - The workflow being tested
- GitHub CLI (`gh`) - For issue management operations
- Projects v2 setup - For project status updates

## Test Issue Management

### Automatic Cleanup
By default, the test offers to clean up the test issue after completion. This:
- Adds a completion summary comment to the issue
- Closes the issue with reason "completed"

### Manual Cleanup
If you choose not to auto-cleanup, you can manually:
1. Review the test issue and all status update comments
2. Verify both 'In Progress' and 'Todo' status update comments are present
3. Close the issue when you're satisfied with the test results

## Troubleshooting

### Assignment Failures
- Ensure `AUTO_APPROVE_TOKEN` is configured in the repository secrets
- Check that the `copilot` environment has proper access to the token
- Verify the Copilot bot has appropriate repository permissions

### Status Update Failures
- Check that the repository has Projects v2 properly configured
- Ensure the project has 'Status' field with 'In Progress' and 'Todo' options
- Verify the `update-project-status-on-assignment.yml` workflow is enabled

### Workflow Trigger Issues
- Confirm the issue was created with an "RFC-" title prefix
- Check GitHub Actions logs for workflow execution details
- Ensure webhooks are properly configured for the repository

## Integration with Overall System

This test validates a critical piece of the automated RFC implementation pipeline:
1. Issues are created (tested by RFC-102-01)
2. Issues are assigned to Copilot (automated by `rfc-assign-cron.yml`)  
3. **Project status updates on assignment (tested by RFC-102-02)** ← This test
4. Copilot creates implementation PRs
5. PRs are automatically reviewed and merged
6. Issues are closed upon successful completion

The assignment status workflow ensures proper project tracking visibility throughout the automation pipeline.