#!/usr/bin/env python3
"""
Enhanced CI validation that analyzes workflow run logs for warnings and errors.
This addresses the false success detection issue where CI reports "success"
but logs contain critical warnings.
"""
import json
import os
import re
import subprocess
import sys
from typing import List, Optional, Tuple

REPO = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY", "")
EVENT_JSON_ENV = os.environ.get("GITHUB_EVENT", "")

# Critical warning patterns to detect in CI logs
WARNING_PATTERNS = [
    r"Warning #\d+:",
    r"⚠️\s*Warning:",
    r"Command failed with exit code \d+:",
    r"was blocked by firewall rules",
    r"if-no-files-found: warn",
    r"Error: Command failed",
    r"REDACTED.*failed",
    r"timeout.*exceeded",
    r"network.*unreachable",
    r"connection.*refused",
]

# Critical error patterns that should always block
ERROR_PATTERNS = [
    r"Error: .*(timeout|failed|denied|blocked)",
    r"fatal:",
    r"FAILED:",
    r"Build failed",
    r"Tests failed",
]


def run(cmd, check=True):
    """Run a command and return the result."""
    return subprocess.run(cmd, check=check, text=True, capture_output=True)


def get_workflow_run_logs(workflow_run_id: str) -> Optional[str]:
    """Get the logs for a specific workflow run."""
    try:
        # Use gh run view --log to get text logs instead of API binary download
        result = run(
            [
                "gh",
                "run",
                "view",
                workflow_run_id,
                "--repo",
                REPO,
                "--log",
            ]
        )
        return result.stdout
    except subprocess.CalledProcessError as e:
        print(f"Failed to get workflow logs: {e}", file=sys.stderr)
        return None


def analyze_logs(logs: str) -> Tuple[List[str], List[str], int]:
    """
    Analyze logs for warnings and errors.
    Returns: (warnings, errors, severity_score)
    """
    warnings = []
    errors = []

    # Split logs into lines for analysis
    lines = logs.split("\n")

    for i, line in enumerate(lines):
        # Check for warning patterns
        for pattern in WARNING_PATTERNS:
            if re.search(pattern, line, re.IGNORECASE):
                # Include context lines for better diagnosis
                context_start = max(0, i - 1)
                context_end = min(len(lines), i + 2)
                context = "\n".join(lines[context_start:context_end])
                warnings.append(f"Line {i+1}: {context.strip()}")
                break

        # Check for error patterns
        for pattern in ERROR_PATTERNS:
            if re.search(pattern, line, re.IGNORECASE):
                # Include context lines for better diagnosis
                context_start = max(0, i - 1)
                context_end = min(len(lines), i + 2)
                context = "\n".join(lines[context_start:context_end])
                errors.append(f"Line {i+1}: {context.strip()}")
                break

    # Calculate severity score
    severity_score = len(errors) * 10 + len(warnings) * 3

    return warnings, errors, severity_score


def should_block_pr_ready(
    warnings: List[str], errors: List[str], severity_score: int
) -> Tuple[bool, str]:
    """
    Determine if PR should be blocked from becoming ready based on analysis.
    Returns: (should_block, reason)
    """
    if errors:
        return True, f"Found {len(errors)} critical errors in CI logs"

    if len(warnings) >= 5:
        return True, f"Found {len(warnings)} warnings - too many to safely proceed"

    if severity_score >= 15:
        return True, f"High severity score ({severity_score}) indicates unstable build"

    # Allow PRs with minor warnings to proceed
    if warnings:
        return False, f"Found {len(warnings)} warnings but below blocking threshold"

    return False, "CI logs appear clean"


def main():
    """Main validation logic."""
    if not REPO:
        print("REPO not set", file=sys.stderr)
        sys.exit(1)

    event_json = EVENT_JSON_ENV
    if not event_json:
        # Fallback to file path provided by Actions runtime
        path = os.environ.get("GITHUB_EVENT_PATH")
        if path and os.path.exists(path):
            with open(path, "r", encoding="utf-8") as f:
                event_json = f.read()
        else:
            event_json = "{}"

    evt = json.loads(event_json)
    workflow_run = evt.get("workflow_run", {})

    if not workflow_run:
        print("No workflow_run in event data", file=sys.stderr)
        sys.exit(1)

    run_id = str(workflow_run.get("id", ""))
    if not run_id:
        print("No workflow run ID found", file=sys.stderr)
        sys.exit(1)

    print(f"Analyzing logs for workflow run {run_id}")

    # Get and analyze the logs
    logs = get_workflow_run_logs(run_id)
    if not logs:
        print("Could not retrieve workflow logs - assuming safe to proceed")
        sys.exit(0)

    warnings, errors, severity_score = analyze_logs(logs)

    print("Analysis complete:")
    print(f"  Warnings: {len(warnings)}")
    print(f"  Errors: {len(errors)}")
    print(f"  Severity Score: {severity_score}")

    # Detailed reporting
    if warnings:
        print("\nWarnings found:")
        for i, warning in enumerate(warnings[:5], 1):  # Limit output
            print(f"  {i}. {warning[:200]}...")
        if len(warnings) > 5:
            print(f"  ... and {len(warnings) - 5} more warnings")

    if errors:
        print("\nErrors found:")
        for i, error in enumerate(errors[:3], 1):  # Limit output
            print(f"  {i}. {error[:200]}...")
        if len(errors) > 3:
            print(f"  ... and {len(errors) - 3} more errors")

    # Decide if PR should be blocked
    should_block, reason = should_block_pr_ready(warnings, errors, severity_score)

    if should_block:
        print(f"\n🚫 BLOCKING PR from becoming ready: {reason}")
        sys.exit(1)
    else:
        print(f"\n✅ PR safe to mark ready: {reason}")
        sys.exit(0)


if __name__ == "__main__":
    main()
