#!/usr/bin/env python3
"""
Test data and mock configurations for RFC automation tests
"""

# Sample RFC content in different formats
SAMPLE_GAME_RFC_CONTENT = """
# Game-RFC-001: 4-Tier Implementation

### Game-RFC-001-01: Create Tier 1 Base Interfaces

**Objective**: Implement foundational service interface contracts for the 4-tier architecture

**Requirements**:
- Create GameConsole.Core.Abstractions project targeting net8.0
- Define IService base interface with lifecycle methods
- Add ICapabilityProvider interface for service discovery
- Include IServiceMetadata interface for service information
- Add XML documentation for all public members
- Follow async/await patterns with CancellationToken support

**Implementation Details**:
- Namespace: GameConsole.Core.Abstractions
- Use only .NET Standard dependencies (no Unity/Godot)
- All async methods return Task or Task<T>
- Include capability discovery via ICapabilityProvider
- Follow Pure.DI compatibility patterns
- Add ServiceAttribute for metadata decoration

**Acceptance Criteria**:
- [ ] IService interface compiles without external dependencies
- [ ] All methods follow async naming conventions (Async suffix)
- [ ] XML documentation coverage >90%
- [ ] Code passes `dotnet format --verify-no-changes`
- [ ] Unit tests cover interface contract behavior
- [ ] No compiler warnings with `dotnet build --warnaserror`

### Game-RFC-001-02: Implement Service Registry Pattern

**Objective**: Build provider selection and service registry system for dependency injection

**Requirements**:
- Create GameConsole.Core.Registry project targeting net8.0
- Implement IServiceRegistry interface for service registration
- Add ServiceProvider class with provider selection logic
- Create ServiceDescriptor for service metadata
- Implement service lifecycle management (Singleton, Transient, Scoped)
- Add service resolution with dependency injection support

**Implementation Details**:
- Use Pure.DI compatibility patterns
- Support both programmatic and attribute-based registration
- Implement circular dependency detection
- Add logging for service registration and resolution
- Support conditional service registration
- Include performance optimizations for service lookup

**Acceptance Criteria**:
- [ ] ServiceRegistry handles all lifecycle types correctly
- [ ] Circular dependency detection prevents infinite loops
- [ ] Service resolution performance <1ms for cached services
- [ ] Unit tests achieve >95% code coverage
- [ ] Integration tests validate Pure.DI compatibility
- [ ] Memory usage remains stable during service resolution cycles
"""

SAMPLE_OLD_RFC_CONTENT = """
# RFC-001: Legacy Format Example

### RFC-001-01: Legacy Task One

This is the first legacy task with some content.

### RFC-001-02: Legacy Task Two

This is the second legacy task with different content.
"""

SAMPLE_TABLE_RFC_CONTENT = """
# RFC-002: Table Format Example

## Implementation Plan (Micro Issues)

| Micro | Task | Acceptance Criteria |
|-------|------|-------------------|
| 01 | Create base interfaces | Code compiles; Tests pass; Documentation complete |
| 02 | Implement service registry | Registry works; Discovery functional; Performance meets requirements |
| 03 | Add plugin support | Plugins load; Dependencies resolve; Lifecycle managed |
"""

# Mock Notion API responses
MOCK_NOTION_PAGE_RESPONSE = {
    "id": "2722b68a-e800-812d-a440-d487142573e2",
    "properties": {
        "title": {"type": "title", "title": [{"plain_text": "Game-RFC-001-01: Create Tier 1 Base Interfaces"}]}
    },
}

MOCK_NOTION_CONTENT_RESPONSE = {
    "results": [
        {
            "type": "paragraph",
            "paragraph": {
                "rich_text": [{"plain_text": "**Objective**: Implement foundational service interface contracts"}]
            },
        },
        {"type": "paragraph", "paragraph": {"rich_text": [{"plain_text": "**Requirements**:"}]}},
        {
            "type": "bulleted_list_item",
            "bulleted_list_item": {"rich_text": [{"plain_text": "Create GameConsole.Core.Abstractions project"}]},
        },
        {
            "type": "bulleted_list_item",
            "bulleted_list_item": {"rich_text": [{"plain_text": "Define IService base interface"}]},
        },
        {"type": "paragraph", "paragraph": {"rich_text": [{"plain_text": "**Acceptance Criteria**:"}]}},
        {
            "type": "to_do",
            "to_do": {"rich_text": [{"plain_text": "Code compiles without external dependencies"}], "checked": False},
        },
        {
            "type": "to_do",
            "to_do": {"rich_text": [{"plain_text": "Unit tests cover interface contract behavior"}], "checked": False},
        },
    ]
}

MOCK_NOTION_COLLECTION_RESPONSE = {
    "results": [
        {
            "type": "child_page",
            "id": "page-1",
            "child_page": {"title": "Game-RFC-001-01: Create Tier 1 Base Interfaces"},
        },
        {
            "type": "child_page",
            "id": "page-2",
            "child_page": {"title": "Game-RFC-001-02: Implement Service Registry Pattern"},
        },
        {
            "type": "child_page",
            "id": "page-3",
            "child_page": {"title": "Game-RFC-001-03: Create Audio Service Interface"},
        },
        {"type": "paragraph", "paragraph": {"rich_text": [{"plain_text": "Some explanatory text"}]}},
    ]
}

# Expected test results
EXPECTED_GAME_RFC_PARSE_RESULT = [
    {"ident": "GAME-RFC-001-01", "rfc_num": 1, "micro_num": 1, "title": "Create Tier 1 Base Interfaces"},
    {"ident": "GAME-RFC-001-02", "rfc_num": 1, "micro_num": 2, "title": "Implement Service Registry Pattern"},
]

EXPECTED_TABLE_RFC_PARSE_RESULT = [
    {"ident": "RFC-002-01", "rfc_num": 2, "micro_num": 1, "title": "Create base interfaces"},
    {"ident": "RFC-002-02", "rfc_num": 2, "micro_num": 2, "title": "Implement service registry"},
    {"ident": "RFC-002-03", "rfc_num": 2, "micro_num": 3, "title": "Add plugin support"},
]

# Mock GitHub API responses
MOCK_GITHUB_REPO_RESPONSE = {
    "repository": {
        "id": "test-repo-id",
        "suggestedActors": {
            "nodes": [
                {"id": "copilot-bot-id", "login": "github-copilot[bot]", "__typename": "Bot"},
                {"id": "user-id", "login": "testuser", "__typename": "User"},
            ]
        },
    }
}

MOCK_GITHUB_CREATE_ISSUE_RESPONSE = {
    "createIssue": {"issue": {"id": "test-issue-id", "number": 42, "url": "https://github.com/test/repo/issues/42"}}
}

MOCK_GITHUB_SEARCH_RESPONSE = {"search": {"issueCount": 0, "edges": []}}

# Test configuration
TEST_CONFIG = {
    "notion": {"api_base": "https://api.notion.com/v1", "version": "2022-06-28"},
    "github": {"api_base": "https://api.github.com/graphql"},
    "database": {"test_db_prefix": "test_rfc_", "cleanup_after_test": True},
    "timeouts": {"api_request": 10, "test_timeout": 30},
}

# Validation patterns
RFC_PATTERNS = {
    "game_rfc": r"^Game-RFC-(\d+)-(\d+):\s*(.+)$",
    "old_rfc": r"^RFC-(\d+)-(\d+):\s*(.+)$",
    "objective": r"\*\*Objective\*\*:\s*(.+)",
    "requirements": r"\*\*Requirements\*\*:",
    "acceptance": r"\*\*Acceptance Criteria\*\*:",
    "checkbox": r"- \[ \] (.+)",
}

# Error messages for testing
ERROR_MESSAGES = {
    "invalid_title": "Page title doesn't match Game-RFC pattern",
    "missing_token": "Missing NOTION_TOKEN environment variable",
    "page_not_found": "Could not find page with ID",
    "duplicate_issue": "Issue already exists for RFC identifier",
    "invalid_content": "No micro-issues found in content",
}
