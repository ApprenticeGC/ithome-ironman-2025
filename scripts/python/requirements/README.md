# Requirements Directory

This directory contains all Python dependency files for the project.

## Files:
- `test-requirements.txt` - Dependencies for testing, linting, and development tools

## Usage:
```bash
# Install all testing dependencies
pip install -r scripts/python/requirements/test-requirements.txt

# Install in development mode
pip install -r scripts/python/requirements/test-requirements.txt --editable
```

## Dependencies Include:
- **Testing**: pytest, pytest-mock
- **Code Quality**: black, isort, flake8
- **Pre-commit**: pre-commit hooks
- **Development**: Various utility packages
