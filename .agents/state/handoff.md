---
updated: 2026-08-02
---

# Handoff

## What was just done (this session)

Phase 0 scaffold, using `github.com/hbkdad/desktop-costum` (cloned empty) as the repository:

- `CLAUDE.md`, `AGENTS.md`, `README.md`, `.gitignore`, `CONTRIBUTING.md`.
- `.agents/state/{current-phase,decisions,handoff}.md`.
- `.agents/prompts/` — 7 role prompts (product-research, windows-architecture, security, ux, performance, commercial, documentation agents).
- `.claude/skills/` — 15 task-specific skills (windows-api-research, competitor-research, product-requirements, architecture-decision, desktop-host-prototype, winui-component, workspace-schema, widget-builder, wallpaper-runtime, automation-rule, security-review, performance-test, release-build, installer-validation, launch-marketing).
- `docs/product/{current-state,prd}.md`, `docs/research/{research-plan,competitor-matrix}.md`, `docs/architecture/adr/000{1,2}-*.md`.
- `backlog/{task-backlog,definition-of-done,risk-register,dependency-register,prototype-backlog}.md`.
- `.github/workflows/ci.yml` (Windows runner, .NET 10, build + test + upload trx results).
- Minimal buildable/testable skeleton: `DesktopRuntime.slnx` with `src/DesktopRuntime.Core` (net10.0 class library) + `tests/DesktopRuntime.Core.Tests` (xUnit) — verified locally: build succeeded (0 warnings/errors), 1/1 tests passed.
- Phase 1 competitor research completed and written to `docs/research/competitor-matrix.md`: 8 products (Fences, Rainmeter, Wallpaper Engine, Lively Wallpaper, DeskScapes, DisplayFusion, Start11, Groupy), sourced, with cross-cutting patterns and candidate underserved segments.

Nothing was committed or pushed — that requires the owner's go-ahead (see Open questions).

## Open questions / blocked on owner

1. **Commit and push?** Working tree has ~13 new top-level entries, all untracked, no commits yet. Say the word and this gets committed and pushed to `hbkdad/desktop-costum`.
2. **Repo visibility and branch protection on `main`** — not set by this session.
3. **Codex-compatible skill mirror** — the user asked that "both systems should share equivalent skill concepts" between Claude Code and Codex. Skills were authored only under `.claude/skills/` (the format this session is confident about). Codex's actual skill-discovery directory convention was not confirmed, so nothing was fabricated there — needs the owner (or a session with current Codex docs) to confirm the real path before mirroring/pointing to it.
4. **Product name** — still unresolved; do not pick one without the owner.
5. **Reddit-corroboration gap** in the competitor matrix (Stardock product line, DisplayFusion) — flagged in the file itself, not hidden.

## Recommended next task

Either:
- (a) Phase 1 close-out: write `docs/research/market-gap-report.md`, `docs/product/{personas,jobs-to-be-done,problem-ranking,pricing-hypotheses,mvp-positioning}.md` from the already-sourced competitor matrix (method already laid out in `docs/research/research-plan.md`), or
- (b) Jump to Phase 3, Prototype 1 (desktop attachment) using the `desktop-host-prototype` skill, if the owner wants a technical-feasibility signal before finishing market docs.

Default recommendation: (a) first — it's cheap (synthesis of research already done) and unblocks the PRD, whereas prototypes are expensive and should be pointed at the right target segment.

## Exact prompt to continue

> Read `.agents/state/current-phase.md` and this handoff. Complete Phase 1 close-out: write `docs/research/market-gap-report.md` and the persona/JTBD/problem-ranking/pricing-hypotheses/mvp-positioning docs listed in `docs/research/research-plan.md`, grounded in `docs/research/competitor-matrix.md`. Update `research-plan.md`'s status table and `.agents/state/{current-phase,decisions,handoff}.md` when done.
