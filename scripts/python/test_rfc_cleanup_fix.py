#!/usr/bin/env python3
"""
Tests for RFC cleanup logic, specifically the broken chain recreation fix.
"""
import unittest
import sys
import os

# Add the production scripts to the path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..', 'scripts', 'python', 'production'))

from rfc_cleanup_duplicates import RFCCleanupLogic


class TestRFCCleanupLogic(unittest.TestCase):
    """Test cases for RFC cleanup logic."""

    def test_extract_clean_rfc_title_normal_title(self):
        """Test extraction with a normal RFC title (no prefix)."""
        title = "GAME-RFC-009-03: Implement AI Agent Actor Clustering"
        result = RFCCleanupLogic.extract_clean_rfc_title(title)
        self.assertEqual(result, title)

    def test_extract_clean_rfc_title_single_prefix(self):
        """Test extraction with a single 'Recreated broken chain:' prefix."""
        title = "Recreated broken chain: GAME-RFC-009-03: Implement AI Agent Actor Clustering"
        expected = "GAME-RFC-009-03: Implement AI Agent Actor Clustering"
        result = RFCCleanupLogic.extract_clean_rfc_title(title)
        self.assertEqual(result, expected)

    def test_extract_clean_rfc_title_double_prefix(self):
        """Test extraction with double 'Recreated broken chain:' prefixes."""
        title = "Recreated broken chain: Recreated broken chain: GAME-RFC-009-03: Implement AI Agent Actor Clustering"
        expected = "GAME-RFC-009-03: Implement AI Agent Actor Clustering"
        result = RFCCleanupLogic.extract_clean_rfc_title(title)
        self.assertEqual(result, expected)

    def test_extract_clean_rfc_title_multiple_prefixes(self):
        """Test extraction with multiple cascading prefixes (the actual broken case)."""
        title = "Recreated broken chain: Recreated broken chain: Recreated broken chain: Recreated broken chain: Recreated broken chain: Recreated broken chain: Recreated broken chain: GAME-RFC-009-03: Implement AI Agent Actor Clustering"
        expected = "GAME-RFC-009-03: Implement AI Agent Actor Clustering"
        result = RFCCleanupLogic.extract_clean_rfc_title(title)
        self.assertEqual(result, expected)

    def test_extract_clean_rfc_title_empty_string(self):
        """Test extraction with empty string."""
        result = RFCCleanupLogic.extract_clean_rfc_title("")
        self.assertEqual(result, "")

    def test_extract_clean_rfc_title_none_input(self):
        """Test extraction with None input."""
        result = RFCCleanupLogic.extract_clean_rfc_title(None)
        self.assertEqual(result, None)

    def test_extract_clean_rfc_title_only_prefix(self):
        """Test extraction with only the prefix and no content."""
        title = "Recreated broken chain: "
        result = RFCCleanupLogic.extract_clean_rfc_title(title)
        self.assertEqual(result, title)  # Should return original if no content left

    def test_extract_clean_rfc_title_whitespace_handling(self):
        """Test extraction properly handles whitespace."""
        title = "Recreated broken chain:    GAME-RFC-009-03: Implement AI Agent Actor Clustering   "
        expected = "GAME-RFC-009-03: Implement AI Agent Actor Clustering   "  # Should preserve trailing whitespace
        result = RFCCleanupLogic.extract_clean_rfc_title(title)
        self.assertEqual(result, expected)

    def test_extract_clean_rfc_title_non_rfc_title(self):
        """Test extraction with non-RFC titles."""
        title = "Some random issue title"
        result = RFCCleanupLogic.extract_clean_rfc_title(title)
        self.assertEqual(result, title)

    def test_extract_clean_rfc_title_mixed_case_prefix(self):
        """Test that the prefix matching is case-sensitive (should not match different cases)."""
        title = "recreated broken chain: GAME-RFC-009-03: Implement AI Agent Actor Clustering"
        result = RFCCleanupLogic.extract_clean_rfc_title(title)
        self.assertEqual(result, title)  # Should not strip lowercase prefix

    def test_recreation_prevents_recursive_prefixes(self):
        """Test that the recreation process prevents recursive prefixes."""
        # Simulate what happens in the broken chain recreation process
        original_broken_title = "Recreated broken chain: Recreated broken chain: GAME-RFC-009-03: Implement AI Agent Actor Clustering"
        
        # Apply the fix: extract clean title, then add single prefix
        clean_title = RFCCleanupLogic.extract_clean_rfc_title(original_broken_title)
        new_title = f"Recreated broken chain: {clean_title}"
        
        expected = "Recreated broken chain: GAME-RFC-009-03: Implement AI Agent Actor Clustering"
        self.assertEqual(new_title, expected)
        
        # Verify that applying the process again doesn't add more prefixes
        clean_title_again = RFCCleanupLogic.extract_clean_rfc_title(new_title)
        new_title_again = f"Recreated broken chain: {clean_title_again}"
        
        # Should still be the same single prefix
        self.assertEqual(new_title_again, expected)


if __name__ == '__main__':
    unittest.main()