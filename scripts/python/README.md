# Python Scripts Directory

This directory contains all Python development utilities and tools for the project.

## Organization:
- `tools/` - Development utilities and scripts
  - `run_tests.py` - Main test runner for GitHub automation scripts
  - `setup_dev.py` - Development environment setup script
- `tests/` - Test files for GitHub automation scripts
  - `test_scripts.py` - Main test suite
  - `test_ensure_automerge.py` - Specific tests
  - `test_runner.py` - Alternative test runner
- `requirements/` - Python dependency files
  - `test-requirements.txt` - Dependencies for testing and development

## Usage:
```bash
# Run the test suite
python scripts/python/tools/run_tests.py

# Run tests with pre-commit validation
python scripts/python/tools/run_tests.py --include-precommit

# Setup development environment
python scripts/python/tools/setup_dev.py

# Install dependencies
pip install -r scripts/python/requirements/test-requirements.txt

# Run specific tests
python -m pytest scripts/python/tests/test_scripts.py -v
```

## Note:
- Production GitHub Actions scripts are now in `production/` for better organization
- This directory consolidates all Python development and testing resources
