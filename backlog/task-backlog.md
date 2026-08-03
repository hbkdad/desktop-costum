# Task Backlog

Status as of 2026-08-02, end of this session.

## Phase 0 — Repository and operating system

- [x] Repository structure, `CLAUDE.md`, `AGENTS.md`, state files, decision log, task backlog, DoD, risk register, dependency register, initial CI workflow, coding standards, branch policy.
- [x] Minimal buildable/testable skeleton so the repo can build, test, and report failures (see `backlog/dependency-register.md` for what's still unvalidated, e.g. WinUI3/MSIX).
- [ ] Owner review, commit, and push (not done automatically this session).
- [ ] Decide repository visibility (public/private) and branch protection rules on `main`.

## Phase 1 — Market validation

- [x] Competitor matrix (8 products, sourced) — `docs/research/competitor-matrix.md`.
- [ ] Market-gap report — `docs/research/market-gap-report.md`.
- [ ] Customer personas — `docs/product/personas.md`.
- [ ] Jobs-to-be-done — `docs/product/jobs-to-be-done.md`.
- [ ] Problem ranking — `docs/product/problem-ranking.md`.
- [ ] Willingness-to-pay hypotheses — `docs/product/pricing-hypotheses.md`.
- [ ] MVP positioning — `docs/product/mvp-positioning.md`.
- [ ] Re-run competitor research with different tooling to close the Reddit-corroboration gap noted for the Stardock line and DisplayFusion.

## Phase 2 — Product requirements

- [ ] Populate `docs/product/prd.md` sections 1–13 (blocked on Phase 1 personas/MVP positioning for sections 1–4).

## Phase 3 — Technical feasibility prototypes

- [ ] Prototype 1: Desktop attachment.
- [ ] Prototype 2: Desktop overlay fallback.
- [ ] Prototype 3: Explorer restart recovery.
- [ ] Remaining 10 prototypes — see `backlog/prototype-backlog.md`.

## Not started

Phases 4–9 (architecture lock, MVP implementation, creator studio, advanced runtime, ecosystem, launch) — do not start until the phases above gate them, per `.agents/state/current-phase.md`.
