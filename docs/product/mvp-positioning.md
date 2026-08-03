# MVP Positioning

Synthesizes `docs/research/market-gap-report.md`, `docs/product/personas.md`, `docs/product/jobs-to-be-done.md`, and `docs/product/problem-ranking.md` into a single positioning statement and MVP scope recommendation.

## Positioning statement

For Windows 11 users who want a personalized, organized desktop (organized icons, live wallpaper, widgets) but have been burned by customization tools breaking on Windows updates or draining laptop battery, **[Desktop Runtime placeholder]** is a desktop creation and runtime platform that is engineered from the ground up to survive Windows feature updates gracefully and to respect a laptop's battery — unlike Fences, Wallpaper Engine, Lively Wallpaper, DeskScapes, Start11, Groupy, and DisplayFusion, which have all documented breakage or resource-usage complaints in this exact category (see `docs/research/competitor-matrix.md`).

## MVP scope recommendation

Primary personas: **Multi-Monitor Power User, Battery-Conscious Laptop User, Desktop Organizer** (personas #1-3). Aesthetic Tinkerer (#4) and IT-Managed (#5) are explicitly deferred past MVP.

Primary JTBD covered: #1, #2, #3, #4, #6 from `docs/product/jobs-to-be-done.md`. JTBD #5 (visual creator tooling) is deferred to Phase 6.

Modules in MVP scope (cross-referencing the module list and Phase 5 vertical slices already defined): Workspace foundation, Desktop containers, Basic widgets, Wallpaper runtime (static + video only — web/shader/particle deferred), Recovery (layout snapshots, Explorer-restart recovery), Packaging. **Automation** (Slice 5) is a judgment call: keep in MVP only if Phase 3 prototypes show it doesn't add meaningful shell-integration risk; otherwise defer one slice.

## MVP acceptance framing (feeds `docs/product/prd.md` §13 once populated)

The MVP is not "done" until it can demonstrably survive the two failure modes competitors are most publicly criticized for: (1) a simulated Explorer restart, and (2) a Windows Insider/feature-update-class shell change — both from Phase 3 Prototype 3 (Explorer restart recovery) — and until every wallpaper/widget feature ships with a reported resource-impact number per the `performance-test` skill, not a "we'll optimize it later" promise.

## What this deliberately does not claim

Full consolidation against Stardock's whole product line (market-gap-report's #4 gap) is a Phase 5-7 execution outcome, not an MVP marketing claim — see `docs/research/market-gap-report.md` recommendation.
