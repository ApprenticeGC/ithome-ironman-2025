#!/usr/bin/env python3
"""
Test runner for RFC automation system with enhanced reporting
"""

import json
import sys
import time
import unittest
from io import StringIO
from pathlib import Path

# Add parent directory to path for imports
parent_dir = Path(__file__).parent.parent
sys.path.insert(0, str(parent_dir))

from test_rfc_automation import *


class RFCTestResult(unittest.TextTestResult):
    """Custom test result class with detailed reporting"""

    def __init__(self, stream, descriptions, verbosity):
        super().__init__(stream, descriptions, verbosity)
        self.test_results = []
        self.start_time = None

    def startTest(self, test):
        super().startTest(test)
        self.start_time = time.time()

    def addSuccess(self, test):
        super().addSuccess(test)
        duration = time.time() - self.start_time
        self.test_results.append({"test": str(test), "status": "PASS", "duration": duration, "error": None})

    def addError(self, test, err):
        super().addError(test, err)
        duration = time.time() - self.start_time
        self.test_results.append(
            {"test": str(test), "status": "ERROR", "duration": duration, "error": self._exc_info_to_string(err, test)}
        )

    def addFailure(self, test, err):
        super().addFailure(test, err)
        duration = time.time() - self.start_time
        self.test_results.append(
            {"test": str(test), "status": "FAIL", "duration": duration, "error": self._exc_info_to_string(err, test)}
        )

    def addSkip(self, test, reason):
        super().addSkip(test, reason)
        duration = time.time() - self.start_time
        self.test_results.append({"test": str(test), "status": "SKIP", "duration": duration, "error": reason})


class RFCTestRunner:
    """Enhanced test runner for RFC automation tests"""

    def __init__(self):
        self.results = None

    def run_tests(self, verbosity=2):
        """Run all RFC automation tests"""
        # Discover tests
        loader = unittest.TestLoader()
        suite = loader.loadTestsFromModule(sys.modules[__name__])

        # Run tests with custom result class
        stream = StringIO()
        runner = unittest.TextTestRunner(stream=stream, verbosity=verbosity, resultclass=RFCTestResult)

        print("Running RFC Automation Tests...")
        print("=" * 70)

        start_time = time.time()
        self.results = runner.run(suite)
        end_time = time.time()

        # Print results
        self._print_summary(end_time - start_time)
        self._print_detailed_results()

        return self.results.wasSuccessful()

    def _print_summary(self, total_time):
        """Print test summary"""
        total_tests = self.results.testsRun
        failures = len(self.results.failures)
        errors = len(self.results.errors)
        skipped = len(self.results.skipped)
        passed = total_tests - failures - errors - skipped

        print("\nTest Summary:")
        print(f"  Total Tests: {total_tests}")
        print(f"  Passed: {passed}")
        print(f"  Failed: {failures}")
        print(f"  Errors: {errors}")
        print(f"  Skipped: {skipped}")
        print(f"  Time: {total_time:.2f}s")

        if self.results.wasSuccessful():
            print("\nAll tests passed!")
        else:
            print(f"\n{failures + errors} test(s) failed!")

    def _print_detailed_results(self):
        """Print detailed test results"""
        if hasattr(self.results, "test_results"):
            print("\nDetailed Results:")
            print("-" * 70)

            for result in self.results.test_results:
                status_mark = {"PASS": "[PASS]", "FAIL": "[FAIL]", "ERROR": "[ERROR]", "SKIP": "[SKIP]"}

                mark = status_mark.get(result["status"], "[?]")
                test_name = result["test"].split(".")[-1]
                duration = result["duration"]

                print(f"{mark} {test_name:<50} ({duration:.3f}s)")

                if result["error"] and result["status"] in ["FAIL", "ERROR"]:
                    # Print first few lines of error
                    error_lines = result["error"].split("\n")[:3]
                    for line in error_lines:
                        if line.strip():
                            print(f"      {line.strip()}")

    def generate_json_report(self, output_file="test_results.json"):
        """Generate JSON test report"""
        if not self.results:
            return

        report = {
            "timestamp": time.time(),
            "summary": {
                "total": self.results.testsRun,
                "passed": self.results.testsRun - len(self.results.failures) - len(self.results.errors),
                "failed": len(self.results.failures),
                "errors": len(self.results.errors),
                "skipped": len(self.results.skipped),
                "success": self.results.wasSuccessful(),
            },
            "tests": getattr(self.results, "test_results", []),
        }

        with open(output_file, "w") as f:
            json.dump(report, f, indent=2)

        print(f"\nJSON report saved to: {output_file}")


def validate_test_environment():
    """Validate that test environment is properly set up"""
    issues = []

    # Check Python version
    if sys.version_info < (3, 8):
        issues.append("Python 3.8+ required")

    # Check required directories exist
    production_dir = Path(__file__).parent.parent / "production"
    if not production_dir.exists():
        issues.append(f"Production directory not found: {production_dir}")

    # Check required files exist
    required_files = [
        "generate_micro_issues_from_rfc.py",
        "generate_micro_issues_collection.py",
        "notion_page_discovery.py",
    ]

    for file_name in required_files:
        file_path = production_dir / file_name
        if not file_path.exists():
            issues.append(f"Required file not found: {file_path}")

    return issues


def main():
    """Main test runner entry point"""
    print("RFC Automation Test Suite")
    print("=" * 70)

    # Validate environment
    issues = validate_test_environment()
    if issues:
        print("Environment validation failed:")
        for issue in issues:
            print(f"  - {issue}")
        return 1

    print("Environment validation passed")

    # Run tests
    runner = RFCTestRunner()
    success = runner.run_tests(verbosity=2)

    # Generate reports
    runner.generate_json_report()

    # Return appropriate exit code
    return 0 if success else 1


if __name__ == "__main__":
    sys.exit(main())
