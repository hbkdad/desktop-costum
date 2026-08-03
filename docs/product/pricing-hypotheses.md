# Willingness-to-Pay Hypotheses

Anchored on observed competitor pricing from `docs/research/competitor-matrix.md`. **Caveat carried over from that file:** Stardock bundle/Object Desktop pricing was inconsistent across sources (roughly $20-40/yr consumer, $79.99/yr business, per one Jan-2025 blog post) and should be reconfirmed before being used as a hard benchmark — treat the ranges below as hypotheses to test, not settled numbers.

## Observed anchors

- **Free, ad-free, open-source:** Rainmeter, Lively Wallpaper. Sets a real "zero" floor this project must justify pricing above.
- **Low one-time, single feature:** Wallpaper Engine $4.99 (animated wallpaper only); Groupy $9.99/5 devices (window tabbing only); Start11 ~$6-7/device (Start menu/taskbar only).
- **Mid one-time, single feature:** DeskScapes ~$7-10/device; DisplayFusion Pro $34-49 (one-time, multi-monitor + wallpaper).
- **Recurring/bundle:** Fences $9.99/yr subscription or $29.99 perpetual (no free major-version upgrades on the perpetual tier); Object Desktop bundle ~$20-49/yr covering 4+ Stardock products together (pricing inconsistent across sources, per caveat above).

## Hypotheses to test (not yet validated with real users/surveys)

1. **H1 — Consolidation commands a premium over any single-feature competitor, but not over the sum of buying them separately.** A user who would otherwise buy Fences + a lightweight wallpaper tool + a widget tool separately (roughly $15-25 one-time-equivalent based on anchors above) should be willing to pay somewhere in that same total range for one coherent product — the win is fewer moving parts and no cross-app conflicts, not a lower price than any single component.
2. **H2 — Zero-cost local features are required to compete with Rainmeter/Lively, not optional.** This is already a non-negotiable decision (`.agents/state/decisions.md`), not just a hypothesis — but it implies monetization must come from something Rainmeter/Lively don't offer: creator marketplace revenue share, optional premium content packs, or a business/fleet tier (Phase 8-9), not a paywall on core workspace/wallpaper/widget functionality.
3. **H3 — A clearly-communicated one-time-feeling price beats a technically-similar subscription, given Stardock's documented licensing confusion.** Test a perpetual-license-with-optional-upgrade-pricing model against a pure subscription in early pricing experiments (Commercial Agent, Phase 9) rather than assuming subscription-by-default.
4. **H4 — The battery-conscious/laptop segment (Persona #2) will not pay extra for "resource discipline" as a labeled feature** — it's table stakes they expect to just work, not a premium upsell. Treat it as a retention/differentiation claim in marketing, not a separate pricing tier.
5. **H5 — Creator/marketplace revenue (Aesthetic Tinkerer persona, Phase 8) is the most plausible long-run monetization lever beyond the base product**, since none of the 8 competitors researched offer a unified creator economy across wallpapers + widgets + automation in one marketplace — Wallpaper Engine's Workshop is the closest analog but is scoped to wallpapers only and has documented moderation/fraud problems worth avoiding (see risk register).

## Next step

These hypotheses need actual pricing experiments (Phase 9, Commercial Agent) or at minimum lightweight validation (landing-page smoke test, survey) before being treated as fact — this document records testable hypotheses, not conclusions.
