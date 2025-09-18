#!/usr/bin/env python3
"""
Test runner utility.
"""

import pytest


def run_tests():
    """Run all tests."""
    pytest.main(["-v"])


if __name__ == "__main__":
    run_tests()
