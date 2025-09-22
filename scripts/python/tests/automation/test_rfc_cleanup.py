#!/usr/bin/env python3
"""
Test suite for RFC cleanup functionality.
"""

import sys
import pathlib

# Add production directory to path
PRODUCTION_DIR = pathlib.Path(__file__).parent.parent / "production"
if str(PRODUCTION_DIR) not in sys.path:
    sys.path.insert(0, str(PRODUCTION_DIR))

import unittest
from rfc_cleanup_duplicates import RFCCleanupRunner


class TestRFCCleanup(unittest.TestCase):
    """Test cases for RFC cleanup functionality."""

    def setUp(self):
        """Set up test fixtures."""
        # Create a mock RFCCleanupRunner for testing
        self.runner = RFCCleanupRunner("test/repo", dry_run=True)

    def test_clean_recreated_title_single_prefix(self):
        """Test removing a single 'Recreated broken chain:' prefix."""
        original = "Recreated broken chain: GAME-RFC-009-03: Implement AI Agent"
        expected = "GAME-RFC-009-03: Implement AI Agent"
        result = self.runner._clean_recreated_title(original)
        self.assertEqual(result, expected)

    def test_clean_recreated_title_multiple_prefixes(self):
        """Test removing multiple 'Recreated broken chain:' prefixes."""
        original = "Recreated broken chain: Recreated broken chain: Recreated broken chain: GAME-RFC-009-03: Implement AI Agent"
        expected = "GAME-RFC-009-03: Implement AI Agent"
        result = self.runner._clean_recreated_title(original)
        self.assertEqual(result, expected)

    def test_clean_recreated_title_no_prefix(self):
        """Test that titles without prefix are left unchanged."""
        original = "GAME-RFC-009-03: Implement AI Agent"
        expected = "GAME-RFC-009-03: Implement AI Agent"
        result = self.runner._clean_recreated_title(original)
        self.assertEqual(result, expected)

    def test_clean_recreated_title_empty_string(self):
        """Test that empty strings are handled correctly."""
        original = ""
        expected = ""
        result = self.runner._clean_recreated_title(original)
        self.assertEqual(result, expected)

    def test_clean_recreated_title_only_prefixes(self):
        """Test title that consists only of prefixes."""
        original = "Recreated broken chain: Recreated broken chain: "
        expected = ""
        result = self.runner._clean_recreated_title(original)
        self.assertEqual(result, expected)

    def test_clean_recreated_title_partial_match(self):
        """Test that partial matches don't get removed."""
        original = "Some Recreated broken chain: in the middle"
        expected = "Some Recreated broken chain: in the middle"
        result = self.runner._clean_recreated_title(original)
        self.assertEqual(result, expected)


if __name__ == "__main__":
    unittest.main()
