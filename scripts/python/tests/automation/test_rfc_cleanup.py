#!/usr/bin/env python3
"""
Test suite for RFC cleanup functionality.
"""

import sys
import os

# Add the production directory to the path for imports
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '../../production'))

from rfc_cleanup_duplicates import extract_original_title


class TestRFCCleanup:
    """Test cases for RFC cleanup functionality."""

    def test_extract_original_title_no_prefix(self):
        """Test extracting title when there's no 'Recreated broken chain:' prefix."""
        title = "GAME-RFC-012-02: Create Deployment Pipeline Automation"
        result = extract_original_title(title)
        assert result == "GAME-RFC-012-02: Create Deployment Pipeline Automation"

    def test_extract_original_title_single_prefix(self):
        """Test extracting title when there's a single 'Recreated broken chain:' prefix."""
        title = "Recreated broken chain: GAME-RFC-012-02: Create Deployment Pipeline Automation"
        result = extract_original_title(title)
        assert result == "GAME-RFC-012-02: Create Deployment Pipeline Automation"

    def test_extract_original_title_multiple_prefix(self):
        """Test extracting title when there are multiple 'Recreated broken chain:' prefixes."""
        title = "Recreated broken chain: Recreated broken chain: Recreated broken chain: GAME-RFC-012-02: Create Deployment Pipeline Automation"
        result = extract_original_title(title)
        assert result == "GAME-RFC-012-02: Create Deployment Pipeline Automation"

    def test_extract_original_title_many_recursive_prefixes(self):
        """Test extracting title when there are many recursive prefixes (matching the issue title pattern)."""
        # This matches the pattern seen in the issue title
        title = "Recreated broken chain: " * 8 + "GAME-RFC-012-02: Create Deployment Pipeline Automation"
        result = extract_original_title(title)
        assert result == "GAME-RFC-012-02: Create Deployment Pipeline Automation"

    def test_extract_original_title_empty_after_prefix(self):
        """Test extracting title when title is just the prefix."""
        title = "Recreated broken chain: "
        result = extract_original_title(title)
        assert result == ""

    def test_placeholder(self):
        """Placeholder test."""
        assert True


if __name__ == "__main__":
    # Simple test runner since pytest is not available
    test_instance = TestRFCCleanup()
    
    # Run all test methods
    test_methods = [method for method in dir(test_instance) if method.startswith('test_')]
    
    passed = 0
    failed = 0
    
    for test_method in test_methods:
        try:
            getattr(test_instance, test_method)()
            print(f"✓ {test_method}")
            passed += 1
        except AssertionError as e:
            print(f"✗ {test_method}: {e}")
            failed += 1
        except Exception as e:
            print(f"✗ {test_method}: Unexpected error: {e}")
            failed += 1
    
    print(f"\nTest Results: {passed} passed, {failed} failed")
    
    if failed > 0:
        sys.exit(1)
