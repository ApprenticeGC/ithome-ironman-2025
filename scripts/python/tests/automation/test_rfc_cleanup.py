#!/usr/bin/env python3
"""
Test suite for RFC cleanup functionality.
"""

import pytest
import sys
import os

# Add the production directory to the path so we can import the module
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "production"))

from rfc_cleanup_duplicates import RFCCleanupLogic


class TestRFCCleanup:
    """Test cases for RFC cleanup functionality."""

    def test_normalize_recreated_title_simple(self):
        """Test normalizing a simple title without any prefixes."""
        title = "GAME-RFC-013-02: Create Configuration Security and Encryption"
        result = RFCCleanupLogic.normalize_recreated_title(title)
        assert result == title

    def test_normalize_recreated_title_single_prefix(self):
        """Test normalizing a title with one 'Recreated broken chain:' prefix."""
        title = "Recreated broken chain: GAME-RFC-013-02: Create Configuration Security and Encryption"
        expected = "GAME-RFC-013-02: Create Configuration Security and Encryption"
        result = RFCCleanupLogic.normalize_recreated_title(title)
        assert result == expected

    def test_normalize_recreated_title_multiple_prefixes(self):
        """Test normalizing a title with multiple nested 'Recreated broken chain:' prefixes."""
        title = "Recreated broken chain: Recreated broken chain: Recreated broken chain: GAME-RFC-013-02: Create Configuration Security and Encryption"
        expected = "GAME-RFC-013-02: Create Configuration Security and Encryption"
        result = RFCCleanupLogic.normalize_recreated_title(title)
        assert result == expected

    def test_normalize_recreated_title_case_insensitive(self):
        """Test that normalization works with different cases."""
        title = "RECREATED BROKEN CHAIN: recreated broken chain: GAME-RFC-013-02: Create Configuration Security and Encryption"
        expected = "GAME-RFC-013-02: Create Configuration Security and Encryption"
        result = RFCCleanupLogic.normalize_recreated_title(title)
        assert result == expected

    def test_normalize_recreated_title_with_extra_whitespace(self):
        """Test normalizing titles with extra whitespace around prefixes."""
        title = "  Recreated broken chain:   Recreated broken chain:   GAME-RFC-013-02: Create Configuration Security and Encryption  "
        expected = "GAME-RFC-013-02: Create Configuration Security and Encryption"
        result = RFCCleanupLogic.normalize_recreated_title(title)
        assert result == expected

    def test_normalize_recreated_title_edge_case_empty(self):
        """Test that empty/whitespace-only titles are handled gracefully."""
        title = "   "
        result = RFCCleanupLogic.normalize_recreated_title(title)
        assert result == ""

    def test_normalize_recreated_title_edge_case_only_prefix(self):
        """Test a title that's only the prefix."""
        title = "Recreated broken chain:"
        result = RFCCleanupLogic.normalize_recreated_title(title)
        # Should return the original since we stripped everything
        assert result == title.strip()

    def test_normalize_recreated_title_partial_match(self):
        """Test that partial matches don't get removed."""
        title = "Recreated something: GAME-RFC-013-02: Create Configuration Security"
        result = RFCCleanupLogic.normalize_recreated_title(title)
        # Should not change since it's not the exact pattern
        assert result == title

    def test_normalize_recreated_title_real_world_scenario(self):
        """Test the exact scenario from the GitHub issue."""
        title = "Recreated broken chain: Recreated broken chain: Recreated broken chain: Recreated broken chain: Recreated broken chain: Recreated broken chain: Recreated broken chain: Recreated broken chain: Recreated broken chain: GAME-RFC-013-02: Create Configuration Security and Encryption"
        expected = "GAME-RFC-013-02: Create Configuration Security and Encryption"
        result = RFCCleanupLogic.normalize_recreated_title(title)
        assert result == expected

    def test_placeholder(self):
        """Placeholder test."""
        assert True


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
