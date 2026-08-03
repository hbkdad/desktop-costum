---
updated: 2026-08-02
---

# Current Phase

**Phase 0 — Repository and Operating System**: complete (this session). Pending owner action: review, commit, push, decide repo visibility.

**Phase 1 — Market Validation**: complete. Competitor matrix, market-gap report, personas, JTBD, problem ranking, pricing hypotheses, and MVP positioning all written and cross-referenced (see `docs/research/research-plan.md` status table). Not independently validated with real users/surveys — pricing hypotheses in particular are explicitly unvalidated.

**Phase 2 — Product Requirements**: in progress. PRD §1-4 and §13 populated from Phase 1 outputs (`docs/product/prd.md` v0.2). §5-12 deliberately left as outline pending Phase 3 prototype input (locking functional/non-functional requirements before knowing what's technically achievable risks specifying something infeasible).

**Phase 3 — Technical Feasibility Prototypes**: in progress, **all three recommended-first prototypes complete** (2026-08-02), all run against the live desktop, not just documented:
- Prototype 1 (desktop attachment): unreliable on this build → opportunistic only.
- Prototype 2 (overlay fallback) + Spikes A/B: z-order overlay structurally impossible behind Progman → discarded for wallpaper, repurposed for widgets; static `SystemParametersInfo` fallback proven reliable.
- Prototype 3 (Explorer restart recovery): Progman handle confirmed to go stale on restart (~2.7s manual recovery time); found Windows 11 has its own auto-recovery that can race with a manual relaunch — recovery adapter should detect-then-wait-then-relaunch, not kill-then-immediately-relaunch.

Desktop-hosting strategy finalized in [ADR-0003](../../docs/architecture/adr/0003-desktop-hosting-strategy.md). `docs/product/prd.md` §4/§13 updated. Next candidates: prototypes 9-11 (monitor/DPI persistence, fullscreen detection, adaptive rendering) per `backlog/prototype-backlog.md`.

## Phase 0 exit criteria

- [x] Repository structure created
- [x] `CLAUDE.md` / `AGENTS.md` in place
- [x] State files (`current-phase.md`, `decisions.md`, `handoff.md`) in place
- [x] Task backlog, definition of done, risk register, dependency register created
- [x] Initial CI workflow created
- [x] Coding standards / branch policy created (`CONTRIBUTING.md`)
- [x] Repository builds and tests report pass/fail (minimal `src`/`tests` skeleton — `dotnet build`/`dotnet test` on `DesktopRuntime.slnx` verified green this session: 1/1 tests passed)
- [ ] Changes committed and pushed by owner (not done automatically — see `handoff.md`)

## Next phase gate

Phase 1 is done when: competitive matrix, market-gap report, personas, JTBD, problem ranking, willingness-to-pay hypotheses, MVP positioning and market risks all exist in `docs/research/`. See `backlog/task-backlog.md`.
