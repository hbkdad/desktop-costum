# Phase 3 Feasibility Prototype Backlog

Each prototype must report: purpose, implementation, test method, measurements, limitations, Windows build tested, recommendation, keep/discard decision (`desktop-host-prototype` skill). None of these merge into production architecture without an ADR review.

## Recommended first three

1. **Desktop attachment** (WorkerW/Progman-style hosting) — the highest-risk, most load-bearing unknown; every competitor researched has documented breakage here, and every other wallpaper/widget/container feature depends on this working (or on knowing definitively that it doesn't and the overlay fallback is primary).
2. **Desktop overlay fallback mode** — required in parallel with #1, not after it, because the non-negotiable principles mandate a fallback independent of undocumented shell behaviour; also de-risks the case where #1 turns out to be too fragile to be the primary path.
3. **Explorer restart recovery** — the second most common cross-competitor failure mode (crash/restart loops), and directly coupled to whichever hosting approach #1/#2 land on; needs to be proven before any persistent desktop state (containers, workspace layout) can be trusted.

Rationale: these three de-risk the single biggest architectural unknown — whether reliable desktop/shell integration is achievable at all on supported terms — before investing in wallpapers, widgets, or automation built on top of it.

## Full backlog

1. Desktop attachment — see above.
2. Desktop overlay fallback — see above.
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
