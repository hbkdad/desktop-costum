---
updated: 2026-08-02
---

# Handoff

## What was just done (this session, continued after Phase 0 report)

User granted standing authorization to commit and push, and asked to proceed autonomously, prompting only when truly blocked. Since then, pushed 5 commits to `main` on `github.com/hbkdad/desktop-costum`:

1. Phase 0 scaffold (repo structure, `CLAUDE.md`/`AGENTS.md`, 15 skills, 7 agent prompts, state files, backlog, CI, minimal buildable `.NET` skeleton, sourced 8-product competitor matrix).
2. Phase 1 close-out: `market-gap-report.md`, `personas.md` (5 personas, 3 MVP-primary), `jobs-to-be-done.md`, `problem-ranking.md`, `pricing-hypotheses.md`, `mvp-positioning.md` — all synthesized from the already-sourced competitor matrix.
3. PRD v0.2: populated §1-4 and §13 (vision, non-goals, target users, core workflows, MVP acceptance criteria); §5-12 deliberately left outline-only pending Phase 3 input.
4. **Phase 3 Prototype 1** (desktop attachment / WorkerW): built, ran against the live desktop. **Result: unreliable on this build** — corroborates the Windows 11 24H2 breakage pattern already found in competitor research.
5. **Phase 3 Prototype 2** (overlay fallback): built, ran against the live desktop. **Result: also insufficient as implemented** — a plain `HWND_BOTTOM` window gets close to but not behind Progman in Z-order.

**This is the most important thing to know:** neither of the two originally-assumed rendering strategies works yet, on real empirical testing, not just research. The desktop-host module now needs a three-tier fallback plan instead of two. See `.agents/state/decisions.md` (top entry) and both `prototypes/*/REPORT.md` files.

## Open questions / blocked on owner (unchanged from before, still not blocking technical work)

1. Repo visibility and branch protection on `main` — not set.
2. Codex-compatible skill mirror path — not fabricated, still unconfirmed.
3. Product name — still unresolved.
4. Reddit-corroboration gap in the competitor matrix — flagged, not fixed.

## Recommended next task

Two follow-up spikes flagged in `prototypes/desktop-overlay-fallback-poc/REPORT.md`, neither built yet:
1. Retry the overlay using `SetWindowPos(hwnd, progmanHandle, ...)` (real handle, not the `HWND_BOTTOM` pseudo-value) as tier 2 of the fallback chain.
2. Build the `SystemParametersInfo(SPI_SETDESKWALLPAPER)` static-image path as the guaranteed-available tier 3.

After that: Phase 3 Prototype 3 (Explorer restart recovery), then decide whether to keep going through the remaining 10 prototypes or pause for an ADR locking in the (now three-tier) desktop-hosting architecture given what's been learned.

## Exact prompt to continue

> Read `.agents/state/current-phase.md` and this handoff. Build the two follow-up spikes flagged in `prototypes/desktop-overlay-fallback-poc/REPORT.md` (Progman-handle `SetWindowPos` variant; `SystemParametersInfo` static fallback tier), test both against the live desktop, then write Phase 3 Prototype 3 (Explorer restart recovery). Update `backlog/prototype-backlog.md`, `backlog/risk-register.md`, and `.agents/state/{current-phase,decisions,handoff}.md` after each. Commit and push after each prototype, per standing authorization.
