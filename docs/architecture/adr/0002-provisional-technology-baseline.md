# 0002. Provisional technology baseline

## Status

Proposed — not locked. Revisit after Phase 3 feasibility prototypes and before Phase 4 architecture lock.

## Context

The project needs a starting technology stack to begin Phase 3 feasibility prototyping. Windows 11 is the primary and first-class target; the non-negotiable principles rule out kernel drivers, DLL injection, and unrestricted global hooks, and require a fallback overlay mode independent of undocumented shell behaviour.

## Decision

Adopt, provisionally: C#, the current supported .NET release, WinUI 3, Windows App SDK, Windows Community Toolkit, SQLite, WebView2, Windows Media Foundation (or a justified alternative media runtime), Direct3D/Win2D (or a justified alternative native rendering layer), MSIX and/or a justified installer strategy, xUnit, Playwright/WinAppDriver-compatible UI automation where practical, GitHub Actions for CI, GitHub Releases for early distribution.

Electron is explicitly excluded unless a future ADR demonstrates in writing that its benefits outweigh its memory, packaging, and native-integration costs versus this baseline.

## Validation status

Parts of this baseline have since been tested rather than assumed:

| Choice | Status |
|---|---|
| C# / current .NET | **Validated** — the whole solution builds and 271 tests pass on .NET 10 |
| Direct Win32 interop for desktop integration | **Validated** — see ADR-0003, ADR-0004 and the Phase 3 prototypes |
| WinUI 3 / Windows App SDK | **Partly validated, with a prerequisite.** The SDK restores from NuGet with no workload, but compiling XAML **fails without Visual Studio's MSIX/PRI packaging tooling** — `Microsoft.Build.Packaging.Pri.Tasks.dll` ships with VS, not the .NET SDK. Reproduced identically on SDK 1.6 and 1.7, and unaffected by `WindowsPackageType=None` / `EnableMsixTooling=false`. See `prototypes/winui-feasibility-probe/REPORT.md`. |
| SQLite, WebView2, media/rendering stack, MSIX | Still unvalidated |

**Decision on WinUI 3: keep it.** Requiring VS Build Tools with the Windows App SDK component is normal, documented WinUI practice, and GitHub's `windows-latest` image ships VS Build Tools. Reversing a framework choice on this evidence would be an overreaction. But the prerequisite is now a **hard requirement** recorded in `backlog/dependency-register.md`, and CI must be *verified* to build a WinUI project before the app shell lands rather than assumed to.

## Consequences

This baseline is unvalidated against rendering, media and packaging constraints until the remaining Phase 3 prototypes report back (see `backlog/prototype-backlog.md`). Do not treat the unvalidated rows above as locked.

One consequence has already proved its worth: because `DesktopRuntime.Core` carries no UI dependency, the WinUI toolchain gap blocks only the UI. The domain layer, the Windows adapter and every test build without it.
