#!/usr/bin/env python3
"""
Simple test runner for GitHub automation scripts
Usage: python run_tests.py [--include-precommit]
"""
import argparse
import os
import subprocess
import sys


def run_command(cmd, description):
    """Run a command and return success status"""
    print(f"\n[TEST] {description}")
    print(f"Running: {' '.join(cmd) if isinstance(cmd, list) else cmd}")

    try:
        result = subprocess.run(cmd, capture_output=True, text=True, check=True)
        print("PASSED")
        if result.stdout:
            print(f"Output: {result.stdout.strip()}")
        return True
    except subprocess.CalledProcessError as e:
        print("FAILED")
        print(f"Error: {e.stderr.strip()}")
        return False


def main():
    """Run all tests"""
    parser = argparse.ArgumentParser(description="Run GitHub automation script tests")
    parser.add_argument(
        "--include-precommit",
        action="store_true",
        help="Include pre-commit validation (requires pre-commit to be installed)",
    )
    args = parser.parse_args()

    print("Running GitHub Automation Script Tests")
    print("=" * 50)

    # Change to the repository root
    repo_root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
    os.chdir(repo_root)

    tests_passed = 0
    total_tests = 0

    # Test 1: Syntax check all Python scripts
    total_tests += 1
    if run_command(
        [
            sys.executable,
            "-m",
            "py_compile",
            "scripts/python/production/ensure_automerge_or_comment.py",
        ],
        "Syntax check: ensure_automerge_or_comment.py",
    ):
        tests_passed += 1

    # Test 2: Syntax check assign_issue_to_copilot.py
    total_tests += 1
    if run_command(
        [
            sys.executable,
            "-m",
            "py_compile",
            "scripts/python/production/assign_issue_to_copilot.py",
        ],
        "Syntax check: assign_issue_to_copilot.py",
    ):
        tests_passed += 1

    # Test 3: Syntax check ensure_closes_link.py
    total_tests += 1
    if run_command(
        [
            sys.executable,
            "-m",
            "py_compile",
            "scripts/python/production/ensure_closes_link.py",
        ],
        "Syntax check: ensure_closes_link.py",
    ):
        tests_passed += 1

    # Test 4: Syntax check auto_approve_or_dispatch.py
    total_tests += 1
    if run_command(
        [
            sys.executable,
            "-m",
            "py_compile",
            "scripts/python/production/auto_approve_or_dispatch.py",
        ],
        "Syntax check: auto_approve_or_dispatch.py",
    ):
        tests_passed += 1

    # Test 5: Syntax check test_pr_creation.py
    total_tests += 1
    if run_command(
        [
            sys.executable,
            "-m",
            "py_compile",
            "scripts/python/production/test_pr_creation.py",
        ],
        "Syntax check: test_pr_creation.py",
    ):
        tests_passed += 1

    # Test 6: Syntax check runner_usage.py
    total_tests += 1
    if run_command(
        [
            sys.executable,
            "-m",
            "py_compile",
            "scripts/python/production/runner_usage.py",
        ],
        "Syntax check: runner_usage.py",
    ):
        tests_passed += 1

    # Test 7: Run comprehensive unit tests
    total_tests += 1
    if run_command(
        [
            sys.executable,
            "-m",
            "pytest",
            "scripts/python/tests/test_scripts.py",
            "-v",
            "--tb=short",
        ],
        "Unit tests: test_scripts.py",
    ):
        tests_passed += 1

    # Test 7: Import test (ensure all modules can be imported)
    total_tests += 1
    if run_command(
        [
            sys.executable,
            "-c",
            """
import sys
sys.path.insert(0, "scripts/python/production")
try:
    import ensure_automerge_or_comment
    import assign_issue_to_copilot
    import ensure_closes_link
    import auto_approve_or_dispatch
    import test_pr_creation
    import runner_usage
    print("All modules imported successfully")
except ImportError as e:
    print(f"Import error: {e}")
    sys.exit(1)
        """,
        ],
        "Import test: All Python modules",
    ):
        tests_passed += 1

    # Test 8: Pre-commit validation (optional)
    if args.include_precommit:
        total_tests += 1
        if run_command(["pre-commit", "run", "--all-files"], "Pre-commit validation: All files"):
            tests_passed += 1

    # Summary
    print("\n" + "=" * 50)
    print(f"Test Results: {tests_passed}/{total_tests} tests passed")

    if tests_passed == total_tests:
        print("All tests passed! Ready for GitHub deployment.")
        return 0
    else:
        print("⚠️  Some tests failed. Please fix before deploying to GitHub.")
        return 1


if __name__ == "__main__":
    sys.exit(main())
