# RFC-102-01: Final Project Board Integration Test

## Overview

This directory contains the implementation for RFC-102-01, which provides a comprehensive integration test for the project board workflow to ensure that the `update-project-board.yml` workflow correctly adds project tracking comments to RFC issues.

## Components

### 1. RFC Document
- **File**: `docs/flow-rfcs/RFC-102-project-board-integration-test.md`
- **Purpose**: Defines the requirements and specifications for the project board integration test

### 2. Integration Test Script
- **File**: `scripts/python/production/test_project_board_integration.py`
- **Purpose**: Automated test script that creates a test RFC issue and validates the project board workflow

### 3. GitHub Actions Workflow
- **File**: `.github/workflows/rfc-102-project-board-test.yml`
- **Purpose**: Provides a workflow to run the integration test from GitHub Actions

### 4. Unit Tests
- **File**: `scripts/python/tests/test_project_board_integration.py`
- **Purpose**: Unit tests for the integration test script functionality

## How to Run the Test

### Option 1: Manual Execution (Recommended for Development)

```bash
# Set environment variables
export REPO="your-org/your-repo"
export GH_TOKEN="your-github-token"

# Run the test script
python3 scripts/python/production/test_project_board_integration.py
```

### Option 2: GitHub Actions Workflow

1. Go to the "Actions" tab in your repository
2. Find "RFC-102-01 Project Board Integration Test" workflow  
3. Click "Run workflow"
4. Choose whether to automatically clean up the test issue after completion
5. Click "Run workflow" button

### Option 3: Using GitHub CLI

```bash
# Trigger the workflow via GitHub CLI
gh workflow run "RFC-102-01 Project Board Integration Test" \
    --repo your-org/your-repo \
    --field cleanup=true
```

## What the Test Validates

The integration test performs the following validations:

1. **Issue Creation**: Creates an RFC-102-01 test issue with proper formatting
2. **Workflow Trigger**: Verifies that the `update-project-board.yml` workflow is triggered
3. **RFC Detection**: Confirms the workflow correctly identifies the issue as an RFC issue (title contains "RFC-")
4. **Comment Addition**: Validates that the workflow adds a project tracking comment
5. **Comment Content**: Ensures the comment contains all expected elements:
   - Project board link: https://github.com/users/ApprenticeGC/projects/2/views/1
   - Status information: "Added to RFC Backlog"
   - Next steps: "Waiting for Copilot implementation"
   - Progress tracking information
   - Proper workflow attribution

## Expected Results

### Success Case
When the test passes, you should see:
```
✅ Project board integration test PASSED
   - RFC issue detection: ✅
   - Workflow trigger: ✅
   - Comment addition: ✅
   - Comment content: ✅
```

### Failure Cases
The test may fail if:
- The `update-project-board.yml` workflow is not working
- The workflow doesn't detect RFC issues correctly
- The project tracking comment is missing or incorrectly formatted
- The workflow takes longer than expected to complete

## Test Issue Management

### Automatic Cleanup
By default, the test offers to clean up the test issue after completion. This:
- Adds a completion comment to the issue
- Closes the issue with reason "completed"

### Manual Cleanup
If you choose not to auto-cleanup, you can manually:
1. Review the test issue and comments
2. Close the issue when you're satisfied with the test results

## Troubleshooting

### Common Issues

1. **"REPO environment variable required"**
   - Set `REPO` or `GITHUB_REPOSITORY` environment variable
   - Format: `owner/repository-name`

2. **"GitHub CLI error"**
   - Ensure GitHub CLI is installed and authenticated
   - Set `GH_TOKEN` environment variable if needed

3. **"Workflow did not complete in expected time"**
   - Check workflow runs in GitHub Actions
   - The test waits up to 5 minutes by default
   - Manual verification may be needed for slower environments

4. **"Project tracking comment validation failed"**
   - Check the actual comment content in the test output
   - Verify the `update-project-board.yml` workflow is functioning correctly
   - Ensure the workflow has the correct comment template

## Running Unit Tests

```bash
# Run unit tests for the integration test script
python3 scripts/python/tests/test_project_board_integration.py
```

## Integration with CI/CD

This test can be integrated into CI/CD pipelines to:
- Validate project board workflow changes
- Ensure workflow reliability after updates
- Provide regression testing for project board functionality

## Related Files

- `docs/GITHUB_PROJECTS_INTEGRATION.md` - Project board integration documentation
- `.github/workflows/update-project-board.yml` - The workflow being tested
- `.github/workflows/test-project-integration.yml` - Alternative project integration test workflow

## Support

For issues with this test:
1. Check the GitHub Actions workflow logs
2. Review the test issue comments for validation details
3. Run the unit tests to ensure script functionality
4. Verify environment variables and GitHub CLI setup