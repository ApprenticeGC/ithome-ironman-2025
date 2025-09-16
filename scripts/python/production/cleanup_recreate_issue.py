#!/usr/bin/env python3
# Simplified port to cleanup failed run/PR/branch and recreate issue assigned to Copilot
from __future__ import annotations

import argparse
import json
import os
import sys
import urllib.error
import urllib.request

REST = "https://api.github.com"
GQL = REST + "/graphql"


def tok() -> str:
    t = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
    if not t:
        print(json.dumps({"error": "missing token"}))
        sys.exit(2)
    return t


def rest(
    method: str, url: str, token: str, data: dict | None = None
) -> tuple[int, dict]:
    hdr = {
        "Authorization": f"token {token}",
        "Accept": "application/vnd.github+json",
        "User-Agent": "agent-watchdog",
    }
    body = json.dumps(data).encode() if data is not None else None
    req = urllib.request.Request(url, data=body, headers=hdr, method=method)
    try:
        with urllib.request.urlopen(req) as r:
            s = r.getcode()
            txt = r.read().decode()
            obj = json.loads(txt) if txt else {}
            return s, obj
    except urllib.error.HTTPError as e:
        txt = e.read().decode()
        obj = json.loads(txt) if txt else {}
        return e.code, obj


def gql(query: str, vars: dict, token: str) -> dict:
    req = urllib.request.Request(
        GQL,
        data=json.dumps({"query": query, "variables": vars}).encode(),
        headers={
            "Authorization": f"bearer {token}",
            "Content-Type": "application/json",
        },
    )
    with urllib.request.urlopen(req) as r:
        obj = json.loads(r.read().decode())
    if obj.get("errors"):
        print(json.dumps(obj["errors"]))
        sys.exit(3)
    return obj.get("data", {})


REPO_Q = "query($owner:String!,$name:String!){repository(owner:$owner,name:$name){id suggestedActors(capabilities:[CAN_BE_ASSIGNED],first:100){nodes{login __typename ... on Bot {id}}}}}"
CRT_M = "mutation($rid:ID!,$title:String!,$body:String,$aids:[ID!]){createIssue(input:{repositoryId:$rid,title:$title,body:$body,assigneeIds:$aids}){issue{number url}}}"


def main(argv: list[str]) -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--owner", required=True)
    p.add_argument("--repo", required=True)
    p.add_argument("--run-id", type=int)
    p.add_argument("--pr-number", type=int)
    p.add_argument("--branch")
    p.add_argument("--issue-number", type=int)
    p.add_argument("--title", default="Recreated micro RFC")
    p.add_argument("--body")
    p.add_argument("--assign-mode", choices=["bot", "user", "auto"], default="bot")
    args = p.parse_args(argv)
    t = tok()
    owner = args.owner
    name = args.repo
    # Cleanup
    if args.run_id:
        rest("DELETE", f"{REST}/repos/{owner}/{name}/actions/runs/{args.run_id}", t)
    if args.pr_number:
        rest(
            "PATCH",
            f"{REST}/repos/{owner}/{name}/issues/{args.pr_number}",
            t,
            {"state": "closed"},
        )
    if args.branch:
        rest("DELETE", f"{REST}/repos/{owner}/{name}/git/refs/heads/{args.branch}", t)
    if args.issue_number:
        rest(
            "PATCH",
            f"{REST}/repos/{owner}/{name}/issues/{args.issue_number}",
            t,
            {"state": "closed"},
        )
    # Recreate
    data = gql(REPO_Q, {"owner": owner, "name": name}, t)
    rid = data["repository"]["id"]
    nodes = data["repository"]["suggestedActors"]["nodes"]
    prefer = {"bot": "Bot", "user": "User"}.get(args.assign_mode)
    sel = None
    if prefer:
        for n in nodes:
            if (
                n.get("__typename") == prefer
                and "copilot" in (n.get("login") or "").lower()
            ):
                sel = n
                break
    if not sel and nodes:
        sel = nodes[0]
    aids = [sel.get("id")] if sel and sel.get("id") else None
    created = gql(
        CRT_M, {"rid": rid, "title": args.title, "body": args.body, "aids": aids}, t
    )
    issue = created["createIssue"]["issue"]
    print(json.dumps({"issue_number": issue["number"], "issue_url": issue["url"]}))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
