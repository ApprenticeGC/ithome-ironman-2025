#!/usr/bin/env python3
"""
Simple test runner for ensure_automerge_or_comment.py using built-in unittest

Run with: python test_runner.py
"""

import os
import sys
import unittest

# Add the scripts directory to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".github", "scripts"))

# Import test module
import test_ensure_automerge


def run_tests():
    """Discover and run all tests"""
    # Create test suite
    loader = unittest.TestLoader()
    suite = unittest.TestSuite()

    # Add test classes from the imported module
    suite.addTest(loader.loadTestsFromTestCase(test_ensure_automerge.TestRunFunction))
    suite.addTest(
        loader.loadTestsFromTestCase(test_ensure_automerge.TestGhJsonFunction)
    )
    suite.addTest(
        loader.loadTestsFromTestCase(test_ensure_automerge.TestFindPrNumberByBranch)
    )
    suite.addTest(
        loader.loadTestsFromTestCase(test_ensure_automerge.TestTryEnableAutomerge)
    )
    suite.addTest(loader.loadTestsFromTestCase(test_ensure_automerge.TestAddComment))
    suite.addTest(loader.loadTestsFromTestCase(test_ensure_automerge.TestMainFunction))

    # Run tests
    runner = unittest.TextTestRunner(verbosity=2)
    result = runner.run(suite)

    # Return exit code based on results
    return 0 if result.wasSuccessful() else 1


if __name__ == "__main__":
    sys.exit(run_tests())
