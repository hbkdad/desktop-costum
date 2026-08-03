# Jobs To Be Done

Format: When [situation], I want to [motivation], so I can [outcome]. Each tied to a persona in `docs/product/personas.md` and a gap in `docs/research/market-gap-report.md`.

## Core JTBD (MVP-relevant)

1. **When** I dock/undock my laptop or reconnect a monitor, **I want** my taskbars, wallpapers, and icon layout to reappear correctly without manual reconfiguration, **so I can** get back to work immediately. *(Multi-Monitor Power User → shell-update/config-resilience gap)*
2. **When** a Windows feature update installs, **I want** my desktop customization to keep working or fail gracefully into a safe fallback, **so I can** trust the tool enough to keep using it long-term instead of uninstalling after the first bad update cycle. *(all personas → shell-update resilience gap, the report's #1 priority)*
3. **When** I want an animated or video wallpaper, **I want** to know upfront what it will cost me in battery/CPU/GPU, and have that cost shrink automatically on battery power, **so I can** have a nice-looking desktop without giving up laptop battery life. *(Battery-Conscious Laptop User → resource-discipline gap)*
4. **When** my desktop icons pile up, **I want** to group them into stable, named containers that don't require re-learning a new licensing model every year, **so I can** stay organized without worrying my organization tool will change its pricing under me. *(Desktop Organizer → licensing-clarity gap)*
5. **When** I want a custom widget, skin, or scene, **I want** a visual editor that gets me most of the way there without hand-editing config files, but still lets me drop into an expression/scripting layer for the last 10%, **so I can** get Rainmeter-level depth without Rainmeter's documented learning-curve complaint. *(Aesthetic Tinkerer → consolidation/creator-tooling gap)*
6. **When** Explorer crashes or restarts (for any reason, not just because of this product), **I want** my desktop layout, widgets, and wallpaper to recover automatically, **so I can** avoid the "everything is gone, I have to redo it" experience multiple competitors' forums document. *(all personas → shell-update resilience gap)*

## Explicitly out of scope for MVP

- Fleet deployment / silent install / centralized config management (IT-Managed persona) — Phase 8-9.
- Marketplace publishing/monetization workflow (Aesthetic Tinkerer's creator side) — Phase 6-8.
