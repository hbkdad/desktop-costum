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

Desktop-hosting strategy finalized in [ADR-0003](../../docs/architecture/adr/0003-desktop-hosting-strategy.md). `docs/product/prd.md` §4/§13 updated.

Prototypes 9, 10, 11 also complete (2026-08-02) — **6 of 13 backlog items now have run-against-real-hardware reports**:
- Prototype 9 (monitor/DPI persistence): device interface path identified as the only viable persistence key; DPI-awareness-before-geometry-read established as a startup invariant. Reconnect stability **not empirically verified** (single monitor on test machine).
- Prototype 10 (fullscreen detection): found and fixed a real false positive (maximized window read as fullscreen). Supported API authoritative over hand-rolled heuristic. ~0.7 ms/check.
- Prototype 11 (adaptive rendering): power API plumbing verified; **on-battery path unverified** (no battery on test machine).

**Two hardware validation gaps now tracked in `backlog/risk-register.md` as owner actions** (no second monitor, no battery) — both block MVP acceptance criteria that are already written into the PRD.

**Phase 4 — Architecture Lock**: in progress. Two deliverables complete, both designed *and implemented as real, tested code* (54 tests passing overall):
- **Workspace schema** (`docs/architecture/workspace-schema.md` → `src/DesktopRuntime.Core/Workspaces/`): model, versioned serializer with migration seam, and a monitor resolver that defers rather than discards layouts for disconnected monitors.
- **Permission model** (`docs/architecture/permission-model.md` → `src/DesktopRuntime.Core/Permissions/`): closed capability catalog with no arbitrary-execution capability by construction, default-deny evaluation, exact-host network scoping with abuse-case tests.

Remaining Phase 4 deliverables: widget schema, wallpaper schema, automation schema, package format, IPC contracts, database design, process/crash-recovery model, rendering pipeline, resource-accounting model, system context/process diagrams.

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
