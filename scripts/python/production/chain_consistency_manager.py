#!/usr/bin/env python3
"""
Chain consistency manager per Flow-RFC-008.

Phase 1 implementation focuses on detection and dry-run planning:
- Identify chains (issue/PR/branch/CI groupings) by RFC identifier (e.g., RFC-093-02)
- Detect broken chain states (branch-only, issue-only-assigned, closed-conflict PRs,
  stuck CI runs, duplicate PRs)
- Emit a machine-readable plan describing remediation actions (no destructive operations yet)

Usage:
    python chain_consistency_manager.py --repo ApprenticeGC/ithome-ironman-2025
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Dict, Iterable, List, Optional
from urllib.parse import urlencode

STATE_BRANCH_ONLY = "BRANCH_ONLY"
STATE_ISSUE_ONLY_ASSIGNED = "ISSUE_ONLY_ASSIGNED"
STATE_PR_CLOSED_CONFLICT = "PR_CLOSED_CONFLICT"
STATE_CI_STUCK = "CI_STUCK"
STATE_DUPLICATE_PRS = "DUPLICATE_PRS"
KNOWN_STATES = {
    STATE_BRANCH_ONLY,
    STATE_ISSUE_ONLY_ASSIGNED,
    STATE_PR_CLOSED_CONFLICT,
    STATE_CI_STUCK,
    STATE_DUPLICATE_PRS,
}

CHAIN_PATTERN = re.compile(r"(?:Game-)?RFC-(\d{1,4})-(\d{1,3})", re.IGNORECASE)
BRANCH_PATTERN = re.compile(r"(?:copilot|automation|bot)/rfc-(\d{1,4})-(\d{1,3})", re.IGNORECASE)
CI_STUCK_THRESHOLD_MINUTES = 30


class ChainConsistencyError(RuntimeError):
    """Raised for fatal errors in the chain consistency manager."""


@dataclass
class IssueInfo:
    number: int
    title: str
    state: str
    assignees: List[str]
    url: str
    updated_at: Optional[str] = None

    def to_dict(self) -> Dict[str, object]:
        return {
            "number": self.number,
            "title": self.title,
            "state": self.state,
            "assignees": self.assignees,
            "url": self.url,
            "updated_at": self.updated_at,
        }


@dataclass
class PullInfo:
    number: int
    title: str
    state: str
    merged: bool
    draft: bool
    head_ref: str
    source_branch: str
    url: str
    updated_at: Optional[str] = None
    closed_at: Optional[str] = None

    def to_dict(self) -> Dict[str, object]:
        return {
            "number": self.number,
            "title": self.title,
            "state": self.state,
            "merged": self.merged,
            "draft": self.draft,
            "head_ref": self.head_ref,
            "source_branch": self.source_branch,
            "url": self.url,
            "updated_at": self.updated_at,
            "closed_at": self.closed_at,
        }


@dataclass
class BranchInfo:
    name: str
    protected: bool
    url: str

    def to_dict(self) -> Dict[str, object]:
        return {
            "name": self.name,
            "protected": self.protected,
            "url": self.url,
        }


@dataclass
class RunInfo:
    run_id: int
    workflow_name: str
    head_branch: str
    status: str
    conclusion: Optional[str]
    created_at: str
    url: str

    def age_minutes(self, reference: datetime) -> float:
        created = parse_github_timestamp(self.created_at)
        return (reference - created).total_seconds() / 60.0

    def to_dict(self) -> Dict[str, object]:
        return {
            "run_id": self.run_id,
            "workflow_name": self.workflow_name,
            "head_branch": self.head_branch,
            "status": self.status,
            "conclusion": self.conclusion,
            "created_at": self.created_at,
            "url": self.url,
        }


@dataclass
class ChainRecord:
    chain_id: str
    issues: List[IssueInfo] = field(default_factory=list)
    pull_requests: List[PullInfo] = field(default_factory=list)
    branches: List[BranchInfo] = field(default_factory=list)
    runs: List[RunInfo] = field(default_factory=list)

    def detect_states(self, *, now: datetime) -> List[str]:
        states: List[str] = []
        open_prs = [pr for pr in self.pull_requests if pr.state.lower() == "open"]
        closed_prs = [pr for pr in self.pull_requests if pr.state.lower() == "closed" and not pr.merged]
        branches_present = len(self.branches) > 0
        open_issues = [iss for iss in self.issues if iss.state.lower() == "open"]
        open_issue_assigned = any(issue.assignees for issue in open_issues)
        closed_issues = [iss for iss in self.issues if iss.state.lower() == "closed"]

        if branches_present and not open_prs and not open_issues and closed_issues:
            states.append(STATE_BRANCH_ONLY)

        if open_issue_assigned and not open_prs and not branches_present:
            states.append(STATE_ISSUE_ONLY_ASSIGNED)

        if closed_prs:
            states.append(STATE_PR_CLOSED_CONFLICT)

        stuck_runs = [
            run
            for run in self.runs
            if run.status in {"in_progress", "queued"} and run.age_minutes(now) > CI_STUCK_THRESHOLD_MINUTES
        ]
        if stuck_runs:
            states.append(STATE_CI_STUCK)

        if len(open_prs) > 1:
            states.append(STATE_DUPLICATE_PRS)

        return states

    def recommended_actions(self, states: Iterable[str]) -> List[str]:
        actions: List[str] = []
        for state in states:
            actions.extend(RECOMMENDATIONS.get(state, []))
        seen = set()
        unique: List[str] = []
        for action in actions:
            if action not in seen:
                unique.append(action)
                seen.add(action)
        return unique

    def evidence(self) -> Dict[str, object]:
        return {
            "issues": [issue.to_dict() for issue in self.issues],
            "pull_requests": [pr.to_dict() for pr in self.pull_requests],
            "branches": [br.to_dict() for br in self.branches],
            "workflow_runs": [run.to_dict() for run in self.runs],
        }


RECOMMENDATIONS: Dict[str, List[str]] = {
    STATE_BRANCH_ONLY: [
        "Delete stale automation branch to prevent duplicate chains",
        "Recreate or reopen the corresponding micro issue if work must resume",
    ],
    STATE_ISSUE_ONLY_ASSIGNED: [
        "Unassign the issue or requeue the next micro task before re-dispatching",
        "Ensure branch creation/PR bootstrapping automation kicks in",
    ],
    STATE_PR_CLOSED_CONFLICT: [
        "Close conflicting PRs with explanatory comment",
        "Delete associated automation branches",
        "Recreate micro issue (suffix -R1) once cleanup completes",
    ],
    STATE_CI_STUCK: [
        "Cancel stuck workflow runs and trigger fresh CI dispatch",
        "Inspect workflow logs for blocking failures",
    ],
    STATE_DUPLICATE_PRS: [
        "Keep oldest PR open and close later duplicates",
        "Ensure assignment mutex prevents parallel automation chains",
    ],
}


def run(cmd: List[str], *, check: bool = True) -> subprocess.CompletedProcess:
    return subprocess.run(
        cmd,
        check=check,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )


def gh_api(path: str, *, params: Optional[Dict[str, str]] = None) -> List[Dict[str, object]]:
    params = params or {}
    query = urlencode(params)
    full_path = path if not query else f"{path}?{query}"
    result = run(["gh", "api", full_path])
    try:
        data = json.loads(result.stdout)
    except json.JSONDecodeError as exc:
        raise ChainConsistencyError(f"Failed to parse gh api response for {full_path}: {exc}") from exc
    if isinstance(data, dict) and "items" in data:
        return data["items"]
    if isinstance(data, list):
        return data
    return [data]


def gh_api_paginated(path: str, *, params: Optional[Dict[str, str]] = None) -> List[Dict[str, object]]:
    params = params.copy() if params else {}
    per_page = int(params.get("per_page", "100"))
    params["per_page"] = str(per_page)
    page = 1
    aggregated: List[Dict[str, object]] = []
    while True:
        params["page"] = str(page)
        batch = gh_api(path, params=params)
        if not batch:
            break
        aggregated.extend(batch)
        if len(batch) < per_page:
            break
        page += 1
    return aggregated


def parse_github_timestamp(value: Optional[str]) -> datetime:
    if not value:
        return datetime.now(timezone.utc)
    try:
        if value.endswith("Z"):
            return datetime.strptime(value, "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)
        return datetime.fromisoformat(value)
    except ValueError:
        return datetime.now(timezone.utc)


def normalize_chain_id(series: str, micro: str) -> str:
    return f"RFC-{int(series):03d}-{int(micro):02d}"


def extract_chain_id(text: Optional[str]) -> Optional[str]:
    if not text:
        return None
    match = CHAIN_PATTERN.search(text)
    if not match:
        return None
    return normalize_chain_id(match.group(1), match.group(2))


def extract_chain_id_from_branch(branch: str) -> Optional[str]:
    match = BRANCH_PATTERN.search(branch)
    if not match:
        return extract_chain_id(branch)
    return normalize_chain_id(match.group(1), match.group(2))


def collect_issues(repo: str) -> Dict[str, List[IssueInfo]]:
    issues_data = gh_api_paginated(f"repos/{repo}/issues", params={"state": "all"})
    chains: Dict[str, List[IssueInfo]] = {}
    for issue in issues_data:
        if "pull_request" in issue:
            continue
        chain_id = extract_chain_id(issue.get("title"))
        if not chain_id:
            continue
        info = IssueInfo(
            number=issue["number"],
            title=issue.get("title", ""),
            state=issue.get("state", ""),
            assignees=[assignee.get("login") for assignee in issue.get("assignees", []) if assignee.get("login")],
            url=issue.get("html_url", ""),
            updated_at=issue.get("updated_at"),
        )
        chains.setdefault(chain_id, []).append(info)
    return chains


def collect_pull_requests(repo: str) -> Dict[str, List[PullInfo]]:
    pulls_data = gh_api_paginated(f"repos/{repo}/pulls", params={"state": "all"})
    chains: Dict[str, List[PullInfo]] = {}
    for pr in pulls_data:
        title = pr.get("title", "")
        head_ref = pr.get("head", {}).get("ref", "")
        chain_id = extract_chain_id(title) or extract_chain_id_from_branch(head_ref)
        if not chain_id:
            continue
        info = PullInfo(
            number=pr["number"],
            title=title,
            state=pr.get("state", ""),
            merged=bool(pr.get("merged_at")),
            draft=pr.get("draft", False),
            head_ref=head_ref,
            source_branch=head_ref,
            url=pr.get("html_url", ""),
            updated_at=pr.get("updated_at"),
            closed_at=pr.get("closed_at"),
        )
        chains.setdefault(chain_id, []).append(info)
    return chains


def collect_branches(repo: str) -> Dict[str, List[BranchInfo]]:
    branches_data = gh_api_paginated(f"repos/{repo}/branches")
    chains: Dict[str, List[BranchInfo]] = {}
    for branch in branches_data:
        name = branch.get("name", "")
        chain_id = extract_chain_id_from_branch(name)
        if not chain_id:
            continue
        info = BranchInfo(
            name=name,
            protected=branch.get("protected", False),
            url=branch.get("_links", {}).get("html", ""),
        )
        chains.setdefault(chain_id, []).append(info)
    return chains


def collect_workflow_runs(repo: str, *, max_runs: int) -> Dict[str, List[RunInfo]]:
    runs_data = gh_api_paginated(f"repos/{repo}/actions/runs", params={"per_page": str(max_runs)})
    chains: Dict[str, List[RunInfo]] = {}
    for run in runs_data[:max_runs]:
        branch = run.get("head_branch", "")
        chain_id = extract_chain_id_from_branch(branch)
        if not chain_id:
            continue
        info = RunInfo(
            run_id=run.get("id"),
            workflow_name=run.get("name", ""),
            head_branch=branch,
            status=run.get("status", ""),
            conclusion=run.get("conclusion"),
            created_at=run.get("created_at", ""),
            url=run.get("html_url", ""),
        )
        chains.setdefault(chain_id, []).append(info)
    return chains


def build_chain_records(repo: str, *, max_runs: int) -> Dict[str, ChainRecord]:
    chains: Dict[str, ChainRecord] = {}

    def ensure(chain_id: str) -> ChainRecord:
        return chains.setdefault(chain_id, ChainRecord(chain_id=chain_id))

    issue_map = collect_issues(repo)
    for chain_id, issues in issue_map.items():
        ensure(chain_id).issues.extend(issues)

    pr_map = collect_pull_requests(repo)
    for chain_id, prs in pr_map.items():
        ensure(chain_id).pull_requests.extend(prs)

    branch_map = collect_branches(repo)
    for chain_id, branches in branch_map.items():
        ensure(chain_id).branches.extend(branches)

    run_map = collect_workflow_runs(repo, max_runs=max_runs)
    for chain_id, runs in run_map.items():
        ensure(chain_id).runs.extend(runs)

    return chains


def generate_plan(repo: str, *, max_runs: int, now: Optional[datetime] = None) -> Dict[str, object]:
    now = now or datetime.now(timezone.utc)
    records = build_chain_records(repo, max_runs=max_runs)
    chains_output: List[Dict[str, object]] = []
    state_counts: Dict[str, int] = {state: 0 for state in KNOWN_STATES}

    for chain_id, record in sorted(records.items()):
        states = record.detect_states(now=now)
        if not states:
            continue
        for state in states:
            state_counts[state] += 1
        chains_output.append(
            {
                "chain_id": chain_id,
                "states": states,
                "recommended_actions": record.recommended_actions(states),
                "evidence": record.evidence(),
            }
        )

    flagged = len(chains_output)
    summary = {
        "chains_total": len(records),
        "chains_flagged": flagged,
        "state_counts": {k: v for k, v in state_counts.items() if v},
    }

    return {
        "generated_at": now.isoformat(),
        "repo": repo,
        "summary": summary,
        "chains": chains_output,
    }


def parse_args(argv: Optional[List[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Detect automation chain inconsistencies")
    parser.add_argument("--repo", help="owner/name repository (defaults to GITHUB_REPOSITORY)")
    parser.add_argument("--output", default="chain_reset_plan.json", help="Path to write remediation plan JSON")
    parser.add_argument("--max-runs", type=int, default=200, help="Maximum workflow runs to inspect")
    parser.add_argument("--destructive", action="store_true", help="Execute destructive cleanup (not yet implemented)")
    parser.add_argument("--print", action="store_true", help="Print plan JSON to stdout as well")
    return parser.parse_args(argv)


def main(argv: Optional[List[str]] = None) -> int:
    args = parse_args(argv)
    repo = args.repo or os.environ.get("GITHUB_REPOSITORY")
    if not repo:
        raise ChainConsistencyError("Repository must be provided via --repo or GITHUB_REPOSITORY env var")

    if args.destructive:
        print(
            "Destructive mode is not implemented yet; generating dry-run plan only.",
            file=sys.stderr,
        )

    plan = generate_plan(repo, max_runs=args.max_runs)
    output_path = os.path.abspath(args.output)
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as fh:
        json.dump(plan, fh, indent=2)

    print(f"Chain consistency plan written to {output_path}")
    flagged = plan["summary"]["chains_flagged"]
    if flagged:
        print(f"Detected {flagged} chain(s) requiring attention.")
    else:
        print("No inconsistent chains detected.")

    if args.print:
        print(json.dumps(plan, indent=2))

    return 0


if __name__ == "__main__":
    sys.exit(main())
