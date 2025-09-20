#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import os
import pathlib
import re
import sqlite3
import sys
import urllib.request
from datetime import datetime
from typing import Any, Dict, List, Optional

# Support both old RFC pattern and new Game-RFC pattern
MICRO_H2 = re.compile(r"^###\s*(RFC-(\d+)-(\d+))\s*:\s*(.+)$", re.IGNORECASE)
GAME_RFC_H3 = re.compile(r"^###\s*(Game-RFC-(\d+)-(\d+))\s*:\s*(.+)$", re.IGNORECASE)


def read_text(path: str) -> str:
    return pathlib.Path(path).read_text(encoding="utf-8")


def token() -> str:
    t = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
    if not t:
        sys.stderr.write("Missing GITHUB_TOKEN/GH_TOKEN\n")
        sys.exit(2)
    return t


def notion_token() -> str:
    t = os.environ.get("NOTION_TOKEN")
    if not t:
        sys.stderr.write("Missing NOTION_TOKEN environment variable\n")
        sys.exit(2)
    return t


class NotionClient:
    """Client for interacting with Notion API"""

    def __init__(self, token: str):
        self.token = token
        self.base_url = "https://api.notion.com/v1"
        self.headers = {
            "Authorization": f"Bearer {token}",
            "Notion-Version": "2022-06-28",
            "Content-Type": "application/json",
        }

    def get_page(self, page_id: str) -> Dict[str, Any]:
        """Fetch page metadata and properties"""
        try:
            # Debug: print the URL and headers being used
            url = f"{self.base_url}/pages/{page_id}"
            print(f"DEBUG: Requesting URL: {url}", file=sys.stderr)
            print(f"DEBUG: Headers: {self.headers}", file=sys.stderr)

            req = urllib.request.Request(url, headers=self.headers)
            with urllib.request.urlopen(req) as response:
                return json.loads(response.read().decode())
        except urllib.error.HTTPError as e:
            print(f"DEBUG: HTTP Error details: {e.code} {e.reason}", file=sys.stderr)
            if hasattr(e, "read"):
                error_body = e.read().decode()
                print(f"DEBUG: Error response body: {error_body}", file=sys.stderr)
            raise RuntimeError(f"Failed to fetch page {page_id}: {e.code} {e.reason}")

    def get_page_content(self, page_id: str) -> Dict[str, Any]:
        """Fetch page content blocks"""
        try:
            req = urllib.request.Request(f"{self.base_url}/blocks/{page_id}/children", headers=self.headers)
            with urllib.request.urlopen(req) as response:
                return json.loads(response.read().decode())
        except urllib.error.HTTPError as e:
            raise RuntimeError(f"Failed to fetch page content {page_id}: {e.code} {e.reason}")

    def extract_content_as_markdown(self, page_id: str) -> str:
        """Extract page content as markdown-like text"""
        page_content = self.get_page_content(page_id)
        blocks = page_content.get("results", [])

        content_parts = []
        for block in blocks:
            text = self._block_to_text(block)
            if text:
                content_parts.append(text)

        return "\n".join(content_parts)

    def _block_to_text(self, block: Dict[str, Any]) -> str:
        """Convert Notion block to markdown-like text"""
        block_type = block.get("type")

        if block_type == "heading_3":
            text = self._extract_rich_text(block["heading_3"]["rich_text"])
            return f"### {text}"
        elif block_type == "paragraph":
            text = self._extract_rich_text(block["paragraph"]["rich_text"])
            return text
        elif block_type == "bulleted_list_item":
            text = self._extract_rich_text(block["bulleted_list_item"]["rich_text"])
            return f"- {text}"
        elif block_type == "to_do":
            text = self._extract_rich_text(block["to_do"]["rich_text"])
            checked = "x" if block["to_do"]["checked"] else " "
            return f"- [{checked}] {text}"

        return ""

    def _extract_rich_text(self, rich_text_array: List[Dict]) -> str:
        """Extract plain text from Notion rich text array"""
        return "".join(item.get("plain_text", "") for item in rich_text_array)


# Legacy TrackingDatabase (v1) retained for backward compatibility.
USE_DB_V2 = os.environ.get("RFC_DB_V2") == "1"
USE_RELIABLE_NOTION = os.environ.get("NOTION_RELIABLE") == "1"

if USE_RELIABLE_NOTION:
    try:
        from notion_reliability import NotionReliableClient  # type: ignore
    except ImportError:
        NotionReliableClient = None  # type: ignore


class TrackingDatabase:
    """SQLite database for tracking RFC pages and GitHub issues"""

    def __init__(self, db_path: str):
        self.db_path = db_path
        self.conn = sqlite3.connect(db_path)
        self._init_db()

    def _init_db(self):
        """Initialize database schema"""
        self.conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS notion_pages (
                page_id TEXT PRIMARY KEY,
                page_title TEXT NOT NULL,
                last_edited_time TEXT NOT NULL,
                content_hash TEXT NOT NULL,
                rfc_identifier TEXT NOT NULL,
                status TEXT DEFAULT 'active',
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS github_issues (
                issue_number INTEGER PRIMARY KEY,
                issue_title TEXT NOT NULL,
                issue_state TEXT NOT NULL,
                notion_page_id TEXT NOT NULL,
                content_hash TEXT NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (notion_page_id) REFERENCES notion_pages(page_id)
            );

            CREATE TABLE IF NOT EXISTS processing_log (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                notion_page_id TEXT NOT NULL,
                action TEXT NOT NULL,
                details TEXT,
                timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
        """
        )
        self.conn.commit()

    def get_stored_page(self, page_id: str) -> Optional[Dict[str, Any]]:
        """Get stored page state from database"""
        cursor = self.conn.execute("SELECT * FROM notion_pages WHERE page_id = ?", (page_id,))
        row = cursor.fetchone()
        if row:
            columns = [description[0] for description in cursor.description]
            return dict(zip(columns, row))
        return None

    def record_page_state(self, page_id: str, page_title: str, content_hash: str, rfc_identifier: str):
        """Record or update page state in database"""
        self.conn.execute(
            """
            INSERT OR REPLACE INTO notion_pages
            (page_id, page_title, last_edited_time, content_hash, rfc_identifier, updated_at)
            VALUES (?, ?, ?, ?, ?, ?)
        """,
            (page_id, page_title, datetime.now().isoformat(), content_hash, rfc_identifier, datetime.now()),
        )
        self.conn.commit()

    def record_issue_creation(self, issue_number: int, issue_title: str, page_id: str, content_hash: str):
        """Record GitHub issue creation"""
        self.conn.execute(
            """
            INSERT OR REPLACE INTO github_issues
            (issue_number, issue_title, issue_state, notion_page_id, content_hash)
            VALUES (?, ?, 'open', ?, ?)
        """,
            (issue_number, issue_title, page_id, content_hash),
        )
        self.conn.commit()

    def check_existing_issue(self, rfc_identifier: str) -> Optional[Dict[str, Any]]:
        """Check if issue already exists for RFC identifier (including closed issues)"""
        cursor = self.conn.execute(
            """
            SELECT gi.* FROM github_issues gi
            JOIN notion_pages np ON gi.notion_page_id = np.page_id
            WHERE UPPER(np.rfc_identifier) = UPPER(?)
            ORDER BY gi.created_at DESC LIMIT 1
        """,
            (rfc_identifier,),
        )
        row = cursor.fetchone()
        if row:
            columns = [description[0] for description in cursor.description]
            return dict(zip(columns, row))
        return None

    def close(self):
        """Close database connection"""
        self.conn.close()


def generate_content_hash(content: str, metadata: Dict[str, Any]) -> str:
    """Generate deterministic hash for content"""
    # Normalize content (remove extra whitespace, normalize formatting)
    normalized_content = re.sub(r"\s+", " ", content.strip())

    # Include relevant metadata
    hash_input = {
        "content": normalized_content,
        "title": metadata.get("title", ""),
        "last_edited_time": metadata.get("last_edited_time", ""),
    }

    # Create SHA-256 hash
    content_str = json.dumps(hash_input, sort_keys=True)
    return hashlib.sha256(content_str.encode()).hexdigest()


def parse_micro_sections(md: str):
    """Parse micro-issue sections from markdown content"""
    lines = md.splitlines()
    items = []
    i = 0
    n = len(lines)
    while i < n:
        # Try both old RFC pattern and new Game-RFC pattern
        m = MICRO_H2.match(lines[i]) or GAME_RFC_H3.match(lines[i])
        if m:
            ident = m.group(1)
            rfc = m.group(2)
            micro = m.group(3)
            title = m.group(4).strip()
            start = i + 1
            j = start
            # Find next header or end of content
            while j < n and not (MICRO_H2.match(lines[j]) or GAME_RFC_H3.match(lines[j])):
                j += 1
            body = "\n".join(lines[start:j]).strip()
            items.append(
                {
                    "ident": ident.upper(),
                    "rfc_num": int(rfc),
                    "micro_num": int(micro),
                    "title": title,
                    "body": body,
                }
            )
            i = j
        else:
            i += 1
    return items


def parse_notion_page(page_id: str, notion_client: NotionClient) -> Dict[str, Any]:
    """Parse Notion page and extract micro-issue information"""
    """Parse a single Notion page into a micro-issue"""
    page_metadata = notion_client.get_page(page_id)
    content = notion_client.extract_content_as_markdown(page_id)

    # Extract title from page properties
    title_prop = page_metadata.get("properties", {}).get("title", {})
    if title_prop.get("type") == "title":
        title_text = "".join(item.get("plain_text", "") for item in title_prop.get("title", []))
    else:
        title_text = f"Page {page_id}"

    # Extract RFC identifier from title (e.g., "Game-RFC-001-01: Create Interfaces")
    title_match = re.match(r"^(Game-RFC-(\d+)-(\d+))\s*:\s*(.+)$", title_text, re.IGNORECASE)
    if not title_match:
        raise ValueError(f"Page title doesn't match Game-RFC pattern: {title_text}")

    ident = title_match.group(1)
    rfc_num = int(title_match.group(2))
    micro_num = int(title_match.group(3))
    task_title = title_match.group(4).strip()

    return {
        "page_id": page_id,
        "ident": ident.upper(),
        "rfc_num": rfc_num,
        "micro_num": micro_num,
        "title": task_title,
        "body": content,
        "page_metadata": page_metadata,
    }


def parse_micro_table(md: str):
    items = []
    lines = md.splitlines()
    # locate the section header
    start = None
    for idx, line in enumerate(lines):
        if line.strip().lower().startswith("## implementation plan (micro issues)".lower()):
            start = idx + 1
            break
    if start is None:
        return items
    # collect table lines
    tbl = []
    for j in range(start, len(lines)):
        s = lines[j].strip()
        if not s:
            if tbl:
                break
            else:
                continue
        if s.startswith("|"):
            tbl.append(s)
        elif tbl:
            break
    # parse rows: skip header and separator
    rows = [r for r in tbl if r.startswith("|")]
    if len(rows) < 3:
        return items
    data_rows = rows[2:]
    # Try to extract RFC number from title at top
    m = re.search(r"RFC-(\d+)", md, re.IGNORECASE)
    rfc_num = int(m.group(1)) if m else 0
    for r in data_rows:
        parts = [p.strip() for p in r.strip("|").split("|")]
        if len(parts) < 2:
            continue
        mic = parts[0]
        title = parts[1]
        acc = parts[2] if len(parts) > 2 else ""
        try:
            micro_num = int(mic)
        except ValueError:
            continue
        ident = f"RFC-{rfc_num:03d}-{micro_num:02d}" if rfc_num else f"RFC-XXX-{micro_num:02d}"

        # Format acceptance criteria into proper checklist
        def to_checklist(text: str) -> str:
            t = (text or "").replace("\n", " ").strip()
            items: list[str] = []
            if "- [ ]" in t:
                parts2 = [p.strip() for p in t.split("- [ ]")]
                for p in parts2:
                    if p:
                        items.append(f"- [ ] {p}")
            else:
                import re as _re

                parts2 = [_re.split(r";\s*", t)][0]
                for p in parts2:
                    p = p.strip()
                    if p:
                        items.append(f"- [ ] {p}")
            return "\n".join(items) if items else "- [ ]"

        checklist = to_checklist(acc)
        body = f"### Objective\n{title}\n\n### Acceptance Criteria\n{checklist}\n"
        items.append(
            {
                "ident": ident,
                "rfc_num": rfc_num,
                "micro_num": micro_num,
                "title": title,
                "body": body,
            }
        )
    return items


API = "https://api.github.com/graphql"


def gql(query: str, variables: dict, tok: str) -> dict:
    req = urllib.request.Request(
        API,
        data=json.dumps({"query": query, "variables": variables}).encode(),
        headers={
            "Authorization": f"bearer {tok}",
            "Content-Type": "application/json",
            "User-Agent": "generate-micro-issues/1.0",
        },
    )
    with urllib.request.urlopen(req) as r:
        obj = json.loads(r.read().decode())
    if obj.get("errors"):
        raise RuntimeError(json.dumps(obj["errors"]))
    return obj.get("data", {})


REPO_Q = (
    "query($owner:String!,$name:String!){ repository(owner:$owner,name:$name){ id "
    "suggestedActors(capabilities:[CAN_BE_ASSIGNED],first:100){ nodes{ login __typename "
    "... on Bot { id } ... on User { id } } } } }"
)
CRT_M = (
    "mutation($rid:ID!,$title:String!,$body:String,$aids:[ID!]){createIssue("
    "input:{repositoryId:$rid,title:$title,body:$body,assigneeIds:$aids}){issue{id number url}}}"
)
REPLACE_M = (
    "mutation($assignableId:ID!,$actorIds:[ID!]!){replaceActorsForAssignable("
    "input:{assignableId:$assignableId,actorIds:$actorIds}){clientMutationId}}"
)


def pick_assignee(nodes: list[dict], mode: str) -> dict | None:
    prefer = {"bot": "Bot", "user": "User"}.get(mode)
    if prefer:
        for n in nodes:
            if n.get("__typename") == prefer and "copilot" in (n.get("login") or "").lower():
                return n
    # For this generator, do not fallback to non-bot; leave unassigned if no Copilot bot
    return None


def get_notion_client(api_token: Optional[str] = None):
    token_value = api_token or notion_token()
    if USE_RELIABLE_NOTION and NotionReliableClient:
        return NotionReliableClient(token_value)
    return NotionClient(token_value)


def main(argv: list[str]) -> int:
    p = argparse.ArgumentParser(description="Generate micro issues from RFC file or Notion page")

    # Input sources (mutually exclusive)
    input_group = p.add_mutually_exclusive_group(required=True)
    input_group.add_argument("--rfc-path", help="Local RFC file path")
    input_group.add_argument("--notion-page-id", help="Notion page ID")

    # Required arguments
    p.add_argument("--owner", required=True)
    p.add_argument("--repo", required=True)

    # Optional arguments
    p.add_argument("--assign-mode", choices=["bot", "user", "auto"], default="bot")
    p.add_argument("--dry-run", action="store_true")
    p.add_argument("--force", action="store_true", help="Skip duplicate detection")
    p.add_argument("--db-path", default="./rfc_tracking.db", help="SQLite database path")
    p.add_argument("--notion-token", help="Notion API token (or use NOTION_TOKEN env var)")

    args = p.parse_args(argv)

    # Initialize tracking database (v1 or v2 wrapper)
    if USE_DB_V2:
        from rfc_db_v2 import PageRecord, open_db

        db_v2_context = open_db(args.db_path)
        db_v2 = db_v2_context.__enter__()

        class V2Adapter:
            def check_existing_issue(self, ident: str):
                return db_v2.latest_issue_for_identifier(ident)

            def record_page_state(self, page_id: str, page_title: str, content_hash: str, rfc_identifier: str):
                rec = PageRecord(
                    page_id=page_id or f"virtual:{rfc_identifier}",
                    page_title=page_title,
                    last_edited_time=datetime.utcnow().isoformat(),
                    content_hash=content_hash,
                    rfc_identifier=rfc_identifier,
                )
                db_v2.upsert_page(rec)

            def record_issue_creation(self, issue_number: int, issue_title: str, page_id: str, content_hash: str):
                db_v2.record_issue(issue_number, issue_title, page_id or f"virtual:{issue_title}", content_hash)

            def close(self):
                db_v2_context.__exit__(None, None, None)

        db = V2Adapter()
    else:
        db = TrackingDatabase(args.db_path)

    try:
        # Parse input content
        if args.rfc_path:
            # File-based processing (existing behavior)
            md = read_text(args.rfc_path)
            micros = parse_micro_sections(md)
            if not micros:
                micros = parse_micro_table(md)
            if not micros:
                print(json.dumps({"found": 0, "items": []}))
                return 0

            # Convert to consistent format
            micro_items = []
            for micro in micros:
                micro_items.append(
                    {
                        "page_id": None,
                        "ident": micro["ident"],
                        "rfc_num": micro["rfc_num"],
                        "micro_num": micro["micro_num"],
                        "title": micro["title"],
                        "body": micro["body"],
                        "page_metadata": {},
                    }
                )

        elif args.notion_page_id:
            # Notion-based processing
            notion_client = get_notion_client(args.notion_token)

            try:
                micro_item = parse_notion_page(args.notion_page_id, notion_client)
                micro_items = [micro_item]
            except Exception as e:
                sys.stderr.write(f"Error parsing Notion page: {e}\n")
                return 1
        else:
            sys.stderr.write("Must specify either --rfc-path or --notion-page-id\n")
            return 1

        if not micro_items:
            print(json.dumps({"found": 0, "items": []}))
            return 0

        # Initialize GitHub API (skip in dry-run mode)
        if not args.dry_run:
            github_token = token()
            data = gql(REPO_Q, {"owner": args.owner, "name": args.repo}, github_token)
            rid = data["repository"]["id"]
            nodes = data["repository"]["suggestedActors"]["nodes"]
            assignee = pick_assignee(nodes, args.assign_mode)
            aids = [assignee.get("id")] if assignee and assignee.get("id") else None
        else:
            # Dummy values for dry-run
            github_token = "dummy"
            rid = "dummy"
            assignee = None
            aids = None

        # Helper: skip creating duplicate issues by exact title (open issues only)
        SEARCH_Q = """
        query($q:String!) {
          search(query:$q, type: ISSUE, first: 1) {
            issueCount
            edges { node { ... on Issue { number title state } } }
          }
        }
        """

        def exists_open_issue_with_title(owner: str, name: str, title: str) -> bool:
            # Build a GitHub search query: repo:owner/name is:issue is:open in:title "title"
            q = f'repo:{owner}/{name} is:issue is:open in:title "{title}"'
            try:
                d = gql(SEARCH_Q, {"q": q}, github_token)
                cnt = ((d.get("search") or {}).get("issueCount")) or 0
                return cnt > 0
            except Exception:
                return False

        results = []
        micros_sorted = sorted(micro_items, key=lambda x: (x["rfc_num"], x["micro_num"]))
        first_ident = micros_sorted[0]["ident"] if micros_sorted else None

        for it in micros_sorted:
            title = f"{it['ident']}: {it['title']}"
            body = it["body"]

            # Generate content hash for tracking
            content_hash = generate_content_hash(body, it.get("page_metadata", {}))

            # Check for duplicates using database and GitHub search
            if not args.force:
                # Check database first
                existing_issue = db.check_existing_issue(it["ident"])
                if existing_issue:
                    results.append(
                        {"title": title, "skipped": "tracked_in_db", "issue_number": existing_issue["issue_number"]}
                    )
                    continue

                # Check GitHub search
                if exists_open_issue_with_title(args.owner, args.repo, title):
                    results.append({"title": title, "skipped": "found_on_github"})
                    continue

            if args.dry_run:
                results.append({"title": title, "action": "would_create"})
                continue

            # Create GitHub issue
            # Assign only the first micro to enforce sequential execution; others remain unassigned
            this_aids = aids if assignee and it["ident"] == first_ident else None
            d = gql(CRT_M, {"rid": rid, "title": title, "body": body, "aids": this_aids}, github_token)
            issue = d["createIssue"]["issue"]

            # If assigned, ensure GraphQL assignment sticks
            if assignee and assignee.get("id") and it["ident"] == first_ident:
                try:
                    gql(
                        REPLACE_M,
                        {"assignableId": issue["id"], "actorIds": [assignee["id"]]},
                        github_token,
                    )
                except Exception:
                    pass

            # Record in tracking database
            if it.get("page_id"):
                db.record_page_state(it["page_id"], title, content_hash, it["ident"])
            else:
                # For file based items still persist a virtual page handle in v2
                if USE_DB_V2:
                    db.record_page_state(None, title, content_hash, it["ident"])
            db.record_issue_creation(issue["number"], title, it.get("page_id", ""), content_hash)

            results.append({"title": title, "number": issue["number"], "url": issue["url"], "action": "created"})

        print(json.dumps({"found": len(micro_items), "processed": results}))
        return 0

    finally:
        db.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
