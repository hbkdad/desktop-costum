---
updated: 2026-08-02
---

# Handoff

## Where things stand

19 commits on `main` at `github.com/hbkdad/desktop-costum`. **CI green throughout.** Build clean, **229/229 tests passing**.

Phases 0–1 complete. Phase 2 partial (PRD §1–4, §13). Phase 3: **7 of 13 prototypes**, all run against real hardware. Phase 4: **9 deliverables**.

Start at [`docs/architecture/system-context.md`](../../docs/architecture/system-context.md) for orientation — it has the diagrams and an honest "what exists" table.

## Real, tested code (`src/DesktopRuntime.Core/`)

| Area | The guarantee it enforces |
|---|---|
| `Workspaces/` | Layouts for absent monitors are **deferred, not discarded** — undock/redock is lossless |
| `Permissions/` | Closed catalog; no arbitrary-execution capability exists *to declare* |
| `Widgets/` | Validation yields a distinct type, so unvalidated manifests can't reach the runtime |
| `Automation/` | Rules validated **against the permission set** — automation can't bypass permissions |
| `Wallpapers/` | Video degrades to static **visibly**, never silently (ADR-0003 made executable) |
| `Recovery/` | Flapping shell still trips the breaker (reset needs *sustained* health) |
| `Resources/` | Declared budgets become checkable; breaches must be sustained, not instantaneous |
| `Packaging/` | Zip-slip, reserved names, ADS, bombs, content allowlist; crypto deliberately abstracted |

All pure policy — no I/O, no clock reads. That's why hour-long scenarios test in milliseconds, and equally why none of it has run against a live system.

## Design themes to preserve

1. **Structural over filtered** — security comes from things not *existing* (no execution capability, no shell action, allowlists not blocklists). Tests fail the build if one is introduced.
2. **Sustained over instantaneous** — recovery and resource accounting both refuse to act on one observation. In both, the *reset* rule is the subtle part: resetting eagerly defeats the breaker.
3. **Enforce on the trusted side** — permission checks live in the core service; the sandbox never decides its own limits.

## Validated on real hardware

- **Behind-icon rendering is not reliably achievable** on current Windows 11 (Prototypes 1, 2 + 2 spikes) → ADR-0003's three tiers.
- **Job memory caps are genuinely enforced**; per-job accounting readable at ~0.17 ms (Prototype 13) → ADR-0004's enforcement half stands.
- **Explorer handles go stale** and Windows self-recovery races a manual relaunch (Prototype 3).
- **`SHQueryUserNotificationState` beats a hand-rolled rect check** — the naive version flagged maximized windows as fullscreen (Prototype 10).

## Open — needs the owner

1. **Two hardware gaps** (`backlog/risk-register.md`): no second monitor, no battery. Both block MVP acceptance criteria already in the PRD, each maps to an MVP-primary persona. Still the only items I genuinely cannot do.
2. Repo visibility / branch protection on `main`.
3. Codex skill-mirror path — unconfirmed, nothing fabricated.
4. Product name — unresolved.
5. Pricing hypotheses unvalidated with users; Reddit-corroboration gap in the competitor matrix.

## Recommended next task

**Close the narrowed Phase 5 blocker**: launch a real process *into* an AppContainer with a restricted token (`CreateProcess` + `STARTUPINFOEX` + `PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES`). Everything the permission model promises rests on this, and it is the last unvalidated load-bearing assumption that is testable on this machine.

After that, Phase 5 Slice 1 (workspace foundation) can begin on validated ground — the schema and resolver already exist and are tested.

Remaining Phase 4 paperwork (IPC message contracts, database design, rendering pipeline) is lower value than the spike above and can follow.

## Exact prompt to continue

> Read `.agents/state/current-phase.md` and this handoff. Build a follow-up to `prototypes/process-isolation-poc` that launches a child process into an AppContainer with a restricted token and confirms the sandbox actually denies filesystem/network access it was not granted. Report per the `desktop-host-prototype` skill, update ADR-0004 and `backlog/prototype-backlog.md` with the result, then commit and push per standing authorization.
