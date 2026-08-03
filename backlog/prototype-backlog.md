# Phase 3 Feasibility Prototype Backlog

Each prototype must report: purpose, implementation, test method, measurements, limitations, Windows build tested, recommendation, keep/discard decision (`desktop-host-prototype` skill). None of these merge into production architecture without an ADR review.

## Recommended first three

1. **Desktop attachment** (WorkerW/Progman-style hosting) — **DONE (2026-08-02), result: unreliable on current build.** See `prototypes/desktop-attachment-poc/REPORT.md`. On Windows build 10.0.26200.0, the classic message+sibling-lookup technique did not locate a usable WorkerW window (15 pre-existing WorkerW windows were found, none where the technique expects; Progman enumerates bottommost, so nothing to find "after" it without the message provably inserting a new one). This corroborates the Windows 11 24H2 breakage pattern already documented across Wallpaper Engine/Lively/Fences/DeskScapes/Start11 in `docs/research/competitor-matrix.md`. **Decision: keep as an opportunistic enhancement, not the primary path.**
2. **Desktop overlay fallback mode** — **promoted to co-primary** as a direct consequence of #1's result (not merely "required in parallel" as originally scoped — it's now the default rendering path candidate).
3. **Explorer restart recovery** — still next; directly coupled to whichever hosting approach #1/#2 land on, needs to be proven before any persistent desktop state (containers, workspace layout) can be trusted.

Rationale: these three de-risk the single biggest architectural unknown — whether reliable desktop/shell integration is achievable at all on supported terms — before investing in wallpapers, widgets, or automation built on top of it.

## Full backlog

1. Desktop attachment — see above. **Status: done, see report.**
2. Desktop overlay fallback — see above. **Status: in progress, next.**
3. Explorer restart recovery — see above.
4. Per-monitor wallpaper hosting.
5. Video wallpaper playback.
6. WebView wallpaper (WebView2-hosted web content as wallpaper).
7. Widget rendering (host + at least one real widget end-to-end).
8. Drag-and-drop desktop containers.
9. Monitor and DPI configuration persistence (save/restore across reconnect/resolution change).
10. Fullscreen detection (for wallpaper/widget render pausing).
11. Adaptive rendering (frame-rate/quality scaling on battery or under load).
12. Workspace restore (save → load → activate a full workspace).
13. Plugin process isolation (AppContainer/restricted-token proof of concept for untrusted widget/plugin code).

Status of all 13: not started. This backlog entry itself is the Phase 0 deliverable; execution begins in Phase 3, after Phase 1 (market validation) and Phase 2 (PRD) inform which prototypes to prioritize beyond the recommended first three.
