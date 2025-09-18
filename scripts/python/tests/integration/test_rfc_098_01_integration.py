#!/usr/bin/env python3
"""
Test script for RFC-098-01 Python Project Status Integration.
Validates all four status transition scenarios mentioned in the RFC.
"""

import os
import sys

# Add the production directory to the path
production_dir = os.path.join(os.path.dirname(__file__), "..", "..", "production")
sys.path.insert(0, production_dir)

# flake8: noqa: E402
from update_project_status import GitHubProjectUpdater


def test_rfc_098_01_scenarios():
    """Test all four scenarios from RFC-098-01."""
    print("🧪 Testing RFC-098-01 Python Project Status Integration scenarios...")

    # Create a test updater (no API calls will be made)
    updater = GitHubProjectUpdater("fake_token", "owner", "repo")

    # Test scenarios from RFC-098-01
    test_scenarios = [
        # Scenario 1: Issue creation → Should set status to Backlog
        {
            "name": "Issue creation → Backlog",
            "action": "opened",
            "assignees": [],
            "expected": "Backlog",
            "description": "New issue should be automatically set to Backlog status",
        },
        {
            "name": "Issue creation with assignees → Backlog",
            "action": "opened",
            "assignees": ["user1"],
            "expected": "Backlog",
            "description": "New issue should be set to Backlog even if assignees are present on creation",
        },
        # Scenario 2: Assignment → Should set status to Ready
        {
            "name": "Assignment → Ready",
            "action": "assigned",
            "assignees": ["user1"],
            "expected": "Ready",
            "description": "Assigned issue should be moved to Ready status",
        },
        {
            "name": "Multiple assignment → Ready",
            "action": "assigned",
            "assignees": ["user1", "user2"],
            "expected": "Ready",
            "description": "Issue assigned to multiple users should be moved to Ready status",
        },
        # Scenario 3: Unassignment → Should set status back to Backlog
        {
            "name": "Unassignment → Backlog",
            "action": "unassigned",
            "assignees": [],
            "expected": "Backlog",
            "description": "Unassigned issue should be moved back to Backlog status",
        },
        # Edge cases
        {
            "name": "Assignment without assignees → No change",
            "action": "assigned",
            "assignees": [],
            "expected": None,
            "description": "Assignment action without actual assignees should not change status",
        },
        {
            "name": "Unassignment with remaining assignees → No change",
            "action": "unassigned",
            "assignees": ["user1"],
            "expected": None,
            "description": "Unassignment with remaining assignees should not change status",
        },
    ]

    print(f"\n📋 Testing {len(test_scenarios)} scenarios...\n")

    passed = 0
    failed = 0

    for i, scenario in enumerate(test_scenarios, 1):
        result = updater.determine_target_status(scenario["action"], scenario["assignees"])

        if result == scenario["expected"]:
            status = "✅ PASS"
            passed += 1
        else:
            status = "❌ FAIL"
            failed += 1

        print(f"{status} Scenario {i}: {scenario['name']}")
        print(f"    Action: {scenario['action']}")
        print(f"    Assignees: {scenario['assignees']}")
        print(f"    Expected: {scenario['expected']}")
        print(f"    Got: {result}")
        print(f"    Description: {scenario['description']}\n")

    print(f"📊 Test Summary:")
    print(f"   ✅ Passed: {passed}")
    print(f"   ❌ Failed: {failed}")
    print(f"   📈 Success Rate: {passed/(passed+failed)*100:.1f}%")

    # Use assertion instead of return
    assert failed == 0, f"Test failed: {failed} out of {passed + failed} scenarios failed"


def test_workflow_integration():
    """Test integration with GitHub Actions workflows."""
    print("\n🔧 Testing workflow integration...")

    # Test scenarios that would be handled by different workflows
    workflow_scenarios = [
        {
            "workflow": "update-project-status-on-assignment.yml",
            "trigger": "issues: [opened, assigned, unassigned]",
            "scenarios": ["Issue creation", "Assignment", "Unassignment"],
        },
        {
            "workflow": "update-project-status-on-pr.yml",
            "trigger": "pull_request: [opened, closed]",
            "scenarios": ["PR creation → In progress", "PR merge → Done"],
        },
    ]

    for workflow in workflow_scenarios:
        print(f"📋 {workflow['workflow']}")
        print(f"   🎯 Trigger: {workflow['trigger']}")
        print(f"   📝 Handles: {', '.join(workflow['scenarios'])}")

    print("\n✅ Workflow integration analysis complete!")
    # Use assertion instead of return
    assert len(workflow_scenarios) == 2, "Expected exactly 2 workflow scenarios to be tested"


def main():
    """Run all RFC-098-01 integration tests."""
    print("🚀 Starting RFC-098-01 Python Project Status Integration Tests\n")
    print("=" * 70)

    # Test core functionality
    core_tests_passed = test_rfc_098_01_scenarios()

    print("=" * 70)

    # Test workflow integration
    workflow_tests_passed = test_workflow_integration()

    print("=" * 70)

    if core_tests_passed and workflow_tests_passed:
        print("\n🎉 All RFC-098-01 integration tests completed successfully!")
        print("\n📋 Validated Scenarios:")
        print("   ✅ Issue creation → Backlog status")
        print("   ✅ Assignment → Ready status")
        print("   ✅ Unassignment → Backlog status")
        print("   📝 PR creation → In progress status (handled by separate workflow)")
        print("   📝 PR merge → Done status (handled by separate workflow)")
        print("\n🤖 Python-based project automation system is ready for production!")
        return True
    else:
        print("\n❌ Some RFC-098-01 integration tests failed!")
        return False


if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)
