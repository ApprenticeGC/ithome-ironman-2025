#!/usr/bin/env python3
"""
Basic tests for legacy scripts compatibility
"""

import pathlib
import sys
import unittest

# Add production directory to path for imports
production_dir = pathlib.Path(__file__).parent.parent / "production"
sys.path.insert(0, str(production_dir))


class TestScripts(unittest.TestCase):
    """Test basic script functionality"""

    def test_imports(self):
        """Test that all production scripts can be imported"""
        try:
            import assign_issue_to_copilot
            import ensure_automerge_or_comment
            import ensure_closes_link

            self.assertTrue(True)
        except ImportError as e:
            self.fail(f"Failed to import scripts: {e}")

    def test_rfc_automation_imports(self):
        """Test that RFC automation scripts can be imported"""
        try:
            import generate_micro_issues_collection
            import generate_micro_issues_from_rfc
            import notion_page_discovery

            self.assertTrue(True)
        except ImportError as e:
            self.fail(f"Failed to import RFC automation scripts: {e}")


if __name__ == "__main__":
    unittest.main()
