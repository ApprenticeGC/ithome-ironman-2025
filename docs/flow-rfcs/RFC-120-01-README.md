# RFC-120-01: Final Automation Test

This directory contains the implementation and validation tools for RFC-120-01, which tests the complete cleaned-up automation workflow.

## Test Overview

RFC-120-01 validates these 4 key automation steps:

1. ✅ **Issue Created**: RFC-120-01 issue creation (manual step)
2. ⏳ **Auto-add to Project #2**: Via `update-project-board.yml` workflow  
3. ⏳ **Manual Assignment Test**: Via `assign-copilot-to-issue.yml` workflow
4. ⏳ **Auto-update Status**: Via `update-project-status-on-assignment.yml` workflow

## Files

- `RFC-120-final-automation-test.md` - The main RFC document defining the test
- `validate_rfc_120_test.py` - Validation script to check test results

## Usage

### Validation Script

To check the automation test results:

```bash
# Set repository context
export REPO="ApprenticeGC/ithome-ironman-2025"
export GH_TOKEN="your-github-token"

# Run validation for a specific issue number
python3 scripts/python/production/validate_rfc_120_test.py <issue_number>
```

Example:
```bash
python3 scripts/python/production/validate_rfc_120_test.py 123
```

### Expected Results

When the automation test completes successfully, you should see:

```
🎉 RFC-120-01 Test: PASSED
   All automation workflow steps completed successfully!

   ✅ Issue created
   ✅ Project tracking comment  
   ✅ Manual assignment
   ✅ Status update automation
```

### Test Timeline

- **Phase 1**: Project board integration (0-10 minutes after issue creation)
- **Phase 2**: Manual assignment test (immediate via workflow dispatch)
- **Phase 3**: Status update automation (0-5 minutes after assignment)
- **Phase 4**: Results validation (immediate)

Total expected completion: Within 30 minutes of RFC-120-01 issue creation.

## Manual Testing Steps

If you need to manually trigger the automation steps:

1. **Check Project Board Comment**: Should appear automatically within 10 minutes of issue creation

2. **Trigger Assignment**: Use GitHub Actions workflow dispatch:
   - Go to Actions → "Assign Copilot to Issue" 
   - Click "Run workflow"
   - Enter the issue number
   - Run workflow

3. **Verify Status Update**: Should happen automatically within 5 minutes of assignment

4. **Validate Results**: Run the validation script to check all steps

## Troubleshooting

- **Missing project comment**: Check if the issue title contains "RFC-" 
- **Assignment failed**: Ensure AUTO_APPROVE_TOKEN is configured
- **Status update missing**: Verify the issue is in Project #2 and has a Status field
- **Script errors**: Ensure GitHub CLI is installed and authenticated

## Success Criteria

The test passes when all 4 automation steps complete:
- ✅ Project tracking comment added automatically
- ✅ Issue successfully assigned via workflow
- ✅ Project board status updated to "In Progress" 
- ✅ All comments and status updates appear as expected

This validates that the complete automation pipeline is working end-to-end.
