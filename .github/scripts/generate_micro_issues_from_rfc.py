#!/usr/bin/env python3
from __future__ import annotations
import argparse, os, re, sys, json, pathlib, urllib.request

MICRO_H2 = re.compile(r"^###\s*(RFC-(\d+)-(\d+))\s*:\s*(.+)$", re.IGNORECASE)

def read_text(path:str)->str:
    return pathlib.Path(path).read_text(encoding='utf-8')

def token()->str:
    t=os.environ.get('GITHUB_TOKEN') or os.environ.get('GH_TOKEN')
    if not t:
        sys.stderr.write('Missing GITHUB_TOKEN/GH_TOKEN\n'); sys.exit(2)
    return t

def parse_micro_sections(md:str):
    lines=md.splitlines()
    items=[]; i=0; n=len(lines)
    while i<n:
        m=MICRO_H2.match(lines[i])
        if m:
            ident=m.group(1)
            rfc=m.group(2); micro=m.group(3)
            title=m.group(4).strip()
            start=i+1; j=start
            while j<n and not MICRO_H2.match(lines[j]):
                j+=1
            body="\n".join(lines[start:j]).strip()
            items.append({
                'ident': ident.upper(),
                'rfc_num': int(rfc),
                'micro_num': int(micro),
                'title': title,
                'body': body,
            })
            i=j
        else:
            i+=1
    return items

def parse_micro_table(md:str):
    items=[]
    lines=md.splitlines()
    # locate the section header
    start=None
    for idx,line in enumerate(lines):
        if line.strip().lower().startswith('## implementation plan (micro issues)'.lower()):
            start=idx+1; break
    if start is None:
        return items
    # collect table lines
    tbl=[]
    for j in range(start, len(lines)):
        s=lines[j].strip()
        if not s:
            if tbl: break
            else: continue
        if s.startswith('|'):
            tbl.append(s)
        elif tbl:
            break
    # parse rows: skip header and separator
    rows=[r for r in tbl if r.startswith('|')]
    if len(rows) < 3:
        return items
    data_rows = rows[2:]
    # Try to extract RFC number from title at top
    m = re.search(r'RFC-(\d+)', md, re.IGNORECASE)
    rfc_num = int(m.group(1)) if m else 0
    for r in data_rows:
        parts=[p.strip() for p in r.strip('|').split('|')]
        if len(parts) < 2:
            continue
        mic = parts[0]
        title = parts[1]
        acc = parts[2] if len(parts) > 2 else ''
        try:
            micro_num = int(mic)
        except ValueError:
            continue
        ident = f"RFC-{rfc_num:03d}-{micro_num:02d}" if rfc_num else f"RFC-XXX-{micro_num:02d}"
        body = f"### Objective\n{title}\n\n### Acceptance Criteria\n{acc}\n"
        items.append({'ident':ident,'rfc_num':rfc_num,'micro_num':micro_num,'title':title,'body':body})
    return items

API = 'https://api.github.com/graphql'

def gql(query:str, variables:dict, tok:str)->dict:
    req=urllib.request.Request(API,
        data=json.dumps({'query':query,'variables':variables}).encode(),
        headers={'Authorization':f'bearer {tok}','Content-Type':'application/json','User-Agent':'generate-micro-issues/1.0'})
    with urllib.request.urlopen(req) as r:
        obj=json.loads(r.read().decode())
    if obj.get('errors'):
        raise RuntimeError(json.dumps(obj['errors']))
    return obj.get('data',{})

REPO_Q = "query($owner:String!,$name:String!){repository(owner:$owner,name:$name){id suggestedActors(capabilities:[CAN_BE_ASSIGNED],first:100){nodes{login __typename ... on Bot {id}}}}}"
CRT_M = "mutation($rid:ID!,$title:String!,$body:String,$aids:[ID!]){createIssue(input:{repositoryId:$rid,title:$title,body:$body,assigneeIds:$aids}){issue{number url}}}"

def pick_assignee(nodes:list[dict], mode:str)->dict|None:
    prefer={'bot':'Bot','user':'User'}.get(mode)
    if prefer:
        for n in nodes:
            if n.get('__typename')==prefer and 'copilot' in (n.get('login') or '').lower():
                return n
    return nodes[0] if nodes else None

def main(argv:list[str])->int:
    p=argparse.ArgumentParser(description='Generate micro issues from an RFC file')
    p.add_argument('--rfc-path', required=True)
    p.add_argument('--owner', required=True)
    p.add_argument('--repo', required=True)
    p.add_argument('--assign-mode', choices=['bot','user','auto'], default='bot')
    p.add_argument('--dry-run', action='store_true')
    args=p.parse_args(argv)

    md=read_text(args.rfc_path)
    micros=parse_micro_sections(md)
    if not micros:
        micros=parse_micro_table(md)
    if not micros:
        print(json.dumps({'found':0,'items':[]}))
        return 0
    tok=token()
    data=gql(REPO_Q,{'owner':args.owner,'name':args.repo},tok)
    rid=data['repository']['id']; nodes=data['repository']['suggestedActors']['nodes']
    assignee=pick_assignee(nodes, args.assign_mode)
    aids=[assignee.get('id')] if assignee and assignee.get('id') else None

    results=[]
    for it in sorted(micros, key=lambda x:(x['rfc_num'], x['micro_num'])):
        title=f"{it['ident']}: {it['title']}"
        body=it['body']
        if args.dry_run:
            results.append({'title':title})
            continue
        d=gql(CRT_M,{'rid':rid,'title':title,'body':body,'aids':aids},tok)
        issue=d['createIssue']['issue']
        results.append({'title':title,'number':issue['number'],'url':issue['url']})
    print(json.dumps({'found':len(micros),'created':results}))
    return 0

if __name__=='__main__':
    sys.exit(main(sys.argv[1:]))
