# Market Gap Report

Source: synthesis of `docs/research/competitor-matrix.md` (8 products, sourced 2026-08-02). No new research in this document — see that file for citations.

## The core finding

Every one of the 8 competitors researched has a **documented Windows-update breakage incident**, and 5 of 8 (Fences, Wallpaper Engine, Lively Wallpaper, DeskScapes, Start11) have one tied specifically to the Windows 11 24H2 update — severe enough that Microsoft applied a formal compatibility safeguard hold blocking 24H2 upgrades on PCs running certain wallpaper apps (lifted Oct 2025). Explorer.exe crash/restart loops show up across nearly every product that hooks the shell, regardless of vendor, price, or whether the product is commercial or open source.

This is not a "one vendor is sloppy" problem — it is a structural property of how every one of these products integrates with the Windows shell today. That makes it the highest-leverage gap: it is unresolved by the entire category, not just a weak competitor.

## Gap ranking (detail in `docs/product/problem-ranking.md`)

1. **Shell-update resilience** — universal weakness, directly addressed by this project's own non-negotiable principles (adapter-isolated shell integration, mandatory overlay fallback, Explorer-restart recovery). We are already structurally positioned to win here if execution matches the principles.
2. **Resource-budget discipline for animated/rendering-heavy content** — near-universal complaint for anything doing continuous rendering (Wallpaper Engine, Lively, DeskScapes); the "auto-pause on fullscreen" bar every competitor already clears is not enough — explicit, reported, laptop-realistic budgets are the differentiator.
3. **Licensing clarity** — a Stardock-specific pain (Fences, Start11, DeskScapes, Groupy all show forum confusion about their subscription/perpetual hybrid model), less universal than #1/#2 but easy to win against simply by being clear and by honoring the "no subscription required for local features" principle already adopted.
4. **Consolidation** — Stardock sells 4+ overlapping desktop-customization products separately (with documented cross-app conflicts, e.g. Fences interacting badly with WindowBlinds/DeskScapes) instead of one coherent product. This is the segment closest to this project's actual scope (workspaces + containers + widgets + wallpapers + automation in one runtime) but it's a harder sell to validate pre-launch since it depends on execution breadth, not a single fixable pain point.

## Recommendation

MVP positioning should lead with **#1 (shell-update resilience) as the trust foundation** and **#2 (resource discipline) as the felt, everyday differentiator**, with **#3 (licensing clarity)** as a low-cost, high-goodwill commitment baked into pricing from day one. **#4 (consolidation)** is the long-run structural advantage but should be a Phase 5+ scope claim, not an MVP promise — an MVP that tries to out-execute Stardock's entire four-product line at once repeats the scope-creep risk already flagged in `backlog/risk-register.md`.

## What this report does not cover

Willingness-to-pay figures (see `docs/product/pricing-hypotheses.md`), persona-level detail (see `docs/product/personas.md`), and JTBD framing (see `docs/product/jobs-to-be-done.md`) are deliberately kept in separate documents per the token-optimization protocol — this file stays focused on the gap analysis itself.
