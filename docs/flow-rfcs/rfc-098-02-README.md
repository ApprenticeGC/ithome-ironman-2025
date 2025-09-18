# RFC-098-02 Assignment Test

This directory contains the test implementation for RFC-098-02: Assignment Test.

## Files

- `test_rfc_098_assignment.py` - Python script for validating RFC-098 assignment workflow
- (Optional) `test-rfc-098-assignment.yml` - GitHub Actions workflow for testing the assignment flow

## Test Overview

The test validates the assignment workflow automation for RFC-098:
- [ ] RFC-098 issues are found and processed
- [ ] Issues get assigned to Copilot bot automatically
- [ ] Assignment happens within 10 minutes
- [ ] Integration with RFC-098-01 project status functionality

## Usage

### Using the Test Script

The Python script can be run locally or in workflows:

```bash
# Set repository context
export REPO="owner/repository-name"
export GH_TOKEN="your-github-token"

# Run the test
python3 scripts/python/production/test_rfc_098_assignment.py
```

### Running the Test Workflow (Optional)

If a workflow is created, the test can be run via GitHub Actions workflow dispatch:

1. Go to Actions tab in the repository
2. Select "test-rfc-098-assignment" workflow
3. Click "Run workflow"
4. Choose parameters as needed

## How It Works

1. **Issue Discovery**: Finds all open issues with RFC-098-XX pattern
2. **Assignment Validation**: Checks that issues get assigned to Copilot
3. **Timing Test**: Validates that assignment happens within 10 minutes
4. **Integration Test**: Validates integration with RFC-098-01 project status updates
5. **Reporting**: Provides detailed status and validation results

## Expected Flow

1. RFC-098 issues are created (manually or via rfc-sync)
2. Assignment automation detects RFC-098 issues
3. `assign_first_open_for_rfc.py` or similar assigns issues to Copilot
4. RFC-098-01 project status integration updates project status
5. Assignment test validates the complete workflow

## Integration with RFC-098-01

RFC-098-02 tests the assignment workflow while RFC-098-01 handles project status integration:

- **RFC-098-01**: Tests that assigned/unassigned issues get proper project status updates
- **RFC-098-02**: Tests that RFC-098 issues get assigned automatically AND validates the integration

Both components work together to provide complete RFC-098 automation.

## Test Results

The test provides detailed output showing:
- Number of RFC-098 issues found
- Assignment status for each issue
- Timing information for assignments
- Integration status with RFC-098-01 project functionality
- Final validation against acceptance criteria

Exit codes:
- 0: All tests passed
- 1: Tests failed
- 2: Tests pending (waiting for assignment)

## Success Criteria

✅ **All Criteria Met**: Assignment workflow automation is working correctly
⏳ **Pending**: Issues found but assignment still in progress
❌ **Failed**: Assignment workflow has issues that need attention

## Relationship to Other RFCs

- **RFC-092-01**: General assignment test pattern (template for this implementation)
- **RFC-098-01**: Project status integration (tested together with assignment)
- **RFC-102-XX**: Project board integration tests (complementary functionality)
