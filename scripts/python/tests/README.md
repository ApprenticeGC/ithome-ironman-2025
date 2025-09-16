# Tests Directory

This directory contains all test files for the GitHub automation scripts.

## Files:
- `test_scripts.py` - Main test suite for GitHub automation scripts
- `test_ensure_automerge.py` - Specific tests for auto-merge functionality
- `test_runner.py` - Alternative test runner

## Running Tests:
```bash
# Run all tests
python scripts/python/tools/run_tests.py

# Run specific test file
python -m pytest scripts/python/tests/test_scripts.py -v

# Run with coverage
python -m pytest scripts/python/tests/ --cov=scripts.python.production --cov-report=html
```
