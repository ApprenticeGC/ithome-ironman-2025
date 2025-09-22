#!/usr/bin/env python3
"""
Test recreation loop prevention fix for RFC cleanup duplicates.
Tests the new utility functions that prevent recreation prefix accumulation.
"""
import pathlib
import sys
import unittest

PRODUCTION_DIR = pathlib.Path(__file__).parent.parent / "production"
if str(PRODUCTION_DIR) not in sys.path:
    sys.path.insert(0, str(PRODUCTION_DIR))

import rfc_cleanup_duplicates
RFCCleanupLogic = rfc_cleanup_duplicates.RFCCleanupLogic


class RecreationLoopFixTests(unittest.TestCase):
    """Test the recreation loop prevention functionality."""
    
    def test_normalize_recreation_title_no_prefix(self):
        """Test normalizing a title with no recreation prefix."""
        title = "GAME-RFC-012-02: Create Deployment Pipeline Automation"
        normalized_title, count = RFCCleanupLogic.normalize_recreation_title(title)
        
        self.assertEqual(normalized_title, title)
        self.assertEqual(count, 0)
    
    def test_normalize_recreation_title_single_prefix(self):
        """Test normalizing a title with single recreation prefix."""
        title = "Recreated broken chain: GAME-RFC-012-02: Create Deployment Pipeline Automation"
        expected = "GAME-RFC-012-02: Create Deployment Pipeline Automation"
        normalized_title, count = RFCCleanupLogic.normalize_recreation_title(title)
        
        self.assertEqual(normalized_title, expected)
        self.assertEqual(count, 1)
    
    def test_normalize_recreation_title_multiple_prefix(self):
        """Test normalizing a title with multiple recreation prefixes (the bug case)."""
        title = ("Recreated broken chain: Recreated broken chain: Recreated broken chain: "
                "GAME-RFC-012-02: Create Deployment Pipeline Automation")
        expected = "GAME-RFC-012-02: Create Deployment Pipeline Automation"
        normalized_title, count = RFCCleanupLogic.normalize_recreation_title(title)
        
        self.assertEqual(normalized_title, expected)
        self.assertEqual(count, 3)
    
    def test_normalize_recreation_title_extreme_case(self):
        """Test normalizing a title with many recreation prefixes."""
        base_title = "GAME-RFC-012-02: Create Deployment Pipeline Automation"
        # Create a title with 8 recreation prefixes
        title = ("Recreated broken chain: " * 8) + base_title
        normalized_title, count = RFCCleanupLogic.normalize_recreation_title(title)
        
        self.assertEqual(normalized_title, base_title)
        self.assertEqual(count, 8)
    
    def test_should_recreate_issue_under_limit(self):
        """Test that issues under recreation limit can be recreated."""
        title = "GAME-RFC-012-02: Create Deployment Pipeline Automation"
        should_recreate, new_title = RFCCleanupLogic.should_recreate_issue(title, max_recreations=3)
        
        self.assertTrue(should_recreate)
        self.assertEqual(new_title, "Recreated broken chain: GAME-RFC-012-02: Create Deployment Pipeline Automation")
    
    def test_should_recreate_issue_at_limit(self):
        """Test that issues at recreation limit are blocked."""
        title = ("Recreated broken chain: " * 3) + "GAME-RFC-012-02: Create Deployment Pipeline Automation"
        should_recreate, new_title = RFCCleanupLogic.should_recreate_issue(title, max_recreations=3)
        
        self.assertFalse(should_recreate)
        self.assertEqual(new_title, "")
    
    def test_should_recreate_issue_over_limit(self):
        """Test that issues over recreation limit are blocked."""
        title = ("Recreated broken chain: " * 5) + "GAME-RFC-012-02: Create Deployment Pipeline Automation"
        should_recreate, new_title = RFCCleanupLogic.should_recreate_issue(title, max_recreations=3)
        
        self.assertFalse(should_recreate)
        self.assertEqual(new_title, "")
    
    def test_should_recreate_issue_incremental(self):
        """Test that recreation titles are created safely."""
        base_title = "GAME-RFC-012-02: Create Deployment Pipeline Automation"
        
        # First recreation 
        title1 = base_title
        should_recreate1, new_title1 = RFCCleanupLogic.should_recreate_issue(title1, max_recreations=3)
        self.assertTrue(should_recreate1)
        self.assertEqual(new_title1, "Recreated broken chain: " + base_title)
        
        # Second recreation (using the result from first)
        title2 = new_title1
        should_recreate2, new_title2 = RFCCleanupLogic.should_recreate_issue(title2, max_recreations=3)
        self.assertTrue(should_recreate2)
        self.assertEqual(new_title2, "Recreated broken chain: " + base_title)  # Should not accumulate
        
        # After max recreations
        title3 = ("Recreated broken chain: " * 3) + base_title
        should_recreate3, new_title3 = RFCCleanupLogic.should_recreate_issue(title3, max_recreations=3)
        self.assertFalse(should_recreate3)
        self.assertEqual(new_title3, "")
    
    def test_get_cleaned_title(self):
        """Test the get_cleaned_title utility function."""
        base_title = "GAME-RFC-012-02: Create Deployment Pipeline Automation"
        
        # Test with no prefixes
        self.assertEqual(RFCCleanupLogic.get_cleaned_title(base_title), base_title)
        
        # Test with single prefix
        title_with_prefix = "Recreated broken chain: " + base_title
        self.assertEqual(RFCCleanupLogic.get_cleaned_title(title_with_prefix), base_title)
        
        # Test with multiple prefixes (the problematic case)
        title_with_multiple = ("Recreated broken chain: " * 8) + base_title
        self.assertEqual(RFCCleanupLogic.get_cleaned_title(title_with_multiple), base_title)


if __name__ == "__main__":
    unittest.main()