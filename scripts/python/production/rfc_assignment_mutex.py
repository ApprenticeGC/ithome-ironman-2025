#!/usr/bin/env python3
"""RFC series assignment mutex helper (Flow-RFC-009).

Ensures only one open issue per RFC series is actively assigned to automation.
- Creates/updates a tracking issue named "RFC-XYZ Series State" storing JSON state
- Fails with status code 1 when another issue in the same series is already active
- Adds queued issues to tracking document for visibility

Usage:
    python rfc_assignment_mutex.py --owner OWNER --repo NAME --issue-number 123
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import urllib.request
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional

API_URL = "https://api.github.com/graphql"
SERIES_PATTERN = re.compile(r"(?:Game-)?RFC-(\d{1,4})-(\d{1,3})", re.IGNORECASE)
TRACKING_TITLE_TEMPLATE = "{series} Series State"


class MutexError(RuntimeError):
    pass


def token() -> str:
    tok = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")
    if not tok:
        raise MutexError("GH_TOKEN or GITHUB_TOKEN must be set")
    return tok


def gql(query: str, variables: Dict[str, Any]) -> Dict[str, Any]:
    payload = json.dumps({"query": query, "variables": variables}).encode("utf-8")
    req = urllib.request.Request(
        API_URL,
        data=payload,
        headers={
            "Authorization": f"bearer {token()}",
            "Content-Type": "application/json",
            "User-Agent": "rfc-assignment-mutex/1.0",
        },
    )
    with urllib.request.urlopen(req) as resp:
        data = json.loads(resp.read().decode("utf-8"))
    if data.get("errors"):
        raise MutexError(json.dumps(data["errors"]))
    return data.get("data", {})


def now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def extract_series(title: str) -> Optional[str]:
    match = SERIES_PATTERN.search(title or "")
    if not match:
        return None
    return f"RFC-{int(match.group(1)):03d}"


def extract_series_micro(title: str) -> Optional[str]:
    match = SERIES_PATTERN.search(title or "")
    if not match:
        return None
    return f"RFC-{int(match.group(1)):03d}-{int(match.group(2)):02d}"


@dataclass
class SeriesState:
    series: str
    active_issue: Optional[int]
    queue: List[int]
    updated_at: str
    version: int = 1

    @classmethod
    def default(cls, series: str) -> "SeriesState":
        return cls(series=series, active_issue=None, queue=[], updated_at=now_iso())

    @classmethod
    def from_body(cls, series: str, body: str) -> "SeriesState":
        json_block: Optional[str] = None
        if body:
            match = re.search(r"```json\s*(\{.*?\})\s*```", body, re.S)
            if match:
                json_block = match.group(1)
        if not json_block:
            return cls.default(series)
        try:
            data = json.loads(json_block)
        except json.JSONDecodeError:
            return cls.default(series)
        return cls(
            series=data.get("series", series),
            active_issue=data.get("active_issue"),
            queue=[int(x) for x in data.get("queue", [])],
            updated_at=data.get("updated_at", now_iso()),
            version=int(data.get("version", 1)),
        )

    def to_body(self) -> str:
        state = {
            "series": self.series,
            "active_issue": self.active_issue,
            "queue": self.queue,
            "updated_at": self.updated_at,
            "version": self.version,
        }
        body_template = "Tracking state for {series} automation.\n\n```json\n{state_json}\n```\n"
        return body_template.format(series=self.series, state_json=json.dumps(state, indent=2))

    def apply_candidate(self, candidate_issue: int, candidate_open: bool, active_issue_open: Optional[bool]) -> str:
        if not candidate_open:
            raise MutexError(f"Issue #{candidate_issue} is not open; cannot acquire lock")

        if self.active_issue == candidate_issue:
            if candidate_issue in self.queue:
                self.queue.remove(candidate_issue)
            self.updated_at = now_iso()
            return "already-active"

        if self.active_issue is None or (active_issue_open is False):
            if self.active_issue and active_issue_open is False and self.active_issue in self.queue:
                self.queue = [q for q in self.queue if q != self.active_issue]
            self.active_issue = candidate_issue
            if candidate_issue in self.queue:
                self.queue.remove(candidate_issue)
            self.updated_at = now_iso()
            return "acquired"

        if candidate_issue not in self.queue:
            self.queue.append(candidate_issue)
        self.updated_at = now_iso()
        return "queued"


ISSUE_QUERY = """
query($owner:String!,$name:String!,$number:Int!){
  repository(owner:$owner,name:$name){
    id
    issue(number:$number){
      id
      number
      title
      state
    }
  }
}
"""

SEARCH_TRACKING_QUERY = """
query($query:String!){
  search(query:$query, type:ISSUE, first:5){
    nodes{
      ... on Issue {
        id
        number
        title
        state
        body
        updatedAt
      }
    }
  }
}
"""

CREATE_TRACKING_MUTATION = """
mutation($repoId:ID!,$title:String!,$body:String!){
  createIssue(input:{repositoryId:$repoId,title:$title,body:$body}){
    issue{ id number body }
  }
}
"""

UPDATE_ISSUE_MUTATION = """
mutation($issueId:ID!,$body:String!){
  updateIssue(input:{id:$issueId,body:$body}){
    issue{ id number body }
  }
}
"""


def load_issue(owner: str, name: str, number: int) -> Dict[str, Any]:
    data = gql(ISSUE_QUERY, {"owner": owner, "name": name, "number": number})
    repo = data["repository"]
    if not repo or not repo.get("issue"):
        raise MutexError(f"Issue #{number} not found in {owner}/{name}")
    return {
        "repo_id": repo["id"],
        "issue": repo["issue"],
    }


def search_tracking_issue(owner: str, name: str, series: str) -> Optional[Dict[str, Any]]:
    query = f'repo:{owner}/{name} "{series} Series State" in:title'
    data = gql(SEARCH_TRACKING_QUERY, {"query": query})
    nodes = data.get("search", {}).get("nodes", [])
    issues = [n for n in nodes if n.get("title") == TRACKING_TITLE_TEMPLATE.format(series=series)]
    if not issues:
        return None
    issues.sort(key=lambda i: i.get("updatedAt"), reverse=True)
    return issues[0]


def create_tracking_issue(repo_id: str, series: str) -> Dict[str, Any]:
    state = SeriesState.default(series)
    body = state.to_body()
    result = gql(
        CREATE_TRACKING_MUTATION,
        {"repoId": repo_id, "title": TRACKING_TITLE_TEMPLATE.format(series=series), "body": body},
    )
    return result["createIssue"]["issue"]


def update_tracking_issue(issue_id: str, body: str) -> Dict[str, Any]:
    result = gql(UPDATE_ISSUE_MUTATION, {"issueId": issue_id, "body": body})
    return result["updateIssue"]["issue"]


def ensure_series_state(owner: str, name: str, issue_number: int) -> Dict[str, Any]:
    repo_issue = load_issue(owner, name, issue_number)
    issue = repo_issue["issue"]
    series_full = extract_series_micro(issue["title"])
    if not series_full:
        return {"status": "no-series", "issue_number": issue_number}
    series = extract_series(issue["title"])
    tracking_issue = search_tracking_issue(owner, name, series)
    if not tracking_issue:
        tracking_issue = create_tracking_issue(repo_issue["repo_id"], series)
    original_body = tracking_issue.get("body") or ""
    state = SeriesState.from_body(series, original_body)

    active_issue_open: Optional[bool] = None
    if state.active_issue and state.active_issue != issue_number:
        active = load_issue(owner, name, state.active_issue)["issue"]
        active_issue_open = active.get("state") == "OPEN"
    status = state.apply_candidate(issue_number, issue.get("state") == "OPEN", active_issue_open)
    new_body = state.to_body()
    if new_body != original_body:
        tracking_info = update_tracking_issue(tracking_issue["id"], new_body)
        tracking_number = tracking_info["number"]
    else:
        tracking_number = tracking_issue.get("number")
    result = {
        "status": status,
        "series": series,
        "chain": series_full,
        "active_issue": state.active_issue,
        "queue": state.queue,
        "tracking_issue_number": tracking_number,
    }
    return result


def parse_args(argv: Optional[List[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Ensure RFC series assignment mutex")
    parser.add_argument("--owner", required=True)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--issue-number", type=int, required=True)
    return parser.parse_args(argv)


def main(argv: Optional[List[str]] = None) -> int:
    args = parse_args(argv)
    try:
        result = ensure_series_state(args.owner, args.repo, args.issue_number)
    except MutexError as exc:
        print(json.dumps({"status": "error", "message": str(exc)}))
        return 1
    print(json.dumps(result))
    if result.get("status") == "queued":
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
