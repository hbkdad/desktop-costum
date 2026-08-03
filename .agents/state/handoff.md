---
updated: 2026-08-03
---

# Handoff

## Where things stand

23 commits on `main` at `github.com/hbkdad/desktop-costum`. CI green throughout. Build clean, **271 tests passing — 262 unit + 9 integration against the real Windows APIs**.

Phases 0–1 complete. Phase 2 partial (PRD §1–4, §13). Phase 3: **8 of 13 prototypes**. Phase 4: **9 deliverables**. **Phase 5 Slice 1 underway.**

Start at [`docs/architecture/system-context.md`](../../docs/architecture/system-context.md) — diagrams plus an honest "what exists" table.

## Project shape

```
DesktopRuntime.Core         pure policy — no I/O, no clock reads, no OS calls
DesktopRuntime.DesktopHost  the Windows adapter — ALL P/Invoke lives here
```

`Core/Hosting/` holds three interfaces (`IMonitorProvider`, `IDesktopAttachmentProbe`, `IWallpaperSurface`) that keep it that way. Undocumented shell behaviour is confined to one class, `WindowsAttachmentProbe`, which only asks a question and is written never to throw.

| Area | The guarantee it enforces |
|---|---|
| `Workspaces/` | Absent monitors' layouts **deferred, not discarded**; atomic saves; activation is best-effort and fully reported |
| `Permissions/` | Closed catalog — no arbitrary-execution capability exists *to declare* |
| `Widgets/` | Validation yields a distinct type, so unvalidated manifests can't reach the runtime |
| `Automation/` | Rules validated **against the permission set** — no bypass |
| `Wallpapers/` | Video degrades to static **visibly**, never silently |
| `Recovery/` | Flapping shell still trips the breaker (reset needs *sustained* health) |
| `Resources/` | Declared budgets checkable; breaches must be sustained |
| `Packaging/` | Zip-slip, reserved names, ADS, bombs, allowlist; crypto deliberately abstracted |

## Design themes to preserve

1. **Structural over filtered** — security comes from things not *existing* (no execution capability, no shell action, allowlists not blocklists). Tests fail the build if one is introduced.
2. **Sustained over instantaneous** — recovery and resource accounting both refuse to act on one observation; the *reset* rule is the subtle part.
3. **Enforce on the trusted side** — permission checks live in the core service; the sandbox never decides its own limits.
4. **Report honestly rather than degrade silently** — `SupportsPerMonitor = false`, `PendingHostSupport`, degradation warnings. Every limitation is surfaced, never papered over.

## Validated on real hardware

- Behind-icon rendering is **not reliably achievable** on current Windows 11 → ADR-0003's tiers.
- **Sandboxing works**: job memory caps enforced, per-job accounting at ~0.17 ms, processes launch into AppContainers, default-deny denies (differential test) → ADR-0004 Accepted.
- Explorer handles go stale; Windows self-recovery races a manual relaunch.
- `SHQueryUserNotificationState` beats a hand-rolled rect check.

## Open — needs the owner

1. **Two hardware gaps** (`backlog/risk-register.md`): no second monitor, no battery. Both block MVP acceptance criteria already in the PRD; each maps to an MVP-primary persona. Still the only items I cannot do myself.
2. Repo visibility / branch protection on `main`.
3. Codex skill-mirror path — unconfirmed, nothing fabricated.
4. Product name — unresolved.
5. Pricing hypotheses unvalidated with users; Reddit-corroboration gap in the competitor matrix.

## There is now something runnable

```bash
dotnet run --project src/DesktopRuntime.Cli -- monitors
```

`src/DesktopRuntime.Cli` (`desktopruntime`) is the interim shell: `monitors`, `list`, `new`, `set-wallpaper`, `activate`, `delete`, `where`. Verified end to end on the real desktop — a *video* wallpaper request degraded to a still image and said so, exactly as PRD §13.7 requires.

## The UI is blocked on tooling, not design

WinUI 3 XAML will not compile here: it needs Visual Studio's MSIX/PRI tooling, which the .NET SDK does not ship, and no Visual Studio is installed. Reproduced on SDK 1.6 and 1.7. **Decision: keep WinUI 3** — the prerequisite is normal documented practice — but it is now a hard requirement in the dependency register and an owner action in the risk register. See `prototypes/winui-feasibility-probe/REPORT.md`.

This blocks *only* the UI. `Core`, the Windows adapter, the CLI and all 271 tests build without it.

## Recommended next task

Whichever of these fits the owner's situation:

1. **If VS Build Tools get installed** — build the WinUI app shell over `WorkspaceStore` + `WorkspaceActivator`, and *verify* CI builds it rather than assuming.
2. **Otherwise, keep extending the runtime through the CLI** — Slice 2 (desktop containers) is the natural next feature, and container layout can be modelled and tested without a UI even though rendering needs one.
3. **Lower value right now:** remaining Phase 4 paperwork (IPC contracts, database design, rendering pipeline), or `IDesktopWallpaper` COM for true per-monitor wallpaper — which would let `SupportsPerMonitor` become true and remove a real limitation users would notice.

## Exact prompt to continue

> Read `.agents/state/current-phase.md` and this handoff. If Visual Studio Build Tools with the Windows App SDK component are now installed, build the WinUI 3 app shell over `WorkspaceStore`/`WorkspaceActivator` and verify CI can build it. If not, continue through the CLI instead — implement `IDesktopWallpaper` COM in `DesktopRuntime.DesktopHost` so `SupportsPerMonitor` can become true, with integration tests that leave the user's wallpaper unchanged. Update state files, run `dotnet test`, and commit/push per standing authorization.
