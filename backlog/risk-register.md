# Risk Register

| Risk | Category | Likelihood | Impact | Mitigation | Status |
|---|---|---|---|---|---|
| Desktop-attachment technique (WorkerW/Progman-style) breaks on a future Windows feature update | Technical | High — every wallpaper/icon-organizer competitor researched has a documented Windows-update breakage incident, several tied to 24H2 specifically | High — core hosting mechanism | Isolate behind an adapter; ship a working overlay fallback mode from day one; track Windows Insider builds | Open |
| Explorer.exe crash/restart loops from shell hooking | Technical | High — reported across Fences, Start11, DisplayFusion, Lively Wallpaper regardless of vendor | High — visible, trust-destroying failure | Explorer-restart recovery as a Phase 3 prototype and a permanent regression test; process isolation so a crash doesn't take down the runtime | Open |
| Malicious or resource-abusive third-party widget/wallpaper/plugin package | Security | Medium | High | Default-deny capability manifests, AppContainer/process isolation, package signing and review pipeline (Phase 8) | Open |
| High idle/active CPU-GPU-battery usage drives uninstalls | Product/Technical | High — the single most common complaint across every animated-wallpaper competitor researched | Medium-High | Explicit performance budgets, fullscreen pause, adaptive frame rate, `performance-test` gate in CI | Open |
| Licensing/pricing confusion (Stardock's subscription-vs-perpetual pattern) | Commercial | Medium | Medium | Simple, clearly-communicated pricing model; no subscription required for local features (non-negotiable) | Open |
| Marketplace fraud / content moderation (adult content, AI-slop, unauthorized reposts) — Wallpaper Engine's Workshop shows this is a live problem at scale | Trust & Safety | Medium (grows with marketplace size) | Medium | Package validator + review workflow + reporting/moderation flow (Phase 8) | Open |
| Scope creep vs. MVP — product spans wallpapers, widgets, containers, automation, creator tools simultaneously | Execution | High | High | Hold the vertical-slice MVP plan (`docs/product/prd.md` §13 once written); Documentation Agent keeps PRD MVP scope explicit and separate from backlog ideas | Open |
| Product name / branding clearance not yet done | Legal/Brand | Medium | Low-Medium (blocks marketing, not engineering) | Resolve before Phase 9 launch prep; must avoid Forge-related naming per explicit constraint | Open |
| GitHub Actions Windows runner cost/availability for a WinUI3 + MSIX build pipeline | Execution | Low-Medium | Medium | Validate CI on `windows-latest` early (this session's minimal skeleton is the first proof point); watch runner minutes if the repo goes private | Open |
| Repository research gap: limited independent (Reddit) corroboration for Stardock product line and DisplayFusion complaints | Research | N/A | Low — affects confidence, not shipped product | Re-run `competitor-research` with different search tooling before relying on these sections for pricing/positioning decisions | Open |

Update this table as risks are mitigated, newly discovered, or re-scored — do not let it go stale across phases.
