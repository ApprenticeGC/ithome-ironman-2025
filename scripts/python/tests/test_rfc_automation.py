#!/usr/bin/env python3
"""
Comprehensive tests for RFC automation system
"""

import json
import os
import pathlib
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

# Add production directory to path for imports
production_dir = pathlib.Path(__file__).parent.parent / "production"
sys.path.insert(0, str(production_dir))

from generate_micro_issues_collection import CollectionProcessor
from generate_micro_issues_from_rfc import (
    NotionClient,
    TrackingDatabase,
    generate_content_hash,
    parse_micro_sections,
    parse_micro_table,
    parse_notion_page,
)
from notion_page_discovery import NotionPageDiscovery


class TestTrackingDatabase(unittest.TestCase):
    """Test SQLite tracking database functionality"""

    def setUp(self):
        self.temp_db = tempfile.NamedTemporaryFile(delete=False)
        self.temp_db.close()
        self.db = TrackingDatabase(self.temp_db.name)

    def tearDown(self):
        self.db.close()
        os.unlink(self.temp_db.name)

    def test_database_initialization(self):
        """Test database schema creation"""
        # Check that tables exist
        cursor = self.db.conn.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = [row[0] for row in cursor.fetchall()]

        expected_tables = ["notion_pages", "github_issues", "processing_log"]
        for table in expected_tables:
            self.assertIn(table, tables)

    def test_record_and_retrieve_page_state(self):
        """Test recording and retrieving page state"""
        page_id = "test-page-123"
        title = "Test RFC Page"
        content_hash = "abc123def456"  # pragma: allowlist secret
        rfc_identifier = "Game-RFC-001-01"

        # Record page state
        self.db.record_page_state(page_id, title, content_hash, rfc_identifier)

        # Retrieve page state
        stored_page = self.db.get_stored_page(page_id)

        self.assertIsNotNone(stored_page)
        self.assertEqual(stored_page["page_id"], page_id)
        self.assertEqual(stored_page["page_title"], title)
        self.assertEqual(stored_page["content_hash"], content_hash)
        self.assertEqual(stored_page["rfc_identifier"], rfc_identifier)

    def test_record_and_check_issue_creation(self):
        """Test recording GitHub issue creation"""
        # First record a page
        page_id = "test-page-123"
        self.db.record_page_state(page_id, "Test Page", "hash123", "Game-RFC-001-01")

        # Record issue creation
        issue_number = 42
        issue_title = "Game-RFC-001-01: Test Issue"
        content_hash = "hash123"

        self.db.record_issue_creation(issue_number, issue_title, page_id, content_hash)

        # Check existing issue
        existing_issue = self.db.check_existing_issue("Game-RFC-001-01")

        self.assertIsNotNone(existing_issue)
        self.assertEqual(existing_issue["issue_number"], issue_number)
        self.assertEqual(existing_issue["issue_title"], issue_title)

    def test_no_duplicate_issue_detection(self):
        """Test that non-existent RFCs return None"""
        existing_issue = self.db.check_existing_issue("Game-RFC-999-99")
        self.assertIsNone(existing_issue)


class TestContentParsing(unittest.TestCase):
    """Test RFC content parsing functionality"""

    def test_parse_game_rfc_sections(self):
        """Test parsing Game-RFC format sections"""
        content = """
# Test RFC

### Game-RFC-001-01: Create Base Interfaces

**Objective**: Implement foundational interfaces

**Requirements**:
- Create project structure
- Define interfaces
- Add documentation

**Acceptance Criteria**:
- [ ] Code compiles
- [ ] Tests pass

### Game-RFC-001-02: Implement Registry

**Objective**: Build service registry

**Requirements**:
- Registry implementation
- Service discovery

**Acceptance Criteria**:
- [ ] Registry works
- [ ] Discovery functional
"""

        items = parse_micro_sections(content)

        self.assertEqual(len(items), 2)

        # Check first item
        self.assertEqual(items[0]["ident"], "GAME-RFC-001-01")
        self.assertEqual(items[0]["rfc_num"], 1)
        self.assertEqual(items[0]["micro_num"], 1)
        self.assertEqual(items[0]["title"], "Create Base Interfaces")
        self.assertIn("**Objective**", items[0]["body"])

        # Check second item
        self.assertEqual(items[1]["ident"], "GAME-RFC-001-02")
        self.assertEqual(items[1]["rfc_num"], 1)
        self.assertEqual(items[1]["micro_num"], 2)
        self.assertEqual(items[1]["title"], "Implement Registry")

    def test_parse_old_rfc_format(self):
        """Test parsing old RFC-XXX-YY format"""
        content = """
### RFC-001-01: Legacy Format Task

Some content here.

### RFC-001-02: Another Legacy Task

More content.
"""

        items = parse_micro_sections(content)

        self.assertEqual(len(items), 2)
        self.assertEqual(items[0]["ident"], "RFC-001-01")
        self.assertEqual(items[1]["ident"], "RFC-001-02")

    def test_parse_micro_table_format(self):
        """Test parsing table format RFCs"""
        content = """
# RFC-001: Test Architecture

## Implementation Plan (Micro Issues)

| Micro | Task | Acceptance Criteria |
|-------|------|-------------------|
| 01 | Create interfaces | Code compiles; Tests pass |
| 02 | Implement registry | Registry works; Discovery functional |
"""

        items = parse_micro_table(content)

        self.assertEqual(len(items), 2)
        self.assertEqual(items[0]["ident"], "RFC-001-01")
        self.assertEqual(items[0]["title"], "Create interfaces")
        self.assertIn("- [ ] Code compiles", items[0]["body"])
        self.assertIn("- [ ] Tests pass", items[0]["body"])

    def test_generate_content_hash(self):
        """Test content hash generation"""
        content1 = "This is test content"
        content2 = "This is test content"
        content3 = "This is different content"

        metadata = {"title": "Test", "last_edited_time": "2025-01-01"}

        hash1 = generate_content_hash(content1, metadata)
        hash2 = generate_content_hash(content2, metadata)
        hash3 = generate_content_hash(content3, metadata)

        # Same content should produce same hash
        self.assertEqual(hash1, hash2)

        # Different content should produce different hash
        self.assertNotEqual(hash1, hash3)

        # Hash should be SHA-256 (64 hex characters)
        self.assertEqual(len(hash1), 64)
        self.assertTrue(all(c in "0123456789abcdef" for c in hash1))


class TestNotionClient(unittest.TestCase):
    """Test Notion API client functionality"""

    def setUp(self):
        self.client = NotionClient("test-token")

    @patch("urllib.request.urlopen")
    def test_get_page_success(self, mock_urlopen):
        """Test successful page retrieval"""
        mock_response = Mock()
        mock_response.read.return_value = json.dumps(
            {"id": "test-page-id", "properties": {"title": {"type": "title", "title": [{"plain_text": "Test Page"}]}}}
        ).encode()
        mock_response.__enter__ = Mock(return_value=mock_response)
        mock_response.__exit__ = Mock(return_value=None)
        mock_urlopen.return_value = mock_response

        result = self.client.get_page("test-page-id")

        self.assertEqual(result["id"], "test-page-id")
        self.assertIn("properties", result)

    @patch("urllib.request.urlopen")
    def test_get_page_content_success(self, mock_urlopen):
        """Test successful page content retrieval"""
        mock_response = Mock()
        mock_response.read.return_value = json.dumps(
            {"results": [{"type": "paragraph", "paragraph": {"rich_text": [{"plain_text": "Test content"}]}}]}
        ).encode()
        mock_response.__enter__ = Mock(return_value=mock_response)
        mock_response.__exit__ = Mock(return_value=None)
        mock_urlopen.return_value = mock_response

        result = self.client.get_page_content("test-page-id")

        self.assertIn("results", result)
        self.assertEqual(len(result["results"]), 1)

    def test_extract_rich_text(self):
        """Test rich text extraction"""
        rich_text_array = [{"plain_text": "Hello "}, {"plain_text": "world"}, {"plain_text": "!"}]

        result = self.client._extract_rich_text(rich_text_array)
        self.assertEqual(result, "Hello world!")

    def test_block_to_text_conversion(self):
        """Test conversion of different block types to text"""
        # Test heading_3 block
        heading_block = {"type": "heading_3", "heading_3": {"rich_text": [{"plain_text": "Test Heading"}]}}
        result = self.client._block_to_text(heading_block)
        self.assertEqual(result, "### Test Heading")

        # Test paragraph block
        paragraph_block = {"type": "paragraph", "paragraph": {"rich_text": [{"plain_text": "Test paragraph"}]}}
        result = self.client._block_to_text(paragraph_block)
        self.assertEqual(result, "Test paragraph")

        # Test bulleted list
        list_block = {"type": "bulleted_list_item", "bulleted_list_item": {"rich_text": [{"plain_text": "List item"}]}}
        result = self.client._block_to_text(list_block)
        self.assertEqual(result, "- List item")

        # Test to-do block
        todo_block = {"type": "to_do", "to_do": {"rich_text": [{"plain_text": "Todo item"}], "checked": False}}
        result = self.client._block_to_text(todo_block)
        self.assertEqual(result, "- [ ] Todo item")


class TestNotionPageParsing(unittest.TestCase):
    """Test parsing of Notion pages into micro-issues"""

    def setUp(self):
        self.mock_client = Mock(spec=NotionClient)

    def test_parse_notion_page_success(self):
        """Test successful parsing of a Notion page"""
        # Mock page metadata
        self.mock_client.get_page.return_value = {
            "properties": {
                "title": {"type": "title", "title": [{"plain_text": "Game-RFC-001-01: Create Base Interfaces"}]}
            }
        }

        # Mock page content
        self.mock_client.extract_content_as_markdown.return_value = """
**Objective**: Implement foundational interfaces

**Requirements**:
- Create project structure
- Define interfaces

**Acceptance Criteria**:
- [ ] Code compiles
- [ ] Tests pass
"""

        result = parse_notion_page("test-page-id", self.mock_client)

        self.assertEqual(result["page_id"], "test-page-id")
        self.assertEqual(result["ident"], "GAME-RFC-001-01")
        self.assertEqual(result["rfc_num"], 1)
        self.assertEqual(result["micro_num"], 1)
        self.assertEqual(result["title"], "Create Base Interfaces")
        self.assertIn("**Objective**", result["body"])

    def test_parse_notion_page_invalid_title(self):
        """Test handling of invalid page titles"""
        self.mock_client.get_page.return_value = {
            "properties": {"title": {"type": "title", "title": [{"plain_text": "Invalid Title Format"}]}}
        }

        with self.assertRaises(ValueError) as context:
            parse_notion_page("test-page-id", self.mock_client)

        self.assertIn("doesn't match Game-RFC pattern", str(context.exception))


class TestCollectionProcessor(unittest.TestCase):
    """Test collection processing functionality"""

    def setUp(self):
        self.temp_db = tempfile.NamedTemporaryFile(delete=False)
        self.temp_db.close()
        self.processor = CollectionProcessor(self.temp_db.name)
        self.mock_client = Mock(spec=NotionClient)

    def tearDown(self):
        self.processor.close()
        os.unlink(self.temp_db.name)

    @patch("generate_micro_issues_collection.parse_notion_page")
    def test_process_notion_collection_success(self, mock_parse):
        """Test successful processing of Notion page collection"""
        # Mock page parsing
        mock_parse.side_effect = [
            {
                "page_id": "page-1",
                "ident": "GAME-RFC-001-01",
                "title": "Test Task 1",
                "body": "Test content 1",
                "page_metadata": {},
            },
            {
                "page_id": "page-2",
                "ident": "GAME-RFC-001-02",
                "title": "Test Task 2",
                "body": "Test content 2",
                "page_metadata": {},
            },
        ]

        page_ids = ["page-1", "page-2"]
        result = self.processor.process_notion_collection(page_ids, self.mock_client)

        self.assertEqual(result["total_pages"], 2)
        self.assertEqual(len(result["processed_pages"]), 2)
        self.assertEqual(len(result["errors"]), 0)

        # All pages should be marked as ready (no duplicates in fresh DB)
        for page in result["processed_pages"]:
            self.assertEqual(page["status"], "ready")

    def test_process_file_collection_success(self):
        """Test processing of file collection"""
        # Create temporary test files
        test_files = []
        for i in range(2):
            temp_file = tempfile.NamedTemporaryFile(mode="w", suffix=".md", delete=False)
            temp_file.write(
                f"""
### Game-RFC-001-0{i+1}: Test Task {i+1}

**Objective**: Test objective {i+1}

**Requirements**:
- Requirement 1
- Requirement 2

**Acceptance Criteria**:
- [ ] Criterion 1
- [ ] Criterion 2
"""
            )
            temp_file.close()
            test_files.append(temp_file.name)

        try:
            result = self.processor.process_file_collection(test_files)

            self.assertEqual(result["total_files"], 2)
            self.assertEqual(len(result["processed_files"]), 2)
            self.assertEqual(len(result["errors"]), 0)

            # Check that micro-issues were found
            for file_result in result["processed_files"]:
                self.assertEqual(len(file_result["micro_issues"]), 1)
                self.assertEqual(file_result["micro_issues"][0]["status"], "ready")

        finally:
            # Clean up test files
            for file_path in test_files:
                os.unlink(file_path)

    @patch("generate_micro_issues_collection.parse_notion_page")
    def test_detect_changes_new_page(self, mock_parse):
        """Test change detection for new pages"""
        mock_parse.return_value = {
            "page_id": "new-page",
            "ident": "GAME-RFC-001-01",
            "title": "New Task",
            "body": "New content",
            "page_metadata": {},
        }

        page_ids = ["new-page"]
        changes = self.processor.detect_changes(page_ids, self.mock_client)

        self.assertEqual(len(changes["new_pages"]), 1)
        self.assertEqual(len(changes["modified_pages"]), 0)
        self.assertEqual(len(changes["unchanged_pages"]), 0)
        self.assertEqual(changes["new_pages"][0]["page_id"], "new-page")

    @patch("generate_micro_issues_collection.parse_notion_page")
    def test_detect_changes_modified_page(self, mock_parse):
        """Test change detection for modified pages"""
        # First, record an existing page state
        page_id = "existing-page"
        old_content = "Old content"
        old_hash = generate_content_hash(old_content, {})
        self.processor.db.record_page_state(page_id, "Test Page", old_hash, "GAME-RFC-001-01")

        # Mock the updated page content
        mock_parse.return_value = {
            "page_id": page_id,
            "ident": "GAME-RFC-001-01",
            "title": "Updated Task",
            "body": "Updated content",  # Different content
            "page_metadata": {},
        }

        changes = self.processor.detect_changes([page_id], self.mock_client)

        self.assertEqual(len(changes["new_pages"]), 0)
        self.assertEqual(len(changes["modified_pages"]), 1)
        self.assertEqual(len(changes["unchanged_pages"]), 0)
        self.assertEqual(changes["modified_pages"][0]["page_id"], page_id)


class TestNotionPageDiscovery(unittest.TestCase):
    """Test Notion page discovery functionality"""

    def setUp(self):
        self.mock_client = Mock(spec=NotionClient)
        self.discovery = NotionPageDiscovery(self.mock_client)

    def test_get_child_pages_success(self):
        """Test getting child pages from a parent page"""
        self.mock_client.get_page_content.return_value = {
            "results": [
                {"type": "child_page", "id": "child-1", "child_page": {"title": "Child Page 1"}},
                {"type": "paragraph", "paragraph": {"rich_text": [{"plain_text": "Some text"}]}},
                {"type": "child_page", "id": "child-2", "child_page": {"title": "Child Page 2"}},
            ]
        }

        result = self.discovery.get_child_pages("parent-id")

        self.assertEqual(len(result), 2)
        self.assertEqual(result[0]["id"], "child-1")
        self.assertEqual(result[0]["title"], "Child Page 1")
        self.assertEqual(result[1]["id"], "child-2")
        self.assertEqual(result[1]["title"], "Child Page 2")

    def test_discover_implementation_pages(self):
        """Test discovering implementation pages"""
        self.mock_client.get_page_content.return_value = {
            "results": [
                {"type": "child_page", "id": "impl-1", "child_page": {"title": "Game-RFC-001-01: Create Interfaces"}},
                {"type": "child_page", "id": "impl-2", "child_page": {"title": "Game-RFC-001-02: Implement Registry"}},
                {"type": "child_page", "id": "other", "child_page": {"title": "Some Other Page"}},
            ]
        }

        result = self.discovery.discover_implementation_pages("section-id")

        # Should only return Game-RFC pages
        self.assertEqual(len(result), 2)
        self.assertIn("impl-1", result)
        self.assertIn("impl-2", result)
        self.assertNotIn("other", result)


class TestIntegration(unittest.TestCase):
    """Integration tests for the complete workflow"""

    def setUp(self):
        self.temp_db = tempfile.NamedTemporaryFile(delete=False)
        self.temp_db.close()

    def tearDown(self):
        os.unlink(self.temp_db.name)

    def test_complete_file_processing_workflow(self):
        """Test complete workflow from file to issue tracking"""
        # Create test RFC file
        test_file = tempfile.NamedTemporaryFile(mode="w", suffix=".md", delete=False)
        test_file.write(
            """
### Game-RFC-001-01: Test Integration Task

**Objective**: Test the complete integration workflow

**Requirements**:
- Process file content
- Track in database
- Generate proper output

**Acceptance Criteria**:
- [ ] File is parsed correctly
- [ ] Database records the processing
- [ ] Output format is correct
"""
        )
        test_file.close()

        try:
            # Initialize components
            db = TrackingDatabase(self.temp_db.name)
            processor = CollectionProcessor(self.temp_db.name)

            # Process the file
            result = processor.process_file_collection([test_file.name])

            # Verify results
            self.assertEqual(result["total_files"], 1)
            self.assertEqual(len(result["processed_files"]), 1)
            self.assertEqual(len(result["errors"]), 0)

            file_result = result["processed_files"][0]
            self.assertEqual(len(file_result["micro_issues"]), 1)

            micro_issue = file_result["micro_issues"][0]
            self.assertEqual(micro_issue["ident"], "GAME-RFC-001-01")
            self.assertEqual(micro_issue["status"], "ready")

            # Clean up
            processor.close()
            db.close()

        finally:
            os.unlink(test_file.name)

    def test_duplicate_detection_workflow(self):
        """Test that duplicate detection works across multiple processing runs"""
        # Initialize database and processor
        db = TrackingDatabase(self.temp_db.name)
        processor = CollectionProcessor(self.temp_db.name)

        try:
            # First, simulate creating an issue
            db.record_page_state("test-page", "Test Page", "hash123", "Game-RFC-001-01")
            db.record_issue_creation(42, "Game-RFC-001-01: Test Issue", "test-page", "hash123")

            # Now try to process the same RFC again
            test_file = tempfile.NamedTemporaryFile(mode="w", suffix=".md", delete=False)
            test_file.write(
                """
### Game-RFC-001-01: Test Integration Task

Same content as before.
"""
            )
            test_file.close()

            result = processor.process_file_collection([test_file.name])

            # Should detect duplicate
            micro_issue = result["processed_files"][0]["micro_issues"][0]
            self.assertEqual(micro_issue["status"], "duplicate")
            self.assertEqual(micro_issue["existing_issue"], 42)

            os.unlink(test_file.name)

        finally:
            processor.close()
            db.close()


if __name__ == "__main__":
    # Run tests with verbose output
    unittest.main(verbosity=2)
