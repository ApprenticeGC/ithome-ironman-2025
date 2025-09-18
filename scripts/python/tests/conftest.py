"""
Shared pytest configuration for the consolidated test suite.

This file provides common fixtures and configuration for all tests
in the ithome-ironman-2025 project.
"""

import os
import sys
from pathlib import Path

import pytest

# Add the production directory to Python path for all tests
PROJECT_ROOT = Path(__file__).parent.parent.parent.parent
PRODUCTION_DIR = Path(__file__).parent.parent / "production"

sys.path.insert(0, str(PRODUCTION_DIR))


@pytest.fixture(scope="session")
def project_root():
    """Provide the project root directory."""
    return PROJECT_ROOT


@pytest.fixture(scope="session")
def production_dir():
    """Provide the production directory."""
    return PRODUCTION_DIR


@pytest.fixture
def mock_github_token():
    """Provide a mock GitHub token for testing."""
    return "test_token_not_real"  # pragma: allowlist secret


@pytest.fixture
def mock_repo_info():
    """Provide mock repository information."""
    return {"owner": "test-owner", "repo": "test-repo", "full_name": "test-owner/test-repo"}


@pytest.fixture
def temp_env_vars():
    """Provide a fixture to temporarily set environment variables."""
    original_env = os.environ.copy()

    def _set_env_vars(**kwargs):
        for key, value in kwargs.items():
            os.environ[key] = value

    yield _set_env_vars

    # Restore original environment
    os.environ.clear()
    os.environ.update(original_env)
