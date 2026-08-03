---
updated: 2026-08-02
---

# Product Current State

**Name:** unresolved. `Desktop Runtime` is an internal placeholder only — never Forge-related.

**What exists as real, tested code** (`src/DesktopRuntime.Core/`, 250 tests passing): workspace schema, serializer and on-disk store with atomic saves; permission model; widget manifest validation; automation rules; wallpaper tier selection; shell recovery policy; resource accounting; package format validation. **No UI, no shell integration, no running processes yet** — everything is domain logic awaiting a host.

**What exists as throwaway prototypes** (isolated in `prototypes/`, never merged into the product): eight Phase 3 feasibility probes, each run against real hardware with a report — desktop attachment, overlay fallback (+2 spikes), Explorer restart recovery, monitor/DPI persistence, adaptive-rendering signals, job-object isolation, and AppContainer launch.

**What's been researched:** sourced competitor matrix for 8 products plus the full Phase 1 deliverable set (market-gap report, personas, JTBD, problem ranking, pricing hypotheses, MVP positioning) — see `docs/research/` and `docs/product/`. Pricing hypotheses are explicitly unvalidated with real users.

**Key technical findings so far:**
- Rendering behind desktop icons is *not* reliably achievable on current Windows 11 builds. [ADR-0003](../architecture/adr/0003-desktop-hosting-strategy.md) settles on a three-tier strategy; video wallpaper is opportunistic, not guaranteed, and must degrade visibly.
- The sandbox model works: job memory caps are genuinely enforced, per-job accounting is readable at ~0.17 ms, and a process launched into an AppContainer with no capabilities was denied a file it could read outside. [ADR-0004](../architecture/adr/0004-process-and-isolation-model.md) is validated.
- Monitor identity must be persisted on the device interface path; `\\.\DISPLAYn` and `HMONITOR` are unsafe.
- Fullscreen detection must trust `SHQueryUserNotificationState` over hand-rolled rect comparison (a naive version flagged maximized windows as fullscreen).

**Two open hardware validation gaps** (owner action, tracked in `backlog/risk-register.md`): no second monitor and no battery on the development machine, both blocking MVP acceptance criteria already written into the PRD.

**Provisional technology baseline:** unchanged from [ADR-0002](../architecture/adr/0002-provisional-technology-baseline.md); WinUI 3 / WebView2 / media stack still unvalidated.

**Authoritative phase status:** `.agents/state/current-phase.md`.
