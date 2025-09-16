#!/usr/bin/env python3
"""
Test suite for RFC cleanup duplicates workflow logic.

This module tests the duplicate detection and cleanup logic used in the
GitHub Actions workflow for maintaining sequential RFC processing.
"""

import re
from dataclasses import dataclass
from typing import Any, Dict, List


@dataclass
class MockPR:
    """Mock PR object for testing."""

    number: int
    title: str

    @property
    def rfc_number(self) -> int:
        """Extract RFC number from title."""
        match = re.search(r"RFC-(\d{3})-", self.title)
        return int(match.group(1)) if match else None

    @property
    def micro_number(self) -> int:
        """Extract micro number from title."""
        match = re.search(r"RFC-\d{3}-(\d{2})", self.title)
        return int(match.group(1)) if match else None

    def is_rfc_pr(self) -> bool:
        """Check if this PR is an RFC-related PR."""
        return bool(re.search(r"RFC-\d{3}-\d{2}", self.title))


class RFCCleanupTester:
    """Test class for RFC cleanup logic."""

    def __init__(self, mock_prs: List[Dict[str, Any]]):
        """Initialize with mock PR data."""
        self.mock_prs = [MockPR(**pr) for pr in mock_prs]
        self.rfc_prs = [pr for pr in self.mock_prs if pr.is_rfc_pr()]

    def find_duplicate_rfcs(self) -> List[Dict[str, Any]]:
        """
        Find RFC series that have multiple open PRs.

        This replicates the logic from the GitHub Actions workflow.
        """
        # Group PRs by RFC number
        rfc_groups = {}
        for pr in self.rfc_prs:
            rfc_num = pr.rfc_number
            if rfc_num not in rfc_groups:
                rfc_groups[rfc_num] = []
            rfc_groups[rfc_num].append(
                {
                    "num": pr.number,
                    "title": pr.title,
                    "r": pr.rfc_number,
                    "m": pr.micro_number,
                }
            )

        # Find groups with multiple PRs
        duplicates = []
        for rfc_num, prs in rfc_groups.items():
            if len(prs) > 1:
                duplicates.append(
                    {"rfc": rfc_num, "prs": sorted(prs, key=lambda x: x["m"])}
                )

        return duplicates

    def simulate_cleanup(
        self, duplicate_rfcs: List[Dict[str, Any]]
    ) -> List[Dict[str, Any]]:
        """
        Simulate the cleanup process for duplicate RFCs.

        Returns a list of actions that would be taken.
        """
        actions = []

        for rfc_data in duplicate_rfcs:
            rfc_num = rfc_data["rfc"]
            prs = rfc_data["prs"]

            # Keep the first (lowest micro number) PR
            pr_to_keep = prs[0]
            actions.append(
                {
                    "action": "keep",
                    "pr_number": pr_to_keep["num"],
                    "title": pr_to_keep["title"],
                    "rfc": rfc_num,
                    "micro": pr_to_keep["m"],
                }
            )

            # Remove the rest
            for pr in prs[1:]:
                actions.extend(
                    [
                        {
                            "action": "close_pr",
                            "pr_number": pr["num"],
                            "title": pr["title"],
                            "comment": (
                                "Closed due to duplicate RFC work. "
                                "Only one micro-issue per RFC series should be active at a time."
                            ),
                        },
                        {
                            "action": "delete_branch",
                            "pr_number": pr["num"],
                            "branch": f'rfc-{rfc_num}-{pr["m"]:02d}',
                        },
                        {
                            "action": "close_issue",
                            "pr_number": pr["num"],
                            "title": pr["title"],
                        },
                        {
                            "action": "recreate_issue",
                            "rfc": rfc_num,
                            "micro": pr["m"],
                            "title": pr["title"],
                            "body": (
                                f'This is a recreated issue for RFC-{rfc_num}-{pr["m"]:02d}. '
                                f"Original issue was closed due to duplicate RFC work. "
                                f"Only one micro-issue per RFC series should be active at a time."
                            ),
                        },
                    ]
                )

        return actions


def test_duplicate_detection():
    """Test the duplicate RFC detection logic."""
    print("🧪 Testing RFC Duplicate Detection")
    print("=" * 40)

    # Test data with various scenarios
    test_cases = [
        {
            "name": "Multiple RFC-093 PRs",
            "prs": [
                {"number": 80, "title": "RFC-093-01: Implement agent flow smoke test"},
                {"number": 81, "title": "RFC-093-02: Add recovery mechanisms"},
                {"number": 82, "title": "RFC-094-01: Implement flow status tracking"},
            ],
            "expected_duplicates": 1,
        },
        {
            "name": "No duplicates",
            "prs": [
                {"number": 80, "title": "RFC-093-01: Implement agent flow smoke test"},
                {"number": 82, "title": "RFC-094-01: Implement flow status tracking"},
            ],
            "expected_duplicates": 0,
        },
        {
            "name": "Multiple series with duplicates",
            "prs": [
                {"number": 80, "title": "RFC-093-01: Base implementation"},
                {"number": 81, "title": "RFC-093-02: Add features"},
                {"number": 82, "title": "RFC-094-01: Status tracking"},
                {"number": 83, "title": "RFC-094-02: UI updates"},
                {"number": 84, "title": "RFC-094-03: Testing"},
                {"number": 85, "title": "RFC-095-01: Reset functionality"},
            ],
            "expected_duplicates": 2,
        },
    ]

    all_passed = True

    for test_case in test_cases:
        print(f"\n📋 {test_case['name']}")
        print("-" * 30)

        # Display test PRs
        for pr in test_case["prs"]:
            print(f"  PR #{pr['number']}: {pr['title']}")

        # Run duplicate detection
        tester = RFCCleanupTester(test_case["prs"])
        duplicates = tester.find_duplicate_rfcs()

        print(f"\n🔍 Found {len(duplicates)} duplicate RFC series")

        if len(duplicates) == test_case["expected_duplicates"]:
            print("✅ PASS: Correct number of duplicates detected")
        else:
            print(
                f"❌ FAIL: Expected {test_case['expected_duplicates']}, got {len(duplicates)}"
            )
            all_passed = False

        # Show details of duplicates found
        for dup in duplicates:
            print(f"  RFC-{dup['rfc']}: {len(dup['prs'])} PRs")
            for pr in dup["prs"]:
                print(f"    - PR #{pr['num']}: {pr['title']}")

    return all_passed


def test_cleanup_simulation():
    """Test the cleanup simulation logic."""
    print("\n🧹 Testing Cleanup Simulation")
    print("=" * 40)

    # Test data
    test_prs = [
        {"number": 80, "title": "RFC-093-01: Base implementation"},
        {"number": 81, "title": "RFC-093-02: Add features"},
        {"number": 82, "title": "RFC-093-03: Bug fixes"},
        {"number": 83, "title": "RFC-094-01: Status tracking"},
        {"number": 84, "title": "RFC-094-02: UI updates"},
    ]

    tester = RFCCleanupTester(test_prs)
    duplicates = tester.find_duplicate_rfcs()
    actions = tester.simulate_cleanup(duplicates)

    print(f"📋 Test PRs: {len(test_prs)}")
    print(f"🔍 Duplicates found: {len(duplicates)}")
    print(f"🧹 Actions to take: {len(actions)}")

    # Group actions by type
    action_counts = {}
    for action in actions:
        action_type = action["action"]
        action_counts[action_type] = action_counts.get(action_type, 0) + 1

    print("\n📊 Action Summary:")
    for action_type, count in action_counts.items():
        print(f"  {action_type}: {count}")

    # Verify expected actions
    expected_actions = {
        "keep": 2,  # One for each RFC series
        "close_pr": 3,  # RFC-093-02, RFC-093-03, RFC-094-02
        "delete_branch": 3,
        "close_issue": 3,
        "recreate_issue": 3,
    }

    all_correct = True
    for expected_type, expected_count in expected_actions.items():
        actual_count = action_counts.get(expected_type, 0)
        if actual_count == expected_count:
            print(f"✅ {expected_type}: {actual_count} (expected {expected_count})")
        else:
            print(f"❌ {expected_type}: {actual_count} (expected {expected_count})")
            all_correct = False

    return all_correct


def test_integration_with_production_script():
    """Test integration with the actual production script."""
    print("\n🔗 Testing Integration with Production Script")
    print("=" * 50)

    import os
    import sys

    script_path = os.path.join(
        os.path.dirname(__file__), "..", "production", "rfc_cleanup_duplicates.py"
    )

    if os.path.exists(script_path):
        print(f"✅ Production script found: {script_path}")

        # Test that we can import the main classes
        try:
            sys.path.insert(0, os.path.dirname(script_path))
            from rfc_cleanup_duplicates import RFCCleanupLogic

            # Test the core logic with our test data
            test_prs = [
                {"number": 80, "title": "RFC-093-01: Base implementation"},
                {"number": 81, "title": "RFC-093-02: Add features"},
                {"number": 82, "title": "RFC-094-01: Status tracking"},
            ]

            duplicates = RFCCleanupLogic.find_duplicate_rfcs(test_prs)
            expected_duplicates = 1  # Only RFC-093 has duplicates

            if len(duplicates) == expected_duplicates:
                print(
                    "✅ Integration test passed - logic matches between "
                    "test and production"
                )
                return True
            else:
                print(
                    f"❌ Integration test failed - expected {expected_duplicates}, got {len(duplicates)}"
                )
                return False

        except ImportError as e:
            print(f"❌ Could not import production script: {e}")
            return False
    else:
        print(f"❌ Production script not found: {script_path}")
        return False


def main():
    """Run all tests."""
    print("🧪 RFC Cleanup Workflow Test Suite")
    print("===================================")

    test1_passed = test_duplicate_detection()
    test2_passed = test_cleanup_simulation()
    test3_passed = test_integration_with_production_script()

    print("\n" + "=" * 50)
    if test1_passed and test2_passed and test3_passed:
        print("🎉 All tests PASSED!")
        return 0
    else:
        print("❌ Some tests FAILED!")
        return 1


if __name__ == "__main__":
    exit(main())
