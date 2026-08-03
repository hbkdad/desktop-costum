# Phase 3 Feasibility Prototype Backlog

Each prototype must report: purpose, implementation, test method, measurements, limitations, Windows build tested, recommendation, keep/discard decision (`desktop-host-prototype` skill). None of these merge into production architecture without an ADR review.

## Recommended first three

1. **Desktop attachment** (WorkerW/Progman-style hosting) — **DONE (2026-08-02), result: unreliable on current build.** See `prototypes/desktop-attachment-poc/REPORT.md`. On Windows build 10.0.26200.0, the classic message+sibling-lookup technique did not locate a usable WorkerW window (15 pre-existing WorkerW windows were found, none where the technique expects; Progman enumerates bottommost, so nothing to find "after" it without the message provably inserting a new one). This corroborates the Windows 11 24H2 breakage pattern already documented across Wallpaper Engine/Lively/Fences/DeskScapes/Start11 in `docs/research/competitor-matrix.md`. **Decision: keep as an opportunistic enhancement, not the primary path.**
2. **Desktop overlay fallback mode** — **DONE (2026-08-02), including both follow-up spikes.** See `prototypes/desktop-overlay-fallback-poc/REPORT.md` and `prototypes/desktop-static-wallpaper-fallback-poc/REPORT.md`. Neither `HWND_BOTTOM` nor `SetWindowPos` with Progman's real handle can place a normal window behind Progman/icons — Windows structurally prevents it. **Z-order-based overlay is discarded as a wallpaper strategy** (repurposed instead as a front-layer widget/HUD building block). The proven, permanent fallback is `SystemParametersInfo(SPI_SETDESKWALLPAPER)` — tested, confirmed working, and confirmed to restore cleanly. **Final desktop-hosting design: WorkerW attach (opportunistic) → static-wallpaper API (guaranteed, static-only) → z-order overlay repurposed for widgets, not wallpaper.** Ready for an ADR.
3. **Explorer restart recovery** — **DONE (2026-08-02).** See `prototypes/explorer-restart-recovery-poc/REPORT.md`. Confirmed Progman's handle goes stale on restart (must be re-acquired, not assumed valid) and measured ~2.7s recovery time for a manual kill+relaunch. **Unexpected finding:** Windows 11 appears to have its own auto-recovery for a killed `explorer.exe`, which raced with this probe's manual relaunch and produced a transient duplicate process / stray Explorer window (self-resolved, no manual cleanup needed). Recommendation: the recovery adapter should detect-then-wait-briefly-then-relaunch, not kill-then-immediately-relaunch, to avoid this race.

Rationale: these three de-risk the single biggest architectural unknown — whether reliable desktop/shell integration is achievable at all on supported terms — before investing in wallpapers, widgets, or automation built on top of it.

**All three recommended-first prototypes are now complete (2026-08-02).** Next candidates: items 9 (monitor/DPI persistence) and 10/11 (fullscreen detection, adaptive rendering) — cheap, low-risk, and feed directly into the performance budgets already scoped in `backlog/dependency-register.md`.

## Full backlog

1. Desktop attachment — see above. **Status: done, see report.**
2. Desktop overlay fallback — see above. **Status: done (both spikes complete, three-tier design finalized).**
3. Explorer restart recovery — see above. **Status: done, see report.**
4. Per-monitor wallpaper hosting. **Status: not started.**
5. Video wallpaper playback. **Status: not started.**
6. WebView wallpaper (WebView2-hosted web content as wallpaper). **Status: not started.**
7. Widget rendering (host + at least one real widget end-to-end). **Status: not started.**
8. Drag-and-drop desktop containers. **Status: not started.**
9. Monitor and DPI configuration persistence (save/restore across reconnect/resolution change). **Status: not started — next candidate.**
10. Fullscreen detection (for wallpaper/widget render pausing). **Status: not started — next candidate.**
11. Adaptive rendering (frame-rate/quality scaling on battery or under load). **Status: not started — next candidate.**
12. Workspace restore (save → load → activate a full workspace). **Status: not started.**
13. Plugin process isolation (AppContainer/restricted-token proof of concept for untrusted widget/plugin code). **Status: not started.**
