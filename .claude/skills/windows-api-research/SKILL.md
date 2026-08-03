---
name: windows-api-research
description: Research and validate a Windows API, shell-integration technique, or platform capability before adopting it in the desktop runtime.
---

Use before any code depends on a new Windows API or shell behaviour.

1. Check official docs first (Win32/WinRT API reference, Windows App SDK docs) for the exact API/technique.
2. Classify it: **Supported** (documented, stable contract) or **Undocumented/compatibility-sensitive** (e.g. WorkerW/Progman-style desktop attachment). Undocumented techniques must be isolated behind an adapter with a working fallback — never a hard dependency.
3. Check the minimum supported Windows 11 build and note any deprecation/behavior-change history across recent feature updates (24H2 has broken several desktop-customization apps in this space — see `docs/research/competitor-matrix.md` for documented examples).
4. If the finding changes an architectural boundary, process model, or a non-negotiable principle, write an ADR (`architecture-decision` skill). Otherwise, record the finding inline in the relevant spec under `docs/architecture/`.
5. Never depend entirely on undocumented behaviour — confirm a fallback mode exists or flag its absence as a risk in `backlog/risk-register.md`.
