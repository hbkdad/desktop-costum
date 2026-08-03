---
updated: 2026-08-02
---

# Handoff

## Where things stand

11 commits pushed to `main` on `github.com/hbkdad/desktop-costum`. **CI is green on every push** (verified via `gh run list`, not assumed). Build clean, 93/93 tests passing.

Phases 0 and 1 complete. Phase 2 partially complete (PRD §1–4, §13; §5–12 deliberately deferred). Phase 3: 6 of 13 prototypes done, including all three "recommended first". Phase 4: 3 of ~12 deliverables done.

## What exists as real, tested code (`src/DesktopRuntime.Core/`)

- **`Workspaces/`** — workspace model, versioned serializer with a migration seam, and `WorkspaceResolver` which matches a saved workspace against connected monitors and *defers rather than discards* layouts for absent ones (lossless undock/redock).
- **`Permissions/`** — closed capability catalog, default-deny `PermissionSet`, exact-host network scoping. No arbitrary-execution capability exists to declare, guarded by a test.
- **`Widgets/`** — manifest validator producing a distinct `ValidatedWidgetManifest` type, allowlist-based id constraints, required resource budget.

Each has a source-of-truth doc in `docs/architecture/`.

## What exists as throwaway prototypes (`prototypes/`, never merged)

Six probes, each run against the live desktop with a REPORT.md. Note these are **not** in `DesktopRuntime.slnx`, so CI does not compile them — deliberate, since they are throwaway.

## Findings that shaped the architecture

- **Behind-icon rendering is not reliably achievable** on current Windows 11. Neither WorkerW attachment nor any z-order overlay approach worked. [ADR-0003](../../docs/architecture/adr/0003-desktop-hosting-strategy.md) settles on three tiers; video wallpaper is opportunistic and must degrade *visibly*.
- **Monitor identity** must use the device interface path; `\\.\DISPLAYn` and `HMONITOR` are unsafe to persist.
- **Fullscreen detection** must trust `SHQueryUserNotificationState`; a naive rect heuristic flagged maximized windows as fullscreen.
- **Explorer restart**: handles go stale and must be re-acquired; Windows has its own auto-recovery that races a manual relaunch, so recovery should detect-then-wait-then-relaunch.

## Open — needs the owner

1. **Two hardware validation gaps** (in `backlog/risk-register.md`): no second monitor and no battery on this machine. Both block MVP acceptance criteria already written into the PRD, and each maps to an MVP-primary persona. These are the only items genuinely blocked on something I cannot do.
2. Repo visibility / branch protection on `main` — not set.
3. Codex skill-mirror path — still unconfirmed; nothing fabricated.
4. Product name — unresolved.
5. Pricing hypotheses are unvalidated with real users; Reddit-corroboration gap in the competitor matrix.

## Recommended next task

Continue Phase 4: wallpaper schema and automation rule schema (both follow the established pattern — doc + validated type + abuse tests), then the package format and process/crash-recovery model. Alternatively, populate PRD §5–12, which now has real technical grounding from ADR-0003 and the prototype findings.

## Exact prompt to continue

> Read `.agents/state/current-phase.md` and this handoff. Continue Phase 4: implement the wallpaper schema and automation rule schema in `src/DesktopRuntime.Core/` following the pattern set by `Workspaces/`, `Permissions/` and `Widgets/` (source-of-truth doc in `docs/architecture/`, validated type, abuse-case tests), then update state files, run `dotnet test`, and commit/push per standing authorization.
