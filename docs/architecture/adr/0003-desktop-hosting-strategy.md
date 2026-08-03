# 0003. Desktop hosting strategy: three-tier fallback for behind-icon rendering

## Status

Accepted, pending Phase 3 Prototype 3 (Explorer restart recovery) and further multi-monitor testing before being treated as fully locked for Phase 4 architecture lock.

## Context

The wallpaper/desktop-host module needs a way to render content behind desktop icons. `docs/architecture/adr/0002-provisional-technology-baseline.md` and `backlog/prototype-backlog.md` originally assumed two tiers: an opportunistic WorkerW attachment (the technique used by Wallpaper Engine, Lively Wallpaper, and similar tools) with a "desktop overlay" as its fallback, per the non-negotiable principle requiring a fallback independent of undocumented shell behaviour.

Three prototypes were built and run against a live Windows 11 desktop (build `10.0.26200.0`) to test this assumption, not just research it:

1. **`prototypes/desktop-attachment-poc`** — the classic Progman `0x052C` message + WorkerW-sibling-lookup technique. Result: unreliable. 15 pre-existing `WorkerW` windows were found on this system; none matched where the technique expects the freshly-spawned one, and Progman enumerates as the bottommost top-level window, so there was nothing conclusively new to find "after" it.
2. **`prototypes/desktop-overlay-fallback-poc`** — a plain top-level window pushed via `SetWindowPos(hwnd, HWND_BOTTOM, ...)`. Result: got close to Progman in Z-order but never behind it (245 of 372 vs. Progman at 247).
3. **Spike A** (same prototype, follow-up) — retried using Progman's *real* window handle as `hWndInsertAfter` instead of the `HWND_BOTTOM` pseudo-value, plus a position re-assertion after 1.5s. Result: identical outcome (245 of 372) — confirms this is not a `HWND_BOTTOM`-specific quirk; Windows does not allow a normal top-level window to be placed behind the shell/desktop window via `SetWindowPos` at all.
4. **`prototypes/desktop-static-wallpaper-fallback-poc`** (Spike B) — `SystemParametersInfo(SPI_SETDESKWALLPAPER)`, the actual supported OS wallpaper-setting API. Result: works reliably — set, read-back-confirmed, and restored cleanly (independently re-verified via the `HKCU:\Control Panel\Desktop\Wallpaper` registry value after the probe exited).

## Decision

Adopt a three-tier desktop-hosting strategy for the wallpaper host module:

1. **Tier 1 — WorkerW attachment (opportunistic).** Attempted first; supports animated/video/interactive content. Isolated behind an adapter per the non-negotiable principles — never a hard dependency. Detection must be event-driven (e.g. `SetWinEventHook` for window creation) rather than a single synchronous post-message enumeration, which this round of testing did not attempt and which may improve reliability; that's a follow-up spike, not a blocker for this decision.
2. **Tier 2 — Static wallpaper fallback (guaranteed).** `SystemParametersInfo(SPI_SETDESKWALLPAPER)` (single-monitor-spanning) or the `IDesktopWallpaper` COM interface (true per-monitor, not yet tested — follow-up spike) for static/slow-changing content when Tier 1 is unavailable. Proven reliable in this round of testing.
3. **Not a wallpaper tier — the overlay window investigated in Prototype 2 is repurposed.** Z-order-based "behind icons" placement is conclusively not achievable for a normal top-level window on this build, with either `HWND_BOTTOM` or a real handle. The click-through/non-activating window shape built for that prototype is retained as a building block for a **front-layer** widget/HUD surface (sits above desktop content, never steals focus from real applications) — a different module (widget host), not the wallpaper host.

## Consequences

- **Video/animated wallpaper is not guaranteed available** on every Windows build/configuration — it depends on Tier 1 succeeding. `docs/product/prd.md` §13 (MVP acceptance criteria) and §2 (non-goals) should reflect this explicitly rather than assuming universal availability; this is a product-scoping consequence, not just a technical footnote.
- The risk register entry for shell-integration fragility is downgraded from "assumed future risk" to "characterized present-day behavior with a finalized mitigation design," per `backlog/risk-register.md`.
- Two follow-up spikes are identified but not yet built: event-driven WorkerW detection (Tier 1 reliability improvement) and the `IDesktopWallpaper` COM interface (Tier 2 true per-monitor support). Neither blocks this ADR; both are tracked in `backlog/prototype-backlog.md`.
- This ADR should be revisited once Phase 3 Prototype 3 (Explorer restart recovery) is complete, since restart recovery behavior may differ meaningfully between a Tier 1 (attached window) and Tier 2 (OS wallpaper) state.
