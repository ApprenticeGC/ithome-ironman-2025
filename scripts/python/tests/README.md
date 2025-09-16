# Tests Directory

This directory contains all test files for the GitHub automation scripts.

## Files:
- `test_scripts.py` - Main test suite for GitHub automation scripts
- `test_ensure_automerge.py` - Specific tests for auto-merge functionality
- `test_runner.py` - Alternative test runner
- `test_rfc_cleanup.py` - Tests for RFC cleanup duplicates functionality
- `run_tests.sh` - Test runner script for RFC cleanup tests

## Running Tests:

### General Tests
```bash
# Run all tests
python scripts/python/tools/run_tests.py

# Run specific test file
python -m pytest scripts/python/tests/test_scripts.py -v

# Run with coverage
python -m pytest scripts/python/tests/ --cov=scripts.python.production --cov-report=html
```

### RFC Cleanup Tests
```bash
# Using the dedicated test runner
bash scripts/python/tests/run_tests.sh

# Or run Python tests directly
cd scripts/python/tests
python3 test_rfc_cleanup.py
```

## RFC Cleanup Test Coverage

The `test_rfc_cleanup.py` tests cover:

1. **Duplicate Detection**: Correctly identifies RFC series with multiple PRs
2. **Cleanup Simulation**: Logic for determining which PRs to keep/remove
3. **Integration**: Ensures test logic matches production script logic
4. **Edge Cases**: Handles various scenarios (no duplicates, single RFC with many PRs, etc.)

### Test Scenarios
- Single RFC with multiple PRs (detects duplicates)
- Multiple RFCs with no duplicates (no false positives)
- Multiple RFCs with some having duplicates (selective detection)
- Complex cleanup actions simulation
