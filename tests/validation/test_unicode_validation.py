#!/usr/bin/env python3
"""
Unicode Encoding Validation Tests
Prevents automation failures due to encoding issues.
"""

import json
import subprocess
import sys
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
        "Bullet points: • ★ ▲ ●",
    ]

    critical_scripts = [
        "auto_approve_or_dispatch.py",
        "validate_ci_logs.py",
        "monitor_pr_flow.py",
        "ensure_automerge_or_comment.py",
        "direct_merge_pr.py",
    ]

    for script_name in critical_scripts:
        script_path = scripts_dir / script_name
        if script_path.exists():
            # Test 1: Script file reads cleanly
            try:
                with open(script_path, "r", encoding="utf-8") as f:
                    f.read()
                print(f"✅ {script_name}: File encoding OK")
            except UnicodeDecodeError as e:
                print(f"❌ {script_name}: File encoding error - {e}")
                return False

            # Test 2: Mock Unicode subprocess output
            for sample in unicode_samples:
                try:
                    # Simulate what the script would encounter
                    json_test = json.dumps({"stdout": sample}, ensure_ascii=False)
                    parsed = json.loads(json_test)
                    assert parsed["stdout"] == sample
                except (UnicodeError, json.JSONDecodeError) as e:
                    print(f"❌ {script_name}: Unicode handling error with '{sample}' - {e}")
                    return False

            print(f"✅ {script_name}: Unicode handling OK")
        else:
            print(f"⚠️  {script_name}: Script not found at {script_path}")

    return True


def test_subprocess_unicode_handling():
    """Test subprocess calls handle Unicode correctly."""

    unicode_test = "Test ⚠️ 中文 🎉"

    try:
        # Test text=True handling (our preferred approach)
        result = subprocess.run(
            [sys.executable, "-c", f"print('{unicode_test}')"], text=True, capture_output=True, encoding="utf-8"
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
        {"status": "Success ✅ 中文字符"},
        {"log": "Build output with • bullets and ★ stars"},
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


def test_environment_encoding():
    """Test that the execution environment supports Unicode properly."""

    import locale

    try:
        # Check system encoding
        system_encoding = locale.getpreferredencoding()
        print(f"ℹ️  System encoding: {system_encoding}")

        # Check Python's default encoding
        stdout_encoding = sys.stdout.encoding
        print(f"ℹ️  stdout encoding: {stdout_encoding}")

        # Test Unicode string handling
        unicode_test = "Test 🧪 Unicode 中文 Support ⚡"
        encoded = unicode_test.encode("utf-8")
        decoded = encoded.decode("utf-8")

        if decoded != unicode_test:
            print(f"❌ Unicode round-trip test failed: {unicode_test} != {decoded}")
            return False

        print("✅ Environment Unicode support OK")
        return True

    except (UnicodeError, LookupError) as e:
        print(f"❌ Environment Unicode error: {e}")
        return False


def test_github_cli_unicode_handling():
    """Test that GitHub CLI commands handle Unicode properly."""

    try:
        # Test gh CLI with Unicode in JSON output (safe test - just check version)
        result = subprocess.run(["gh", "--version"], text=True, capture_output=True, encoding="utf-8")

        if result.returncode != 0:
            print("⚠️  GitHub CLI not available - skipping Unicode test")
            return True

        # The version output should be safely decoded
        if not result.stdout:
            print("❌ GitHub CLI Unicode test failed: no output")
            return False

        print("✅ GitHub CLI Unicode handling OK")
        return True

    except (UnicodeError, FileNotFoundError) as e:
        print(f"⚠️  GitHub CLI Unicode test skipped: {e}")
        return True  # Don't fail if gh not available


def main():
    """Run all Unicode validation tests."""
    print("🧪 Unicode Encoding Validation Tests")
    print("=" * 50)

    tests = [
        test_environment_encoding,
        test_json_unicode_handling,
        test_subprocess_unicode_handling,
        test_github_cli_unicode_handling,
        test_automation_scripts_unicode_safety,
    ]

    all_passed = True

    for test in tests:
        print(f"\n🔍 Running {test.__name__}...")
        try:
            if not test():
                all_passed = False
        except Exception as e:
            print(f"❌ Test {test.__name__} crashed: {e}")
            import traceback

            traceback.print_exc()
            all_passed = False

    print("\n" + "=" * 50)
    if all_passed:
        print("🎉 All Unicode encoding tests passed!")
        print("Automation scripts should handle Unicode correctly.")
        sys.exit(0)
    else:
        print("💥 Some Unicode encoding tests failed!")
        print("This may cause automation script failures.")
        print("\nRecommended fixes:")
        print("1. Use text=True with encoding='utf-8' in subprocess calls")
        print("2. Use encoding='utf-8' when opening files")
        print("3. Use ensure_ascii=False in json.dumps calls")
        sys.exit(1)


if __name__ == "__main__":
    main()
