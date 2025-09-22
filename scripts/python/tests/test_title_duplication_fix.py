#!/usr/bin/env python3
"""
Test for RFC cleanup title duplication fix.
"""

import unittest
from unittest.mock import Mock, patch, MagicMock
import sys
import os

# Add the production directory to sys.path so we can import the module
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'production'))


class TestTitleDuplicationFix(unittest.TestCase):
    """Test cases for preventing title duplication in broken issue recreation."""

    def setUp(self):
        """Set up test fixtures."""
        # Mock the necessary dependencies to avoid actual GitHub API calls
        self.mock_gh = Mock()
        self.mock_gh.token = "fake_token"
        
        # Import after setting up mocks to avoid import errors
        from rfc_cleanup_duplicates import RFCCleanupRunner
        
        self.runner = RFCCleanupRunner("owner/repo", dry_run=True)
        self.runner.gh = self.mock_gh

    @patch('subprocess.run')
    def test_recreate_broken_issue_prevents_duplicate_prefix(self, mock_subprocess):
        """Test that title prefix is not duplicated when recreating broken issues."""
        # Setup
        mock_result = Mock()
        mock_result.returncode = 0
        mock_subprocess.return_value = mock_result
        
        # Test case 1: Original title without prefix should get prefix added
        result1 = self.runner._recreate_broken_issue(123, "GAME-RFC-013-02: Create Configuration Security")
        self.assertTrue(result1)
        
        # Verify the subprocess was called with correct title
        call_args = mock_subprocess.call_args[0][0]  # Get the first positional argument (cmd list)
        title_arg_index = call_args.index("--title") + 1
        expected_title1 = "Recreated broken chain: GAME-RFC-013-02: Create Configuration Security"
        self.assertEqual(call_args[title_arg_index], expected_title1)
        
        # Reset mock for next test
        mock_subprocess.reset_mock()
        
        # Test case 2: Title already with prefix should NOT get prefix duplicated
        existing_prefixed_title = "Recreated broken chain: GAME-RFC-013-02: Create Configuration Security"
        result2 = self.runner._recreate_broken_issue(456, existing_prefixed_title)
        self.assertTrue(result2)
        
        # Verify the subprocess was called with the same title (no duplication)
        call_args2 = mock_subprocess.call_args[0][0]
        title_arg_index2 = call_args2.index("--title") + 1
        # Should remain the same, not add another "Recreated broken chain:" prefix
        expected_title2 = "Recreated broken chain: GAME-RFC-013-02: Create Configuration Security"
        self.assertEqual(call_args2[title_arg_index2], expected_title2)
        
        # Test case 3: Title with multiple existing prefixes should be normalized to single prefix
        multiple_prefix_title = "Recreated broken chain: Recreated broken chain: Recreated broken chain: GAME-RFC-013-02: Create Configuration Security"
        result3 = self.runner._recreate_broken_issue(789, multiple_prefix_title)
        self.assertTrue(result3)
        
        # Verify the subprocess was called with normalized title
        call_args3 = mock_subprocess.call_args[0][0]
        title_arg_index3 = call_args3.index("--title") + 1
        expected_title3 = "Recreated broken chain: GAME-RFC-013-02: Create Configuration Security"
        self.assertEqual(call_args3[title_arg_index3], expected_title3)

    @patch('subprocess.run')
    def test_recreate_broken_issue_handles_edge_cases(self, mock_subprocess):
        """Test edge cases for title handling."""
        mock_result = Mock()
        mock_result.returncode = 0
        mock_subprocess.return_value = mock_result
        
        # Test case: Empty or minimal titles
        result1 = self.runner._recreate_broken_issue(100, "")
        self.assertTrue(result1)
        call_args1 = mock_subprocess.call_args[0][0]
        title_arg_index1 = call_args1.index("--title") + 1
        self.assertEqual(call_args1[title_arg_index1], "Recreated broken chain: ")
        
        mock_subprocess.reset_mock()
        
        # Test case: Title that contains the prefix but not at the beginning
        embedded_prefix_title = "Some title with Recreated broken chain: in middle"
        result2 = self.runner._recreate_broken_issue(101, embedded_prefix_title)
        self.assertTrue(result2)
        call_args2 = mock_subprocess.call_args[0][0]
        title_arg_index2 = call_args2.index("--title") + 1
        expected_title2 = "Recreated broken chain: Some title with Recreated broken chain: in middle"
        self.assertEqual(call_args2[title_arg_index2], expected_title2)


def normalize_recreated_title(title: str) -> str:
    """
    Normalize title to ensure only one 'Recreated broken chain:' prefix.
    This is a helper function to demonstrate the fix logic.
    """
    prefix = "Recreated broken chain: "
    
    # Remove all existing prefixes
    while title.startswith(prefix):
        title = title[len(prefix):]
    
    # Add single prefix back
    return prefix + title


class TestNormalizeTitleFunction(unittest.TestCase):
    """Test the title normalization logic separately."""
    
    def test_normalize_recreated_title(self):
        """Test the normalize function works correctly."""
        # Test case 1: No prefix should get one added
        result1 = normalize_recreated_title("GAME-RFC-013-02: Create Configuration Security")
        expected1 = "Recreated broken chain: GAME-RFC-013-02: Create Configuration Security"
        self.assertEqual(result1, expected1)
        
        # Test case 2: One prefix should remain one
        result2 = normalize_recreated_title("Recreated broken chain: GAME-RFC-013-02: Create Configuration Security")
        expected2 = "Recreated broken chain: GAME-RFC-013-02: Create Configuration Security"
        self.assertEqual(result2, expected2)
        
        # Test case 3: Multiple prefixes should be normalized to one
        result3 = normalize_recreated_title("Recreated broken chain: Recreated broken chain: Recreated broken chain: GAME-RFC-013-02: Create Configuration Security")
        expected3 = "Recreated broken chain: GAME-RFC-013-02: Create Configuration Security"
        self.assertEqual(result3, expected3)
        
        # Test case 4: Empty string
        result4 = normalize_recreated_title("")
        expected4 = "Recreated broken chain: "
        self.assertEqual(result4, expected4)


if __name__ == "__main__":
    unittest.main()