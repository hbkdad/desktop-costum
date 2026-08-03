---
updated: 2026-08-02
---

# Current Phase

**Phase 0 — Repository and Operating System**: complete (this session). Pending owner action: review, commit, push, decide repo visibility.

**Phase 1 — Market Validation**: complete. Competitor matrix, market-gap report, personas, JTBD, problem ranking, pricing hypotheses, and MVP positioning all written and cross-referenced (see `docs/research/research-plan.md` status table). Not independently validated with real users/surveys — pricing hypotheses in particular are explicitly unvalidated.

**Phase 2 — Product Requirements**: in progress. PRD §1-4 and §13 populated from Phase 1 outputs (`docs/product/prd.md` v0.2). §5-12 deliberately left as outline pending Phase 3 prototype input (locking functional/non-functional requirements before knowing what's technically achievable risks specifying something infeasible).

**Phase 3 — Technical Feasibility Prototypes**: 8 of 13 complete, all run against real hardware. Most recent: **Prototype 13 + 13b (process isolation)** — the sandbox model is now validated end-to-end and the Phase 5 blocker is cleared.

All three recommended-first prototypes complete (2026-08-02):
- Prototype 1 (desktop attachment): unreliable on this build → opportunistic only.
- Prototype 2 (overlay fallback) + Spikes A/B: z-order overlay structurally impossible behind Progman → discarded for wallpaper, repurposed for widgets; static `SystemParametersInfo` fallback proven reliable.
- Prototype 3 (Explorer restart recovery): Progman handle confirmed to go stale on restart (~2.7s manual recovery time); found Windows 11 has its own auto-recovery that can race with a manual relaunch — recovery adapter should detect-then-wait-then-relaunch, not kill-then-immediately-relaunch.

Desktop-hosting strategy finalized in [ADR-0003](../../docs/architecture/adr/0003-desktop-hosting-strategy.md). `docs/product/prd.md` §4/§13 updated.

Prototypes 9, 10, 11 also complete (2026-08-02) — **6 of 13 backlog items now have run-against-real-hardware reports**:
- Prototype 9 (monitor/DPI persistence): device interface path identified as the only viable persistence key; DPI-awareness-before-geometry-read established as a startup invariant. Reconnect stability **not empirically verified** (single monitor on test machine).
- Prototype 10 (fullscreen detection): found and fixed a real false positive (maximized window read as fullscreen). Supported API authoritative over hand-rolled heuristic. ~0.7 ms/check.
- Prototype 11 (adaptive rendering): power API plumbing verified; **on-battery path unverified** (no battery on test machine).

**Two hardware validation gaps now tracked in `backlog/risk-register.md` as owner actions** (no second monitor, no battery) — both block MVP acceptance criteria that are already written into the PRD.

**Phase 4 — Architecture Lock**: in progress. Nine deliverables complete (229 tests passing overall). One is design-only:
- **Process/isolation model** ([ADR-0004](../../docs/architecture/adr/0004-process-and-isolation-model.md), now **Accepted**) + [`system-context.md`](../../docs/architecture/system-context.md) orientation doc. **Validated end-to-end by Prototypes 13 and 13b** — job memory caps enforced, per-job accounting readable at ~0.17 ms, processes launchable into an AppContainer, and default-deny confirmed denying via a differential test. **The Phase 5 blocker is cleared.**

The rest are implemented as real, tested code:
- **Package format + signing** (`docs/architecture/package-format.md` → `src/DesktopRuntime.Core/Packaging/`): the marketplace trust boundary. Zip-slip/reserved-name/ADS/bomb defences, content-type allowlist, signing policy with cryptography deliberately abstracted rather than hand-rolled.
- **Resource accounting** (`docs/architecture/resource-accounting.md` → `src/DesktopRuntime.Core/Resources/`): makes declared widget budgets checkable; sustained-breach detection plus aggregate totals. Addresses the #2 market gap.
- **Shell recovery model** (`docs/architecture/recovery-model.md` → `src/DesktopRuntime.Core/Recovery/`): detect→wait→relaunch→backoff→safe-mode policy; the attempt counter resets only on *sustained* health, so a flapping shell still trips the breaker. Directly addresses the #1 market gap.
- **Automation rule schema** (`docs/architecture/automation-schema.md` → `src/DesktopRuntime.Core/Automation/`): closed trigger/action catalogs, and — the key property — rules are validated **against the package's permission set**, so automation cannot bypass the permission model.
- **Wallpaper tier selection** (`src/DesktopRuntime.Core/Wallpapers/`): makes ADR-0003 executable; video degrades to static *visibly*, never silently.
- **Widget manifest schema** (`docs/architecture/widget-manifest.md` → `src/DesktopRuntime.Core/Widgets/`): validator producing a distinct validated type, allowlist-based id constraints, required resource budget, abuse-case tests.
- **Workspace schema** (`docs/architecture/workspace-schema.md` → `src/DesktopRuntime.Core/Workspaces/`): model, versioned serializer with migration seam, and a monitor resolver that defers rather than discards layouts for disconnected monitors.
- **Permission model** (`docs/architecture/permission-model.md` → `src/DesktopRuntime.Core/Permissions/`): closed capability catalog with no arbitrary-execution capability by construction, default-deny evaluation, exact-host network scoping with abuse-case tests.

Remaining Phase 4 deliverables: IPC message contracts (shape sketched in ADR-0004, not specified), database design, rendering pipeline.

**Phase 5 — MVP Implementation**: started, unblocked by the isolation validation above.
- **Slice 1 (workspace foundation)**: `WorkspaceStore` — atomic save (temp file → flush-to-disk → atomic move), load, list, delete, import/export. Filenames derive from the workspace **id**, never its user-supplied name. One corrupted file cannot make other workspaces unreachable. Import assigns a new id so it can never silently overwrite.
- **Workspace activation**: `WorkspaceActivator` composes the resolver, tier selection and the wallpaper surface. Best-effort and fully reported — one missing wallpaper file does not abandon the rest, and every degradation produces a warning (PRD §13.7).
- **First OS adapter**: `src/DesktopRuntime.DesktopHost` (net10.0-windows) implements the hosting abstractions with the P/Invoke validated in Phase 3 — monitor enumeration with stable identity, the attachment probe (undocumented behaviour confined to this one class, never throwing), and the static wallpaper surface which honestly reports `SupportsPerMonitor = false`.
- **271 tests**: 262 unit + **9 integration tests that exercise the real Windows APIs**, including re-applying the already-set wallpaper to prove the production write path works without changing the desktop.
- **Interim CLI shell** (`src/DesktopRuntime.Cli`, `desktopruntime`): `monitors`, `list`, `new`, `set-wallpaper`, `activate`, `delete`, `where`. **The first runnable artifact.** Verified end to end on the real desktop: requested a *video* wallpaper → ADR-0003 tier selection found attachment unavailable → degraded to a still image → applied through the real Windows API → the degradation surfaced as a visible warning, exactly as PRD §13.7 requires.
- Remaining in Slice 1: applying container/widget layout (needs a UI surface), and the real app shell.

**Blocked on tooling, not design:** the WinUI 3 app shell cannot be built on this machine — XAML compilation needs Visual Studio's MSIX/PRI tooling, which the .NET SDK does not ship and no Visual Studio is installed. See `prototypes/winui-feasibility-probe/REPORT.md`; ADR-0002 and the dependency/risk registers are updated. Notably this blocks *only* the UI: `Core`, the Windows adapter, the CLI and all 271 tests build without it — which is what keeping the domain layer UI-free was for.

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
