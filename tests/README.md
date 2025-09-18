# Tests Directory

Consolidated test suite for the ithome-ironman-2025 project, organized by functionality.

## Directory Structure

```
tests/
├── automation/          # GitHub automation and workflow tests
│   ├── test_project_status.py     # Project status automation tests
│   ├── test_github_scripts.py     # General GitHub automation scripts tests
│   ├── test_automerge.py          # Auto-merge functionality tests
│   ├── test_rfc_cleanup.py        # RFC cleanup workflow tests
│   └── test_project_board.py      # Project board integration tests
├── validation/          # Data validation and encoding tests
│   └── test_unicode_validation.py # Unicode encoding validation tests
├── integration/         # End-to-end integration tests
│   └── (future integration tests)
├── utils/              # Test utilities and runners
│   ├── test_runner.py             # Custom Python test runner
│   └── run_tests.sh              # Shell test runner script
└── fixtures/           # Test data and fixtures
    └── (test data files)
```

## Running Tests

### All Tests
```bash
# Run all tests with pytest
python -m pytest tests/ -v

# Run tests with coverage
python -m pytest tests/ --cov=scripts.python.production --cov-report=html
```

### Specific Test Categories
```bash
# Run automation tests only
python -m pytest tests/automation/ -v

# Run validation tests only
python -m pytest tests/validation/ -v

# Run specific test file
python -m pytest tests/automation/test_project_status.py -v
```

### Custom Test Runners
```bash
# Using the Python test runner
python tests/utils/test_runner.py

# Using the shell test runner (for RFC cleanup)
bash tests/utils/run_tests.sh
```

## Test Categories

### 🤖 Automation Tests (`tests/automation/`)
Tests for GitHub automation workflows and scripts:
- **Project Status**: Tests for GitHub Projects v2 status automation
- **Auto-merge**: Tests for automated PR merging functionality  
- **RFC Cleanup**: Tests for RFC duplicate detection and cleanup
- **Project Board**: Tests for project board integration
- **GitHub Scripts**: General GitHub automation script tests

### ✅ Validation Tests (`tests/validation/`)
Tests for data validation and encoding:
- **Unicode Validation**: Tests for proper Unicode encoding handling

### 🔗 Integration Tests (`tests/integration/`)
End-to-end integration tests (future expansion)

### 🛠️ Utilities (`tests/utils/`)
Test utilities and custom runners:
- **Test Runner**: Custom Python test execution
- **Shell Runner**: Bash script for running specific test suites

## Configuration

The tests use pytest as the primary test runner with the following features:
- Automatic test discovery
- Coverage reporting
- Verbose output support
- Parallel test execution support

## Adding New Tests

1. Place tests in the appropriate category directory
2. Name test files with `test_*.py` prefix
3. Use descriptive test function names with `test_` prefix
4. Add docstrings to explain test purpose
5. Update this README if adding new test categories

## Dependencies

Test dependencies are managed in the main project requirements:
- `pytest` - Test framework
- `pytest-cov` - Coverage reporting
- `requests` - For API testing
- Custom project modules as needed
