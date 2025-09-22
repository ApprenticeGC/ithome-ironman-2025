#!/usr/bin/env python3
"""
Test for the RFC cleanup title fix to prevent prefix accumulation.
"""

import unittest
import sys
import os

# Add the production directory to the path to import the module
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '../production'))

from rfc_cleanup_duplicates import RFCCleanupRunner


class TestRFCCleanupTitleFix(unittest.TestCase):
    """Test cases for the RFC cleanup title fix."""

    def setUp(self):
        """Set up test instance."""
        self.runner = RFCCleanupRunner("test/repo", dry_run=True)

    def test_clean_recreated_title_single_prefix(self):
        """Test cleaning a title with a single 'Recreated broken chain: ' prefix."""
        title = "Recreated broken chain: GAME-RFC-013-02: Create Configuration Security and Encryption"
        expected = "GAME-RFC-013-02: Create Configuration Security and Encryption"
        result = self.runner._clean_recreated_title(title)
        self.assertEqual(result, expected)

    def test_clean_recreated_title_multiple_prefixes(self):
        """Test cleaning a title with multiple 'Recreated broken chain: ' prefixes."""
        title = "Recreated broken chain: Recreated broken chain: Recreated broken chain: GAME-RFC-013-02: Create Configuration Security and Encryption"
        expected = "GAME-RFC-013-02: Create Configuration Security and Encryption"
        result = self.runner._clean_recreated_title(title)
        self.assertEqual(result, expected)

    def test_clean_recreated_title_no_prefix(self):
        """Test cleaning a title with no 'Recreated broken chain: ' prefix."""
        title = "GAME-RFC-013-02: Create Configuration Security and Encryption"
        expected = "GAME-RFC-013-02: Create Configuration Security and Encryption"
        result = self.runner._clean_recreated_title(title)
        self.assertEqual(result, expected)

    def test_clean_recreated_title_empty_string(self):
        """Test cleaning an empty string."""
        title = ""
        expected = ""
        result = self.runner._clean_recreated_title(title)
        self.assertEqual(result, expected)

    def test_clean_recreated_title_many_prefixes(self):
        """Test cleaning a title with many accumulated prefixes (like the issue scenario)."""
        # Simulate the actual issue scenario with many repeated prefixes
        title = ("Recreated broken chain: " * 10) + "GAME-RFC-013-02: Create Configuration Security and Encryption"
        expected = "GAME-RFC-013-02: Create Configuration Security and Encryption"
        result = self.runner._clean_recreated_title(title)
        self.assertEqual(result, expected)

    def test_clean_recreated_title_partial_match(self):
        """Test that partial matches don't get cleaned."""
        title = "Recreated broken chain: Some other text with Recreated broken chain: inside"
        expected = "Some other text with Recreated broken chain: inside"
        result = self.runner._clean_recreated_title(title)
        self.assertEqual(result, expected)


if __name__ == '__main__':
    unittest.main()