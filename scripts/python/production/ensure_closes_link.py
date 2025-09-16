#!/usr/bin/env python3
# Ensure PR body contains Closes #<issue> link for Copilot PRs

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import urllib.request

API = "https://api.github.com/graphql"


def token() -> str:
    t = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
    if not t:
        sys.stderr.write("Missing GITHUB_TOKEN/GH_TOKEN\n")
        sys.exit(2)
    return t


def gql(query: str, vars: dict, tok: str) -> dict:
    req = urllib.request.Request(
        API,
        data=json.dumps({"query": query, "variables": vars}).encode(),
        headers={
            "Authorization": f"bearer {tok}",
            "Content-Type": "application/json",
            "User-Agent": "ensure-closes-link/1.0",
        },
    )
    with urllib.request.urlopen(req) as r:
        obj = json.loads(r.read().decode())
    if obj.get("errors"):
        sys.stderr.write(json.dumps(obj["errors"]) + "\n")
        sys.exit(3)
    return obj.get("data", {})


def ensure_closes_link(owner: str, repo: str, pr_number: int) -> dict:
    tok = token()

    # Get PR details
    pr_q = "query($owner:String!,$name:String!,$number:Int!){repository(owner:$owner,name:$name){pullRequest(number:$number){id title body author{login}}}}"
    pr_data = gql(pr_q, {"owner": owner, "name": repo, "number": pr_number}, tok)
    pr = pr_data["repository"]["pullRequest"]

    # Skip non-Copilot PRs
    author = pr["author"]["login"]
    if author not in ["Copilot", "app/copilot-swe-agent"]:
        return {"success": True, "message": "Non-Copilot PR", "skipped": True}

    # Skip non-RFC titles
    title = pr["title"]
    if "RFC-" not in title:
        return {"success": True, "message": "No RFC in title", "skipped": True}

    # Check if body already has closes reference
    body = pr["body"] or ""
    if re.search(
        r"\b(close[sd]?|fixe?[sd]?|resolve[sd]?) #[0-9]+", body, re.IGNORECASE
    ):
        return {"success": True, "message": "Closes link exists", "skipped": True}

    # Extract RFC token from title
    rfc_match = re.search(r"RFC-(\d{3}-\d{2})", title)
    if not rfc_match:
        return {"success": True, "message": "No RFC token", "skipped": True}

    rfc_token = f"RFC-{rfc_match.group(1)}"

    # Find matching open issue
    issues_q = "query($owner:String!,$name:String!){repository(owner:$owner,name:$name){issues(first:100,states:[OPEN]){nodes{number title}}}}"
    issues_data = gql(issues_q, {"owner": owner, "name": repo}, tok)
    issues = issues_data["repository"]["issues"]["nodes"]

    issue_number = None
    for issue in issues:
        if rfc_token in issue["title"]:
            issue_number = issue["number"]
            break

    if not issue_number:
        return {
            "success": True,
            "message": f"No matching issue for {rfc_token}",
            "skipped": True,
        }

    # Update PR body
    new_body = f"{body}\n\nCloses #{issue_number}"
    update_m = "mutation($id: ID!, $body: String!) { updatePullRequest(input: {pullRequestId: $id, body: $body}) { clientMutationId } }"
    gql(update_m, {"id": pr["id"], "body": new_body}, tok)

    return {
        "success": True,
        "message": f"Appended Closes #{issue_number}",
        "pr_number": pr_number,
        "issue_number": issue_number,
    }


def main(argv: list[str]) -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--owner", required=True)
    p.add_argument("--repo", required=True)
    p.add_argument("--pr-number", type=int, required=True)
    args = p.parse_args(argv)

    result = ensure_closes_link(args.owner, args.repo, args.pr_number)
    print(json.dumps(result))
    return 0 if result.get("success") else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
