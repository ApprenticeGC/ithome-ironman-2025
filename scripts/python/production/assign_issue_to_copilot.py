#!/usr/bin/env python3
# Assign existing issue to Copilot using GraphQL suggestedActors

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


def assign_issue_to_copilot(
    owner: str, repo: str, issue_number: int, assign_mode: str = "bot"
) -> dict:
    tok = token()

    # Get issue ID
    issue_q = "query($owner:String!,$name:String!,$number:Int!){repository(owner:$owner,name:$name){issue(number:$number){id}}}"
    issue_data = gql(
        issue_q, {"owner": owner, "name": repo, "number": issue_number}, tok
    )
    issue_id = issue_data["repository"]["issue"]["id"]

    # Get suggested actors
    sug_q = "query($owner:String!,$name:String!){repository(owner:$owner,name:$name){suggestedActors(capabilities:[CAN_BE_ASSIGNED],first:100){nodes{login __typename ... on Bot {id}}}}}"
    nodes = gql(sug_q, {"owner": owner, "name": repo}, tok)["repository"][
        "suggestedActors"
    ]["nodes"]

    # Find Copilot actor
    sel = None
    pref = {"bot": "Bot", "user": "User"}.get(assign_mode)
    if pref:
        for n in nodes:
            if (
                n.get("__typename") == pref
                and "copilot" in (n.get("login") or "").lower()
            ):
                sel = n
                break

    # Fallback options
    if not sel:
        # Try user login fallback
        for login in ["Copilot", "copilot-swe-agent"]:
            try:
                user_q = "query($login:String!){ user(login:$login){ id __typename } }"
                user_data = gql(user_q, {"login": login}, tok)
                if user_data.get("user"):
                    sel = user_data["user"]
                    break
            except:
                continue

    if not sel:
        sys.stderr.write("No Copilot actor found\n")
        return {"success": False, "error": "No Copilot actor found"}

    # Assign using appropriate mutation
    if sel.get("__typename") == "Bot":
        mut = "mutation($assignableId: ID!, $actorIds: [ID!]!){ replaceActorsForAssignable(input:{ assignableId:$assignableId, actorIds:$actorIds }){ clientMutationId } }"
        gql(mut, {"assignableId": issue_id, "actorIds": [sel["id"]]}, tok)
    else:
        mut = "mutation($assignableId: ID!, $assigneeIds: [ID!]!){ addAssigneesToAssignable(input:{ assignableId:$assignableId, assigneeIds:$assigneeIds }){ clientMutationId } }"
        gql(mut, {"assignableId": issue_id, "assigneeIds": [sel["id"]]}, tok)

    return {
        "success": True,
        "issue_number": issue_number,
        "assignee_login": sel.get("login"),
        "assignee_type": sel.get("__typename"),
    }


def main(argv: list[str]) -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--owner", required=True)
    p.add_argument("--repo", required=True)
    p.add_argument("--issue-number", type=int, required=True)
    p.add_argument("--assign-mode", choices=["bot", "user", "auto"], default="bot")
    args = p.parse_args(argv)

    result = assign_issue_to_copilot(
        args.owner, args.repo, args.issue_number, args.assign_mode
    )
    print(json.dumps(result))
    return 0 if result.get("success") else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
