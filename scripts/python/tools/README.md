# Python Tools Directory

This directory contains all development and utility scripts for the project.

## Files:
- `setup_dev.py` - Complete development environment setup for the entire project
- `run_tests.py` - Test runner for Python automation scripts

## Usage:
```bash
# Setup entire development environment
python scripts/python/tools/setup_dev.py

# Run the Python test suite
python scripts/python/tools/run_tests.py

# Run tests with pre-commit validation
python scripts/python/tools/run_tests.py --include-precommit
```

## What setup_dev.py does:
- Validates Python installation
- Installs project dependencies
- Sets up pre-commit hooks  
- Configures development environment
