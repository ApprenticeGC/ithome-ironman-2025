# ithome-ironman-2025 – Code Review Style Guide

This guide informs Gemini Code Review feedback for this multi-repo workspace. It complements project docs in `docs/` and `.github/copilot-instructions.md`.

## Principles
- Keep PRs small and scoped to a single RFC (see `docs/game-rfcs/`, `docs/flow-rfcs/`).
- Treat warnings as errors; code should build and tests should pass locally.
- Prefer minimal changes that follow existing structure and `.editorconfig`.

## .NET Console Game (if applicable)
- Use `Console.SetCursorPosition` for rendering; avoid heavy allocations per frame.
- Namespaces and layout follow project docs (e.g., `Breakout.Game.{System}` in breakout-like projects).
- Target .NET 8 features when they improve clarity without adding complexity.

## General C# Standards
- Naming: PascalCase for types/methods/properties; camelCase for locals; `_camelCase` for private fields.
- Brace style: Allman; always use braces.
- Null checks: `is null`/`is not null` pattern; avoid negation confusion.
- Exceptions: Do not use for control flow; catch narrowly and log/handle.

## Review Focus Areas
- Adherence to RFC scope and acceptance criteria.
- Build cleanliness: no warnings, no unused code.
- Tests: Include minimal tests for new behavior where practical.
- Console perf: stable frame timing, minimal per-frame allocations.

## What to Avoid
- Introducing external engines/libs unless RFC-specified.
- Large refactors or layout changes outside the RFC.
- Network/persistence features unless explicitly required.
