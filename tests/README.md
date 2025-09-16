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
python -m pytest tests/test_scripts.py -v

# Run with coverage
python -m pytest tests/ --cov=.github.scripts --cov-report=html
```
