#!/usr/bin/env python3
"""
Unit tests for RFC cleanup functionality.
"""
import unittest
import pathlib
import sys

# Ensure production directory is on path
PRODUCTION_DIR = pathlib.Path(__file__).parent.parent / "production"
if str(PRODUCTION_DIR) not in sys.path:
    sys.path.insert(0, str(PRODUCTION_DIR))

try:
    from rfc_cleanup_duplicates import RFCCleanupRunner
except ImportError:
    # Skip tests if we can't import the module
    RFCCleanupRunner = None


class TestRFCCleanupRunner(unittest.TestCase):
    """Test the RFC cleanup runner functionality."""

    def setUp(self):
        """Set up test fixtures."""
        if RFCCleanupRunner is None:
            self.skipTest("rfc_cleanup_duplicates module not available")
        
        # Create a mock runner instance (dry run mode)
        self.cleanup_runner = RFCCleanupRunner("test/repo", dry_run=True)

    def test_clean_recreated_title_simple(self):
        """Test cleaning simple recreated titles."""
        test_cases = [
            ("GAME-RFC-013-02: Create Configuration Security and Encryption", 
             "GAME-RFC-013-02: Create Configuration Security and Encryption"),
            ("Recreated broken chain: GAME-RFC-013-02: Create Configuration Security and Encryption",
             "GAME-RFC-013-02: Create Configuration Security and Encryption"),
            ("Normal title without prefix",
             "Normal title without prefix"),
        ]
        
        for input_title, expected in test_cases:
            with self.subTest(input=input_title):
                result = self.cleanup_runner._clean_recreated_title(input_title)
                self.assertEqual(result, expected)

    def test_clean_recreated_title_multiple_prefixes(self):
        """Test cleaning titles with multiple recreated prefixes."""
        # The specific case from the issue
        problematic_title = ("Recreated broken chain: Recreated broken chain: "
                           "Recreated broken chain: Recreated broken chain: "
                           "Recreated broken chain: Recreated broken chain: "
                           "Recreated broken chain: GAME-RFC-013-02: Create Configuration Security and Encryption")
        expected = "GAME-RFC-013-02: Create Configuration Security and Encryption"
        
        result = self.cleanup_runner._clean_recreated_title(problematic_title)
        self.assertEqual(result, expected)

    def test_clean_recreated_title_with_whitespace(self):
        """Test cleaning titles with extra whitespace."""
        test_cases = [
            ("   Recreated broken chain: GAME-RFC-013-02: Test   ",
             "GAME-RFC-013-02: Test"),
            ("Recreated broken chain:    Spaced title   ",
             "Spaced title"),
            ("   ",
             ""),
        ]
        
        for input_title, expected in test_cases:
            with self.subTest(input=input_title):
                result = self.cleanup_runner._clean_recreated_title(input_title)
                self.assertEqual(result, expected)

    def test_clean_recreated_title_preserves_middle_occurrences(self):
        """Test that prefixes in the middle of titles are preserved."""
        title = "Title with Recreated broken chain: in the middle should stay"
        result = self.cleanup_runner._clean_recreated_title(title)
        self.assertEqual(result, title)

    def test_clean_recreated_title_empty_string(self):
        """Test cleaning empty strings."""
        result = self.cleanup_runner._clean_recreated_title("")
        self.assertEqual(result, "")


if __name__ == "__main__":
    unittest.main()