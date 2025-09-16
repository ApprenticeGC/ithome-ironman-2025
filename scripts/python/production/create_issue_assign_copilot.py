#!/usr/bin/env python3
# Simplified port of dungeon-coding-agent-02 create_issue_assign_copilot.py
# Creates an issue and assigns Copilot using GraphQL suggestedActors

from __future__ import annotations

import argparse
import json
import os
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
            "User-Agent": "assign-copilot/1.0",
        },
    )
    with urllib.request.urlopen(req) as r:
        obj = json.loads(r.read().decode())
    if obj.get("errors"):
        sys.stderr.write(json.dumps(obj["errors"]) + "\n")
        sys.exit(3)
    return obj.get("data", {})


REPO_Q = "query($owner:String!,$name:String!){repository(owner:$owner,name:$name){id}}"
SUG_Q = "query($owner:String!,$name:String!){repository(owner:$owner,name:$name){suggestedActors(capabilities:[CAN_BE_ASSIGNED],first:100){nodes{login __typename ... on Bot {id}}}}}"
CRT_M = "mutation($rid:ID!,$title:String!,$body:String,$aids:[ID!]){createIssue(input:{repositoryId:$rid,title:$title,body:$body,assigneeIds:$aids}){issue{number url}}}"


def main(argv: list[str]) -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--owner", required=True)
    p.add_argument("--repo", required=True)
    p.add_argument("--title", required=True)
    p.add_argument("--body")
    p.add_argument("--assign-mode", choices=["bot", "user", "auto"], default="bot")
    args = p.parse_args(argv)
    tok = token()
    rid = gql(REPO_Q, {"owner": args.owner, "name": args.repo}, tok)["repository"]["id"]
    nodes = gql(SUG_Q, {"owner": args.owner, "name": args.repo}, tok)["repository"][
        "suggestedActors"
    ]["nodes"]
    sel = None
    pref = {"bot": "Bot", "user": "User"}.get(args.assign_mode)
    if pref:
        for n in nodes:
            if (
                n.get("__typename") == pref
                and "copilot" in (n.get("login") or "").lower()
            ):
                sel = n
                break
    if not sel and nodes:
        sel = nodes[0]
    if not sel:
        sys.stderr.write("No suggestedActors found\n")
        return 4
    aids = [sel.get("id")] if sel.get("id") else None
    data = gql(
        CRT_M, {"rid": rid, "title": args.title, "body": args.body, "aids": aids}, tok
    )
    issue = data["createIssue"]["issue"]
    out = {
        "issue_number": issue["number"],
        "issue_url": issue["url"],
        "assignee_login": sel.get("login"),
        "assignee_type": sel.get("__typename"),
    }
    print(json.dumps(out))
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ.get("GITHUB_OUTPUT"), "a", encoding="utf-8") as f:
            f.write(f"issue_number={issue['number']}\nissue_url={issue['url']}\n")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
