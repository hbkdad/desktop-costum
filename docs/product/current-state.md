---
updated: 2026-08-02
---

# Product Current State

**Name:** unresolved. `Desktop Runtime` is an internal placeholder only — never Forge-related.

**What exists as real, tested code:** the workspace schema — model types, versioned serializer with a migration seam, and a resolver that matches a saved workspace against currently-connected monitors without discarding layouts for absent ones (`src/DesktopRuntime.Core/Workspaces/`, 14 tests passing). No UI, no shell integration, no wallpaper/widget/automation runtime yet.

**What exists as throwaway prototypes** (isolated in `prototypes/`, never merged into the product): six Phase 3 feasibility probes, each run against a live Windows 11 desktop with a report — desktop attachment, overlay fallback (+2 spikes), Explorer restart recovery, monitor/DPI persistence, and adaptive-rendering signals.

**What's been researched:** sourced competitor matrix for 8 products plus the full Phase 1 deliverable set (market-gap report, personas, JTBD, problem ranking, pricing hypotheses, MVP positioning) — see `docs/research/` and `docs/product/`. Pricing hypotheses are explicitly unvalidated with real users.

**Key technical findings so far:**
- Rendering behind desktop icons is *not* reliably achievable on current Windows 11 builds. [ADR-0003](../architecture/adr/0003-desktop-hosting-strategy.md) settles on a three-tier strategy; video wallpaper is opportunistic, not guaranteed, and must degrade visibly.
- Monitor identity must be persisted on the device interface path; `\\.\DISPLAYn` and `HMONITOR` are unsafe.
- Fullscreen detection must trust `SHQueryUserNotificationState` over hand-rolled rect comparison (a naive version flagged maximized windows as fullscreen).

**Two open hardware validation gaps** (owner action, tracked in `backlog/risk-register.md`): no second monitor and no battery on the development machine, both blocking MVP acceptance criteria already written into the PRD.

**Provisional technology baseline:** unchanged from [ADR-0002](../architecture/adr/0002-provisional-technology-baseline.md); WinUI 3 / WebView2 / media stack still unvalidated.

**Authoritative phase status:** `.agents/state/current-phase.md`.
