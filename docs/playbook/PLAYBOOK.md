# Agent Playbook (Canonical)

This playbook defines the **what-to-do rules** for all agents.

---

## Process {#process}
- Use the RFC → Issue → PR → Review → Merge flow.
- Branch names: `feat/`, `fix/`, `chore/`.
- Commits follow Conventional Commits (`feat: …`, `fix: …`).

---

## Coding Standards {#coding-standards}
- Language version: C# 12 (Unity/.NET 8 baseline).
- Enforce analyzers & linters via CI.
- All code must have tests when logic is added/changed.

---

## File Focus {#file-focus}
- Prefer editing files in `/src`, `/tests`, `/.github/workflows`.
- Avoid `third_party/**`, `build/**`, and generated code.

---

## PR Review Checklist {#pr-checklist}
- ✅ CI green (build + test).
- ✅ Tests updated or added.
- ✅ Docs or README updated if user-facing change.
- ✅ No secrets committed.

---

## Security {#security}
- Do not commit API keys or secrets.
- Use GitHub Secrets for tokens.
