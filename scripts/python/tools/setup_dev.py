#!/usr/bin/env python3
"""
Setup script for pre-commit hooks and development environment.
"""
import subprocess
import sys
from pathlib import Path


def run_command(cmd, description):
    """Run a command and return success status."""
    print(f"🔧 {description}...")
    try:
        result = subprocess.run(
            cmd, shell=True, check=True, capture_output=True, text=True
        )
        print(f"✅ {description} completed")
        return True
    except subprocess.CalledProcessError as e:
        print(f"❌ {description} failed: {e.stderr}")
        return False


def main():
    """Set up pre-commit hooks and install dependencies."""
    print("🚀 Setting up development environment...\n")

    # Install Python dependencies
    if not run_command(
        "pip install -r scripts/python/requirements/test-requirements.txt",
        "Installing Python dependencies",
    ):
        return False

    # Install pre-commit hooks
    if not run_command("pre-commit install", "Installing pre-commit hooks"):
        return False

    # Run pre-commit on all files to validate setup
    print("\n🔍 Running pre-commit validation...")
    result = subprocess.run(
        "pre-commit run --all-files", shell=True, capture_output=True, text=True
    )

    if result.returncode == 0:
        print("✅ Pre-commit setup successful!")
        print("\n📝 Pre-commit hooks are now active. They will run automatically on:")
        print("   • git commit (before the commit is created)")
        print("   • You can also run manually with: pre-commit run --all-files")
    else:
        print("⚠️  Pre-commit validation found issues:")
        print(result.stdout)
        if result.stderr:
            print("Errors:", result.stderr)
        print("\n💡 You may need to fix these issues before committing.")

    return result.returncode == 0


if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)
