#!/usr/bin/env python3
from __future__ import annotations
import json, os, re, subprocess, sys

RFC_RX = re.compile(r"RFC-(\d{3})-(\d{2})", re.IGNORECASE)

def run_gh_json(args:list[str]):
    res = subprocess.run(['gh']+args, capture_output=True, text=True)
    if res.returncode != 0:
        return None
    return json.loads(res.stdout) if res.stdout.strip() else None

def pick_bot_id(owner:str, name:str) -> str|None:
    q = '''query($owner:String!,$name:String!){ repository(owner:$owner,name:$name){ suggestedActors(capabilities:[CAN_BE_ASSIGNED],first:100){ nodes{ login __typename ... on Bot { id } } } } }'''
    res = subprocess.run(['gh','api','graphql','-f',f'query={q}','-F',f'owner={owner}','-F',f'name={name}'], capture_output=True, text=True)
    if res.returncode != 0:
        return None
    data = json.loads(res.stdout)
    nodes = (((data.get('data') or {}).get('repository') or {}).get('suggestedActors') or {}).get('nodes') or []
    for n in nodes:
        if n.get('__typename') == 'Bot' and 'copilot' in (n.get('login') or '').lower():
            return n.get('id')
    # Fallback 1: try resolving by login 'Copilot'
    res2 = subprocess.run(['gh','api','graphql','-f','query=query($login:String!){ user(login:$login){ id __typename } }','-F','login=Copilot'], capture_output=True, text=True)
    if res2.returncode == 0 and res2.stdout.strip():
        try:
            uid = json.loads(res2.stdout)['data']['user']['id']
            if uid:
                return uid
        except Exception:
            pass
    # Fallback 2: try resolving by login 'copilot-swe-agent'
    res3 = subprocess.run(['gh','api','graphql','-f','query=query($login:String!){ user(login:$login){ id __typename } }','-F','login=copilot-swe-agent'], capture_output=True, text=True)
    if res3.returncode == 0 and res3.stdout.strip():
        try:
            uid = json.loads(res3.stdout)['data']['user']['id']
            if uid:
                return uid
        except Exception:
            pass
    return None

def main(argv:list[str]) -> int:
    if len(argv) < 1:
        print('usage: assign_first_open_for_rfc.py <rfc_num_3digits>', file=sys.stderr)
        return 2
    rfc = argv[0]
    repo = os.environ.get('REPO') or os.environ.get('GITHUB_REPOSITORY')
    if not repo:
        print('missing REPO', file=sys.stderr)
        return 2
    owner, name = repo.split('/')
    bot_id = pick_bot_id(owner, name)

    # Guard: if a PR already exists for this RFC, do not assign a new micro
    prs = run_gh_json(['pr','list','--repo',repo,'--state','open','--json','number,title']) or []
    for pr in prs:
        title = pr.get('title') or ''
        if (re.search(fr"RFC-{re.escape(rfc)}-\d{{2}}", title, re.IGNORECASE)):
            print(f'skip: open PR #{pr.get("number")} exists for RFC-{rfc}')
            return 0

    issues = run_gh_json(['issue','list','--repo',repo,'--state','open','--limit','200','--json','number,title,assignees']) or []
    candidates = []
    for it in issues:
        m = RFC_RX.search(it.get('title') or '')
        if not m:
            continue
        r = m.group(1)
        if r != rfc:
            continue
        mic = int(m.group(2))
        assignees = it.get('assignees') or []
        if len(assignees) == 0:
            candidates.append((mic, int(it['number'])))
    if not candidates:
        print(f'no unassigned for RFC-{rfc}')
        return 0
    candidates.sort()  # by micro, then issue number
    sel_num = candidates[0][1]
    # assign
    issue_node = run_gh_json(['issue','view',str(sel_num),'--repo',repo,'--json','id']) or {}
    iid = issue_node.get('id')
    if not iid:
        return 0
    if bot_id:
        mut = 'mutation($assignableId: ID!, $assigneeIds: [ID!]!){ addAssigneesToAssignable(input:{ assignableId: $assignableId, assigneeIds: $assigneeIds }){ clientMutationId } }'
        subprocess.run(['gh','api','graphql','-f',f'query={mut}','-F',f'assignableId={iid}','-F',f'assigneeIds={bot_id}'], check=False)
    else:
        print('no copilot bot id', file=sys.stderr)
    print(f'assigned #{sel_num} for RFC-{rfc}')
    return 0

if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
