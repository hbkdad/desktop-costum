# Customer Personas

Derived from complaint/usage patterns actually observed in `docs/research/competitor-matrix.md` — not invented from scratch. Each persona names which competitor(s) it currently uses/tolerates and why that's an incomplete fit.

## 1. The Multi-Monitor Power User

Runs 2-3+ monitors, often mixed resolution/DPI (docked laptop + externals, or a mixed old/new monitor setup). Currently patches together DisplayFusion (for per-monitor taskbars/wallpaper) plus maybe Fences (icon organization), and fights DisplayFusion's own documented DPI-scaling bugs (secondary-monitor taskbar misrendering, full-screen apps landing on the wrong monitor at 200% scaling) — the tool bought specifically to fix multi-monitor pain has its own multi-monitor bugs. Values: reliability over feature depth; will pay for a tool that "just works" across a monitor change/dock-undock cycle without reconfiguration.

## 2. The Battery-Conscious Laptop User

Wants their desktop to look good (video/animated wallpaper, widgets) but has learned to avoid Wallpaper Engine/Lively/DeskScapes because of documented CPU/GPU load and battery drain — community guidance for all three is "switch to static wallpaper or a power-saving profile," i.e., turn the feature off to get battery life back. Currently either doesn't use animated wallpapers at all, or uses them and is quietly annoyed at reduced battery life. Values: an explicit, honest resource budget and real adaptive behavior on battery — not just a fullscreen-pause checkbox every competitor already has.

## 3. The Desktop Organizer

Uses Fences (or wants to) to declutter icons into named groups with folder portals. Frustrated by the 5→6 paid-upgrade cycle, forum-documented licensing/upgrade-price confusion, and (for a subset) CPU spikes while dragging fences or occasional crashes. Wants icon/file organization, launchers, and folder portals as a stable, boring, always-on utility — not something they have to think about upgrade cycles for. Values: predictable one-time-feeling pricing; will not tolerate frequent breakage in something this foundational to daily desktop use.

## 4. The Aesthetic Tinkerer

Rainmeter or Wallpaper Engine user who wants deep visual customization (skins, scenes, shaders, live wallpapers) and is willing to invest real time. Rainmeter's own community literally has a thread titled "Too complicated and difficult to use" — this persona wants Rainmeter-level depth without raw `.ini`-file editing as the only path in. Also the segment most likely to become a **creator** (publishing skins/widgets/wallpapers to a marketplace) rather than only a consumer. Values: a visual/no-code path to the same depth power users get from text-based config, plus, for the subset who create content, a real distribution/monetization path (which none of the 8 competitors researched offer in a unified way).

## 5. The IT-Managed / Small-Business User

Buys DisplayFusion or Start11's Business/Enterprise tiers for fleet deployment, silent install, and config export/import (both products document this explicitly). Cares about not breaking on Windows feature updates across a fleet of managed machines more than about aesthetics. Currently the segment best served today, but still exposed to the same shell-update fragility as everyone else, and to licensing-model confusion at the site-license/enterprise pricing tier (DisplayFusion's site-license price reportedly rose from ~$645 to $899 over time — see competitor matrix). Out of scope for MVP but worth tracking for Phase 8-9 (packaging/deployment story).

## Primary MVP personas

**#1, #2, and #3** are the primary MVP targets — their pain points map directly to the top three market gaps in `docs/research/market-gap-report.md` (shell-update resilience, resource discipline, licensing clarity) and to modules already in scope (multi-monitor manager, wallpaper host, file organization engine). **#4** is the Phase 6+ (creator studio) target. **#5** is explicitly deferred past MVP.
