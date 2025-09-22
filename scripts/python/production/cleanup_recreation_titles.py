#!/usr/bin/env python3
"""
Utility script to clean up issue titles that have accumulated recreation prefixes.

Usage:
    python cleanup_recreation_titles.py --repo ApprenticeGC/ithome-ironman-2025 [--dry-run]

This script will:
1. Find all issues with "Recreated broken chain:" prefixes
2. Clean up their titles by removing all accumulated prefixes
3. Update the issues with cleaned titles
"""

import argparse
import json
import os
import subprocess
import sys
from typing import List, Dict, Any

# Add production directory to path so we can import our utilities
import pathlib
PRODUCTION_DIR = pathlib.Path(__file__).parent
if str(PRODUCTION_DIR) not in sys.path:
    sys.path.insert(0, str(PRODUCTION_DIR))

from rfc_cleanup_duplicates import RFCCleanupLogic


def run_gh_command(args: List[str], repo: str = None) -> str:
    """Run a GitHub CLI command."""
    cmd = ["gh"] + args
    if repo:
        cmd.extend(["--repo", repo])
    
    result = subprocess.run(cmd, capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"[ERROR] Command failed: {' '.join(cmd)}")
        if result.stderr:
            print(f"Error: {result.stderr}")
        return ""
    
    return result.stdout.strip()


def get_issues_with_recreation_prefixes(repo: str) -> List[Dict[str, Any]]:
    """Get all issues that have recreation prefixes in their titles."""
    output = run_gh_command([
        "issue", "list",
        "--state", "all",
        "--json", "number,title,state",
        "--limit", "1000"
    ], repo)
    
    if not output:
        return []
    
    try:
        all_issues = json.loads(output)
    except json.JSONDecodeError:
        print("[ERROR] Failed to parse issue list")
        return []
    
    # Filter to issues with recreation prefixes
    issues_with_prefixes = []
    for issue in all_issues:
        title = issue.get("title", "")
        normalized_title, count = RFCCleanupLogic.normalize_recreation_title(title)
        if count > 0:
            issue["recreation_count"] = count
            issue["cleaned_title"] = normalized_title
            issues_with_prefixes.append(issue)
    
    return issues_with_prefixes


def update_issue_title(repo: str, issue_number: int, new_title: str, dry_run: bool = False) -> bool:
    """Update an issue's title."""
    if dry_run:
        print(f"[DRY_RUN] Would update issue #{issue_number} title to: {new_title}")
        return True
    
    result = run_gh_command([
        "issue", "edit", str(issue_number),
        "--title", new_title
    ], repo)
    
    return bool(result is not None)  # Success if no error


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description="Clean up issue titles with recreation prefixes")
    parser.add_argument("--repo", required=True, help="Repository (owner/name)")
    parser.add_argument("--dry-run", action="store_true", help="Show what would be done without making changes")
    parser.add_argument("--max-count", type=int, default=1, 
                        help="Only clean issues with more than this many recreation prefixes (default: 1)")
    
    args = parser.parse_args()
    
    print(f"🔍 Scanning for issues with recreation prefixes in {args.repo}")
    if args.dry_run:
        print("🔄 Running in DRY-RUN mode - no changes will be made")
    
    issues = get_issues_with_recreation_prefixes(args.repo)
    
    if not issues:
        print("✅ No issues found with recreation prefixes")
        return 0
    
    print(f"📋 Found {len(issues)} issues with recreation prefixes")
    
    # Filter issues that have more prefixes than the threshold
    issues_to_clean = [issue for issue in issues if issue["recreation_count"] > args.max_count]
    
    if not issues_to_clean:
        print(f"✅ No issues found with more than {args.max_count} recreation prefix(es)")
        return 0
    
    print(f"🧹 {len(issues_to_clean)} issues need cleaning:")
    
    success_count = 0
    for issue in issues_to_clean:
        issue_num = issue["number"]
        old_title = issue["title"]
        new_title = issue["cleaned_title"]
        recreation_count = issue["recreation_count"]
        
        print(f"\n📝 Issue #{issue_num} (State: {issue['state']}):")
        print(f"   Recreation count: {recreation_count}")
        print(f"   Old title: {old_title[:100]}{'...' if len(old_title) > 100 else ''}")
        print(f"   New title: {new_title}")
        
        if update_issue_title(args.repo, issue_num, new_title, args.dry_run):
            success_count += 1
            if not args.dry_run:
                print(f"   ✅ Successfully updated")
        else:
            print(f"   ❌ Failed to update")
    
    if args.dry_run:
        print(f"\n🎯 DRY-RUN: Would clean {success_count} of {len(issues_to_clean)} issues")
    else:
        print(f"\n🎉 Successfully cleaned {success_count} of {len(issues_to_clean)} issues")
    
    return 0 if success_count == len(issues_to_clean) else 1


if __name__ == "__main__":
    sys.exit(main())