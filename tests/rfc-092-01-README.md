# RFC-092-01 Assignment Test

This directory contains the test implementation for RFC-092-01: Validate Issue Assignment.

## Files

- `test-rfc-assignment.yml` - GitHub Actions workflow for testing the assignment flow
- `test_rfc_assignment.py` - Python script for validating assignment status

## Test Overview

The test validates the acceptance criteria for RFC-092-01:
- [ ] Issue created via rfc-sync
- [ ] Issue assigned to Copilot bot
- [ ] Assignment happens automatically within 10 minutes

## Usage

### Running the Test Workflow

The test can be run via GitHub Actions workflow dispatch:

1. Go to Actions tab in the repository
2. Select "test-rfc-assignment" workflow
3. Click "Run workflow"
4. Choose parameters:
   - `dry_run`: Set to 'false' for full test, 'true' for validation only
   - `cleanup`: Set to 'true' to clean up test data after completion

### Using the Test Script

The Python script can be run locally or in workflows:

```bash
# Set repository context
export REPO="owner/repository-name"
export GH_TOKEN="your-github-token"

# Run the test
python3 scripts/python/production/test_rfc_assignment.py
```

## How It Works

1. **Test Setup**: Creates RFC-092 in `docs/game-rfcs/` to trigger rfc-sync
2. **Issue Creation**: Validates that rfc-sync creates micro-issues
3. **Assignment Validation**: Checks that issues get assigned to Copilot
4. **Timing Test**: Validates that assignment happens within 10 minutes
5. **Cleanup**: Removes test data when cleanup is enabled

## Expected Flow

1. RFC-092 file is pushed to `docs/game-rfcs/`
2. `rfc-sync.yml` workflow detects the change
3. `generate_micro_issues_from_rfc.py` creates issues for each micro-task
4. `assign_first_open_for_rfc.py` assigns the first issue to Copilot
5. If assignment fails, `rfc-assign-cron.yml` will retry within 10 minutes

## Test Results

The test provides detailed output showing:
- Number of issues found/created
- Assignment status for each issue
- Timing information for assignments
- Final validation against acceptance criteria

Exit codes:
- 0: All tests passed
- 1: Tests failed
- 2: Tests pending (waiting for assignment)