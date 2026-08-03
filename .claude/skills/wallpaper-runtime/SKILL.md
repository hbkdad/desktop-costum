---
name: wallpaper-runtime
description: Implement or modify wallpaper runtime behaviour (static, video, web, shader/particle) in the wallpaper host module.
---

Use when touching wallpaper rendering, playback, or per-monitor hosting.

1. Support per-monitor wallpapers as a first-class case, not an afterthought — test on 2+ monitors with different resolutions/DPI.
2. Pause or degrade rendering automatically when a fullscreen application/game is active (non-negotiable principle) — verify this with the `fullscreen-detection` prototype behaviour, not just a manual check.
3. Reduce frame rate/quality automatically on battery power; make the budget explicit and testable (`performance-test` skill).
4. Every competitor researched in `docs/research/competitor-matrix.md` has documented CPU/GPU/battery complaints for animated wallpapers — treat resource budget as a shipping requirement, not a nice-to-have.
5. Web wallpapers (WebView2-based) must run under the same untrusted-content rules as plugins: no unrestricted network/file access, default-deny capabilities.
