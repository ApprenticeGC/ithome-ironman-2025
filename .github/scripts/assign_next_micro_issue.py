#!/usr/bin/env python3
# Simplified port to auto-select next micro issue and assign Copilot
from __future__ import annotations
import json, os, re, subprocess, sys

RFC_RX = re.compile(r"(?:Game-)?RFC-(\d+)-(\d+)")

def run_gh_json(args:list[str]):
    try:
        # Force UTF-8 to avoid Windows codepage decode issues
        env = {**os.environ, 'LC_ALL': 'C.UTF-8', 'LANG': 'C.UTF-8'}
        res = subprocess.run(['gh']+args, capture_output=True, text=True, check=True, env=env)
        return json.loads(res.stdout) if res.stdout.strip() else None
    except Exception as e:
        print(f"gh failed: {e}"); return None

def parse_issue_title(t:str):
    m = RFC_RX.search(t or "");
    return (int(m.group(1)), int(m.group(2))) if m else (None, None)

def select_next(rfc:int, next_micro:int, issues:list[dict]):
    variants = [
        f"Game-RFC-{rfc:03d}-{next_micro:02d}", f"Game-RFC-{rfc}-{next_micro}",
        f"RFC-{rfc:03d}-{next_micro:02d}", f"RFC-{rfc}-{next_micro}"
    ]
    cands=[]
    for it in issues:
        if it.get('state') and it.get('state')!='open':
            continue
        title = it.get('title') or ''
        for rank,v in enumerate(variants):
            idx = title.find(v)
            if idx>=0: cands.append((rank, idx, int(it['number']), it)); break
    if not cands: return None
    cands.sort(key=lambda x:(x[0],x[1],x[2]))
    return cands[0][3]

def assign_issue_to_copilot(repo:str, issue:int)->bool:
    # Use GraphQL suggestedActors to pick the Copilot Bot id and replace actors
    owner, name = repo.split('/')
    q = '''query($owner:String!,$name:String!){repository(owner:$owner,name:$name){suggestedActors(capabilities:[CAN_BE_ASSIGNED],first:100){nodes{login __typename ... on Bot {id}}}}}'''
    try:
        env = {**os.environ, 'LC_ALL': 'C.UTF-8', 'LANG': 'C.UTF-8'}
        out = subprocess.run(['gh','api','graphql','-f',f'query={q}','-F',f'owner={owner}','-F',f'name={name}'], capture_output=True, text=True, check=True, env=env)
        data=json.loads(out.stdout)
        nodes=data['data']['repository']['suggestedActors']['nodes']
        bot=None
        for n in nodes:
            if n.get('__typename')=='Bot' and 'copilot' in (n.get('login') or '').lower() and n.get('id'):
                bot=n; break
        if not bot:
            # Fallback: resolve by login as user; GraphQL returns a node id usable in replaceActorsForAssignable
            out = subprocess.run(['gh','api','graphql','-f','query=query($login:String!){ user(login:$login){ id __typename } }','-F','login=copilot-swe-agent'], capture_output=True, text=True, check=False, env=env)
            if out.returncode == 0 and out.stdout.strip():
                try:
                    uid = json.loads(out.stdout)['data']['user']['id']
                    if uid:
                        bot = {'id': uid}
                except Exception:
                    pass
        if not bot:
            print('No Copilot id available to assign')
            return False
        issue_id = run_gh_json(['issue','view',str(issue),'--repo',repo,'--json','id']).get('id')
        mut=f'''mutation{{ replaceActorsForAssignable(input:{{ assignableId:"{issue_id}", actorIds:["{bot['id']}"] }}){{ clientMutationId }} }}'''
        _=subprocess.run(['gh','api','graphql','-f',f'query={mut}'], capture_output=True, text=True, check=True, env=env)
        print(f"Assigned issue #{issue} to Copilot (bot id {bot['id']})")
        return True
    except Exception as e:
        print(f"Assignment failed: {e}")
        return False

def main():
    repo=os.environ.get('REPO'); pr=os.environ.get('PR_NUMBER'); assign='--assign' in sys.argv or os.environ.get('ASSIGN')=='1'
    if not repo or not pr: print('Missing REPO/PR_NUMBER'); sys.exit(1)
    prj=run_gh_json(['pr','view',str(pr),'--repo',repo,'--json','body,title']) or {}
    closed=None
    # find linked issue number
    for text in [prj.get('body',''), prj.get('title','')]:
        m=re.search(r'(?:close[sd]?|fixe?[sd]?|resolve[sd]?)\s+#(\d+)', text, re.I)
        if m: closed=int(m.group(1)); break
    rfc=None; mic=None
    if not closed:
        # Fallback: infer RFC/micro from PR title (e.g., contains RFC-090-01)
        rfc,mic = parse_issue_title(prj.get('title',''))
    else:
        issue=run_gh_json(['issue','view',str(closed),'--repo',repo,'--json','title']) or {}
        rfc,mic=parse_issue_title(issue.get('title',''))
    if not rfc or not mic: print('Not a micro RFC title'); sys.exit(0)
    nexti=mic+1
    open_issues=run_gh_json(['issue','list','--repo',repo,'--state','open','--limit','200','--json','number,title,state']) or []
    sel=select_next(rfc,nexti,open_issues)
    if not sel: print('No next micro issue'); sys.exit(0)
    print(json.dumps({'selected': sel}))
    if assign:
        ok=assign_issue_to_copilot(repo, int(sel['number']))
        if not ok:
            print('Auto-advance: assignment not completed')
            sys.exit(1)
        else:
            print('Auto-advance: assignment succeeded')

if __name__=='__main__':
    main()
