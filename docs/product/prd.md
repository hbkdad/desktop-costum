# Product Requirements Document

**Version:** 0.2 (sections 1-4 and 13 populated; 5-12 remain outline pending Phase 3 prototype input)
**Status:** Phase 2 in progress

## Changelog

- 0.2 (2026-08-02): Populated vision, non-goals, target users, core workflows (§1-4) and MVP acceptance criteria (§13) from Phase 1 research (`docs/research/market-gap-report.md`, `docs/product/personas.md`, `docs/product/jobs-to-be-done.md`, `docs/product/mvp-positioning.md`). Sections 5-12 remain outline-only, deliberately, until Phase 3 prototypes validate what's technically achievable per module.
- 0.1 (2026-08-02): Initial section skeleton created during Phase 0 scaffold.

## 1. Product vision

A Windows 11 desktop creation and runtime platform — workspaces, desktop containers, widgets, animated wallpapers, automation, and creator tools as one coherent product — engineered from the outset to survive Windows feature updates gracefully and to respect laptop battery life, addressing the two failure modes every researched competitor has been publicly criticized for (see `docs/research/market-gap-report.md`). Internal placeholder name: `Desktop Runtime`. Never Forge-related branding.

## 2. Non-goals

- Not merely a wallpaper application, a widget application, a desktop icon organizer, a Rainmeter clone, a Fences clone, or a Wallpaper Engine clone — those are modules within a larger runtime, not the product itself.
- **MVP-specific non-goals** (deferred, not abandoned — see `docs/product/mvp-positioning.md`):
  - No creator marketplace, publishing, or monetization workflow at MVP (Aesthetic Tinkerer persona / Phase 6-8).
  - No fleet deployment, silent install, or centralized config management at MVP (IT-Managed persona / Phase 8-9).
  - No shader/particle/3D/audio-reactive wallpapers, desktop pets, or AI-assisted creation at MVP (Phase 7, "Advanced runtime," explicitly gated on MVP stability).
  - No web (WebView2) wallpapers at MVP — static and video only; web wallpapers carry the same untrusted-content rules as plugins and are deferred to reduce MVP security surface.
- **Permanent non-goals** (non-negotiable, not phase-gated): kernel drivers, DLL injection, unrestricted global hooks, requiring a subscription for local features, silently claiming a feature works without verification.

## 3. Target users

MVP-primary (see `docs/product/personas.md` for full detail):

1. **Multi-Monitor Power User** — fights DPI/resolution-mismatch bugs in current tools (DisplayFusion specifically); wants configuration to survive a dock/undock or monitor-change cycle without manual rework.
2. **Battery-Conscious Laptop User** — wants animated/video wallpaper aesthetics without the battery drain that makes them avoid Wallpaper Engine/Lively/DeskScapes today.
3. **Desktop Organizer** — wants stable icon/container organization (Fences-equivalent) without Stardock's documented licensing-model confusion.

Deferred past MVP: Aesthetic Tinkerer (creator/marketplace user), IT-Managed / small-business user.

## 4. Core workflows (MVP)

1. **Create and activate a workspace** — a named collection of container layout, widget placement, and wallpaper assignment that can be saved, loaded, and switched.
2. **Organize desktop icons into containers** — create/move/resize/rename/collapse a container; folder-portal a folder's live contents onto the desktop without opening Explorer.
3. **Set a wallpaper (static or video) per monitor** — with an enforced, reported resource budget and automatic quality/frame-rate reduction on battery power (Job #3 in `docs/product/jobs-to-be-done.md`).
4. **Add a basic widget** (clock, CPU/memory/storage monitor, notes, app launcher, recent files) to the desktop.
5. **Recover automatically from Explorer restart or a Windows update-related shell disruption** without the user having to manually rebuild their layout (Job #2 and #6).
6. **Reconnect a monitor or dock/undock a laptop** and have containers, widgets, and per-monitor wallpapers reappear correctly (Job #1).

## 5. Functional requirements

TODO — one subsection per module (see module list in `AGENTS.md`/`docs/architecture/`). **Blocked on Phase 3**: do not lock detailed functional requirements per module (especially desktop host / wallpaper host / widget host) until the relevant feasibility prototype (`backlog/prototype-backlog.md`) has reported what's technically achievable — locking requirements before that risks specifying something the shell-integration prototypes prove infeasible.

## 6. Non-functional requirements

TODO — ties to performance budgets in `backlog/dependency-register.md` and the benchmark profiles in `.agents/prompts/performance-agent.md`. Blocked on the same Phase 3 input as §5.

## 7. Accessibility requirements

TODO — ties to the `winui-component` skill's accessibility checklist (automation names/roles, keyboard nav, text scaling, high contrast). Populate alongside §5 once concrete components are being specified.

## 8. Telemetry boundaries

TODO — must respect the offline-first, privacy-conscious non-negotiables; define what is/isn't collected before any telemetry code is written, not after.

## 9. Privacy requirements

TODO — coordinate with the Security Agent's threat model (`.agents/prompts/security-agent.md`) once the plugin/package permission model is drafted.

## 10. Offline behaviour

Core principle already locked (non-negotiable #11: core application must be functional offline). Detailed per-module offline/degraded-mode behavior TODO, populated alongside §5.

## 11. Licensing assumptions

Non-negotiable #12 already locked: no subscription required for local features. Ties to `docs/product/pricing-hypotheses.md` H2/H3. Detailed tier definitions TODO — Commercial Agent, closer to Phase 9.

## 12. Success metrics

TODO — define once MVP acceptance criteria (§13) are validated as measurable; avoid metrics that can't actually be instrumented under the telemetry boundaries in §8.

## 13. MVP acceptance criteria

Derived from `docs/product/mvp-positioning.md`. The MVP is not done until:

1. All six core workflows in §4 work end-to-end on a real Windows 11 desktop (not just in isolation/mocked).
2. The system survives a simulated Explorer restart (Phase 3 Prototype 3) without losing workspace/container/widget/wallpaper state.
3. The system survives at least one Windows-update-class shell disruption test (informed by the specific breakage patterns documented per-competitor in `docs/research/competitor-matrix.md`, e.g. the 24H2-class failures) via the overlay fallback mode, without silent failure.
4. Every wallpaper and widget feature reports a real, measured resource-impact number (`performance-test` skill) for idle, active, and fullscreen-paused states — no feature ships with an unmeasured performance claim.
5. Multi-monitor / DPI-change / dock-undock is tested and passes on at least a 2-monitor mixed-DPI configuration (Multi-Monitor Power User persona is the acceptance bar).
6. No MVP feature depends solely on an undocumented Windows behavior without a working, tested fallback (non-negotiable #3/#5).
