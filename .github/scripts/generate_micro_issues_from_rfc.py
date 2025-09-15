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

