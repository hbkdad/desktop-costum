# Phase 1 Research Plan

## Scope

Validate market demand and identify underserved segments for a Windows 11 desktop creation/runtime platform, before committing to a locked architecture or MVP scope.

## Competitor set

Stardock Fences, Rainmeter, Wallpaper Engine, Lively Wallpaper, Stardock DeskScapes, Binary Fortress DisplayFusion, Stardock Start11, Stardock Groupy — plus any additional alternative surfaced during research (e.g. RocketDock, ObjectDock, WinDynamicDesktop) should be added as a new section using the `competitor-research` skill rather than expanding this list speculatively.

## Sources per competitor

Official site/pricing page; Microsoft Store or Steam reviews; GitHub issue tracker (if open source or issue tracker is public); vendor support forum; Reddit; professional reviews (PCWorld, XDA Developers, How-To Geek, TechRadar, etc.).

## Deliverables and status

| Deliverable | Location | Status |
|---|---|---|
| Competitive matrix | `docs/research/competitor-matrix.md` | Done (2026-08-02) — 8 products, sourced |
| Market-gap report | `docs/research/market-gap-report.md` | Not started |
| Customer personas | `docs/product/personas.md` | Not started |
| Jobs-to-be-done | `docs/product/jobs-to-be-done.md` | Not started |
| Problem ranking | `docs/product/problem-ranking.md` | Not started |
| Willingness-to-pay hypotheses | `docs/product/pricing-hypotheses.md` | Not started |
| MVP positioning | `docs/product/mvp-positioning.md` | Not started |
| Market risks | folded into `backlog/risk-register.md` | Seeded, needs Phase 1 review pass |

## Method for remaining deliverables

1. **Market-gap report**: synthesize `competitor-matrix.md`'s "Cross-cutting patterns" and "Candidate underserved segments" sections into a standalone report with a recommendation on which gap(s) the MVP should target.
2. **Personas / JTBD**: derive from the complaint patterns already sourced (e.g. laptop users avoiding animated wallpapers over battery drain; multi-monitor power users fighting DPI bugs; users confused by Stardock's licensing) rather than inventing personas from scratch.
3. **Problem ranking**: rank candidate segments from the matrix by (a) how many competitors leave it unresolved, (b) implementation risk per the non-negotiable principles, (c) monetization potential.
4. **Willingness-to-pay hypotheses**: use observed competitor pricing (`competitor-matrix.md`) as anchors; note explicitly that Stardock's bundle pricing was inconsistent across sources and needs reconfirmation before being used as a benchmark.

## Non-goals

Do not copy proprietary UI, code, names, branding or assets from any researched competitor.
