#!/usr/bin/env python3
"""
Collection processor for generating micro-issues from multiple RFC pages.
Supports both Notion collections and file-based collections.
"""

from __future__ import annotations

import argparse
import json
import os
import pathlib
import sys
from typing import Any, Dict, List

from generate_micro_issues_from_rfc import (
    NotionClient,
    TrackingDatabase,
    generate_content_hash,
    get_notion_client,
    parse_micro_sections,
    parse_micro_table,
    parse_notion_page,
    read_text,
)

USE_DB_V2 = os.environ.get("RFC_DB_V2") == "1"
if USE_DB_V2:
    from rfc_db_v2 import PageRecord, open_db


class CollectionProcessor:
    """Process multiple RFC pages as a collection"""

    def __init__(self, db_path: str):
        if USE_DB_V2:
            ctx = open_db(db_path)
            inner = ctx.__enter__()

            class V2Adapter:
                def check_existing_issue(self, ident: str):
                    return inner.latest_issue_for_identifier(ident)

                def get_stored_page(self, page_id: str):
                    # Simple query replicating legacy method
                    return None  # minimal for now; change detection uses legacy path only when v1

                def record_page_state(self, page_id: str, page_title: str, content_hash: str, rfc_identifier: str):
                    rec = PageRecord(
                        page_id=page_id or f"virtual:{rfc_identifier}",
                        page_title=page_title,
                        last_edited_time="",
                        content_hash=content_hash,
                        rfc_identifier=rfc_identifier,
                    )
                    inner.upsert_page(rec)

                def close(self):
                    ctx.__exit__(None, None, None)

            self.db = V2Adapter()
        else:
            self.db = TrackingDatabase(db_path)

    def process_notion_collection(self, page_ids: List[str], notion_client: NotionClient) -> Dict[str, Any]:
        """Process a collection of Notion pages"""
        results = {"total_pages": len(page_ids), "processed_pages": [], "errors": []}

        for page_id in page_ids:
            try:
                # Parse the page
                micro_item = parse_notion_page(page_id, notion_client)

                # Check for duplicates
                existing_issue = self.db.check_existing_issue(micro_item["ident"])
                if existing_issue:
                    results["processed_pages"].append(
                        {
                            "page_id": page_id,
                            "ident": micro_item["ident"],
                            "title": micro_item["title"],
                            "status": "duplicate",
                            "existing_issue": existing_issue["issue_number"],
                        }
                    )
                else:
                    # Generate content hash
                    content_hash = generate_content_hash(micro_item["body"], micro_item.get("page_metadata", {}))

                    # Record in database for tracking
                    self.db.record_page_state(page_id, micro_item["title"], content_hash, micro_item["ident"])

                    results["processed_pages"].append(
                        {
                            "page_id": page_id,
                            "ident": micro_item["ident"],
                            "title": micro_item["title"],
                            "status": "ready",
                            "content_hash": content_hash,
                        }
                    )

            except Exception as e:
                results["errors"].append({"page_id": page_id, "error": str(e)})

        return results

    def process_file_collection(self, file_paths: List[str]) -> Dict[str, Any]:
        """Process a collection of RFC files"""
        results = {"total_files": len(file_paths), "processed_files": [], "errors": []}

        for file_path in file_paths:
            try:
                # Read and parse the file
                md = read_text(file_path)
                micros = parse_micro_sections(md)
                if not micros:
                    micros = parse_micro_table(md)

                if not micros:
                    results["errors"].append({"file_path": file_path, "error": "No micro-issues found in file"})
                    continue

                file_results = []
                for micro in micros:
                    # Check for duplicates
                    existing_issue = self.db.check_existing_issue(micro["ident"])
                    if existing_issue:
                        file_results.append(
                            {
                                "ident": micro["ident"],
                                "title": micro["title"],
                                "status": "duplicate",
                                "existing_issue": existing_issue["issue_number"],
                            }
                        )
                    else:
                        # Generate content hash
                        content_hash = generate_content_hash(micro["body"], {})

                        file_results.append(
                            {
                                "ident": micro["ident"],
                                "title": micro["title"],
                                "status": "ready",
                                "content_hash": content_hash,
                            }
                        )

                results["processed_files"].append({"file_path": file_path, "micro_issues": file_results})

            except Exception as e:
                results["errors"].append({"file_path": file_path, "error": str(e)})

        return results

    def detect_changes(self, page_ids: List[str], notion_client: NotionClient) -> Dict[str, Any]:
        """Detect changes in a collection of Notion pages"""
        changes = {"new_pages": [], "modified_pages": [], "unchanged_pages": [], "errors": []}

        for page_id in page_ids:
            try:
                # Get current page state
                micro_item = parse_notion_page(page_id, notion_client)
                current_hash = generate_content_hash(micro_item["body"], micro_item.get("page_metadata", {}))

                # Check against stored state
                stored_page = None if USE_DB_V2 else self.db.get_stored_page(page_id)

                if not stored_page:
                    changes["new_pages"].append(
                        {"page_id": page_id, "ident": micro_item["ident"], "title": micro_item["title"]}
                    )
                elif stored_page["content_hash"] != current_hash:
                    changes["modified_pages"].append(
                        {
                            "page_id": page_id,
                            "ident": micro_item["ident"],
                            "title": micro_item["title"],
                            "old_hash": stored_page["content_hash"],
                            "new_hash": current_hash,
                        }
                    )
                else:
                    changes["unchanged_pages"].append(page_id)

            except Exception as e:
                changes["errors"].append({"page_id": page_id, "error": str(e)})

        return changes

    def close(self):
        """Close database connection"""
        self.db.close()


def discover_notion_pages_by_pattern(notion_client: NotionClient, pattern: str) -> List[str]:
    """Discover Notion pages that match a specific pattern"""
    # This is a placeholder - in reality, you'd use Notion's search API
    # or get child pages from a database/collection
    # For now, return empty list as this requires specific Notion database setup
    return []


def discover_files_by_pattern(directory: str, pattern: str) -> List[str]:
    """Discover files that match a specific pattern"""
    path = pathlib.Path(directory)
    if pattern == "game-rfc":
        # Find files that contain Game-RFC patterns
        matching_files = []
        for file_path in path.glob("*.md"):
            try:
                content = file_path.read_text(encoding="utf-8")
                if "Game-RFC-" in content:
                    matching_files.append(str(file_path))
            except Exception:
                continue
        return matching_files
    elif pattern == "rfc":
        # Find files that contain RFC patterns
        matching_files = []
        for file_path in path.glob("*.md"):
            try:
                content = file_path.read_text(encoding="utf-8")
                if "### RFC-" in content:
                    matching_files.append(str(file_path))
            except Exception:
                continue
        return matching_files
    else:
        return list(path.glob(pattern))


def main(argv: List[str]) -> int:
    p = argparse.ArgumentParser(description="Process RFC collections for micro-issue generation")

    # Processing modes
    mode_group = p.add_mutually_exclusive_group(required=True)
    mode_group.add_argument("--notion-pages", nargs="+", help="List of Notion page IDs")
    mode_group.add_argument("--notion-collection", help="Auto-discover pages under Notion collection/section")
    mode_group.add_argument("--file-paths", nargs="+", help="List of RFC file paths")
    mode_group.add_argument("--discover-files", help="Discover files in directory (pattern: game-rfc, rfc, or glob)")
    mode_group.add_argument("--detect-changes", nargs="+", help="Detect changes in Notion pages")

    # Optional arguments
    p.add_argument("--db-path", default="./rfc_tracking.db", help="SQLite database path")
    p.add_argument("--notion-token", help="Notion API token (or use NOTION_TOKEN env var)")
    p.add_argument("--output-format", choices=["json", "summary"], default="json", help="Output format")

    args = p.parse_args(argv)

    # Initialize processor
    processor = CollectionProcessor(args.db_path)

    try:
        if args.notion_pages:
            # Process specific Notion pages
            notion_client = get_notion_client(args.notion_token)

            results = processor.process_notion_collection(args.notion_pages, notion_client)

        elif args.notion_collection:
            # Auto-discover and process pages under a collection
            notion_client = get_notion_client(args.notion_token)

            from notion_page_discovery import NotionPageDiscovery

            discovery = NotionPageDiscovery(notion_client)

            # Discover implementation pages under the collection
            implementation_pages = discovery.discover_implementation_pages(args.notion_collection)

            if not implementation_pages:
                results = {"error": f"No implementation pages found under collection {args.notion_collection}"}
            else:
                results = processor.process_notion_collection(implementation_pages, notion_client)
                results["discovered_pages"] = len(implementation_pages)

        elif args.file_paths:
            # Process specific file paths
            results = processor.process_file_collection(args.file_paths)

        elif args.discover_files:
            # Discover and process files
            directory, pattern = (
                args.discover_files.split(":", 1) if ":" in args.discover_files else (".", args.discover_files)
            )
            discovered_files = discover_files_by_pattern(directory, pattern)

            if not discovered_files:
                print(json.dumps({"error": f"No files found matching pattern '{pattern}' in '{directory}'"}))
                return 1

            results = processor.process_file_collection(discovered_files)

        elif args.detect_changes:
            # Detect changes in Notion pages
            notion_client = get_notion_client(args.notion_token)

            results = processor.detect_changes(args.detect_changes, notion_client)

        else:
            sys.stderr.write("Must specify a processing mode\n")
            return 1

        # Output results
        if args.output_format == "json":
            print(json.dumps(results, indent=2))
        else:
            # Summary format
            if "total_pages" in results:
                print(f"Processed {results['total_pages']} Notion pages:")
                print(f"  Ready: {len([p for p in results['processed_pages'] if p['status'] == 'ready'])}")
                print(f"  Duplicates: {len([p for p in results['processed_pages'] if p['status'] == 'duplicate'])}")
                print(f"  Errors: {len(results['errors'])}")
            elif "total_files" in results:
                print(f"Processed {results['total_files']} RFC files:")
                total_ready = sum(
                    len([m for m in f["micro_issues"] if m["status"] == "ready"]) for f in results["processed_files"]
                )
                total_duplicates = sum(
                    len([m for m in f["micro_issues"] if m["status"] == "duplicate"])
                    for f in results["processed_files"]
                )
                print(f"  Ready: {total_ready}")
                print(f"  Duplicates: {total_duplicates}")
                print(f"  Errors: {len(results['errors'])}")
            elif "new_pages" in results:
                print("Change detection results:")
                print(f"  New pages: {len(results['new_pages'])}")
                print(f"  Modified pages: {len(results['modified_pages'])}")
                print(f"  Unchanged pages: {len(results['unchanged_pages'])}")
                print(f"  Errors: {len(results['errors'])}")

        return 0

    finally:
        processor.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
