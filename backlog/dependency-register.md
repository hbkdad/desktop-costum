# Dependency Register

| Dependency | Purpose | Status | Risk note |
|---|---|---|---|
| .NET SDK (10.0.204 installed locally, this session) | Build/runtime | Available locally | Confirm the CI runner image matches; pin an explicit SDK version once the real product TFM is chosen (Phase 3/4) rather than floating on "latest" |
| WinUI 3 / Windows App SDK | UI framework | **Partly validated — blocked on this machine.** NuGet restore works with no workload, but XAML compilation fails without VS packaging tooling. See `prototypes/winui-feasibility-probe/REPORT.md`. | **HARD PREREQUISITE: Visual Studio Build Tools with the Windows App SDK / MSIX packaging component.** Not optional, and not supplied by the .NET SDK. No Visual Studio is installed on the current development machine, so the UI layer cannot be built here until it is. CI must be *verified* to have it before the app shell lands. |
| Windows Community Toolkit | UI/behaviors helpers | Not yet validated | Low risk, well-maintained |
| SQLite | Local storage | Not yet integrated | Low risk |
| WebView2 | Web wallpapers, plugin web content | Not yet integrated | Runtime must be present on target machines (evergreen, usually preinstalled on Win11) — confirm and document fallback if absent |
| Windows Media Foundation (or justified alternative) | Video wallpaper playback | Not yet chosen | Needs a Phase 3 prototype decision; alternative media runtimes must be justified in an ADR if MF is rejected |
| Direct3D / Win2D (or justified alternative) | Native rendering (shader/particle wallpapers) | Not yet chosen | Same as above — Phase 3 prototype + ADR if an alternative is picked |
| MSIX (or justified installer strategy) | Packaging/install/update/repair | Not yet validated | Needs a signing certificate before real releases; document the packaging ADR when decided |
| xUnit | Test framework | In use (this session's skeleton) | None known |
| Playwright / WinAppDriver-compatible automation | UI test automation | Not yet integrated | Evaluate during Phase 5 slice work |
| GitHub Actions | CI | Minimal `ci.yml` created this session (build + test on `windows-latest`) | Needs a Windows App SDK-capable runner once WinUI3 projects exist; watch minutes/cost if repo is private |
| GitHub Releases | Early distribution | Not yet used | Requires explicit user confirmation before any public release/push |
| Code-signing certificate | MSIX trust, avoiding SmartScreen friction | Not acquired | Needed before Phase 9 launch; owner decision (cost, EV vs. standard) |

Update when a dependency is added, validated, or replaced — link the relevant ADR when a choice changes.
