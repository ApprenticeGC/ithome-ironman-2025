# Flow-RFC-004: Deduplication & Change Detection System

- **Start Date**: 2025-09-18
- **RFC Author**: Claude
- **Status**: Draft
- **Type**: Flow/Automation Enhancement
- **Depends On**: Flow-RFC-001, Flow-RFC-002

## Summary

This RFC defines a comprehensive deduplication and change detection system to prevent duplicate GitHub issues when new implementation RFCs are created or existing ones are modified. The system tracks Notion pages, content changes, and GitHub issue relationships to ensure idempotent automation.

## Motivation

Critical challenges for production automation:

1. **Duplicate Issue Creation**: Adding new RFCs could recreate existing issues
2. **Content Change Detection**: No way to detect when Notion pages are modified
3. **State Synchronization**: No tracking between Notion pages and GitHub issues
4. **Incremental Updates**: Can't process only changed content
5. **Rollback Safety**: No way to handle deleted or renamed RFCs

## Detailed Design

### Issue Tracking Database

#### Core Schema
```python
# SQLite database: rfc_issue_tracking.db

CREATE TABLE notion_pages (
    page_id TEXT PRIMARY KEY,
    page_title TEXT NOT NULL,
    last_edited_time TEXT NOT NULL,
    content_hash TEXT NOT NULL,
    rfc_identifier TEXT NOT NULL,  -- e.g., "RFC-001-01"
    status TEXT DEFAULT 'active',  -- active, deleted, renamed
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE github_issues (
    issue_number INTEGER PRIMARY KEY,
    issue_title TEXT NOT NULL,
    issue_state TEXT NOT NULL,  -- open, closed
    notion_page_id TEXT NOT NULL,
    content_hash TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (notion_page_id) REFERENCES notion_pages(page_id)
);

CREATE TABLE processing_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    notion_page_id TEXT NOT NULL,
    action TEXT NOT NULL,  -- created, updated, skipped, error
    details TEXT,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### Content Hash Generation
```python
import hashlib
import json

def generate_content_hash(page_content: str, page_metadata: dict) -> str:
    """Generate deterministic hash for Notion page content"""
    # Normalize content (remove formatting variations)
    normalized_content = normalize_notion_content(page_content)

    # Include relevant metadata
    hash_input = {
        "content": normalized_content,
        "title": page_metadata.get("title", ""),
        "last_edited_time": page_metadata.get("last_edited_time", "")
    }

    # Create SHA-256 hash
    content_str = json.dumps(hash_input, sort_keys=True)
    return hashlib.sha256(content_str.encode()).hexdigest()

def normalize_notion_content(content: str) -> str:
    """Normalize content to ignore formatting changes"""
    import re

    # Remove extra whitespace
    content = re.sub(r'\s+', ' ', content.strip())

    # Normalize list formatting
    content = re.sub(r'[-*+]\s+', '- ', content)

    # Normalize checkbox formatting
    content = re.sub(r'-\s*\[\s*\]\s*', '- [ ] ', content)

    return content
```

### Change Detection Logic

#### Notion Page Monitoring
```python
class NotionChangeDetector:
    def __init__(self, db_path: str, notion_client: NotionClient):
        self.db = sqlite3.connect(db_path)
        self.notion = notion_client
        self._init_db()

    def detect_changes(self, page_ids: List[str]) -> ChangeReport:
        """Detect changes in a list of Notion pages"""
        changes = ChangeReport()

        for page_id in page_ids:
            try:
                # Fetch current page state
                current_page = self.notion.get_page(page_id)
                current_content = self.notion.get_page_content(page_id)
                current_hash = generate_content_hash(current_content, current_page)

                # Check against stored state
                stored_page = self._get_stored_page(page_id)

                if not stored_page:
                    changes.new_pages.append(NewPage(page_id, current_hash))
                elif stored_page['content_hash'] != current_hash:
                    changes.modified_pages.append(ModifiedPage(
                        page_id,
                        stored_page['content_hash'],
                        current_hash
                    ))
                else:
                    changes.unchanged_pages.append(page_id)

            except Exception as e:
                changes.errors.append(ErrorPage(page_id, str(e)))

        return changes

    def _get_stored_page(self, page_id: str) -> Optional[dict]:
        """Get stored page state from database"""
        cursor = self.db.execute(
            "SELECT * FROM notion_pages WHERE page_id = ?",
            (page_id,)
        )
        row = cursor.fetchone()
        if row:
            return dict(zip([col[0] for col in cursor.description], row))
        return None
```

#### Duplicate Detection Strategy
```python
class DuplicateDetector:
    def __init__(self, github_client: GitHubClient, db_path: str):
        self.github = github_client
        self.db = sqlite3.connect(db_path)

    def check_for_duplicates(self, rfc_identifier: str, page_title: str) -> DuplicateCheck:
        """Check if issue already exists for RFC"""

        # Method 1: Check tracking database
        existing_issue = self._get_tracked_issue(rfc_identifier)
        if existing_issue:
            return DuplicateCheck(
                is_duplicate=True,
                reason="tracked_in_db",
                existing_issue=existing_issue
            )

        # Method 2: Search GitHub issues by title
        github_issues = self.github.search_issues(
            query=f'repo:{REPO} is:issue "{rfc_identifier}"'
        )

        for issue in github_issues:
            if rfc_identifier in issue.title:
                return DuplicateCheck(
                    is_duplicate=True,
                    reason="found_on_github",
                    existing_issue=issue
                )

        return DuplicateCheck(is_duplicate=False)

    def _get_tracked_issue(self, rfc_identifier: str) -> Optional[dict]:
        """Get issue from tracking database"""
        cursor = self.db.execute("""
            SELECT gi.* FROM github_issues gi
            JOIN notion_pages np ON gi.notion_page_id = np.page_id
            WHERE np.rfc_identifier = ? AND gi.issue_state = 'open'
        """, (rfc_identifier,))

        row = cursor.fetchone()
        if row:
            return dict(zip([col[0] for col in cursor.description], row))
        return None
```

### Incremental Processing Workflow

#### Main Processing Logic
```python
class IncrementalProcessor:
    def __init__(self, notion_client, github_client, db_path):
        self.notion = notion_client
        self.github = github_client
        self.change_detector = NotionChangeDetector(db_path, notion_client)
        self.duplicate_detector = DuplicateDetector(github_client, db_path)
        self.db = sqlite3.connect(db_path)

    def process_implementation_collection(self, collection_id: str) -> ProcessingReport:
        """Process entire implementation RFC collection incrementally"""
        report = ProcessingReport()

        # Step 1: Discover all implementation pages
        implementation_pages = self.notion.get_child_pages(collection_id)
        page_ids = [page['id'] for page in implementation_pages]

        # Step 2: Detect changes
        changes = self.change_detector.detect_changes(page_ids)
        report.add_change_summary(changes)

        # Step 3: Process new pages
        for new_page in changes.new_pages:
            try:
                result = self._process_new_page(new_page.page_id)
                report.add_processing_result(result)
            except Exception as e:
                report.add_error(new_page.page_id, e)

        # Step 4: Process modified pages
        for modified_page in changes.modified_pages:
            try:
                result = self._process_modified_page(modified_page.page_id)
                report.add_processing_result(result)
            except Exception as e:
                report.add_error(modified_page.page_id, e)

        # Step 5: Update tracking database
        self._update_tracking_database(changes)

        return report

    def _process_new_page(self, page_id: str) -> ProcessingResult:
        """Process a new implementation RFC page"""
        # Get page content
        page_data = self.notion.get_page(page_id)
        page_content = self.notion.get_page_content(page_id)

        # Extract RFC identifier from title
        rfc_identifier = self._extract_rfc_identifier(page_data['title'])
        if not rfc_identifier:
            return ProcessingResult.error(page_id, "Invalid RFC identifier in title")

        # Check for duplicates
        duplicate_check = self.duplicate_detector.check_for_duplicates(
            rfc_identifier, page_data['title']
        )

        if duplicate_check.is_duplicate:
            return ProcessingResult.skipped(
                page_id,
                f"Duplicate detected: {duplicate_check.reason}"
            )

        # Create GitHub issue
        issue = self.github.create_issue(
            title=page_data['title'],
            body=self._format_issue_body(page_content)
        )

        # Record in tracking database
        self._record_issue_creation(page_id, issue, page_data, page_content)

        return ProcessingResult.created(page_id, issue.number)

    def _process_modified_page(self, page_id: str) -> ProcessingResult:
        """Process a modified implementation RFC page"""
        # Get current page state
        page_data = self.notion.get_page(page_id)
        page_content = self.notion.get_page_content(page_id)

        # Get associated GitHub issue
        tracked_issue = self._get_tracked_issue_by_page(page_id)
        if not tracked_issue:
            # Page was modified but no tracked issue - treat as new
            return self._process_new_page(page_id)

        # Update GitHub issue
        updated_issue = self.github.update_issue(
            issue_number=tracked_issue['issue_number'],
            title=page_data['title'],
            body=self._format_issue_body(page_content)
        )

        # Update tracking database
        self._update_issue_tracking(page_id, updated_issue, page_data, page_content)

        return ProcessingResult.updated(page_id, updated_issue.number)
```

### Batch Processing & Collection Management

#### Collection Processor
```python
class CollectionProcessor:
    def __init__(self, processor: IncrementalProcessor):
        self.processor = processor

    def process_all_collections(self, collections: List[str]) -> BatchReport:
        """Process multiple RFC collections"""
        batch_report = BatchReport()

        for collection_id in collections:
            try:
                collection_report = self.processor.process_implementation_collection(collection_id)
                batch_report.add_collection_report(collection_id, collection_report)
            except Exception as e:
                batch_report.add_collection_error(collection_id, e)

        return batch_report

    def validate_before_processing(self, collection_id: str) -> ValidationResult:
        """Validate collection before processing"""
        validation = ValidationResult()

        # Check collection access
        try:
            pages = self.processor.notion.get_child_pages(collection_id)
            validation.collection_accessible = True
            validation.page_count = len(pages)
        except Exception as e:
            validation.add_error(f"Cannot access collection: {e}")
            return validation

        # Validate page titles follow RFC pattern
        for page in pages:
            title = page.get('title', '')
            if not re.match(r'^RFC-\d+-\d+:', title):
                validation.add_warning(f"Page title doesn't follow RFC pattern: {title}")

        return validation
```

### Command Line Interface

#### Enhanced Script Interface
```python
def main(argv: list[str]) -> int:
    p = argparse.ArgumentParser(description="Generate micro issues from Notion RFCs")

    # Processing modes
    mode_group = p.add_mutually_exclusive_group(required=True)
    mode_group.add_argument("--single-page", help="Process single Notion page")
    mode_group.add_argument("--collection", help="Process entire collection")
    mode_group.add_argument("--incremental", help="Process only changed pages in collection")

    # Deduplication options
    p.add_argument("--force", action="store_true", help="Skip duplicate detection")
    p.add_argument("--dry-run", action="store_true", help="Show what would be processed")
    p.add_argument("--db-path", default="./rfc_tracking.db", help="SQLite database path")

    # Existing options
    p.add_argument("--owner", required=True)
    p.add_argument("--repo", required=True)
    p.add_argument("--notion-token")

    args = p.parse_args(argv)

    # Initialize components
    notion_client = NotionClient(args.notion_token)
    github_client = GitHubClient(args.owner, args.repo)
    processor = IncrementalProcessor(notion_client, github_client, args.db_path)

    if args.single_page:
        return process_single_page(processor, args.single_page, args)
    elif args.collection:
        return process_collection(processor, args.collection, args)
    elif args.incremental:
        return process_incremental(processor, args.incremental, args)
```

## Implementation Strategy

### Phase 1: Database & Change Detection
1. Create SQLite schema for tracking
2. Implement content hash generation
3. Build change detection logic

### Phase 2: Duplicate Prevention
1. Add duplicate detection methods
2. Implement GitHub issue search
3. Create validation before processing

### Phase 3: Incremental Processing
1. Build incremental workflow logic
2. Add collection processing capabilities
3. Implement error handling and recovery

### Phase 4: Enhanced CLI
1. Add collection processing options
2. Implement force and dry-run modes
3. Add comprehensive reporting

## Success Metrics

- **Zero Duplicates**: 0% duplicate issue creation rate
- **Change Detection Accuracy**: 100% detection of meaningful content changes
- **Processing Efficiency**: Only process changed content (not full collection)
- **Error Recovery**: Graceful handling of API failures and corrupted state

## Monitoring & Alerts

- **Daily Change Reports**: Summary of detected changes and processing results
- **Duplicate Alerts**: Immediate notification if duplicate detection fails
- **Processing Metrics**: Track success rates and processing times
- **Database Health**: Monitor tracking database consistency
