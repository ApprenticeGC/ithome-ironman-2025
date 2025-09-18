# iThome Ironman 2025

[![CI](https://github.com/ApprenticeGC/ithome-ironman-2025/actions/workflows/ci.yml/badge.svg)](https://github.com/ApprenticeGC/ithome-ironman-2025/actions/workflows/ci.yml)
[![LOC](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/ApprenticeGC/ithome-ironman-2025/main/.github/badges/dotnet_game_loc.json)](./.github/badges/dotnet_game_loc.json)
[![CI minutes (total)](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/ApprenticeGC/ithome-ironman-2025/main/.github/badges/runner-usage.json)](https://github.com/ApprenticeGC/ithome-ironman-2025/actions/workflows/runner-usage-badge.yml)

Concise repo for notes, demos, and artifacts related to the 2025 iThome Ironman challenge. It uses a lightweight docs-first workflow with small, focused commits and simple automation.

## Repo layout

- `docs/` – Playbook, RFCs, and supporting docs
- `dotnet/` – .NET projects (libs, samples, tests)
- `build/` – Generated artifacts (ignored in development)
- `scripts/python/tests/` – Python test files for GitHub automation scripts
- `scripts/python/tools/` – Development utilities and scripts
- `scripts/python/requirements/` – Python dependency files
- `logs/` – Log files (git ignored)
- `scripts/` – Automation scripts by language/type
- `.github/scripts/` – GitHub Actions automation scripts
- `AGENTS.md` – Coding agent rules (GitHub Copilot, etc.)
- `CLAUDE.md` – Claude-specific agent rules

## Getting started

This is a test change to verify Gemini Code Assist integration.

Prerequisites depend on what you want to run. For .NET examples:

- Install .NET SDK 8.0+
- Optionally: a recent Node/Python runtime if examples appear later

Common actions:

- Build .NET: `dotnet build`
- Test .NET: `dotnet test`
- Test Python scripts: `python scripts/python/tools/run_tests.py`
- Setup development environment: `python scripts/python/tools/setup_dev.py`
- Install dependencies: `pip install -r scripts/python/requirements/test-requirements.txt`
- Run pre-commit hooks: `pre-commit run --all-files`

## Python Testing

The repository includes comprehensive testing for GitHub automation scripts:

- **Test Runner**: `python scripts/python/tools/run_tests.py` - Runs all validation checks
- **Unit Tests**: `python -m pytest scripts/python/tests/` - Runs pytest test suite
- **Pre-commit Hooks**: Automatic testing on commit via `.pre-commit-config.yaml`

### Test Structure:
- `scripts/python/tests/test_scripts.py` - Main test suite for automation scripts
- `scripts/python/tools/run_tests.py` - Test runner with multiple validation layers
- `scripts/python/production/` - Production automation scripts (tested by the above)

All Python scripts are validated for syntax, imports, and functionality before commits.

## Notes

- Main may be fast-moving initially; branch protection can be added later.
- Keep edits minimal and focused; avoid formatting-only diffs unless needed.

## Badges

[![CI](https://github.com/ApprenticeGC/ithome-ironman-2025/actions/workflows/ci.yml/badge.svg)](https://github.com/ApprenticeGC/ithome-ironman-2025/actions/workflows/ci.yml)
[![LOC](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/ApprenticeGC/ithome-ironman-2025/main/.github/badges/dotnet_game_loc.json)](./.github/badges/dotnet_game_loc.json)
[![CI minutes (total)](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/ApprenticeGC/ithome-ironman-2025/main/.github/badges/runner-usage.json)](https://github.com/ApprenticeGC/ithome-ironman-2025/actions/workflows/runner-usage-badge.yml)
