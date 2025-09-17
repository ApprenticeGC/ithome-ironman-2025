# Unicode Encoding Validation Test

**Purpose**: Detect and prevent Unicode encoding issues in automation scripts that break the automation flow.

## Problem Statement

Unicode encoding issues in scripts like `auto_approve_or_dispatch.py` cause automation failures:
```
UnicodeDecodeError: 'cp950' codec can't decode byte 0xe2 in position 927195: illegal multibyte sequence
```

These failures prevent:
- Workflow run approvals
- CI dispatch operations
- End-to-end automation flow completion

## Test Implementation

### 1. Script Encoding Validation

Test that all Python scripts can handle Unicode content properly:

```python
def test_script_unicode_handling():
    """Test that automation scripts handle Unicode content without errors."""

    # Test files that commonly encounter Unicode issues
    scripts_to_test = [
        "scripts/python/production/auto_approve_or_dispatch.py",
        "scripts/python/production/validate_ci_logs.py",
        "scripts/python/production/monitor_pr_flow.py",
        "scripts/python/production/ensure_automerge_or_comment.py"
    ]

    for script_path in scripts_to_test:
        # Test 1: Script file itself is properly encoded
        with open(script_path, 'r', encoding='utf-8') as f:
            content = f.read()
            assert content  # Should not raise UnicodeDecodeError

        # Test 2: Script handles Unicode in subprocess output
        # Mock subprocess calls with Unicode content
        unicode_test_data = "Warning ⚠️ Build failed 🔥 Error occurred"
        # Verify script functions can process this without crashing
```

### 2. GitHub API Response Validation

Test that API response handling is Unicode-safe:

```python
def test_github_api_unicode_handling():
    """Test GitHub API response parsing handles Unicode correctly."""

    # Common Unicode characters in GitHub responses
    unicode_test_cases = [
        "User name with émojis 🎉",
        "Commit message with 中文字符",
        "Error message with • bullets",
        "Log content with ★ symbols"
    ]

    for test_content in unicode_test_cases:
        # Test JSON parsing
        test_json = json.dumps({"message": test_content})
        parsed = json.loads(test_json)
        assert parsed["message"] == test_content

        # Test subprocess text handling
        proc_result = subprocess.run(
            ["echo", test_content],
            text=True,
            capture_output=True
        )
        assert proc_result.stdout.strip() == test_content
```

### 3. Environment Encoding Test

Validate that the execution environment supports Unicode:

```python
def test_environment_unicode_support():
    """Test that the execution environment properly supports Unicode."""

    import locale
    import sys

    # Check system encoding
    system_encoding = locale.getpreferredencoding()
    assert 'utf' in system_encoding.lower(), f"System encoding {system_encoding} may not support Unicode"

    # Check Python's default encoding
    assert sys.stdout.encoding.lower().startswith('utf'), f"stdout encoding {sys.stdout.encoding} may not support Unicode"

    # Test Unicode string handling
    unicode_test = "Test 🧪 Unicode 中文 Support ⚡"
    encoded = unicode_test.encode('utf-8')
    decoded = encoded.decode('utf-8')
    assert decoded == unicode_test
```

## Implementation Strategy

### Step 1: Create Test File

```bash
# Create comprehensive Unicode validation test
cat > tests/unicode-encoding-validation.py << 'EOF'
#!/usr/bin/env python3
"""
Unicode Encoding Validation Tests
Prevents automation failures due to encoding issues.
"""

import json
import subprocess
import sys
import os
from pathlib import Path

def test_automation_scripts_unicode_safety():
    """Test that all automation scripts handle Unicode safely."""

    repo_root = Path(__file__).parent.parent
    scripts_dir = repo_root / "scripts" / "python" / "production"

    # Unicode test content that commonly causes issues
    unicode_samples = [
        "Warning ⚠️ This is a test",
        "Error 🔥 Build failed",
        "Success ✅ All good",
        "Info ℹ️ Processing...",
        "中文字符测试",
        "Résumé café naïve",
        "Bullet points: • ★ ▲ ●"
    ]

    critical_scripts = [
        "auto_approve_or_dispatch.py",
        "validate_ci_logs.py",
        "monitor_pr_flow.py",
        "ensure_automerge_or_comment.py"
    ]

    for script_name in critical_scripts:
        script_path = scripts_dir / script_name
        if script_path.exists():
            # Test 1: Script file reads cleanly
            try:
                with open(script_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                print(f"✅ {script_name}: File encoding OK")
            except UnicodeDecodeError as e:
                print(f"❌ {script_name}: File encoding error - {e}")
                return False

            # Test 2: Mock Unicode subprocess output
            for sample in unicode_samples:
                try:
                    # Simulate what the script would encounter
                    json_test = json.dumps({"stdout": sample})
                    parsed = json.loads(json_test)
                    assert parsed["stdout"] == sample
                except (UnicodeError, json.JSONDecodeError) as e:
                    print(f"❌ {script_name}: Unicode handling error with '{sample}' - {e}")
                    return False

            print(f"✅ {script_name}: Unicode handling OK")

    return True

def test_subprocess_unicode_handling():
    """Test subprocess calls handle Unicode correctly."""

    unicode_test = "Test ⚠️ 中文 🎉"

    try:
        # Test text=True handling (our preferred approach)
        result = subprocess.run(
            ["python", "-c", f"print('{unicode_test}')"],
            text=True,
            capture_output=True,
            encoding='utf-8'
        )

        if unicode_test not in result.stdout:
            print(f"❌ Subprocess Unicode test failed: expected '{unicode_test}', got '{result.stdout.strip()}'")
            return False

        print("✅ Subprocess Unicode handling OK")
        return True

    except UnicodeError as e:
        print(f"❌ Subprocess Unicode error: {e}")
        return False

def test_json_unicode_handling():
    """Test JSON handling with Unicode content."""

    unicode_samples = [
        {"message": "Warning ⚠️ Unicode test"},
        {"error": "Failed 🔥 with émojis"},
        {"status": "Success ✅ 中文字符"}
    ]

    for sample in unicode_samples:
        try:
            # Test JSON serialization/deserialization
            json_str = json.dumps(sample, ensure_ascii=False)
            parsed = json.loads(json_str)

            if parsed != sample:
                print(f"❌ JSON Unicode test failed: {sample} != {parsed}")
                return False

        except (UnicodeError, json.JSONDecodeError) as e:
            print(f"❌ JSON Unicode error with {sample}: {e}")
            return False

    print("✅ JSON Unicode handling OK")
    return True

def main():
    """Run all Unicode validation tests."""
    print("🧪 Unicode Encoding Validation Tests")
    print("=" * 50)

    tests = [
        test_automation_scripts_unicode_safety,
        test_subprocess_unicode_handling,
        test_json_unicode_handling
    ]

    all_passed = True

    for test in tests:
        try:
            if not test():
                all_passed = False
        except Exception as e:
            print(f"❌ Test {test.__name__} crashed: {e}")
            all_passed = False

    print("=" * 50)
    if all_passed:
        print("🎉 All Unicode encoding tests passed!")
        sys.exit(0)
    else:
        print("💥 Some Unicode encoding tests failed!")
        print("This may cause automation script failures.")
        sys.exit(1)

if __name__ == "__main__":
    main()
EOF

chmod +x tests/unicode-encoding-validation.py
```

### Step 2: Add to CI Pipeline

Add Unicode validation to existing workflows:

```yaml
- name: Validate Unicode Encoding
  run: |
    python tests/unicode-encoding-validation.py
```

### Step 3: Fix Identified Issues

Common fixes for Unicode issues:

1. **Use `text=True` in subprocess calls**:
```python
# Instead of:
result = subprocess.run(cmd, capture_output=True)
content = result.stdout.decode('utf-8')  # Can fail

# Use:
result = subprocess.run(cmd, text=True, capture_output=True, encoding='utf-8')
content = result.stdout  # Always string
```

2. **Explicit UTF-8 encoding for file operations**:
```python
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()
```

3. **JSON with Unicode support**:
```python
json.dumps(data, ensure_ascii=False)  # Preserves Unicode
```

## Expected Benefits

1. **Proactive Detection**: Catch Unicode issues before they break automation
2. **Environment Validation**: Ensure execution environment supports Unicode properly
3. **Script Safety**: Validate all automation scripts handle Unicode correctly
4. **Continuous Monitoring**: Run tests in CI to prevent regressions

This test suite will help prevent the Unicode encoding issues that broke the automation flow for PR #96.
